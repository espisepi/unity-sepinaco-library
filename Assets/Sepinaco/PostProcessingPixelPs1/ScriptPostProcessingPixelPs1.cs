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
///
/// En runtime, pulsa 1 para abrir un panel OnGUI (arriba-derecha) con controles
/// interactivos para modificar todos los parámetros del efecto.
/// </summary>
[ExecuteInEditMode]
public class ScriptPostProcessingPixelPs1 : MonoBehaviour
{
    // ──────────────── Pixelación ────────────────

    [Header("Pixelación")]
    [Tooltip("Tamaño de cada 'píxel' en pantalla. Valores más altos = resolución más baja. Sin límite.")]
    [SerializeField] private float _pixelSize = 4f;

    // ──────────────── Color ────────────────

    [Header("Profundidad de color")]
    [Tooltip("Niveles por canal de color. PS1 usaba 32 (5 bits). Valores bajos = más posterización. Sin límite.")]
    [SerializeField] private float _colorDepth = 32f;

    // ──────────────── Jitter de texturas ────────────────

    [Header("Temblor de texturas (Jitter)")]
    [Tooltip("Intensidad del desplazamiento UV. Simula la inestabilidad de las texturas en PS1. Sin límite.")]
    [SerializeField] private float _jitterIntensity = 0.002f;

    [Tooltip("Velocidad del temblor. Cuántas veces por segundo cambia el patrón de jitter. Sin límite.")]
    [SerializeField] private float _jitterSpeed = 30f;

    // ──────────────── Dithering ────────────────

    [Header("Dithering (tramado)")]
    [Tooltip("Intensidad del patrón de dithering Bayer 4×4. 0 = desactivado. Sin límite.")]
    [SerializeField] private float _ditherIntensity = 0.03f;

    // ──────────────── Estado inicial del efecto ────────────────

    [Header("Estado inicial del efecto")]
    [Tooltip("Si está activado, el efecto se aplica al iniciar la escena. Se puede alternar en runtime con la tecla de efecto.")]
    [SerializeField] private bool _effectEnabledOnStart = true;

    // ──────────────── Límites de rango ────────────────

    [Header("Límites de rango")]
    [Tooltip("Si está activado, los valores se restringen a los rangos clásicos de PS1. Si no, se permiten valores infinitos.")]
    [SerializeField] private bool _clampToRange = true;

    // ──────────────── Cámara ────────────────

    [Header("Cámara")]
    [Tooltip("Cámara sobre la que aplicar el efecto. Si se deja vacío, se usa Camera.main.")]
    [SerializeField] private Camera _targetCamera;

    // ──────────────── Menú ────────────────

    [Header("Menú")]
    [Tooltip("Tecla para abrir/cerrar el menú de controles del efecto PS1")]
    public KeyCode menuKey = KeyCode.F1;

    [Header("Controles de efecto (solo funcionan con el menú abierto)")]
    [Tooltip("Tecla para activar/desactivar el efecto completo")]
    public KeyCode toggleEffectKey = KeyCode.Alpha2;

    [Tooltip("Tecla para activar/desactivar el clamp de rango en las variables")]
    public KeyCode toggleClampKey = KeyCode.C;

    [Tooltip("Tecla para aumentar el tamaño de píxel")]
    public KeyCode pixelSizeUpKey = KeyCode.Alpha3;

    [Tooltip("Tecla para disminuir el tamaño de píxel")]
    public KeyCode pixelSizeDownKey = KeyCode.Alpha4;

    [Tooltip("Tecla para aumentar la profundidad de color")]
    public KeyCode colorDepthUpKey = KeyCode.Alpha5;

    [Tooltip("Tecla para disminuir la profundidad de color")]
    public KeyCode colorDepthDownKey = KeyCode.Alpha6;

    [Tooltip("Tecla para aumentar la intensidad del jitter")]
    public KeyCode jitterIntensityUpKey = KeyCode.Alpha7;

    [Tooltip("Tecla para disminuir la intensidad del jitter")]
    public KeyCode jitterIntensityDownKey = KeyCode.Alpha8;

    [Tooltip("Tecla para aumentar la velocidad del jitter")]
    public KeyCode jitterSpeedUpKey = KeyCode.Alpha9;

