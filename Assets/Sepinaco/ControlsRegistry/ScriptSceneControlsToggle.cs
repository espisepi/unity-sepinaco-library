using System;
using System.Collections.Generic;
using PG;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Alterna entre los controles de Prototype y BuildEngine en escenas aditivas
/// sin desactivar raices completas por defecto (para preservar configuraciones).
/// </summary>
public class ScriptSceneControlsToggle : MonoBehaviour
{
    public enum ControlMode
    {
        PrototypeCharacterController,
        BuildEngine
    }

    [Serializable]
    private class SceneControlSet
    {
        public string sceneName;

        [Tooltip("Tipos de scripts de control a habilitar/deshabilitar en esta escena.")]
        public string[] controlBehaviourTypeNames;

        [Tooltip("Si esta activo, habilita/deshabilita todas las camaras de esta escena.")]
        public bool manageCameras = true;

        [Tooltip("Si esta activo, habilita/deshabilita todos los AudioListener de esta escena.")]
        public bool manageAudioListeners = true;

        [Tooltip("Si esta activo, habilita/deshabilita EventSystem e InputModules de esta escena.")]
        public bool manageEventSystems = true;

        [Tooltip("Behaviours extra para esta escena (manual).")]
        public Behaviour[] extraBehaviours;

        [Tooltip("Objetos extra para esta escena (manual).")]
        public GameObject[] extraObjects;

        [Tooltip("Solo si necesitas forzarlo: activa/desactiva estos objetos raiz por nombre.")]
        public bool toggleRootObjects;

        public string[] rootObjectNames;
    }

    private class SceneRuntimeRefs
    {
        public readonly List<Behaviour> controlBehaviours = new List<Behaviour>();
        public readonly List<Camera> cameras = new List<Camera>();
        public readonly List<AudioListener> audioListeners = new List<AudioListener>();
        public readonly List<EventSystem> eventSystems = new List<EventSystem>();
        public readonly List<BaseInputModule> inputModules = new List<BaseInputModule>();
        public readonly List<GameObject> rootObjects = new List<GameObject>();

        public void Clear()
        {
            controlBehaviours.Clear();
            cameras.Clear();
            audioListeners.Clear();
            eventSystems.Clear();
            inputModules.Clear();
            rootObjects.Clear();
        }
    }

