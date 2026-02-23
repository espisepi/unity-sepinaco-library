using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador de cámara en Modo Dios para navegación libre por la escena.
///
/// Al activarse, desactiva los controles de cámara y player existentes
/// y ofrece vuelo libre estilo editor de Unity (clic-derecho + WASD).
/// Al desactivarse, restaura todos los controles previos.
///
/// Todos los campos serializados son compatibles con ScriptDebugInspector.
/// Funciona tanto en el editor como en builds.
/// </summary>
public class ScriptGodModeCamera : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    //  Configuración — God Mode
    // ═══════════════════════════════════════════════════════════

    [Header("God Mode")]
    [Tooltip("Activar/desactivar el modo dios de la cámara. " +
             "Modificable desde el Inspector y desde ScriptDebugInspector.")]
    public bool godModeEnabled;

    [Tooltip("Tecla para alternar god mode en runtime.")]
    public KeyCode toggleKey = KeyCode.Alpha3;

    // ═══════════════════════════════════════════════════════════
    //  Configuración — Cámara objetivo
    // ═══════════════════════════════════════════════════════════

    [Header("Cámara")]
    [Tooltip("Cámara a controlar. Si no se asigna, se usa Camera.main.")]
    [SerializeField] private Camera _targetCamera;

    // ═══════════════════════════════════════════════════════════
    //  Configuración — Scripts a desactivar
    // ═══════════════════════════════════════════════════════════

    [Header("Scripts a desactivar")]
    [Tooltip("MonoBehaviours que se desactivarán al entrar en God Mode " +
             "y se reactivarán al salir.")]
    [SerializeField] private MonoBehaviour[] _scriptsToDisable = Array.Empty<MonoBehaviour>();

    [Tooltip("Buscar y desactivar automáticamente MonoBehaviours " +
             "en la cámara y en objetos con tag Player.")]
    public bool autoDetectControls = true;

    // ═══════════════════════════════════════════════════════════
    //  Configuración — Movimiento
    // ═══════════════════════════════════════════════════════════

    [Header("Movimiento")]
    [Tooltip("Velocidad base de movimiento (unidades/segundo).")]
    public float moveSpeed = 10f;

    [Tooltip("Multiplicador de velocidad al mantener la tecla rápida.")]
    public float fastMultiplier = 3f;

    [Tooltip("Multiplicador de velocidad al mantener la tecla lenta.")]
    public float slowMultiplier = 0.25f;

    [Tooltip("Suavizado del movimiento. 0 = instantáneo, valores altos = más suave.")]
    [Range(0f, 0.99f)]
    public float moveSmoothing = 0.1f;

    // ═══════════════════════════════════════════════════════════
    //  Configuración — Rotación (mirada)
    // ═══════════════════════════════════════════════════════════

    [Header("Rotación")]
    [Tooltip("Sensibilidad del ratón para rotar la cámara.")]
    public float lookSensitivity = 2f;

    [Tooltip("Suavizado de la rotación. 0 = instantáneo, valores altos = más suave.")]
    [Range(0f, 0.99f)]
    public float lookSmoothing = 0.02f;

    [Tooltip("Invertir el eje Y del ratón.")]
    public bool invertY;

    // ═══════════════════════════════════════════════════════════
    //  Configuración — Ajuste de velocidad con scroll
    // ═══════════════════════════════════════════════════════════

    [Header("Velocidad con scroll")]
    [Tooltip("Permite cambiar la velocidad con la rueda del ratón.")]
    public bool scrollAdjustsSpeed = true;

    [Tooltip("Factor multiplicador por paso de scroll.")]
    public float scrollSpeedFactor = 1.15f;

    [Tooltip("Velocidad mínima permitida.")]
    public float minSpeed = 0.1f;

    [Tooltip("Velocidad máxima permitida.")]
    public float maxSpeed = 200f;

    // ═══════════════════════════════════════════════════════════
    //  Configuración — Teclas de movimiento
    // ═══════════════════════════════════════════════════════════

    [Header("Teclas de movimiento")]
    public KeyCode forwardKey = KeyCode.W;
    public KeyCode backKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode upKey = KeyCode.E;
    public KeyCode downKey = KeyCode.Q;
    public KeyCode fastKey = KeyCode.LeftShift;
    public KeyCode slowKey = KeyCode.LeftControl;

    [Header("Ratón")]
    [Tooltip("Botón del ratón para activar la rotación (0 = izq, 1 = der, 2 = medio).")]
    public int lookMouseButton = 0;

    [Tooltip("Ocultar el cursor del ratón mientras God Mode está activo.")]
    public bool hideCursor = true;

    // ═══════════════════════════════════════════════════════════
    //  Estado interno
    // ═══════════════════════════════════════════════════════════

    private bool _wasEnabled;
    private Camera _activeCamera;

    private float _yaw;
    private float _pitch;
    private float _smoothYaw;
    private float _smoothPitch;
    private Vector3 _smoothVelocity;

    private Vector3 _savedCameraLocalPos;
    private Quaternion _savedCameraLocalRot;
    private Transform _savedCameraParent;

    private CursorLockMode _savedCursorLock;
    private bool _savedCursorVisible;

    private readonly List<MonoBehaviour> _disabledScripts = new List<MonoBehaviour>();
    private readonly List<bool> _originalEnabledStates = new List<bool>();

    // ═══════════════════════════════════════════════════════════
    //  OnValidate — responde a cambios desde el Inspector
    //  y desde ScriptDebugInspector (vía callOnValidateAfterChange)
    // ═══════════════════════════════════════════════════════════

    private void OnValidate()
    {
        if (!Application.isPlaying) return;

        if (godModeEnabled && !_wasEnabled)
            EnableGodMode();
        else if (!godModeEnabled && _wasEnabled)
            DisableGodMode();
    }

    // ═══════════════════════════════════════════════════════════
    //  Update — input y control de la cámara
    // ═══════════════════════════════════════════════════════════

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            godModeEnabled = !godModeEnabled;

        if (godModeEnabled && !_wasEnabled)
            EnableGodMode();
        else if (!godModeEnabled && _wasEnabled)
            DisableGodMode();

        if (!godModeEnabled || _activeCamera == null) return;

        HandleLook();
        HandleMovement();
        HandleScrollSpeed();
    }

    // ═══════════════════════════════════════════════════════════
    //  Activar / Desactivar God Mode
    // ═══════════════════════════════════════════════════════════

    private void EnableGodMode()
    {
        _activeCamera = _targetCamera != null ? _targetCamera : Camera.main;
        if (_activeCamera == null)
        {
            Debug.LogWarning("[GodModeCamera] No se encontró ninguna cámara.");
            godModeEnabled = false;
            return;
        }

        _wasEnabled = true;

        Transform camT = _activeCamera.transform;
        _savedCameraLocalPos = camT.localPosition;
        _savedCameraLocalRot = camT.localRotation;
        _savedCameraParent = camT.parent;

        camT.SetParent(null);

        Vector3 euler = camT.eulerAngles;
        _yaw = euler.y;
        _pitch = euler.x;
        if (_pitch > 180f) _pitch -= 360f;
        _smoothYaw = _yaw;
        _smoothPitch = _pitch;
        _smoothVelocity = Vector3.zero;

        _savedCursorLock = Cursor.lockState;
        _savedCursorVisible = Cursor.visible;

        Cursor.visible = !hideCursor;
        Cursor.lockState = hideCursor ? CursorLockMode.Confined : CursorLockMode.None;

        DisableControlScripts();

        Debug.Log("[GodModeCamera] God Mode ACTIVADO.");
    }

    private void DisableGodMode()
    {
        _wasEnabled = false;

        RestoreControlScripts();

        if (_activeCamera != null)
        {
            Transform camT = _activeCamera.transform;
            camT.SetParent(_savedCameraParent);
            camT.localPosition = _savedCameraLocalPos;
            camT.localRotation = _savedCameraLocalRot;
        }

        Cursor.lockState = _savedCursorLock;
        Cursor.visible = _savedCursorVisible;

        Debug.Log("[GodModeCamera] God Mode DESACTIVADO.");
    }

    // ═══════════════════════════════════════════════════════════
    //  Gestión de scripts a desactivar / restaurar
    // ═══════════════════════════════════════════════════════════

    private void DisableControlScripts()
    {
        _disabledScripts.Clear();
        _originalEnabledStates.Clear();

        if (_scriptsToDisable != null)
        {
            foreach (MonoBehaviour mb in _scriptsToDisable)
            {
                if (mb != null && mb.enabled && mb != this)
                    TrackAndDisable(mb);
            }
        }

        if (!autoDetectControls) return;

        if (_activeCamera != null)
        {
            foreach (MonoBehaviour mb in _activeCamera.GetComponents<MonoBehaviour>())
            {
                if (ShouldSkip(mb)) continue;
                TrackAndDisable(mb);
            }
        }

        try
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject player in players)
            {
                foreach (MonoBehaviour mb in player.GetComponents<MonoBehaviour>())
                {
                    if (ShouldSkip(mb)) continue;
                    TrackAndDisable(mb);
                }
            }
        }
        catch (UnityException) { }
    }

    private void TrackAndDisable(MonoBehaviour mb)
    {
        if (_disabledScripts.Contains(mb)) return;
        _disabledScripts.Add(mb);
        _originalEnabledStates.Add(mb.enabled);
        mb.enabled = false;
    }

    private bool ShouldSkip(MonoBehaviour mb)
    {
        if (mb == null || mb == this || !mb.enabled) return true;

        string typeName = mb.GetType().Name;
        return typeName == "ScriptDebugInspector" ||
               typeName == "ScriptGodModeCamera";
    }

    private void RestoreControlScripts()
    {
        for (int i = 0; i < _disabledScripts.Count; i++)
        {
            if (_disabledScripts[i] != null)
                _disabledScripts[i].enabled = _originalEnabledStates[i];
        }

        _disabledScripts.Clear();
        _originalEnabledStates.Clear();
    }

    // ═══════════════════════════════════════════════════════════
    //  Control de cámara — Rotación (mirada)
    // ═══════════════════════════════════════════════════════════

    private void HandleLook()
    {
        Cursor.visible = !hideCursor;

        if (!Input.GetMouseButton(lookMouseButton))
        {
            Cursor.lockState = hideCursor ? CursorLockMode.Confined : CursorLockMode.None;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;

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
    //  Control de cámara — Movimiento (vuelo libre)
    // ═══════════════════════════════════════════════════════════

    private void HandleMovement()
    {
        Vector3 input = Vector3.zero;

        if (Input.GetKey(forwardKey)) input += Vector3.forward;
        if (Input.GetKey(backKey))    input += Vector3.back;
        if (Input.GetKey(leftKey))    input += Vector3.left;
        if (Input.GetKey(rightKey))   input += Vector3.right;
        if (Input.GetKey(upKey))      input += Vector3.up;
        if (Input.GetKey(downKey))    input += Vector3.down;

        float speed = moveSpeed;
        if (Input.GetKey(fastKey)) speed *= fastMultiplier;
        if (Input.GetKey(slowKey)) speed *= slowMultiplier;

        Vector3 targetVelocity = input.normalized * speed;

        float smoothT = 1f - moveSmoothing;
        _smoothVelocity = Vector3.Lerp(_smoothVelocity, targetVelocity, smoothT);

        Transform camT = _activeCamera.transform;
        camT.position += camT.TransformDirection(_smoothVelocity) * Time.unscaledDeltaTime;
    }

    // ═══════════════════════════════════════════════════════════
    //  Control de cámara — Ajuste de velocidad con scroll
    // ═══════════════════════════════════════════════════════════

    private void HandleScrollSpeed()
    {
        if (!scrollAdjustsSpeed) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        moveSpeed *= scroll > 0f ? scrollSpeedFactor : 1f / scrollSpeedFactor;
        moveSpeed = Mathf.Clamp(moveSpeed, minSpeed, maxSpeed);
    }

    // ═══════════════════════════════════════════════════════════
    //  Seguridad — desactivar limpiamente si se destruye
    // ═══════════════════════════════════════════════════════════

    private void OnDisable()
    {
        if (!_wasEnabled) return;
        godModeEnabled = false;
        DisableGodMode();
    }
}