    [Tooltip("Tecla para disminuir la velocidad del jitter")]
    public KeyCode jitterSpeedDownKey = KeyCode.Alpha0;

    [Tooltip("Tecla para aumentar la intensidad del dithering")]
    public KeyCode ditherIntensityUpKey = KeyCode.U;

    [Tooltip("Tecla para disminuir la intensidad del dithering")]
    public KeyCode ditherIntensityDownKey = KeyCode.Y;

    [Header("Presets (solo funcionan con el menú abierto)")]
    [Tooltip("Tecla para aplicar el preset PS1 auténtico")]
    public KeyCode presetAuthenticKey = KeyCode.Alpha1;

    [Tooltip("Tecla para aplicar el preset retro sutil")]
    public KeyCode presetSubtleKey = KeyCode.End;

    [Header("Incrementos por pulsación")]
    [Tooltip("Incremento del tamaño de píxel por pulsación")]
    public float pixelSizeStep = 1f;

    [Tooltip("Incremento de la profundidad de color por pulsación")]
    public float colorDepthStep = 8f;

    [Tooltip("Incremento de la intensidad del jitter por pulsación")]
    public float jitterIntensityStep = 0.001f;

    [Tooltip("Incremento de la velocidad del jitter por pulsación")]
    public float jitterSpeedStep = 5f;

    [Tooltip("Incremento de la intensidad del dithering por pulsación")]
    public float ditherIntensityStep = 0.005f;

    [Header("Scroll del menú (solo funcionan con el menú abierto)")]
    [Tooltip("Tecla para hacer scroll hacia arriba en el menú")]
    public KeyCode scrollUpKey = KeyCode.UpArrow;

    [Tooltip("Tecla para hacer scroll hacia abajo en el menú")]
    public KeyCode scrollDownKey = KeyCode.DownArrow;

    [Tooltip("Velocidad de scroll en píxeles por segundo")]
    public float scrollSpeed = 200f;

    [Header("Zoom de la UI (solo funcionan con el menú abierto)")]
    [Tooltip("Tecla para aumentar el tamaño de las letras de la UI")]
    public KeyCode zoomInKey = KeyCode.I;

    [Tooltip("Tecla para disminuir el tamaño de las letras de la UI")]
    public KeyCode zoomOutKey = KeyCode.O;

    [Header("Tamaño de la UI (solo funcionan con el menú abierto)")]
    [Tooltip("Tecla para aumentar el ancho de la UI")]
    public KeyCode uiWidthIncreaseKey = KeyCode.RightArrow;

    [Tooltip("Tecla para disminuir el ancho de la UI")]
    public KeyCode uiWidthDecreaseKey = KeyCode.LeftArrow;

    [Tooltip("Tecla para aumentar el alto de la UI")]
    public KeyCode uiHeightIncreaseKey = KeyCode.PageUp;

    [Tooltip("Tecla para disminuir el alto de la UI")]
    public KeyCode uiHeightDecreaseKey = KeyCode.PageDown;

    // ──────────────── Interno ────────────────

    private PS1CameraHook _hook;
    private bool _effectEnabled = true;

    private bool _menuActive;
    private Vector2 _scrollPosition;

    private GUIStyle _boxStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _labelStyle;
    private bool _stylesInitialized;
    private int _guiFontSize = 14;
    private const int GuiFontSizeMin = 8;
    private const int GuiFontSizeMax = 40;
    private const int GuiFontSizeStep = 2;

    private float _guiBoxWidth = 420f;
    private float _guiBoxHeight = 440f;
    private const float GuiBoxSizeStep = 20f;
    private const float GuiBoxWidthMin = 200f;
    private const float GuiBoxWidthMax = 1200f;
    private const float GuiBoxHeightMin = 100f;
    private const float GuiBoxHeightMax = 1200f;

    private void Start()
    {
        _effectEnabled = _effectEnabledOnStart;
    }

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
        {
            if (_effectEnabled)
                _hook.PushSettings(_pixelSize, _colorDepth, _jitterIntensity, _jitterSpeed, _ditherIntensity);
            else
                _hook.PushDisabled();
        }

        if (!Application.isPlaying) return;

        if (Input.GetKeyDown(menuKey))
            _menuActive = !_menuActive;

        if (!_menuActive) return;

        if (Input.GetKeyDown(toggleEffectKey))
            _effectEnabled = !_effectEnabled;

