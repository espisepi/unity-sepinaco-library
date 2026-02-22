using UnityEngine;

/// <summary>
/// Efecto de post-procesado que emula las limitaciones visuales de la PlayStation 1:
///   • Pixelación — resolución interna reducida (320×240 típica de PS1).
///   • Reducción de profundidad de color — 5 bits por canal (32 niveles), simulando el framebuffer de 15-bit.
///   • Temblor de texturas — desplazamiento UV pseudo-aleatorio por "píxel" que imita la falta de
///     corrección de perspectiva y el jitter de vértices de la PS1.
///   • Dithering ordenado (Bayer 4×4) — patrón de tramado como el que usaba la PS1 para suavizar
///     los gradientes de color en escenas oscuras.
///
/// Uso: añadir este componente a CUALQUIER GameObject de la escena.
/// El script localiza automáticamente Camera.main y le inyecta un componente
/// auxiliar invisible que ejecuta el post-procesado. No es necesario tocar la cámara.
/// </summary>
[ExecuteInEditMode]
public class ScriptPostProcessingPixelPs1 : MonoBehaviour
{
    // ──────────────── Pixelación ────────────────

    [Header("Pixelación")]
    [Tooltip("Tamaño de cada 'píxel' en pantalla. Valores más altos = resolución más baja.")]
    [Range(1f, 16f)]
    [SerializeField] private float _pixelSize = 4f;

    // ──────────────── Color ────────────────

    [Header("Profundidad de color")]
    [Tooltip("Niveles por canal de color. PS1 usaba 32 (5 bits). Valores bajos = más posterización.")]
    [Range(2f, 256f)]
    [SerializeField] private float _colorDepth = 32f;

    // ──────────────── Jitter de texturas ────────────────

    [Header("Temblor de texturas (Jitter)")]
    [Tooltip("Intensidad del desplazamiento UV. Simula la inestabilidad de las texturas en PS1.")]
    [Range(0f, 0.02f)]
    [SerializeField] private float _jitterIntensity = 0.002f;

    [Tooltip("Velocidad del temblor. Cuántas veces por segundo cambia el patrón de jitter.")]
    [Range(1f, 60f)]
    [SerializeField] private float _jitterSpeed = 30f;

    // ──────────────── Dithering ────────────────

    [Header("Dithering (tramado)")]
    [Tooltip("Intensidad del patrón de dithering Bayer 4×4. 0 = desactivado.")]
    [Range(0f, 0.15f)]
    [SerializeField] private float _ditherIntensity = 0.03f;

    // ──────────────── Cámara ────────────────

    [Header("Cámara")]
    [Tooltip("Cámara sobre la que aplicar el efecto. Si se deja vacío, se usa Camera.main.")]
    [SerializeField] private Camera _targetCamera;

    // ──────────────── Interno ────────────────

    private PS1CameraHook _hook;

    private void OnEnable()
    {
        AttachHook();
    }

    private void OnDisable()
    {
        DetachHook();
    }

    private void Update()
    {
        if (_hook == null || !_hook.IsValid)
            AttachHook();

        if (_hook != null)
            _hook.PushSettings(_pixelSize, _colorDepth, _jitterIntensity, _jitterSpeed, _ditherIntensity);
    }

    private Camera ResolveCamera()
    {
        if (_targetCamera != null) return _targetCamera;

        Camera main = Camera.main;
        if (main != null) return main;

        Debug.LogWarning("[PS1 PostProcess] No se encontró Camera.main ni una cámara asignada.");
        return null;
    }

    private void AttachHook()
    {
        DetachHook();

        Camera cam = ResolveCamera();
        if (cam == null) return;

        _hook = cam.GetComponent<PS1CameraHook>();
        if (_hook == null)
            _hook = cam.gameObject.AddComponent<PS1CameraHook>();

        _hook.Init(this);
    }

    private void DetachHook()
    {
        if (_hook != null)
        {
            _hook.Cleanup();
            if (Application.isPlaying)
                Destroy(_hook);
            else
                DestroyImmediate(_hook);
            _hook = null;
        }
    }

    // ──────────────── API pública ────────────────

    /// <summary>Ajusta el tamaño de píxel en runtime.</summary>
    public void SetPixelSize(float size)
    {
        _pixelSize = Mathf.Clamp(size, 1f, 16f);
    }

