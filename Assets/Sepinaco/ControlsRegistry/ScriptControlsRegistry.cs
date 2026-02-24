using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PG;

/// <summary>
/// Escanea MonoBehaviour de escenas cargadas para registrarlos como "controles".
/// Permite activarlos/desactivarlos por control o por grupo, y persistir configuración.
/// </summary>
public class ScriptControlsRegistry : MonoBehaviour
{
    [Serializable]
    public class ControlEntry
    {
        public string id;
        public string displayName;
        public MonoBehaviour behaviour;
        public bool isEnabled = true;
        public List<string> groups = new List<string>();
    }

    [Serializable]
    public class GroupEntry
    {
        public string name;
        public bool isEnabled = true;
        public List<string> controlIds = new List<string>();
    }

    [Serializable]
    public class CameraEntry
    {
        public string id;
        public string displayName;
        public Camera camera;
    }

    [Serializable]
    private class SaveControlEntry
    {
        public string id;
        public bool isEnabled;
        public List<string> groups = new List<string>();
    }

    [Serializable]
    private class SaveGroupEntry
    {
        public string name;
        public bool isEnabled;
        public List<string> controlIds = new List<string>();
    }

    [Serializable]
    private class SaveData
    {
        public List<SaveControlEntry> controls = new List<SaveControlEntry>();
        public List<SaveGroupEntry> groups = new List<SaveGroupEntry>();
    }

    [Header("Escaneo")]
    [Tooltip("Si está activo, inicia automáticamente el escaneo al arrancar.")]
    public bool autoStartScan = true;

    [Tooltip("Intervalo de tiempo (segundos) entre escaneos.")]
    [Min(0.1f)]
    public float scanIntervalSeconds = 1f;

    [Tooltip("Límite de tiempo total (segundos) para el proceso de escaneo.")]
    [Min(0.1f)]
    public float scanDurationLimitSeconds = 10f;

    [Tooltip("Si está activo, incluye GameObjects inactivos en la búsqueda.")]
    public bool includeInactiveGameObjects = true;

    [Tooltip("Si está activo, escribe en consola los controles detectados.")]
    public bool logDiscoveredControls = true;

    [Header("Auto detección estilo God Mode")]
    [Tooltip("Si está activo, registra controles como God Mode: componentes en cámara activa y objetos con tag Player.")]
    public bool useGodModeStyleDetection = true;

    [Tooltip("Cámara a inspeccionar. Si no se asigna, se usa Camera.main.")]
    [SerializeField] private Camera _targetCamera;

    [Tooltip("Tag usado para detectar objetos jugador.")]
    public string playerTag = "Player";

    [Tooltip("Si está activo, también incluye MonoBehaviours en hijos de los objetos Player.")]
    public bool includePlayerChildren = false;

    [Header("Persistencia")]
    [Tooltip("Si está activo, intenta cargar la configuración guardada al iniciar.")]
    public bool loadConfigurationOnStart = true;

    [Tooltip("Si está activo, guarda configuración al deshabilitar este componente.")]
    public bool saveConfigurationOnDisable = true;

    [Tooltip("Clave de PlayerPrefs usada para guardar/cargar configuración.")]
    public string saveKey = "Sepinaco.ScriptControlsRegistry.Config.v1";

    [Header("Datos runtime")]
    [SerializeField] private List<ControlEntry> _controls = new List<ControlEntry>();
    [SerializeField] private List<GroupEntry> _groups = new List<GroupEntry>();
    [SerializeField] private List<CameraEntry> _sceneCameras = new List<CameraEntry>();

    [Header("Comandos runtime (toggle una vez)")]
    [Tooltip("Nombre de grupo usado por los comandos de añadir/eliminar/asignar.")]
    public string commandGroupName = "Grupo1";

    [Tooltip("ID de control usado por comandos de asignar/desasignar.")]
    public string commandControlId = string.Empty;

    [Tooltip("Ponlo en true para lanzar un escaneo inmediato.")]
    public bool commandScanNow;

    [Tooltip("Ponlo en true para guardar configuración en PlayerPrefs.")]
    public bool commandSaveConfiguration;

    [Tooltip("Ponlo en true para cargar configuración de PlayerPrefs.")]
    public bool commandLoadConfiguration;