        if (Input.GetKeyDown(toggleClampKey))
        {
            _clampToRange = !_clampToRange;
            if (_clampToRange) ApplyClamp();
        }

        if (Input.GetKeyDown(pixelSizeUpKey))
            _pixelSize += pixelSizeStep;

        if (Input.GetKeyDown(pixelSizeDownKey))
            _pixelSize -= pixelSizeStep;

        if (Input.GetKeyDown(colorDepthUpKey))
            _colorDepth += colorDepthStep;

        if (Input.GetKeyDown(colorDepthDownKey))
            _colorDepth -= colorDepthStep;

        if (Input.GetKeyDown(jitterIntensityUpKey))
            _jitterIntensity += jitterIntensityStep;

        if (Input.GetKeyDown(jitterIntensityDownKey))
            _jitterIntensity -= jitterIntensityStep;

        if (Input.GetKeyDown(jitterSpeedUpKey))
            _jitterSpeed += jitterSpeedStep;

        if (Input.GetKeyDown(jitterSpeedDownKey))
            _jitterSpeed -= jitterSpeedStep;

        if (Input.GetKeyDown(ditherIntensityUpKey))
            _ditherIntensity += ditherIntensityStep;

        if (Input.GetKeyDown(ditherIntensityDownKey))
            _ditherIntensity -= ditherIntensityStep;

        if (_clampToRange) ApplyClamp();

        if (Input.GetKeyDown(presetAuthenticKey))
            ApplyAuthenticPS1Preset();

        if (Input.GetKeyDown(presetSubtleKey))
            ApplySubtleRetroPreset();

        if (Input.GetKey(scrollUpKey))
            _scrollPosition.y -= scrollSpeed * Time.deltaTime;

        if (Input.GetKey(scrollDownKey))
            _scrollPosition.y += scrollSpeed * Time.deltaTime;

        if (_scrollPosition.y < 0f) _scrollPosition.y = 0f;

        if (Input.GetKeyDown(zoomInKey))
        {
            _guiFontSize = Mathf.Min(_guiFontSize + GuiFontSizeStep, GuiFontSizeMax);
            _stylesInitialized = false;
        }

        if (Input.GetKeyDown(zoomOutKey))
        {
            _guiFontSize = Mathf.Max(_guiFontSize - GuiFontSizeStep, GuiFontSizeMin);
            _stylesInitialized = false;
        }

        if (Input.GetKeyDown(uiWidthIncreaseKey))
            _guiBoxWidth = Mathf.Min(_guiBoxWidth + GuiBoxSizeStep, GuiBoxWidthMax);

        if (Input.GetKeyDown(uiWidthDecreaseKey))
            _guiBoxWidth = Mathf.Max(_guiBoxWidth - GuiBoxSizeStep, GuiBoxWidthMin);

        if (Input.GetKeyDown(uiHeightIncreaseKey))
            _guiBoxHeight = Mathf.Min(_guiBoxHeight + GuiBoxSizeStep, GuiBoxHeightMax);

        if (Input.GetKeyDown(uiHeightDecreaseKey))
            _guiBoxHeight = Mathf.Max(_guiBoxHeight - GuiBoxSizeStep, GuiBoxHeightMin);
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

    // ──────────────── Clamp ────────────────

    private const float PixelSizeMin = 1f;
    private const float PixelSizeMax = 16f;
    private const float ColorDepthMin = 2f;
    private const float ColorDepthMax = 256f;
    private const float JitterIntensityMin = 0f;
    private const float JitterIntensityMax = 0.02f;
    private const float JitterSpeedMin = 1f;
    private const float JitterSpeedMax = 60f;
    private const float DitherIntensityMin = 0f;
    private const float DitherIntensityMax = 0.15f;

    private void ApplyClamp()
    {
        _pixelSize = Mathf.Clamp(_pixelSize, PixelSizeMin, PixelSizeMax);
        _colorDepth = Mathf.Clamp(_colorDepth, ColorDepthMin, ColorDepthMax);
        _jitterIntensity = Mathf.Clamp(_jitterIntensity, JitterIntensityMin, JitterIntensityMax);
        _jitterSpeed = Mathf.Clamp(_jitterSpeed, JitterSpeedMin, JitterSpeedMax);
        _ditherIntensity = Mathf.Clamp(_ditherIntensity, DitherIntensityMin, DitherIntensityMax);
    }