    [Header("General")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Alpha5;
    [SerializeField] private ControlMode startMode = ControlMode.PrototypeCharacterController;
    [SerializeField] private bool autoResolveReferences = true;
    [SerializeField] private bool logChanges = true;

    [Header("Prototype Camera Isolation")]
    [Tooltip("Si esta activo, crea/usa una camara dedicada para Prototype y evita usar la camara de BuildEngine.")]
    [SerializeField] private bool isolatePrototypeCamera = true;

    [Tooltip("Nombre del objeto camara dedicado para Prototype.")]
    [SerializeField] private string prototypeDedicatedCameraName = "Prototype_Dedicated_MainCamera";

    [Tooltip("Si no existe camara para Prototype, la crea automaticamente.")]
    [SerializeField] private bool createPrototypeCameraIfMissing = true;

    [Tooltip("Si esta activo, copia los parametros de una camara de BuildEngine al crear la de Prototype.")]
    [SerializeField] private bool copyBuildCameraSettingsForPrototype = false;

    [Header("PrototypeScene_CharacterController")]
    [SerializeField] private SceneControlSet prototypeSet = new SceneControlSet
    {
        sceneName = "PrototypeScene_CharacterController",
        controlBehaviourTypeNames = new[]
        {
            "PG.SimpleCharacterController",
            "PG.CharacterInput",
            "PG.CarControllerInput",
            "PG.PlayerController",
            "PG.CameraController"
        }
    };

    [Header("BuildEngine")]
    [SerializeField] private SceneControlSet buildEngineSet = new SceneControlSet
    {
        sceneName = "BuildEngine",
        controlBehaviourTypeNames = new[]
        {
            "CameraController",
            "BuildingsMenu"
        }
    };

    [Header("Runtime")]
    [SerializeField] private ControlMode currentMode;
    [SerializeField] private Camera prototypeDedicatedCamera;

    private readonly SceneRuntimeRefs _prototypeRefs = new SceneRuntimeRefs();
    private readonly SceneRuntimeRefs _buildEngineRefs = new SceneRuntimeRefs();

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void Start()
    {
        if (autoResolveReferences)
            ResolveReferences();

        SetMode(startMode, force: true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleMode();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            currentMode = startMode;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!autoResolveReferences)
            return;

        ResolveReferences();
        SetMode(currentMode, force: true);
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (!autoResolveReferences)
            return;

        ResolveReferences();
        SetMode(currentMode, force: true);
    }

    public void ToggleMode()
    {
        ControlMode nextMode = currentMode == ControlMode.PrototypeCharacterController
            ? ControlMode.BuildEngine
            : ControlMode.PrototypeCharacterController;

        SetMode(nextMode);
    }

    public void SetMode(ControlMode mode, bool force = false)
    {
        if (autoResolveReferences)
            ResolveReferences();

        if (isolatePrototypeCamera)
            EnsurePrototypeCameraIsolation();

        if (!force && mode == currentMode)
            return;

        currentMode = mode;

        bool prototypeActive = currentMode == ControlMode.PrototypeCharacterController;
        bool buildEngineActive = !prototypeActive;

        ApplySetState(prototypeSet, _prototypeRefs, prototypeActive);
        ApplySetState(buildEngineSet, _buildEngineRefs, buildEngineActive);
        EnsureAnyCameraRendering(currentMode == ControlMode.PrototypeCharacterController ? prototypeSet : buildEngineSet);

        if (logChanges)
            Debug.Log("[ScriptSceneControlsToggle] Modo activo: " + currentMode);
    }

    [ContextMenu("Resolve References Now")]
    public void ResolveReferences()
    {
        ResolveSetReferences(prototypeSet, _prototypeRefs);
        ResolveSetReferences(buildEngineSet, _buildEngineRefs);
    }

    [ContextMenu("Activate Prototype Mode")]
    public void ActivatePrototypeMode()
    {
        SetMode(ControlMode.PrototypeCharacterController, force: true);
    }

    [ContextMenu("Activate BuildEngine Mode")]
    public void ActivateBuildEngineMode()
    {
        SetMode(ControlMode.BuildEngine, force: true);
    }

    private static void ResolveSetReferences(SceneControlSet set, SceneRuntimeRefs refs)
    {
        refs.Clear();

        if (set == null || string.IsNullOrWhiteSpace(set.sceneName))
            return;

        Scene scene = SceneManager.GetSceneByName(set.sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
                continue;

            MonoBehaviour[] allBehaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int b = 0; b < allBehaviours.Length; b++)
            {
                MonoBehaviour behaviour = allBehaviours[b];
                if (behaviour == null)
                    continue;

                if (MatchesConfiguredType(behaviour.GetType(), set.controlBehaviourTypeNames))
                    AddUnique(refs.controlBehaviours, behaviour);
            }

            if (set.manageCameras)
            {
                Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
                for (int c = 0; c < cameras.Length; c++)
                    AddUnique(refs.cameras, cameras[c]);
            }

            if (set.manageAudioListeners)
            {
                AudioListener[] listeners = root.GetComponentsInChildren<AudioListener>(true);
                for (int a = 0; a < listeners.Length; a++)
                    AddUnique(refs.audioListeners, listeners[a]);
            }

            if (set.manageEventSystems)
            {
                EventSystem[] systems = root.GetComponentsInChildren<EventSystem>(true);
                for (int e = 0; e < systems.Length; e++)
                    AddUnique(refs.eventSystems, systems[e]);

                BaseInputModule[] modules = root.GetComponentsInChildren<BaseInputModule>(true);
                for (int m = 0; m < modules.Length; m++)
                    AddUnique(refs.inputModules, modules[m]);
            }
        }

        if (set.toggleRootObjects && set.rootObjectNames != null)
        {
            for (int i = 0; i < set.rootObjectNames.Length; i++)
            {
                string rootName = set.rootObjectNames[i];
                if (string.IsNullOrWhiteSpace(rootName))
                    continue;

                for (int r = 0; r < roots.Length; r++)
                {
                    GameObject root = roots[r];
                    if (root != null && root.name == rootName)
                    {
                        AddUnique(refs.rootObjects, root);
                        break;
                    }
                }
            }
        }
    }