    [Tooltip("Ponlo en true para borrar configuración guardada.")]
    public bool commandDeleteSavedConfiguration;

    [Tooltip("Ponlo en true para crear el grupo indicado en commandGroupName.")]
    public bool commandAddGroup;

    [Tooltip("Ponlo en true para eliminar el grupo indicado en commandGroupName.")]
    public bool commandRemoveGroup;

    [Tooltip("Ponlo en true para asignar commandControlId al grupo commandGroupName.")]
    public bool commandAssignControlToGroup;

    [Tooltip("Ponlo en true para desasignar commandControlId del grupo commandGroupName.")]
    public bool commandUnassignControlFromGroup;

    [Header("Atajo cíclico")]
    [Tooltip("Si está activo, permite alternar controles cíclicamente con cycleToggleKey.")]
    public bool enableCycleToggleShortcut = true;

    [Tooltip("Tecla para alternar el control actual y avanzar al siguiente (cíclico).")]
    public KeyCode cycleToggleKey = KeyCode.Alpha3;

    [Tooltip("Si está activo, escribe en consola cada cambio realizado con el atajo cíclico.")]
    public bool logCycleActions = true;

    [SerializeField] private int _cycleIndex;

    [Header("Atajo cíclico de cámaras")]
    [Tooltip("Si está activo, permite navegar cámaras cíclicamente con cameraCycleKey.")]
    public bool enableCameraCycleShortcut = true;

    [Tooltip("Tecla para navegar entre cámaras (cíclico).")]
    public KeyCode cameraCycleKey = KeyCode.Alpha4;

    [Tooltip("Si está activo, al navegar se asigna la cámara seleccionada como _targetCamera.")]
    public bool assignTargetCameraOnCycle = true;

    [Tooltip("Si está activo, escribe en consola cada navegación de cámara.")]
    public bool logCameraCycleActions = true;

    [SerializeField] private int _cameraCycleIndex;

    private Coroutine _scanRoutine;
    private readonly Dictionary<string, SaveControlEntry> _pendingLoadedControlStates = new Dictionary<string, SaveControlEntry>();
    private readonly Dictionary<string, bool> _groupStateCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ControlEntry> Controls => _controls;
    public IReadOnlyList<GroupEntry> Groups => _groups;

    private void Awake()
    {
        NormalizeConfigurationValues();
    }

    private void Start()
    {
        if (loadConfigurationOnStart)
            LoadConfiguration();

        if (autoStartScan)
            StartScan();
    }

    private void OnDisable()
    {
        if (_scanRoutine != null)
        {
            StopCoroutine(_scanRoutine);
            _scanRoutine = null;
        }

        if (saveConfigurationOnDisable)
            SaveConfiguration();
    }

    private void Update()
    {
        ProcessCommandFlags();
        SyncManualGroupToggles();
        ProcessCycleToggleShortcut();
        ProcessCameraCycleShortcut();
        SyncControlEnabledStatesToBehaviours();
    }

    private void OnValidate()
    {
        NormalizeConfigurationValues();
        NormalizeData();
    }

    private void NormalizeConfigurationValues()
    {
        if (scanIntervalSeconds < 0.1f) scanIntervalSeconds = 0.1f;
        if (scanDurationLimitSeconds < 0.1f) scanDurationLimitSeconds = 0.1f;
        if (string.IsNullOrWhiteSpace(saveKey))
            saveKey = "Sepinaco.ScriptControlsRegistry.Config.v1";
    }

    // ───────────────────────── Escaneo ─────────────────────────

    public void StartScan()
    {
        if (_scanRoutine != null)
            StopCoroutine(_scanRoutine);

        _scanRoutine = StartCoroutine(ScanRoutine());
    }

    public void StopScan()
    {
        if (_scanRoutine == null) return;
        StopCoroutine(_scanRoutine);
        _scanRoutine = null;
    }

    public void ScanNow()
    {
        ScanSceneControls();
    }