    /// <summary>Ajusta la profundidad de color en runtime.</summary>
    public void SetColorDepth(float levels)
    {
        _colorDepth = Mathf.Clamp(levels, 2f, 256f);
    }

    /// <summary>Ajusta la intensidad del jitter en runtime.</summary>
    public void SetJitterIntensity(float intensity)
    {
        _jitterIntensity = Mathf.Clamp(intensity, 0f, 0.02f);
    }

    /// <summary>Ajusta la velocidad del jitter en runtime.</summary>
    public void SetJitterSpeed(float speed)
    {
        _jitterSpeed = Mathf.Clamp(speed, 1f, 60f);
    }

    /// <summary>Ajusta la intensidad del dithering en runtime.</summary>
    public void SetDitherIntensity(float intensity)
    {
        _ditherIntensity = Mathf.Clamp(intensity, 0f, 0.15f);
    }

    /// <summary>Aplica un preset "PS1 auténtico" con los valores más fieles a la consola.</summary>
    public void ApplyAuthenticPS1Preset()
    {
        _pixelSize = 4f;
        _colorDepth = 32f;
        _jitterIntensity = 0.002f;
        _jitterSpeed = 30f;
        _ditherIntensity = 0.03f;
    }

    /// <summary>Aplica un preset más sutil, útil para juegos que quieren un toque retro sin ser extremo.</summary>
    public void ApplySubtleRetroPreset()
    {
        _pixelSize = 2f;
        _colorDepth = 64f;
        _jitterIntensity = 0.001f;
        _jitterSpeed = 15f;
        _ditherIntensity = 0.015f;
    }
}

/// <summary>
/// Componente auxiliar que se inyecta automáticamente en la cámara.
/// Ejecuta OnRenderImage con el material PS1 y los parámetros que recibe
/// del ScriptPostProcessingPixelPs1 principal.
/// Se oculta en el Inspector para no confundir al usuario.
/// </summary>
[ExecuteInEditMode]
[AddComponentMenu("")]
public class PS1CameraHook : MonoBehaviour
{
    private ScriptPostProcessingPixelPs1 _owner;
    private Material _material;

    private static readonly string ShaderName = "Sepinaco/PS1PostProcess";

    private static readonly int PropPixelSize = Shader.PropertyToID("_PixelSize");
    private static readonly int PropColorDepth = Shader.PropertyToID("_ColorDepth");
    private static readonly int PropJitterIntensity = Shader.PropertyToID("_JitterIntensity");
    private static readonly int PropJitterSpeed = Shader.PropertyToID("_JitterSpeed");
    private static readonly int PropDitherIntensity = Shader.PropertyToID("_DitherIntensity");
    private static readonly int PropTime2 = Shader.PropertyToID("_Time2");

    public bool IsValid => _owner != null && _material != null;

    public void Init(ScriptPostProcessingPixelPs1 owner)
    {
        _owner = owner;
        hideFlags = HideFlags.HideAndDontSave;
        EnsureMaterial();
    }

    public void PushSettings(float pixelSize, float colorDepth, float jitterIntensity, float jitterSpeed, float ditherIntensity)
    {
        if (_material == null) return;

        _material.SetFloat(PropPixelSize, pixelSize);
        _material.SetFloat(PropColorDepth, colorDepth);
        _material.SetFloat(PropJitterIntensity, jitterIntensity);
        _material.SetFloat(PropJitterSpeed, jitterSpeed);
        _material.SetFloat(PropDitherIntensity, ditherIntensity);
        _material.SetFloat(PropTime2, Time.unscaledTime);
    }

    private void EnsureMaterial()
    {
        if (_material != null) return;

        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"[PS1 PostProcess] No se encontró el shader '{ShaderName}'. " +
                           "Asegúrate de que el archivo PS1PostProcess.shader está en la carpeta Shaders.");
            return;
        }

        if (!shader.isSupported)
        {
            Debug.LogWarning("[PS1 PostProcess] El shader no es compatible con esta GPU.");
            return;
        }

        _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
    }

    public void Cleanup()
    {
        if (_material != null)
        {
            if (Application.isPlaying)
                Destroy(_material);
            else
                DestroyImmediate(_material);
            _material = null;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_material == null || _owner == null || !_owner.enabled)
        {
            Graphics.Blit(source, destination);
            return;
        }

        Graphics.Blit(source, destination, _material);
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
