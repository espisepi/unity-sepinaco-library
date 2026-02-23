using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builder de escenarios en runtime.
///
/// Permite instanciar, eliminar y modificar GameObjects en la escena,
/// guardar/cargar escenarios como JSON, y navegar en God Mode (cámara libre).
///
/// - Arrastra prefabs al array "availablePrefabs" para poder colocarlos.
/// - Al activar builderModeEnabled la cámara pasa a modo dios (WASD + ratón).
/// - Panel OnGUI con navegación por teclado y colocación con click.
/// - Compatible con ScriptDebugInspector (todos los campos serializados).
/// - Funciona en editor y en builds.
/// </summary>
public class ScriptSceneBuilder : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    //  Builder Mode
    // ═══════════════════════════════════════════════════════════

    [Header("Builder Mode")]
    [Tooltip("Activar/desactivar el modo builder. Modificable desde el Inspector y ScriptDebugInspector.")]
    public bool builderModeEnabled;

    [Tooltip("Tecla para alternar el modo builder en runtime.")]
    public KeyCode toggleBuilderKey = KeyCode.F2;

    // ═══════════════════════════════════════════════════════════
    //  Prefabs disponibles
    // ═══════════════════════════════════════════════════════════

    [Header("Prefabs disponibles")]
    [Tooltip("GameObjects que se pueden instanciar en la escena desde el builder.")]
    public GameObject[] availablePrefabs = Array.Empty<GameObject>();

    [Header("Colocación")]
    [Tooltip("Distancia por defecto cuando el raycast no impacta nada.")]
    public float placementDistance = 10f;

    [Tooltip("Capas del raycast para colocación y selección.")]
    public LayerMask raycastLayers = ~0;

    // ═══════════════════════════════════════════════════════════
    //  Cámara God Mode
    // ═══════════════════════════════════════════════════════════

    [Header("Cámara")]
    [Tooltip("Cámara a controlar. Si no se asigna, se usa Camera.main.")]
    [SerializeField] private Camera _targetCamera;

    [Header("Scripts a desactivar")]
    [Tooltip("MonoBehaviours que se desactivarán al entrar en modo builder.")]
    [SerializeField] private MonoBehaviour[] _scriptsToDisable = Array.Empty<MonoBehaviour>();

    [Tooltip("Buscar y desactivar automáticamente scripts en la cámara y en objetos Player.")]
    public bool autoDetectControls = true;

    [Header("Movimiento")]
    public float moveSpeed = 10f;
    public float fastMultiplier = 3f;
    public float slowMultiplier = 0.25f;
    [Range(0f, 0.99f)] public float moveSmoothing = 0.1f;

    [Header("Rotación")]
    public float lookSensitivity = 2f;
    [Range(0f, 0.99f)] public float lookSmoothing = 0.02f;
    public bool invertY;

    [Header("Velocidad con scroll")]
    public bool scrollAdjustsSpeed = true;
    public float scrollSpeedFactor = 1.15f;
    public float minSpeed = 0.1f;
    public float maxSpeed = 200f;

    [Header("Teclas de movimiento")]
    public KeyCode forwardKey = KeyCode.W;
    public KeyCode backMoveKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode camUpKey = KeyCode.E;
    public KeyCode camDownKey = KeyCode.Q;
    public KeyCode fastKey = KeyCode.LeftShift;
    public KeyCode slowKey = KeyCode.LeftControl;

    [Header("Ratón")]
    [Tooltip("Botón del ratón para rotar la cámara (0=izq, 1=der, 2=medio).")]
    public int lookMouseButton = 1;
    public bool hideCursorOnLook = true;

    // ═══════════════════════════════════════════════════════════
    //  Guardado / Carga
    // ═══════════════════════════════════════════════════════════

    [Header("Guardado / Carga")]
    [Tooltip("Carpeta de guardado (relativa a persistentDataPath).")]
    public string saveFolderName = "ScenarioSaves";

    // ═══════════════════════════════════════════════════════════
    //  UI — Navegación
    // ═══════════════════════════════════════════════════════════

    [Header("UI — Navegación")]
    public KeyCode navUpKey = KeyCode.UpArrow;
    public KeyCode navDownKey = KeyCode.DownArrow;
    public KeyCode confirmKey = KeyCode.Return;
    public KeyCode deleteKey = KeyCode.Delete;
    public KeyCode backKey = KeyCode.Backspace;
    public KeyCode escapeKey = KeyCode.Escape;

    [Header("UI — Modificación de transform")]
    public KeyCode valueUpKey = KeyCode.RightArrow;
    public KeyCode valueDownKey = KeyCode.LeftArrow;
    public float transformStep = 0.5f;
    public float transformFastStep = 2f;
    public float rotationStep = 5f;
    public float rotationFastStep = 45f;
    public float scaleStep = 0.1f;
    public float scaleFastStep = 1f;

    [Header("UI — Key Repeat")]
    [Tooltip("Tiempo antes de que comience la repetición al mantener una tecla.")]
    [Range(0.1f, 1f)]
    public float keyRepeatDelay = 0.4f;

    [Tooltip("Intervalo entre cada repetición mientras se mantiene la tecla.")]
    [Range(0.02f, 0.3f)]
    public float keyRepeatInterval = 0.08f;

    // ═══════════════════════════════════════════════════════════
    //  Serialización de escenario
    // ═══════════════════════════════════════════════════════════

    [Serializable]
    private class ScenarioObjectData
    {
        public int prefabIndex;
        public string prefabName;
        public string objectName;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public bool active;
    }

    [Serializable]
    private class ScenarioData
    {
        public string scenarioName;
        public string timestamp;
        public List<ScenarioObjectData> objects = new List<ScenarioObjectData>();
    }

    // ═══════════════════════════════════════════════════════════
    //  Estado interno — Cámara
    // ═══════════════════════════════════════════════════════════

    private bool _wasEnabled;
    private Camera _activeCamera;

    private float _yaw, _pitch;
    private float _smoothYaw, _smoothPitch;
    private Vector3 _smoothVelocity;

    private Vector3 _savedCamLocalPos;
    private Quaternion _savedCamLocalRot;
    private Transform _savedCamParent;
    private CursorLockMode _savedCursorLock;
    private bool _savedCursorVisible;

    private readonly List<MonoBehaviour> _disabledScripts = new List<MonoBehaviour>();
    private readonly List<bool> _originalStates = new List<bool>();

    // ═══════════════════════════════════════════════════════════
    //  Estado interno — Builder
    // ═══════════════════════════════════════════════════════════

    private readonly List<GameObject> _placedObjects = new List<GameObject>();
    private readonly List<int> _placedPrefabIndices = new List<int>();
    private GameObject _selectedObject;
    private bool _placementMode;
    private int _armedPrefabIndex = -1;

    // ═══════════════════════════════════════════════════════════
    //  Estado interno — UI
    // ═══════════════════════════════════════════════════════════

    private enum BuilderScreen { Main, Prefabs, SceneObjects, EditObject, Save, Load }
    private BuilderScreen _screen = BuilderScreen.Main;
    private int _cursor;
    private Vector2 _scroll;

    private string _saveNameBuf = "";
    private string[] _savedFiles = Array.Empty<string>();
    private readonly List<GameObject> _sceneObjectsCache = new List<GameObject>();

    private string _statusMessage = "";
    private float _statusMessageTime;
    private Rect _panelRect;

    private readonly Dictionary<KeyCode, float> _keyNextRepeat = new Dictionary<KeyCode, float>();

    // ═══════════════════════════════════════════════════════════
    //  Estado interno — GUI
    // ═══════════════════════════════════════════════════════════

    private GUIStyle _sBox, _sTitle, _sLabel, _sSel, _sGray, _sHeader, _sStatus;
    private bool _stylesReady;
    private int _fontSize = 18;

    private static readonly string[] MainOptions =
    {
        "Añadir Objeto",
        "Objetos de la Escena",
        "Guardar Escenario",
        "Cargar Escenario"
    };

    private static readonly string[] TransformLabels =
    {
        "Posición X", "Posición Y", "Posición Z",
        "Rotación X", "Rotación Y", "Rotación Z",
        "Escala X", "Escala Y", "Escala Z",
        "Activo"
    };

    // ═══════════════════════════════════════════════════════════
    //  OnValidate
    // ═══════════════════════════════════════════════════════════

    private void OnValidate()
    {
        if (!Application.isPlaying) return;

        if (builderModeEnabled && !_wasEnabled)
            EnableBuilder();
        else if (!builderModeEnabled && _wasEnabled)
            DisableBuilder();
    }

    // ═══════════════════════════════════════════════════════════
    //  Update
    // ═══════════════════════════════════════════════════════════

    private void Update()
    {
        if (Input.GetKeyDown(toggleBuilderKey))
            builderModeEnabled = !builderModeEnabled;

        if (builderModeEnabled && !_wasEnabled)
            EnableBuilder();
        else if (!builderModeEnabled && _wasEnabled)
            DisableBuilder();

        if (!builderModeEnabled || _activeCamera == null) return;

        CleanPlacedObjects();

        bool isTyping = _screen == BuilderScreen.Save;

        if (!isTyping)
        {
            HandleCameraLook();
            HandleCameraMovement();
            HandleCameraScrollSpeed();
            HandleUIInput();
        }

        HandleMouseInteraction();
    }

    // ═══════════════════════════════════════════════════════════
    //  Activar / Desactivar Builder
    // ═══════════════════════════════════════════════════════════

    private void EnableBuilder()
    {
        _activeCamera = _targetCamera != null ? _targetCamera : Camera.main;
        if (_activeCamera == null)
        {
            Debug.LogWarning("[SceneBuilder] No se encontró ninguna cámara.");
            builderModeEnabled = false;
            return;
        }

        _wasEnabled = true;
        _screen = BuilderScreen.Main;
        _cursor = 0;
        _scroll = Vector2.zero;
        _placementMode = false;
        _armedPrefabIndex = -1;

        Transform camT = _activeCamera.transform;
        _savedCamLocalPos = camT.localPosition;
        _savedCamLocalRot = camT.localRotation;
        _savedCamParent = camT.parent;
        camT.SetParent(null);

        Vector3 euler = camT.eulerAngles;
        _yaw = euler.y;
        _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
        _smoothYaw = _yaw;
        _smoothPitch = _pitch;
        _smoothVelocity = Vector3.zero;

        _savedCursorLock = Cursor.lockState;
        _savedCursorVisible = Cursor.visible;

        DisableControlScripts();
        RefreshSceneObjects();

        SetStatus("Builder Mode activado");
        Debug.Log("[SceneBuilder] Builder Mode ACTIVADO.");
    }

    private void DisableBuilder()
    {
        _wasEnabled = false;
        _placementMode = false;
        _armedPrefabIndex = -1;

        RestoreControlScripts();

        if (_activeCamera != null)
        {
            Transform camT = _activeCamera.transform;
            camT.SetParent(_savedCamParent);
            camT.localPosition = _savedCamLocalPos;
            camT.localRotation = _savedCamLocalRot;
        }

        Cursor.lockState = _savedCursorLock;
        Cursor.visible = _savedCursorVisible;

        Debug.Log("[SceneBuilder] Builder Mode DESACTIVADO.");
    }

    // ═══════════════════════════════════════════════════════════
    //  Cámara — Rotación
    // ═══════════════════════════════════════════════════════════

    private void HandleCameraLook()
    {
        if (!Input.GetMouseButton(lookMouseButton))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (hideCursorOnLook)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        float mx = Input.GetAxis("Mouse X") * lookSensitivity;
        float my = Input.GetAxis("Mouse Y") * lookSensitivity;

        _yaw += mx;
        _pitch += invertY ? my : -my;
        _pitch = Mathf.Clamp(_pitch, -90f, 90f);

        float t = 1f - lookSmoothing;
        _smoothYaw = Mathf.LerpAngle(_smoothYaw, _yaw, t);
        _smoothPitch = Mathf.Lerp(_smoothPitch, _pitch, t);

        _activeCamera.transform.rotation = Quaternion.Euler(_smoothPitch, _smoothYaw, 0f);
    }

    // ═══════════════════════════════════════════════════════════
    //  Cámara — Movimiento
    // ═══════════════════════════════════════════════════════════

    private void HandleCameraMovement()
    {
        Vector3 input = Vector3.zero;

        if (Input.GetKey(forwardKey))  input += Vector3.forward;
        if (Input.GetKey(backMoveKey)) input += Vector3.back;
        if (Input.GetKey(leftKey))     input += Vector3.left;
        if (Input.GetKey(rightKey))    input += Vector3.right;
        if (Input.GetKey(camUpKey))    input += Vector3.up;
        if (Input.GetKey(camDownKey))  input += Vector3.down;

        float speed = moveSpeed;
        if (Input.GetKey(fastKey)) speed *= fastMultiplier;
        if (Input.GetKey(slowKey)) speed *= slowMultiplier;

        Vector3 targetVel = input.normalized * speed;
        float smoothT = 1f - moveSmoothing;
        _smoothVelocity = Vector3.Lerp(_smoothVelocity, targetVel, smoothT);

        _activeCamera.transform.position +=
            _activeCamera.transform.TransformDirection(_smoothVelocity) * Time.unscaledDeltaTime;
    }

    // ═══════════════════════════════════════════════════════════
    //  Cámara — Scroll velocidad
    // ═══════════════════════════════════════════════════════════

    private void HandleCameraScrollSpeed()
    {
        if (!scrollAdjustsSpeed) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        moveSpeed *= scroll > 0f ? scrollSpeedFactor : 1f / scrollSpeedFactor;
        moveSpeed = Mathf.Clamp(moveSpeed, minSpeed, maxSpeed);
    }

    // ═══════════════════════════════════════════════════════════
    //  Scripts — Desactivar / Restaurar
    // ═══════════════════════════════════════════════════════════

    private void DisableControlScripts()
    {
        _disabledScripts.Clear();
        _originalStates.Clear();

        if (_scriptsToDisable != null)
            foreach (var mb in _scriptsToDisable)
                if (mb != null && mb.enabled && mb != this)
                    TrackAndDisable(mb);

        if (!autoDetectControls) return;

        if (_activeCamera != null)
            foreach (var mb in _activeCamera.GetComponents<MonoBehaviour>())
                if (!ShouldSkip(mb))
                    TrackAndDisable(mb);

        try
        {
            foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
                foreach (var mb in player.GetComponents<MonoBehaviour>())
                    if (!ShouldSkip(mb))
                        TrackAndDisable(mb);
        }
        catch (UnityException) { }

        var godCam = FindObjectOfType<ScriptGodModeCamera>();
        if (godCam != null && godCam.enabled && godCam != this)
            TrackAndDisable(godCam);
    }

    private void TrackAndDisable(MonoBehaviour mb)
    {
        if (_disabledScripts.Contains(mb)) return;
        _disabledScripts.Add(mb);
        _originalStates.Add(mb.enabled);
        mb.enabled = false;
    }

    private bool ShouldSkip(MonoBehaviour mb)
    {
        if (mb == null || mb == this || !mb.enabled) return true;
        string typeName = mb.GetType().Name;
        return typeName == "ScriptDebugInspector" ||
               typeName == "ScriptSceneBuilder";
    }

    private void RestoreControlScripts()
    {
        for (int i = 0; i < _disabledScripts.Count; i++)
            if (_disabledScripts[i] != null)
                _disabledScripts[i].enabled = _originalStates[i];

        _disabledScripts.Clear();
        _originalStates.Clear();
    }

    // ═══════════════════════════════════════════════════════════
    //  Key Repeat
    // ═══════════════════════════════════════════════════════════

    private bool IsKeyTriggered(KeyCode key)
    {
        if (Input.GetKeyDown(key))
        {
            _keyNextRepeat[key] = Time.unscaledTime + keyRepeatDelay;
            return true;
        }

        if (Input.GetKey(key)
            && _keyNextRepeat.TryGetValue(key, out float nextTime)
            && Time.unscaledTime >= nextTime)
        {
            _keyNextRepeat[key] = Time.unscaledTime + keyRepeatInterval;
            return true;
        }

        return false;
    }

    // ═══════════════════════════════════════════════════════════
    //  UI — Input
    // ═══════════════════════════════════════════════════════════

    private void HandleUIInput()
    {
        if (Input.GetKeyDown(escapeKey))
        {
            if (_placementMode)
            {
                _placementMode = false;
                _armedPrefabIndex = -1;
                SetStatus("Colocación cancelada");
                return;
            }
            if (_screen != BuilderScreen.Main)
            {
                GoToMain();
                return;
            }
        }

        switch (_screen)
        {
            case BuilderScreen.Main:         InputMain();         break;
            case BuilderScreen.Prefabs:      InputPrefabs();      break;
            case BuilderScreen.SceneObjects: InputSceneObjects(); break;
            case BuilderScreen.EditObject:   InputEditObject();   break;
            case BuilderScreen.Load:         InputLoad();         break;
        }
    }

    private void GoToMain()
    {
        _screen = BuilderScreen.Main;
        _cursor = 0;
        _scroll = Vector2.zero;
        _placementMode = false;
        _armedPrefabIndex = -1;
    }

    private void InputMain()
    {
        int count = MainOptions.Length;
        if (IsKeyTriggered(navDownKey)) _cursor = (_cursor + 1) % count;
        if (IsKeyTriggered(navUpKey))   _cursor = (_cursor - 1 + count) % count;

        if (Input.GetKeyDown(confirmKey))
        {
            switch (_cursor)
            {
                case 0:
                    _screen = BuilderScreen.Prefabs;
                    _cursor = 0;
                    _scroll = Vector2.zero;
                    break;
                case 1:
                    RefreshSceneObjects();
                    _screen = BuilderScreen.SceneObjects;
                    _cursor = 0;
                    _scroll = Vector2.zero;
                    break;
                case 2:
                    _screen = BuilderScreen.Save;
                    _saveNameBuf = "Escenario_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    break;
                case 3:
                    RefreshSavedFiles();
                    _screen = BuilderScreen.Load;
                    _cursor = 0;
                    _scroll = Vector2.zero;
                    break;
            }
        }
    }

    private void InputPrefabs()
    {
        if (Input.GetKeyDown(backKey)) { GoToMain(); return; }

        int count = availablePrefabs != null ? availablePrefabs.Length : 0;
        if (count == 0) return;

        if (IsKeyTriggered(navDownKey)) _cursor = (_cursor + 1) % count;
        if (IsKeyTriggered(navUpKey))   _cursor = (_cursor - 1 + count) % count;

        if (Input.GetKeyDown(confirmKey))
        {
            if (_cursor >= 0 && _cursor < count && availablePrefabs[_cursor] != null)
            {
                _placementMode = true;
                _armedPrefabIndex = _cursor;
                SetStatus("Click en escena para colocar: " + availablePrefabs[_cursor].name);
            }
        }
    }

    private void InputSceneObjects()
    {
        if (Input.GetKeyDown(backKey)) { GoToMain(); return; }

        int count = _sceneObjectsCache.Count;
        if (count == 0) return;

        if (IsKeyTriggered(navDownKey)) _cursor = (_cursor + 1) % count;
        if (IsKeyTriggered(navUpKey))   _cursor = (_cursor - 1 + count) % count;

        if (Input.GetKeyDown(confirmKey))
        {
            if (_cursor >= 0 && _cursor < count)
            {
                GameObject obj = _sceneObjectsCache[_cursor];
                if (obj != null)
                {
                    _selectedObject = obj;
                    _screen = BuilderScreen.EditObject;
                    _cursor = 0;
                    _scroll = Vector2.zero;
                }
            }
        }

        if (Input.GetKeyDown(deleteKey))
        {
            if (_cursor >= 0 && _cursor < count)
            {
                GameObject obj = _sceneObjectsCache[_cursor];
                if (obj != null)
                {
                    DeleteObject(obj);
                    RefreshSceneObjects();
                    if (_cursor >= _sceneObjectsCache.Count)
                        _cursor = Mathf.Max(0, _sceneObjectsCache.Count - 1);
                }
            }
        }
    }

    private void InputEditObject()
    {
        if (Input.GetKeyDown(backKey))
        {
            RefreshSceneObjects();
            _screen = BuilderScreen.SceneObjects;
            _cursor = 0;
            _scroll = Vector2.zero;
            return;
        }

        if (_selectedObject == null)
        {
            SetStatus("Objeto eliminado");
            GoToMain();
            return;
        }

        int count = TransformLabels.Length;
        if (IsKeyTriggered(navDownKey)) _cursor = (_cursor + 1) % count;
        if (IsKeyTriggered(navUpKey))   _cursor = (_cursor - 1 + count) % count;

        if (_cursor == 9)
        {
            if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(valueUpKey) || Input.GetKeyDown(valueDownKey))
                _selectedObject.SetActive(!_selectedObject.activeSelf);
            return;
        }

        bool fast = Input.GetKey(fastKey);
        float step;
        if (_cursor < 3)
            step = fast ? transformFastStep : transformStep;
        else if (_cursor < 6)
            step = fast ? rotationFastStep : rotationStep;
        else
            step = fast ? scaleFastStep : scaleStep;

        float delta = 0f;
        if (IsKeyTriggered(valueUpKey))   delta = step;
        if (IsKeyTriggered(valueDownKey)) delta = -step;

        if (Mathf.Abs(delta) < 0.0001f) return;

        Transform tr = _selectedObject.transform;

        if (_cursor < 3)
        {
            Vector3 pos = tr.position;
            pos[_cursor] += delta;
            tr.position = pos;
        }
        else if (_cursor < 6)
        {
            Vector3 euler = tr.eulerAngles;
            euler[_cursor - 3] += delta;
            tr.eulerAngles = euler;
        }
        else
        {
            Vector3 sc = tr.localScale;
            sc[_cursor - 6] += delta;
            tr.localScale = sc;
        }
    }

    private void InputLoad()
    {
        if (Input.GetKeyDown(backKey)) { GoToMain(); return; }

        int count = _savedFiles.Length;
        if (count == 0) return;

        if (IsKeyTriggered(navDownKey)) _cursor = (_cursor + 1) % count;
        if (IsKeyTriggered(navUpKey))   _cursor = (_cursor - 1 + count) % count;

        if (Input.GetKeyDown(confirmKey))
        {
            LoadScenario(_savedFiles[_cursor]);
            GoToMain();
        }

        if (Input.GetKeyDown(deleteKey))
        {
            DeleteSavedFile(_savedFiles[_cursor]);
            RefreshSavedFiles();
            if (_cursor >= _savedFiles.Length)
                _cursor = Mathf.Max(0, _savedFiles.Length - 1);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Mouse — Interacción
    // ═══════════════════════════════════════════════════════════

    private void HandleMouseInteraction()
    {
        if (Input.GetMouseButton(lookMouseButton)) return;
        if (IsMouseOverPanel()) return;
        if (_screen == BuilderScreen.Save) return;

        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = _activeCamera.ScreenPointToRay(Input.mousePosition);

        if (_placementMode && _armedPrefabIndex >= 0)
        {
            Vector3 pos;
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, raycastLayers))
                pos = hit.point;
            else
                pos = ray.origin + ray.direction * placementDistance;

            PlacePrefab(_armedPrefabIndex, pos);
        }
        else
        {
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, raycastLayers))
            {
                _selectedObject = hit.collider.gameObject;
                SetStatus("Seleccionado: " + _selectedObject.name);
            }
        }
    }

    private bool IsMouseOverPanel()
    {
        Vector2 mouseGUI = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        return _panelRect.Contains(mouseGUI);
    }

    // ═══════════════════════════════════════════════════════════
    //  Gestión de objetos
    // ═══════════════════════════════════════════════════════════

    private void PlacePrefab(int index, Vector3 position)
    {
        if (index < 0 || index >= availablePrefabs.Length || availablePrefabs[index] == null) return;

        GameObject instance = Instantiate(availablePrefabs[index], position, Quaternion.identity);
        instance.name = availablePrefabs[index].name + "_" + (_placedObjects.Count + 1);

        _placedObjects.Add(instance);
        _placedPrefabIndices.Add(index);
        _selectedObject = instance;

        SetStatus("Colocado: " + instance.name);
        RefreshSceneObjects();
    }

    private void DeleteObject(GameObject obj)
    {
        if (obj == null) return;

        int idx = _placedObjects.IndexOf(obj);
        if (idx >= 0)
        {
            _placedObjects.RemoveAt(idx);
            _placedPrefabIndices.RemoveAt(idx);
        }

        if (_selectedObject == obj) _selectedObject = null;

        string objName = obj.name;
        Destroy(obj);
        SetStatus("Eliminado: " + objName);
    }

    private void CleanPlacedObjects()
    {
        for (int i = _placedObjects.Count - 1; i >= 0; i--)
        {
            if (_placedObjects[i] == null)
            {
                _placedObjects.RemoveAt(i);
                _placedPrefabIndices.RemoveAt(i);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Caché de objetos de escena
    // ═══════════════════════════════════════════════════════════

    private void RefreshSceneObjects()
    {
        _sceneObjectsCache.Clear();

        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in rootObjects)
        {
            if (root == null || root == gameObject) continue;
            _sceneObjectsCache.Add(root);
            AddChildrenToCache(root.transform, 0);
        }
    }

    private void AddChildrenToCache(Transform parent, int depth)
    {
        if (depth > 10) return;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null) continue;
            _sceneObjectsCache.Add(child.gameObject);
            AddChildrenToCache(child, depth + 1);
        }
    }

    private int GetHierarchyDepth(Transform t)
    {
        int depth = 0;
        Transform cur = t.parent;
        while (cur != null) { depth++; cur = cur.parent; }
        return depth;
    }

    // ═══════════════════════════════════════════════════════════
    //  Guardar / Cargar escenario
    // ═══════════════════════════════════════════════════════════

    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, saveFolderName);
    }

    private void SaveScenario(string scenarioName)
    {
        if (string.IsNullOrWhiteSpace(scenarioName))
        {
            SetStatus("Nombre de escenario inválido");
            return;
        }

        CleanPlacedObjects();

        var data = new ScenarioData
        {
            scenarioName = scenarioName,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            objects = new List<ScenarioObjectData>()
        };

        for (int i = 0; i < _placedObjects.Count; i++)
        {
            GameObject obj = _placedObjects[i];
            if (obj == null) continue;

            int prefabIdx = _placedPrefabIndices[i];
            Transform tr = obj.transform;

            data.objects.Add(new ScenarioObjectData
            {
                prefabIndex = prefabIdx,
                prefabName = (prefabIdx >= 0 && prefabIdx < availablePrefabs.Length && availablePrefabs[prefabIdx] != null)
                    ? availablePrefabs[prefabIdx].name : "?",
                objectName = obj.name,
                position = tr.position,
                rotation = tr.eulerAngles,
                scale = tr.localScale,
                active = obj.activeSelf
            });
        }

        string dir = GetSavePath();
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string filePath = Path.Combine(dir, scenarioName + ".json");
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);

        SetStatus("Guardado: " + scenarioName + ".json (" + data.objects.Count + " objetos)");
        Debug.Log("[SceneBuilder] Escenario guardado en: " + filePath);
    }

    private void LoadScenario(string fileName)
    {
        string filePath = Path.Combine(GetSavePath(), fileName);
        if (!File.Exists(filePath))
        {
            SetStatus("Archivo no encontrado: " + fileName);
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            ScenarioData data = JsonUtility.FromJson<ScenarioData>(json);

            foreach (var obj in _placedObjects)
                if (obj != null) Destroy(obj);
            _placedObjects.Clear();
            _placedPrefabIndices.Clear();
            _selectedObject = null;

            int loaded = 0;
            int skipped = 0;

            foreach (var objData in data.objects)
            {
                if (objData.prefabIndex < 0 || objData.prefabIndex >= availablePrefabs.Length ||
                    availablePrefabs[objData.prefabIndex] == null)
                {
                    Debug.LogWarning("[SceneBuilder] Prefab index " + objData.prefabIndex +
                        " (" + objData.prefabName + ") no válido. Saltando.");
                    skipped++;
                    continue;
                }

                GameObject instance = Instantiate(
                    availablePrefabs[objData.prefabIndex],
                    objData.position,
                    Quaternion.Euler(objData.rotation)
                );
                instance.name = objData.objectName;
                instance.transform.localScale = objData.scale;
                instance.SetActive(objData.active);

                _placedObjects.Add(instance);
                _placedPrefabIndices.Add(objData.prefabIndex);
                loaded++;
            }

            RefreshSceneObjects();

            string msg = "Cargado: " + data.scenarioName + " (" + loaded + " objetos)";
            if (skipped > 0) msg += " [" + skipped + " saltados]";
            SetStatus(msg);
            Debug.Log("[SceneBuilder] Escenario cargado: " + filePath);
        }
        catch (Exception ex)
        {
            SetStatus("Error al cargar: " + ex.Message);
            Debug.LogError("[SceneBuilder] Error al cargar escenario: " + ex);
        }
    }

    private void RefreshSavedFiles()
    {
        string dir = GetSavePath();
        if (!Directory.Exists(dir))
        {
            _savedFiles = Array.Empty<string>();
            return;
        }

        string[] paths = Directory.GetFiles(dir, "*.json");
        _savedFiles = new string[paths.Length];
        for (int i = 0; i < paths.Length; i++)
            _savedFiles[i] = Path.GetFileName(paths[i]);
    }

    private void DeleteSavedFile(string fileName)
    {
        string filePath = Path.Combine(GetSavePath(), fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            SetStatus("Archivo eliminado: " + fileName);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Status
    // ═══════════════════════════════════════════════════════════

    private void SetStatus(string msg)
    {
        _statusMessage = msg;
        _statusMessageTime = Time.unscaledTime;
    }

    // ═══════════════════════════════════════════════════════════
    //  OnGUI — Estilos
    // ═══════════════════════════════════════════════════════════

    private void InitStyles()
    {
        if (_stylesReady) return;

        Texture2D bg = new Texture2D(1, 1);
        bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.85f));
        bg.Apply();

        _sBox = new GUIStyle(GUI.skin.box);
        _sBox.normal.background = bg;
        _sBox.padding = new RectOffset(12, 12, 10, 10);

        _sTitle = new GUIStyle(GUI.skin.label)
        {
            fontSize = _fontSize + 4,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _sTitle.normal.textColor = Color.white;

        _sLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = _fontSize,
            richText = true
        };
        _sLabel.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

        _sSel = new GUIStyle(_sLabel);
        _sSel.normal.textColor = new Color(1f, 1f, 0.4f);

        _sGray = new GUIStyle(_sLabel);
        _sGray.normal.textColor = new Color(0.5f, 0.5f, 0.5f);

        _sHeader = new GUIStyle(_sLabel);
        _sHeader.normal.textColor = new Color(0.6f, 0.85f, 1f);

        _sStatus = new GUIStyle(_sLabel)
        {
            fontSize = _fontSize - 2,
            alignment = TextAnchor.MiddleCenter
        };
        _sStatus.normal.textColor = new Color(0.4f, 1f, 0.4f);

        _stylesReady = true;
    }

    // ═══════════════════════════════════════════════════════════
    //  OnGUI — Render
    // ═══════════════════════════════════════════════════════════

    private void OnGUI()
    {
        if (!builderModeEnabled || !_wasEnabled) return;

        InitStyles();

        float w = 440f;
        float h = Mathf.Min(620f, Screen.height * 0.9f);
        float x = 10f;
        float y = (Screen.height - h) * 0.5f;
        _panelRect = new Rect(x, y, w, h);

        GUI.Box(_panelRect, GUIContent.none, _sBox);

        Rect area = new Rect(x + 12, y + 10, w - 24, h - 20);
        GUILayout.BeginArea(area);
        _scroll = GUILayout.BeginScrollView(_scroll);

        switch (_screen)
        {
            case BuilderScreen.Main:         DrawMain();         break;
            case BuilderScreen.Prefabs:      DrawPrefabs();      break;
            case BuilderScreen.SceneObjects: DrawSceneObjects(); break;
            case BuilderScreen.EditObject:   DrawEditObject();   break;
            case BuilderScreen.Save:         DrawSave();         break;
            case BuilderScreen.Load:         DrawLoad();         break;
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        DrawStatusBar();

        if (_placementMode)
            DrawPlacementIndicator();
    }

    // ── Pantalla: Menú principal ──────────────────────────────

    private void DrawMain()
    {
        GUILayout.Label("Scene Builder", _sTitle);
        GUILayout.Space(4);

        GUILayout.Label(
            "<b>[" + toggleBuilderKey + "]</b> Toggle builder   " +
            "<b>[" + navUpKey + "/" + navDownKey + "]</b> Navegar   " +
            "<b>[" + confirmKey + "]</b> Seleccionar",
            _sGray);
        GUILayout.Label(
            "<b>Ratón-" + (lookMouseButton == 1 ? "Der" : lookMouseButton == 0 ? "Izq" : "Med") +
            " + WASD</b> Mover cámara   <b>Scroll</b> Velocidad",
            _sGray);
        GUILayout.Space(4);

        if (_activeCamera != null)
        {
            Vector3 pos = _activeCamera.transform.position;
            GUILayout.Label(
                "<color=#888888>Cámara: (" + pos.x.ToString("F1") + ", " +
                pos.y.ToString("F1") + ", " + pos.z.ToString("F1") +
                ")  Vel: " + moveSpeed.ToString("F1") + "</color>",
                _sLabel);
        }

        GUILayout.Space(8);

        for (int i = 0; i < MainOptions.Length; i++)
        {
            string prefix = i == _cursor ? "►  " : "    ";
            GUIStyle style = i == _cursor ? _sSel : _sLabel;
            GUILayout.Label(prefix + "<b>" + MainOptions[i] + "</b>", style);
        }

        GUILayout.Space(12);

        GUILayout.Label("<color=#888888>Objetos colocados: " + _placedObjects.Count + "</color>", _sLabel);

        if (_selectedObject != null)
            GUILayout.Label("<color=#88CCFF>Seleccionado: " + _selectedObject.name + "</color>", _sLabel);

        if (_placementMode && _armedPrefabIndex >= 0 && _armedPrefabIndex < availablePrefabs.Length)
        {
            string prefabName = availablePrefabs[_armedPrefabIndex] != null
                ? availablePrefabs[_armedPrefabIndex].name : "?";
            GUILayout.Label("<color=#FFAA44>Colocando: " + prefabName + " (click en escena)</color>", _sLabel);
        }
    }

    // ── Pantalla: Lista de prefabs ───────────────────────────

    private void DrawPrefabs()
    {
        GUILayout.Label("◄ Añadir Objeto", _sTitle);
        GUILayout.Space(4);
        GUILayout.Label(
            "<b>[" + confirmKey + "]</b> Armar para colocar   " +
            "<b>[" + backKey + "]</b> Volver",
            _sGray);
        GUILayout.Space(8);

        int count = availablePrefabs != null ? availablePrefabs.Length : 0;
        if (count == 0)
        {
            GUILayout.Label("<color=#FF6666>No hay prefabs asignados en el array.</color>", _sLabel);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            string name = availablePrefabs[i] != null ? availablePrefabs[i].name : "<color=#FF6666>(vacío)</color>";
            string prefix = i == _cursor ? "►  " : "    ";
            GUIStyle style = i == _cursor ? _sSel : _sLabel;

            string armed = (_placementMode && _armedPrefabIndex == i)
                ? " <color=#FFAA44>[ACTIVO]</color>"
                : "";
            GUILayout.Label(prefix + name + armed, style);
        }
    }

    // ── Pantalla: Objetos en escena ──────────────────────────

    private void DrawSceneObjects()
    {
        GUILayout.Label("◄ Objetos en Escena", _sTitle);
        GUILayout.Space(4);
        GUILayout.Label(
            "<b>[" + confirmKey + "]</b> Editar   " +
            "<b>[" + deleteKey + "]</b> Eliminar   " +
            "<b>[" + backKey + "]</b> Volver",
            _sGray);
        GUILayout.Space(8);

        int count = _sceneObjectsCache.Count;
        if (count == 0)
        {
            GUILayout.Label("<color=#FF6666>No hay objetos en la escena.</color>", _sLabel);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject obj = _sceneObjectsCache[i];
            if (obj == null) continue;

            bool isPlaced = _placedObjects.Contains(obj);
            string tag = isPlaced ? "<color=#FFAA44>[B]</color> " : "";

            int depth = GetHierarchyDepth(obj.transform);
            string indent = depth > 0 ? new string(' ', depth * 3) : "";

            string activeIcon = obj.activeSelf ? "" : " <color=#FF6666>[OFF]</color>";
            string selected = (obj == _selectedObject) ? " <color=#88CCFF>◆</color>" : "";

            string prefix = i == _cursor ? "►  " : "    ";
            GUIStyle style = i == _cursor ? _sSel : _sLabel;

            GUILayout.Label(prefix + indent + tag + obj.name + activeIcon + selected, style);
        }
    }

    // ── Pantalla: Editar objeto ──────────────────────────────

    private void DrawEditObject()
    {
        if (_selectedObject == null)
        {
            GUILayout.Label("Objeto no disponible", _sTitle);
            return;
        }

        GUILayout.Label("◄ " + _selectedObject.name, _sTitle);
        GUILayout.Space(4);
        GUILayout.Label(
            "<b>[" + valueDownKey + "/" + valueUpKey + "]</b> Modificar   " +
            "<b>[" + fastKey + "]</b> Rápido   " +
            "<b>[" + backKey + "]</b> Volver",
            _sGray);
        GUILayout.Space(8);

        Transform tr = _selectedObject.transform;
        Vector3 pos = tr.position;
        Vector3 rot = tr.eulerAngles;
        Vector3 sc = tr.localScale;

        float[] values =
        {
            pos.x, pos.y, pos.z,
            rot.x, rot.y, rot.z,
            sc.x, sc.y, sc.z
        };

        for (int i = 0; i < TransformLabels.Length; i++)
        {
            string prefix = i == _cursor ? "►  " : "    ";
            GUIStyle style = i == _cursor ? _sSel : _sLabel;

            if (i < 9)
            {
                GUILayout.Label(
                    prefix + "<b>" + TransformLabels[i] + "</b>: " +
                    "<color=#FFCC00>" + values[i].ToString("F2") + "</color>",
                    style);
            }
            else
            {
                string active = _selectedObject.activeSelf
                    ? "<color=#66FF66>True</color>"
                    : "<color=#FF6666>False</color>";
                GUILayout.Label(
                    prefix + "<b>" + TransformLabels[i] + "</b>: " + active,
                    style);
            }
        }

        GUILayout.Space(8);
        bool isPlaced = _placedObjects.Contains(_selectedObject);
        GUILayout.Label(
            "<color=#888888>Tipo: " + (isPlaced ? "Builder" : "Escena") + "</color>",
            _sLabel);
    }

    // ── Pantalla: Guardar ────────────────────────────────────

    private void DrawSave()
    {
        GUILayout.Label("◄ Guardar Escenario", _sTitle);
        GUILayout.Space(4);
        GUILayout.Label("<b>[Enter]</b> Guardar   <b>[Esc]</b> Cancelar", _sGray);
        GUILayout.Space(8);

        GUILayout.Label(
            "Objetos a guardar: <color=#FFCC00>" + _placedObjects.Count + "</color>",
            _sLabel);
        GUILayout.Space(4);

        GUILayout.Label("Nombre:", _sLabel);

        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                SaveScenario(_saveNameBuf);
                GoToMain();
                e.Use();
                return;
            }
            else if (e.keyCode == KeyCode.Escape)
            {
                GoToMain();
                e.Use();
                return;
            }
            else if (e.keyCode == KeyCode.Backspace)
            {
                if (_saveNameBuf.Length > 0)
                    _saveNameBuf = _saveNameBuf.Substring(0, _saveNameBuf.Length - 1);
                e.Use();
            }
            else if (e.character != '\0' && !char.IsControl(e.character))
            {
                _saveNameBuf += e.character;
                e.Use();
            }
        }

        bool blink = (int)(Time.unscaledTime * 2f) % 2 == 0;
        string cursor = blink ? "<color=#FFCC00>|</color>" : " ";
        GUILayout.Label("  > <color=#AADDFF>" + _saveNameBuf + "</color>" + cursor, _sSel);

        GUILayout.Space(4);
        GUILayout.Label("<color=#888888>Ruta: " + GetSavePath() + "</color>", _sGray);
    }

    // ── Pantalla: Cargar ─────────────────────────────────────

    private void DrawLoad()
    {
        GUILayout.Label("◄ Cargar Escenario", _sTitle);
        GUILayout.Space(4);
        GUILayout.Label(
            "<b>[" + confirmKey + "]</b> Cargar   " +
            "<b>[" + deleteKey + "]</b> Eliminar archivo   " +
            "<b>[" + backKey + "]</b> Volver",
            _sGray);
        GUILayout.Space(8);

        if (_savedFiles.Length == 0)
        {
            GUILayout.Label("<color=#FF6666>No hay escenarios guardados.</color>", _sLabel);
            GUILayout.Space(4);
            GUILayout.Label("<color=#888888>Ruta: " + GetSavePath() + "</color>", _sGray);
            return;
        }

        for (int i = 0; i < _savedFiles.Length; i++)
        {
            string prefix = i == _cursor ? "►  " : "    ";
            GUIStyle style = i == _cursor ? _sSel : _sLabel;
            GUILayout.Label(prefix + _savedFiles[i], style);
        }

        GUILayout.Space(4);
        GUILayout.Label("<color=#888888>Ruta: " + GetSavePath() + "</color>", _sGray);
    }

    // ── Barra de estado ──────────────────────────────────────

    private void DrawStatusBar()
    {
        if (string.IsNullOrEmpty(_statusMessage)) return;

        float elapsed = Time.unscaledTime - _statusMessageTime;
        if (elapsed > 4f) { _statusMessage = ""; return; }

        float alpha = elapsed > 3f ? 1f - (elapsed - 3f) : 1f;

        Rect statusRect = new Rect(10f, Screen.height - 40f, 500f, 30f);
        Color c = _sStatus.normal.textColor;
        _sStatus.normal.textColor = new Color(c.r, c.g, c.b, alpha);
        GUI.Label(statusRect, _statusMessage, _sStatus);
        _sStatus.normal.textColor = c;
    }

    // ── Indicador de colocación ──────────────────────────────

    private void DrawPlacementIndicator()
    {
        if (!_placementMode || _armedPrefabIndex < 0) return;

        string name = (_armedPrefabIndex < availablePrefabs.Length && availablePrefabs[_armedPrefabIndex] != null)
            ? availablePrefabs[_armedPrefabIndex].name
            : "?";

        GUIStyle centered = new GUIStyle(_sStatus)
        {
            alignment = TextAnchor.MiddleCenter
        };
        centered.normal.textColor = new Color(1f, 0.7f, 0.2f);

        Rect rect = new Rect(Screen.width * 0.5f - 200f, 10f, 400f, 30f);
        GUI.Label(rect, "Click para colocar: " + name + "   [Esc] Cancelar", centered);
    }

    // ═══════════════════════════════════════════════════════════
    //  Seguridad — limpieza al desactivar
    // ═══════════════════════════════════════════════════════════

    private void OnDisable()
    {
        if (!_wasEnabled) return;
        builderModeEnabled = false;
        DisableBuilder();
    }
}
