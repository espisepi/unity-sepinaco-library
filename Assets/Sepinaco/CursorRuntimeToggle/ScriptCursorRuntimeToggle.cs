using UnityEngine;

/// <summary>
/// Permite mostrar u ocultar el icono del cursor en runtime.
/// </summary>
public class ScriptCursorRuntimeToggle : MonoBehaviour
{
    [Header("Configuracion")]
    [Tooltip("Estado inicial del icono del raton al iniciar la escena.")]
    [SerializeField] private bool showCursor = true;

    [Tooltip("Tecla para alternar mostrar/ocultar el cursor en runtime.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Alpha4;

    [Header("Bloqueo del cursor")]
    [Tooltip("Estado de bloqueo cuando el cursor esta visible.")]
    [SerializeField] private CursorLockMode lockWhenVisible = CursorLockMode.None;

    [Tooltip("Estado de bloqueo cuando el cursor esta oculto.")]
    [SerializeField] private CursorLockMode lockWhenHidden = CursorLockMode.Confined;

    [Header("Integracion Sepinaco")]
    [Tooltip("Si existe ScriptGodModeCamera, sincroniza su hideCursor para que no sobreescriba este script.")]
    [SerializeField] private ScriptGodModeCamera godModeCamera;

    [Tooltip("Busca ScriptGodModeCamera automaticamente si no se asigna en Inspector.")]
    [SerializeField] private bool autoFindGodModeCamera = true;

    private void Awake()
    {
        if (godModeCamera == null && autoFindGodModeCamera)
            godModeCamera = FindObjectOfType<ScriptGodModeCamera>();
    }

    private void Start()
    {
        ApplyCursorState();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showCursor = !showCursor;
            ApplyCursorState();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        Cursor.visible = showCursor;
        Cursor.lockState = showCursor ? lockWhenVisible : lockWhenHidden;

        if (godModeCamera != null)
            godModeCamera.hideCursor = !showCursor;
    }
}