    private IEnumerator ScanRoutine()
    {
        float startedAt = Time.unscaledTime;
        var wait = new WaitForSecondsRealtime(scanIntervalSeconds);

        while (Time.unscaledTime - startedAt <= scanDurationLimitSeconds)
        {
            ScanSceneControls();
            yield return wait;
            wait = new WaitForSecondsRealtime(scanIntervalSeconds);
        }

        _scanRoutine = null;
    }

    private void ScanSceneControls()
    {
        ScanSceneCameras();

        var foundById = new Dictionary<string, MonoBehaviour>();
        if (useGodModeStyleDetection)
            ScanGodModeStyleControls(foundById);
        else
            ScanAllSceneControls(foundById);

        var oldById = new Dictionary<string, ControlEntry>(_controls.Count);
        for (int i = 0; i < _controls.Count; i++)
        {
            ControlEntry entry = _controls[i];
            if (entry == null || string.IsNullOrEmpty(entry.id)) continue;
            oldById[entry.id] = entry;
        }

        var newControls = new List<ControlEntry>(foundById.Count);
        foreach (var kvp in foundById)
        {
            string id = kvp.Key;
            MonoBehaviour behaviour = kvp.Value;

            if (oldById.TryGetValue(id, out ControlEntry existing))
            {
                existing.behaviour = behaviour;
                existing.displayName = BuildDisplayName(behaviour);
                existing.isEnabled = behaviour.enabled;
                EnsureGroupList(existing);
                newControls.Add(existing);
            }
            else
            {
                var created = new ControlEntry
                {
                    id = id,
                    displayName = BuildDisplayName(behaviour),
                    behaviour = behaviour,
                    isEnabled = behaviour.enabled,
                    groups = new List<string>()
                };
                newControls.Add(created);

                if (logDiscoveredControls)
                    Debug.Log($"[ScriptControlsRegistry] Control detectado: {created.displayName}");
            }
        }

        _controls = newControls;
        EnsureCycleIndexInRange();
        RemoveOrphanControlIdsFromGroups();
        ApplyPendingLoadedStates();
        NormalizeData();
    }