    // ──────────────── API pública ────────────────

    /// <summary>Ajusta el tamaño de píxel en runtime.</summary>
    public void SetPixelSize(float size)
    {
        _pixelSize = _clampToRange ? Mathf.Clamp(size, PixelSizeMin, PixelSizeMax) : size;
    }

    /// <summary>Ajusta la profundidad de color en runtime.</summary>
    public void SetColorDepth(float levels)
    {
        _colorDepth = _clampToRange ? Mathf.Clamp(levels, ColorDepthMin, ColorDepthMax) : levels;
    }

    /// <summary>Ajusta la intensidad del jitter en runtime.</summary>
    public void SetJitterIntensity(float intensity)
    {
        _jitterIntensity = _clampToRange ? Mathf.Clamp(intensity, JitterIntensityMin, JitterIntensityMax) : intensity;
    }

    /// <summary>Ajusta la velocidad del jitter en runtime.</summary>
    public void SetJitterSpeed(float speed)
    {
        _jitterSpeed = _clampToRange ? Mathf.Clamp(speed, JitterSpeedMin, JitterSpeedMax) : speed;
    }

    /// <summary>Ajusta la intensidad del dithering en runtime.</summary>
    public void SetDitherIntensity(float intensity)
    {
        _ditherIntensity = _clampToRange ? Mathf.Clamp(intensity, DitherIntensityMin, DitherIntensityMax) : intensity;
    }

    /// <summary>Aplica un preset "PS1 auténtico" con los valores más fieles a la consola.</summary>
    public void ApplyAuthenticPS1Preset()
    {
        _pixelSize = 4f;
        _colorDepth = 32f;
        _jitterIntensity = 0.002f;
        _jitterSpeed = 30f;
        _ditherIntensity = 0.03f;
        _effectEnabled = true;
    }

    /// <summary>Aplica un preset más sutil, útil para juegos que quieren un toque retro sin ser extremo.</summary>
    public void ApplySubtleRetroPreset()
    {
        _pixelSize = 2f;
        _colorDepth = 64f;
        _jitterIntensity = 0.001f;
        _jitterSpeed = 15f;
        _ditherIntensity = 0.015f;
        _effectEnabled = true;
    }

    // ──────────────── OnGUI Menu ────────────────

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        Texture2D bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
        bgTex.Apply();

        _boxStyle = new GUIStyle(GUI.skin.box);
        _boxStyle.normal.background = bgTex;
        _boxStyle.padding = new RectOffset(16, 16, 12, 12);

        _titleStyle = new GUIStyle(GUI.skin.label);
        _titleStyle.fontSize = _guiFontSize + 4;
        _titleStyle.fontStyle = FontStyle.Bold;
        _titleStyle.normal.textColor = Color.white;
        _titleStyle.alignment = TextAnchor.MiddleCenter;

        _labelStyle = new GUIStyle(GUI.skin.label);
        _labelStyle.fontSize = _guiFontSize;
        _labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
        _labelStyle.richText = true;