    private static bool MatchesConfiguredType(Type type, string[] configuredTypeNames)
    {
        if (type == null || configuredTypeNames == null || configuredTypeNames.Length == 0)
            return false;

        string fullName = type.FullName;
        string shortName = type.Name;

        for (int i = 0; i < configuredTypeNames.Length; i++)
        {
            string configuredName = configuredTypeNames[i];
            if (string.IsNullOrWhiteSpace(configuredName))
                continue;

            if (string.Equals(fullName, configuredName, StringComparison.Ordinal) ||
                string.Equals(shortName, configuredName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static void ApplySetState(SceneControlSet set, SceneRuntimeRefs refs, bool enabledState)
    {
        for (int i = 0; i < refs.controlBehaviours.Count; i++)
        {
            Behaviour behaviour = refs.controlBehaviours[i];
            if (behaviour != null)
                behaviour.enabled = enabledState;
        }

        if (set.manageCameras)
        {
            for (int i = 0; i < refs.cameras.Count; i++)
            {
                Camera camera = refs.cameras[i];
                if (camera != null)
                    camera.enabled = enabledState;
            }
        }

        if (set.manageAudioListeners)
        {
            for (int i = 0; i < refs.audioListeners.Count; i++)
            {
                AudioListener listener = refs.audioListeners[i];
                if (listener != null)
                    listener.enabled = enabledState;
            }
        }

        if (set.manageEventSystems)
        {
            for (int i = 0; i < refs.eventSystems.Count; i++)
            {
                EventSystem eventSystem = refs.eventSystems[i];
                if (eventSystem != null)
                    eventSystem.enabled = enabledState;
            }

            for (int i = 0; i < refs.inputModules.Count; i++)
            {
                BaseInputModule module = refs.inputModules[i];
                if (module != null)
                    module.enabled = enabledState;
            }
        }

        if (set.extraBehaviours != null)
        {
            for (int i = 0; i < set.extraBehaviours.Length; i++)
            {
                Behaviour behaviour = set.extraBehaviours[i];
                if (behaviour != null)
                    behaviour.enabled = enabledState;
            }
        }

        if (set.extraObjects != null)
        {
            for (int i = 0; i < set.extraObjects.Length; i++)
            {
                GameObject obj = set.extraObjects[i];
                if (obj != null)
                    obj.SetActive(enabledState);
            }
        }

        if (set.toggleRootObjects)
        {
            for (int i = 0; i < refs.rootObjects.Count; i++)
            {
                GameObject root = refs.rootObjects[i];
                if (root != null)
                    root.SetActive(enabledState);
            }
        }
    }

    private static void AddUnique<T>(List<T> list, T value) where T : class
    {
        if (value == null || list == null)
            return;

        if (!list.Contains(value))
            list.Add(value);
    }

    private void EnsureAnyCameraRendering(SceneControlSet activeSet)
    {
        Camera[] allCameras = FindObjectsOfType<Camera>(true);
        for (int i = 0; i < allCameras.Length; i++)
        {
            Camera cam = allCameras[i];
            if (cam != null && cam.enabled && cam.gameObject.activeInHierarchy)
                return;
        }

        if (activeSet == null || string.IsNullOrWhiteSpace(activeSet.sceneName))
            return;

        Scene scene = SceneManager.GetSceneByName(activeSet.sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            Camera[] sceneCameras = roots[r].GetComponentsInChildren<Camera>(true);
            for (int c = 0; c < sceneCameras.Length; c++)
            {
                Camera cam = sceneCameras[c];
                if (cam == null)
                    continue;

                if (!cam.gameObject.activeSelf)
                    cam.gameObject.SetActive(true);

                cam.enabled = true;

                if (logChanges)
                    Debug.Log("[ScriptSceneControlsToggle] Camara forzada para evitar Display 1 no cameras rendering: " + cam.name);

                return;
            }
        }
    }

    private void EnsurePrototypeCameraIsolation()
    {
        if (string.IsNullOrWhiteSpace(prototypeSet.sceneName))
            return;

        Scene prototypeScene = SceneManager.GetSceneByName(prototypeSet.sceneName);
        if (!prototypeScene.IsValid() || !prototypeScene.isLoaded)
            return;

        if (prototypeDedicatedCamera == null)
            prototypeDedicatedCamera = FindCameraByNameInScene(prototypeScene, prototypeDedicatedCameraName);

        if (prototypeDedicatedCamera == null && createPrototypeCameraIfMissing)
            prototypeDedicatedCamera = CreatePrototypeDedicatedCamera(prototypeScene);

        if (prototypeDedicatedCamera == null)
            return;

        AddUnique(_prototypeRefs.cameras, prototypeDedicatedCamera);
        AudioListener dedicatedListener = prototypeDedicatedCamera.GetComponent<AudioListener>();
        if (dedicatedListener != null)
            AddUnique(_prototypeRefs.audioListeners, dedicatedListener);

        SimpleCharacterController[] simpleControllers = FindObjectsOfType<SimpleCharacterController>(true);
        for (int i = 0; i < simpleControllers.Length; i++)
        {
            SimpleCharacterController controller = simpleControllers[i];
            if (controller == null || controller.gameObject.scene != prototypeScene)
                continue;

            // Importante: si el personaje esta inactivo (normalmente porque el jugador esta dentro del coche),
            // no debemos re-parentar la camara al CameraParent del personaje, ya que quedaria bajo una rama desactivada.
            bool characterIsActive = controller.gameObject.activeInHierarchy;
            if (characterIsActive && controller.CameraParent != null && prototypeDedicatedCamera.transform.parent != controller.CameraParent)
            {
                prototypeDedicatedCamera.transform.SetParent(controller.CameraParent);
                prototypeDedicatedCamera.transform.localPosition = Vector3.zero;
                prototypeDedicatedCamera.transform.localRotation = Quaternion.identity;
            }

            controller.Camera = prototypeDedicatedCamera;
        }

        PG.CameraController[] vehicleCameras = FindObjectsOfType<PG.CameraController>(true);
        for (int i = 0; i < vehicleCameras.Length; i++)
        {
            PG.CameraController vehicleCamera = vehicleCameras[i];
            if (vehicleCamera == null || vehicleCamera.gameObject.scene != prototypeScene)
                continue;

            vehicleCamera.MainCamera = prototypeDedicatedCamera;
        }
    }

    private Camera CreatePrototypeDedicatedCamera(Scene prototypeScene)
    {
        GameObject go = new GameObject(prototypeDedicatedCameraName);
        SceneManager.MoveGameObjectToScene(go, prototypeScene);

        Camera cam = go.AddComponent<Camera>();
        go.tag = "MainCamera";

        Camera sourceBuildCamera = GetPrimaryBuildCamera();
        if (sourceBuildCamera != null && copyBuildCameraSettingsForPrototype)
        {
            cam.CopyFrom(sourceBuildCamera);
        }
        else
        {
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.cullingMask = ~0;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 1500f;
            cam.depth = 0f;
        }

        AudioListener listener = go.GetComponent<AudioListener>();
        if (listener == null)
            listener = go.AddComponent<AudioListener>();

        listener.enabled = false;

        go.transform.position = new Vector3(0f, 2f, -5f);
        go.transform.rotation = Quaternion.identity;

        if (logChanges)
            Debug.Log("[ScriptSceneControlsToggle] Camara dedicada de Prototype creada: " + go.name);

        return cam;
    }

    private Camera GetPrimaryBuildCamera()
    {
        if (string.IsNullOrWhiteSpace(buildEngineSet.sceneName))
            return null;

        Scene buildScene = SceneManager.GetSceneByName(buildEngineSet.sceneName);
        if (!buildScene.IsValid() || !buildScene.isLoaded)
            return null;

        Camera byName = FindCameraByNameInScene(buildScene, "Main Camera");
        if (byName != null)
            return byName;

        GameObject[] roots = buildScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Camera[] sceneCameras = roots[i].GetComponentsInChildren<Camera>(true);
            for (int c = 0; c < sceneCameras.Length; c++)
            {
                if (sceneCameras[c] != null)
                    return sceneCameras[c];
            }
        }

        return null;
    }

    private static Camera FindCameraByNameInScene(Scene scene, string cameraName)
    {
        if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(cameraName))
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Camera[] sceneCameras = roots[i].GetComponentsInChildren<Camera>(true);
            for (int c = 0; c < sceneCameras.Length; c++)
            {
                Camera cam = sceneCameras[c];
                if (cam != null && cam.name == cameraName)
                    return cam;
            }
        }

        return null;
    }
}