    private void ScanSceneCameras()
    {
        var foundById = new Dictionary<string, Camera>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                Camera[] cameras = roots[r].GetComponentsInChildren<Camera>(includeInactiveGameObjects);
                for (int c = 0; c < cameras.Length; c++)
                {
                    Camera camera = cameras[c];
                    if (camera == null) continue;

                    string id = BuildCameraId(camera);
                    if (!foundById.ContainsKey(id))
                        foundById.Add(id, camera);
                }
            }
        }

        var newCameras = new List<CameraEntry>(foundById.Count);
        foreach (var kvp in foundById)
        {
            newCameras.Add(new CameraEntry
            {
                id = kvp.Key,
                displayName = BuildCameraDisplayName(kvp.Value),
                camera = kvp.Value
            });
        }

        _sceneCameras = newCameras;
        EnsureCameraCycleIndexInRange();
    }

    private void ScanAllSceneControls(Dictionary<string, MonoBehaviour> foundById)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                MonoBehaviour[] behaviours = roots[r].GetComponentsInChildren<MonoBehaviour>(includeInactiveGameObjects);
                for (int b = 0; b < behaviours.Length; b++)
                {
                    MonoBehaviour mb = behaviours[b];
                    if (mb == null || mb == this) continue;

                    AddControlIfMissing(foundById, mb);
                }
            }
        }
    }

    private void ScanGodModeStyleControls(Dictionary<string, MonoBehaviour> foundById)
    {
        Camera cameraToScan = ResolveAutoDetectionCamera();
        if (cameraToScan != null)
        {
            MonoBehaviour[] cameraBehaviours = cameraToScan.GetComponents<MonoBehaviour>();
            for (int i = 0; i < cameraBehaviours.Length; i++)
            {
                MonoBehaviour mb = cameraBehaviours[i];
                if (ShouldSkipAutoDetectedBehaviour(mb)) continue;
                AddControlIfMissing(foundById, mb);
            }
        }

        ScanControlsFromGameControllerPlayers(foundById);

        try
        {
            if (string.IsNullOrWhiteSpace(playerTag)) return;

            GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null) continue;

                MonoBehaviour[] behaviours = includePlayerChildren
                    ? players[i].GetComponentsInChildren<MonoBehaviour>(includeInactiveGameObjects)
                    : players[i].GetComponents<MonoBehaviour>();

                for (int b = 0; b < behaviours.Length; b++)
                {
                    MonoBehaviour mb = behaviours[b];
                    if (ShouldSkipAutoDetectedBehaviour(mb)) continue;
                    AddControlIfMissing(foundById, mb);
                }
            }
        }
        catch (UnityException)
        {
            // Si el tag no existe en el proyecto, simplemente no se añaden players.
        }
    }

    private Camera ResolveAutoDetectionCamera()
    {
        if (_targetCamera != null)
            return _targetCamera;

        Camera camera = Camera.main;
        if (camera != null)
            return camera;

        var gameController = FindObjectOfType<GameController>(includeInactiveGameObjects);
        if (gameController == null)
            return null;

        return TryGetCameraFromPlayer(gameController.Player1) ?? TryGetCameraFromPlayer(gameController.Player2);
    }

    private void ScanControlsFromGameControllerPlayers(Dictionary<string, MonoBehaviour> foundById)
    {
        var gameController = FindObjectOfType<GameController>(includeInactiveGameObjects);
        if (gameController == null)
            return;

        AddControlsFromPlayer(gameController.Player1, foundById);
        AddControlsFromPlayer(gameController.Player2, foundById);
    }

    private void AddControlsFromPlayer(InitializePlayer player, Dictionary<string, MonoBehaviour> foundById)
    {
        if (player == null)
            return;

        MonoBehaviour[] behaviours = includePlayerChildren
            ? player.GetComponentsInChildren<MonoBehaviour>(includeInactiveGameObjects)
            : player.GetComponents<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour mb = behaviours[i];
            if (ShouldSkipAutoDetectedBehaviour(mb)) continue;
            AddControlIfMissing(foundById, mb);
        }
    }

    private static Camera TryGetCameraFromPlayer(InitializePlayer player)
    {
        if (player == null)
            return null;

        CameraController cameraController = player.GetComponentInChildren<CameraController>(true);
        if (cameraController != null && cameraController.MainCamera != null)
            return cameraController.MainCamera;

        return null;
    }

    // ───────────────────────── Grupos ─────────────────────────

    public bool AddGroup(string groupName)
    {
        string normalized = NormalizeGroupName(groupName);
        if (string.IsNullOrEmpty(normalized)) return false;
        if (FindGroup(normalized) != null) return false;

        _groups.Add(new GroupEntry
        {
            name = normalized,
            isEnabled = true,
            controlIds = new List<string>()
        });
        return true;
    }

    public bool RemoveGroup(string groupName, bool unassignFromControls = true)
    {
        GroupEntry group = FindGroup(groupName);
        if (group == null) return false;

        string normalized = group.name;
        _groups.Remove(group);

        if (unassignFromControls)
        {
            for (int i = 0; i < _controls.Count; i++)
            {
                ControlEntry control = _controls[i];
                if (control?.groups == null) continue;
                control.groups.RemoveAll(g => string.Equals(g, normalized, StringComparison.OrdinalIgnoreCase));
            }
        }

        return true;
    }

    public bool AssignControlToGroup(string controlId, string groupName)
    {
        ControlEntry control = FindControl(controlId);
        GroupEntry group = FindGroup(groupName);
        if (control == null || group == null) return false;

        EnsureGroupList(control);
        if (!ContainsIgnoreCase(control.groups, group.name))
            control.groups.Add(group.name);

        if (!ContainsIgnoreCase(group.controlIds, control.id))
            group.controlIds.Add(control.id);

        return true;
    }

    public bool UnassignControlFromGroup(string controlId, string groupName)
    {
        ControlEntry control = FindControl(controlId);
        GroupEntry group = FindGroup(groupName);
        if (control == null || group == null) return false;

        control.groups?.RemoveAll(g => string.Equals(g, group.name, StringComparison.OrdinalIgnoreCase));
        group.controlIds?.RemoveAll(id => string.Equals(id, control.id, StringComparison.Ordinal));
        return true;
    }

    public bool SetGroupEnabled(string groupName, bool enabled)
    {
        GroupEntry group = FindGroup(groupName);
        if (group == null) return false;

        group.isEnabled = enabled;
        if (group.controlIds == null) return true;

        for (int i = 0; i < group.controlIds.Count; i++)
            SetControlEnabled(group.controlIds[i], enabled);

        return true;
    }

    public void SetAllControlsEnabled(bool enabled)
    {
        for (int i = 0; i < _controls.Count; i++)
        {
            ControlEntry control = _controls[i];
            if (control == null) continue;
            SetControlEnabled(control.id, enabled);
        }
    }

    public bool SetControlEnabled(string controlId, bool enabled)
    {
        ControlEntry control = FindControl(controlId);
        if (control == null) return false;

        control.isEnabled = enabled;
        if (control.behaviour != null)
            control.behaviour.enabled = enabled;

        return true;
    }

    public bool SetControlEnabledAtIndex(int index, bool enabled)
    {
        if (index < 0 || index >= _controls.Count) return false;
        ControlEntry control = _controls[index];
        if (control == null) return false;
        return SetControlEnabled(control.id, enabled);
    }

    // ───────────────────────── Guardado/Carga ─────────────────────────

    public void SaveConfiguration()
    {
        NormalizeData();

        var data = new SaveData();
        data.controls = new List<SaveControlEntry>(_controls.Count);
        data.groups = new List<SaveGroupEntry>(_groups.Count);

        for (int i = 0; i < _controls.Count; i++)
        {
            ControlEntry c = _controls[i];
            if (c == null || string.IsNullOrEmpty(c.id)) continue;

            data.controls.Add(new SaveControlEntry
            {
                id = c.id,
                isEnabled = c.isEnabled,
                groups = c.groups != null ? new List<string>(c.groups) : new List<string>()
            });
        }

        for (int i = 0; i < _groups.Count; i++)
        {
            GroupEntry g = _groups[i];
            if (g == null || string.IsNullOrWhiteSpace(g.name)) continue;

            data.groups.Add(new SaveGroupEntry
            {
                name = g.name.Trim(),
                isEnabled = g.isEnabled,
                controlIds = g.controlIds != null ? new List<string>(g.controlIds) : new List<string>()
            });
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
    }

    public bool LoadConfiguration()
    {
        if (!PlayerPrefs.HasKey(saveKey))
            return false;

        string json = PlayerPrefs.GetString(saveKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return false;

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ScriptControlsRegistry] Error cargando configuración: {ex.Message}");
            return false;
        }

        if (data == null)
            return false;

        _pendingLoadedControlStates.Clear();
        if (data.controls != null)
        {
            for (int i = 0; i < data.controls.Count; i++)
            {
                SaveControlEntry saved = data.controls[i];
                if (saved == null || string.IsNullOrEmpty(saved.id)) continue;
                _pendingLoadedControlStates[saved.id] = saved;
            }
        }

        _groups = new List<GroupEntry>();
        if (data.groups != null)
        {
            for (int i = 0; i < data.groups.Count; i++)
            {
                SaveGroupEntry sg = data.groups[i];
                if (sg == null || string.IsNullOrWhiteSpace(sg.name)) continue;

                _groups.Add(new GroupEntry
                {
                    name = sg.name.Trim(),
                    isEnabled = sg.isEnabled,
                    controlIds = sg.controlIds != null ? new List<string>(sg.controlIds) : new List<string>()
                });
            }
        }

        ApplyPendingLoadedStates();
        NormalizeData();
        return true;
    }

    public void DeleteSavedConfiguration()
    {
        PlayerPrefs.DeleteKey(saveKey);
    }

    // ───────────────────────── Utilidad ─────────────────────────

    private static string BuildControlId(MonoBehaviour behaviour)
    {
        string sceneName = behaviour.gameObject.scene.name;
        string hierarchyPath = BuildHierarchyPath(behaviour.transform);
        string typeName = behaviour.GetType().AssemblyQualifiedName;
        return $"{sceneName}|{hierarchyPath}|{typeName}";
    }

    private static string BuildDisplayName(MonoBehaviour behaviour)
    {
        return $"{behaviour.GetType().Name} ({BuildHierarchyPath(behaviour.transform)})";
    }

    private static string BuildHierarchyPath(Transform t)
    {
        if (t == null) return "<null>";
        var stack = new Stack<string>();
        Transform current = t;
        while (current != null)
        {
            stack.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", stack.ToArray());
    }

    private ControlEntry FindControl(string controlId)
    {
        if (string.IsNullOrEmpty(controlId)) return null;
        for (int i = 0; i < _controls.Count; i++)
        {
            ControlEntry c = _controls[i];
            if (c == null) continue;
            if (string.Equals(c.id, controlId, StringComparison.Ordinal))
                return c;
        }
        return null;
    }

    private GroupEntry FindGroup(string groupName)
    {
        string normalized = NormalizeGroupName(groupName);
        if (string.IsNullOrEmpty(normalized)) return null;

        for (int i = 0; i < _groups.Count; i++)
        {
            GroupEntry g = _groups[i];
            if (g == null || string.IsNullOrWhiteSpace(g.name)) continue;
            if (string.Equals(g.name.Trim(), normalized, StringComparison.OrdinalIgnoreCase))
                return g;
        }
        return null;
    }

    private static string NormalizeGroupName(string groupName)
    {
        return string.IsNullOrWhiteSpace(groupName) ? string.Empty : groupName.Trim();
    }

    private static bool ContainsIgnoreCase(List<string> values, string target)
    {
        if (values == null || string.IsNullOrEmpty(target)) return false;
        for (int i = 0; i < values.Count; i++)
        {
            if (string.Equals(values[i], target, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static void EnsureGroupList(ControlEntry control)
    {
        if (control.groups == null)
            control.groups = new List<string>();
    }

    private void ApplyPendingLoadedStates()
    {
        if (_pendingLoadedControlStates.Count == 0) return;

        var appliedIds = new List<string>();
        for (int i = 0; i < _controls.Count; i++)
        {
            ControlEntry control = _controls[i];
            if (control == null || string.IsNullOrEmpty(control.id)) continue;

            if (_pendingLoadedControlStates.TryGetValue(control.id, out SaveControlEntry saved))
            {
                control.isEnabled = saved.isEnabled;
                control.groups = saved.groups != null ? new List<string>(saved.groups) : new List<string>();
                if (control.behaviour != null)
                    control.behaviour.enabled = control.isEnabled;
                appliedIds.Add(control.id);
            }
        }

        for (int i = 0; i < appliedIds.Count; i++)
            _pendingLoadedControlStates.Remove(appliedIds[i]);

        for (int i = 0; i < _groups.Count; i++)
        {
            GroupEntry group = _groups[i];
            if (group == null || string.IsNullOrWhiteSpace(group.name)) continue;
            if (group.controlIds == null) group.controlIds = new List<string>();

            if (!group.isEnabled) SetGroupEnabled(group.name, false);
        }
    }

    private void SyncManualGroupToggles()
    {
        for (int i = 0; i < _groups.Count; i++)
        {
            GroupEntry group = _groups[i];
            if (group == null || string.IsNullOrWhiteSpace(group.name)) continue;

            string key = group.name.Trim();
            if (_groupStateCache.TryGetValue(key, out bool previousState))
            {
                if (previousState != group.isEnabled)
                    SetGroupEnabled(key, group.isEnabled);
            }

            _groupStateCache[key] = group.isEnabled;
        }
    }

    private void SyncControlEnabledStatesToBehaviours()
    {
        for (int i = 0; i < _controls.Count; i++)
        {
            ControlEntry control = _controls[i];
            if (control == null || control.behaviour == null) continue;
            if (control.behaviour.enabled != control.isEnabled)
                control.behaviour.enabled = control.isEnabled;
        }
    }

    private void NormalizeData()
    {
        if (_controls == null) _controls = new List<ControlEntry>();
        if (_groups == null) _groups = new List<GroupEntry>();

        var uniqueControlById = new Dictionary<string, ControlEntry>();
        var normalizedControls = new List<ControlEntry>(_controls.Count);
        for (int i = 0; i < _controls.Count; i++)
        {
            ControlEntry c = _controls[i];
            if (c == null || string.IsNullOrEmpty(c.id)) continue;
            if (uniqueControlById.ContainsKey(c.id)) continue;

            EnsureGroupList(c);
            uniqueControlById.Add(c.id, c);
            normalizedControls.Add(c);
        }
        _controls = normalizedControls;

        var uniqueGroupByName = new Dictionary<string, GroupEntry>(StringComparer.OrdinalIgnoreCase);
        var normalizedGroups = new List<GroupEntry>(_groups.Count);
        for (int i = 0; i < _groups.Count; i++)
        {
            GroupEntry g = _groups[i];
            if (g == null) continue;
            string normalizedName = NormalizeGroupName(g.name);
            if (string.IsNullOrEmpty(normalizedName)) continue;
            if (uniqueGroupByName.ContainsKey(normalizedName)) continue;

            g.name = normalizedName;
            if (g.controlIds == null) g.controlIds = new List<string>();
            g.controlIds.RemoveAll(id => string.IsNullOrEmpty(id) || !uniqueControlById.ContainsKey(id));
            uniqueGroupByName.Add(g.name, g);
            normalizedGroups.Add(g);
        }
        _groups = normalizedGroups;
        _groupStateCache.Clear();
        for (int i = 0; i < _groups.Count; i++)
        {
            GroupEntry g = _groups[i];
            if (g == null || string.IsNullOrWhiteSpace(g.name)) continue;
            _groupStateCache[g.name] = g.isEnabled;
        }

        for (int i = 0; i < _controls.Count; i++)
        {
            ControlEntry c = _controls[i];
            EnsureGroupList(c);
            c.groups.RemoveAll(g => string.IsNullOrWhiteSpace(g));

            for (int gi = c.groups.Count - 1; gi >= 0; gi--)
            {
                string groupName = NormalizeGroupName(c.groups[gi]);
                if (!uniqueGroupByName.TryGetValue(groupName, out GroupEntry group))
                {
                    group = new GroupEntry
                    {
                        name = groupName,
                        isEnabled = true,
                        controlIds = new List<string>()
                    };
                    uniqueGroupByName[groupName] = group;
                    _groups.Add(group);
                }

                c.groups[gi] = group.name;
                if (!ContainsIgnoreCase(group.controlIds, c.id))
                    group.controlIds.Add(c.id);
            }
        }

        EnsureCycleIndexInRange();
        EnsureCameraCycleIndexInRange();
    }

    private void RemoveOrphanControlIdsFromGroups()
    {
        var validIds = new HashSet<string>();
        for (int i = 0; i < _controls.Count; i++)
        {
            ControlEntry c = _controls[i];
            if (c == null || string.IsNullOrEmpty(c.id)) continue;
            validIds.Add(c.id);
        }

        for (int i = 0; i < _groups.Count; i++)
        {
            GroupEntry g = _groups[i];
            if (g?.controlIds == null) continue;
            g.controlIds.RemoveAll(id => !validIds.Contains(id));
        }
    }

    private void ProcessCommandFlags()
    {
        if (commandScanNow)
        {
            commandScanNow = false;
            ScanNow();
        }

        if (commandSaveConfiguration)
        {
            commandSaveConfiguration = false;
            SaveConfiguration();
        }

        if (commandLoadConfiguration)
        {
            commandLoadConfiguration = false;
            LoadConfiguration();
        }

        if (commandDeleteSavedConfiguration)
        {
            commandDeleteSavedConfiguration = false;
            DeleteSavedConfiguration();
        }

        if (commandAddGroup)
        {
            commandAddGroup = false;
            AddGroup(commandGroupName);
        }

        if (commandRemoveGroup)
        {
            commandRemoveGroup = false;
            RemoveGroup(commandGroupName);
        }

        if (commandAssignControlToGroup)
        {
            commandAssignControlToGroup = false;
            AssignControlToGroup(commandControlId, commandGroupName);
        }

        if (commandUnassignControlFromGroup)
        {
            commandUnassignControlFromGroup = false;
            UnassignControlFromGroup(commandControlId, commandGroupName);
        }
    }

    private void ProcessCycleToggleShortcut()
    {
        if (!enableCycleToggleShortcut) return;
        if (!Input.GetKeyDown(cycleToggleKey)) return;

        ControlEntry control = GetNextCyclableControl();
        if (control == null)
        {
            if (logCycleActions)
                Debug.LogWarning("[ScriptControlsRegistry] No hay controles ciclables disponibles.");
            return;
        }

        bool newState = !control.isEnabled;
        SetControlEnabled(control.id, newState);

        if (logCycleActions)
            Debug.Log($"[ScriptControlsRegistry] {(newState ? "Activado" : "Desactivado")}: {control.displayName}");

        _cycleIndex = (_cycleIndex + 1) % _controls.Count;
    }

    private void ProcessCameraCycleShortcut()
    {
        if (!enableCameraCycleShortcut) return;
        if (!Input.GetKeyDown(cameraCycleKey)) return;

        CameraEntry cameraEntry = GetNextCyclableCamera();
        if (cameraEntry == null)
        {
            if (logCameraCycleActions)
                Debug.LogWarning("[ScriptControlsRegistry] No hay cámaras registradas para navegar.");
            return;
        }

        if (assignTargetCameraOnCycle)
            _targetCamera = cameraEntry.camera;

        if (logCameraCycleActions)
            Debug.Log($"[ScriptControlsRegistry] Cámara seleccionada: {cameraEntry.displayName}");

        _cameraCycleIndex = (_cameraCycleIndex + 1) % _sceneCameras.Count;
    }

    private ControlEntry GetNextCyclableControl()
    {
        if (_controls.Count == 0) return null;
        EnsureCycleIndexInRange();

        for (int offset = 0; offset < _controls.Count; offset++)
        {
            int index = (_cycleIndex + offset) % _controls.Count;
            ControlEntry candidate = _controls[index];
            if (candidate == null || candidate.behaviour == null) continue;
            _cycleIndex = index;
            return candidate;
        }

        return null;
    }

    private CameraEntry GetNextCyclableCamera()
    {
        if (_sceneCameras.Count == 0) return null;
        EnsureCameraCycleIndexInRange();

        for (int offset = 0; offset < _sceneCameras.Count; offset++)
        {
            int index = (_cameraCycleIndex + offset) % _sceneCameras.Count;
            CameraEntry candidate = _sceneCameras[index];
            if (candidate == null || candidate.camera == null) continue;
            _cameraCycleIndex = index;
            return candidate;
        }

        return null;
    }

    private void EnsureCycleIndexInRange()
    {
        if (_controls.Count == 0)
        {
            _cycleIndex = 0;
            return;
        }

        if (_cycleIndex < 0)
            _cycleIndex = 0;
        else if (_cycleIndex >= _controls.Count)
            _cycleIndex %= _controls.Count;
    }

    private void EnsureCameraCycleIndexInRange()
    {
        if (_sceneCameras.Count == 0)
        {
            _cameraCycleIndex = 0;
            return;
        }

        if (_cameraCycleIndex < 0)
            _cameraCycleIndex = 0;
        else if (_cameraCycleIndex >= _sceneCameras.Count)
            _cameraCycleIndex %= _sceneCameras.Count;
    }

    private void AddControlIfMissing(Dictionary<string, MonoBehaviour> foundById, MonoBehaviour behaviour)
    {
        if (behaviour == null || behaviour == this) return;

        string id = BuildControlId(behaviour);
        if (!foundById.ContainsKey(id))
            foundById.Add(id, behaviour);
    }

    private bool ShouldSkipAutoDetectedBehaviour(MonoBehaviour mb)
    {
        if (mb == null || mb == this || !mb.enabled) return true;

        string typeName = mb.GetType().Name;
        return string.Equals(typeName, "ScriptDebugInspector", StringComparison.Ordinal) ||
               string.Equals(typeName, nameof(ScriptControlsRegistry), StringComparison.Ordinal) ||
               string.Equals(typeName, "ScriptGodModeCamera", StringComparison.Ordinal);
    }

    private static string BuildCameraId(Camera camera)
    {
        string sceneName = camera.gameObject.scene.name;
        string hierarchyPath = BuildHierarchyPath(camera.transform);
        return $"{sceneName}|{hierarchyPath}|{nameof(Camera)}";
    }

    private static string BuildCameraDisplayName(Camera camera)
    {
        return $"{camera.name} ({BuildHierarchyPath(camera.transform)})";
    }
}