        _stylesInitialized = true;
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || !_menuActive) return;

        InitStyles();

        string effectColor = _effectEnabled ? "<color=#66FF66>ON</color>" : "<color=#FF6666>OFF</color>";

        float boxWidth = _guiBoxWidth;
        float contentHeight = 500f;
        float maxBoxHeight = Mathf.Min(contentHeight, _guiBoxHeight, Screen.height * 0.95f);
        float x = Screen.width - boxWidth - 10f;
        float y = Screen.height - maxBoxHeight - 10f;
        Rect boxRect = new Rect(x, y, boxWidth, maxBoxHeight);

        GUI.Box(boxRect, GUIContent.none, _boxStyle);

        Rect areaRect = new Rect(x + 16, y + 12, boxWidth - 32, maxBoxHeight - 24);
        GUILayout.BeginArea(areaRect);
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        GUILayout.Label("PS1 PostProcess Controls", _titleStyle);
        GUILayout.Space(10);

        string clampColor = _clampToRange ? "<color=#66FF66>ON</color>" : "<color=#FF6666>OFF</color>";
        string rangePixel = _clampToRange ? $"({PixelSizeMin}–{PixelSizeMax})" : "(-∞ … +∞)";
        string rangeColor = _clampToRange ? $"({ColorDepthMin}–{ColorDepthMax})" : "(-∞ … +∞)";
        string rangeJitterI = _clampToRange ? $"({JitterIntensityMin}–{JitterIntensityMax})" : "(-∞ … +∞)";
        string rangeJitterS = _clampToRange ? $"({JitterSpeedMin}–{JitterSpeedMax})" : "(-∞ … +∞)";
        string rangeDither = _clampToRange ? $"({DitherIntensityMin}–{DitherIntensityMax})" : "(-∞ … +∞)";

        GUILayout.Label($"<b>[{toggleEffectKey}]</b>  Efecto: {effectColor}", _labelStyle);
        GUILayout.Label($"<b>[{toggleClampKey}]</b>  Limitar rango: {clampColor}", _labelStyle);
        GUILayout.Space(4);
        GUILayout.Label($"<b>[{pixelSizeDownKey}]</b> / <b>[{pixelSizeUpKey}]</b>  Tamaño píxel: <color=#FFCC00>{_pixelSize:F1}</color>  {rangePixel}", _labelStyle);
        GUILayout.Label($"<b>[{colorDepthDownKey}]</b> / <b>[{colorDepthUpKey}]</b>  Prof. color: <color=#FFCC00>{_colorDepth:F0}</color>  {rangeColor}", _labelStyle);
        GUILayout.Label($"<b>[{jitterIntensityDownKey}]</b> / <b>[{jitterIntensityUpKey}]</b>  Jitter intens.: <color=#FFCC00>{_jitterIntensity:F4}</color>  {rangeJitterI}", _labelStyle);
        GUILayout.Label($"<b>[{jitterSpeedDownKey}]</b> / <b>[{jitterSpeedUpKey}]</b>  Jitter veloc.: <color=#FFCC00>{_jitterSpeed:F1}</color>  {rangeJitterS}", _labelStyle);
        GUILayout.Label($"<b>[{ditherIntensityDownKey}]</b> / <b>[{ditherIntensityUpKey}]</b>  Dither intens.: <color=#FFCC00>{_ditherIntensity:F3}</color>  {rangeDither}", _labelStyle);
        GUILayout.Space(4);
        GUILayout.Label($"<b>[{presetAuthenticKey}]</b>  Preset PS1 auténtico", _labelStyle);
        GUILayout.Label($"<b>[{presetSubtleKey}]</b>  Preset retro sutil", _labelStyle);
        GUILayout.Space(4);
        GUILayout.Label($"<b>[{scrollUpKey}]</b> / <b>[{scrollDownKey}]</b>  Scroll menú", _labelStyle);
        GUILayout.Label($"<b>[{zoomInKey}]</b> / <b>[{zoomOutKey}]</b>  Zoom UI ({_guiFontSize}px)", _labelStyle);
        GUILayout.Label($"<b>[{uiWidthDecreaseKey}]</b> / <b>[{uiWidthIncreaseKey}]</b>  Ancho UI ({_guiBoxWidth}px)", _labelStyle);
        GUILayout.Label($"<b>[{uiHeightDecreaseKey}]</b> / <b>[{uiHeightIncreaseKey}]</b>  Alto UI ({_guiBoxHeight}px)", _labelStyle);
        GUILayout.Space(4);
        GUILayout.Label($"<color=#888888>[{menuKey}] para cerrar</color>", _labelStyle);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
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

    public bool IsDisabled { get; private set; }

    public void PushSettings(float pixelSize, float colorDepth, float jitterIntensity, float jitterSpeed, float ditherIntensity)
    {
        IsDisabled = false;
        if (_material == null) return;

        _material.SetFloat(PropPixelSize, pixelSize);
        _material.SetFloat(PropColorDepth, colorDepth);
        _material.SetFloat(PropJitterIntensity, jitterIntensity);
        _material.SetFloat(PropJitterSpeed, jitterSpeed);
        _material.SetFloat(PropDitherIntensity, ditherIntensity);
        _material.SetFloat(PropTime2, Time.unscaledTime);
    }

    public void PushDisabled()
    {
        IsDisabled = true;
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
        if (_material == null || _owner == null || !_owner.enabled || IsDisabled)
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
