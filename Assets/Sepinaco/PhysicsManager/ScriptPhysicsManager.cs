using System;
using UnityEngine;

public enum StartCollidersMode
{
    [Tooltip("Usa el estado individual (collidersEnabled) de cada target.")]
    UseIndividualSettings,

    [Tooltip("Fuerza todos los colliders activados al iniciar.")]
    EnableAll,

    [Tooltip("Fuerza todos los colliders desactivados al iniciar.")]
    DisableAll
}

[Serializable]
public class PhysicsTarget
{
    [Tooltip("GameObject objetivo. Sus colliders y los de todos sus hijos serán gestionados.")]
    public GameObject target;

    [Tooltip("Marca/desmarca para activar o desactivar los colliders.")]
    public bool collidersEnabled = true;

    [NonSerialized] public Collider[] cachedColliders;
}

/// <summary>
/// Gestiona la activación/desactivación de colliders de múltiples GameObjects.
///
/// Los valores de "collidersEnabled" se serializan en la escena y se aplican en
/// Awake al arrancar en cualquier plataforma.
///
/// Para cambios en runtime desde código, usar la API pública:
///   SetTargetState(index, enabled), EnableAll(), DisableAll(), RefreshCache().
///
/// En el Editor, OnValidate aplica los cambios inmediatamente al modificar el Inspector.
/// Un Custom Editor (ScriptPhysicsManagerEditor) añade botones y toggles mejorados.
///
/// En runtime, pulsa la tecla de menú para abrir un panel OnGUI con controles
/// interactivos para activar/desactivar colliders de cada target.
/// </summary>
public class ScriptPhysicsManager : MonoBehaviour
{
    [Header("Start Behaviour")]
    [Tooltip("Qué hacer con los colliders al arrancar la escena.")]
    [SerializeField] private StartCollidersMode _startMode = StartCollidersMode.UseIndividualSettings;

    [Header("Physics Targets")]
    [Tooltip("Arrastra aquí los GameObjects cuyas físicas quieres gestionar.")]
    [SerializeField] private PhysicsTarget[] _targets = Array.Empty<PhysicsTarget>();

    [Header("Menú")]
    [Tooltip("Tecla para abrir/cerrar el menú de controles de físicas")]
    public KeyCode menuKey = KeyCode.F1;

    [Header("Controles (solo funcionan con el menú abierto)")]
    [Tooltip("Tecla para activar todos los colliders")]
    public KeyCode enableAllKey = KeyCode.E;

    [Tooltip("Tecla para desactivar todos los colliders")]
    public KeyCode disableAllKey = KeyCode.Q;

    [Tooltip("Tecla para alternar el collider del target seleccionado")]
    public KeyCode toggleSelectedKey = KeyCode.G;

    [Tooltip("Tecla para seleccionar el siguiente target")]
    public KeyCode nextTargetKey = KeyCode.X;

    [Tooltip("Tecla para seleccionar el target anterior")]
    public KeyCode prevTargetKey = KeyCode.B;

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

    private static readonly Collider[] EmptyColliders = Array.Empty<Collider>();
    private bool _cacheReady;

    private bool _menuActive;
    private int _selectedIndex;
    private Vector2 _scrollPosition;

    private GUIStyle _boxStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _selectedLabelStyle;
    private bool _stylesInitialized;
    private int _guiFontSize = 14;
    private const int GuiFontSizeMin = 8;
    private const int GuiFontSizeMax = 40;
    private const int GuiFontSizeStep = 2;

    private float _guiBoxWidth = 380f;
    private float _guiBoxHeight = 400f;
    private const float GuiBoxSizeStep = 20f;
    private const float GuiBoxWidthMin = 200f;
    private const float GuiBoxWidthMax = 1200f;
    private const float GuiBoxHeightMin = 100f;
    private const float GuiBoxHeightMax = 1200f;

    public int TargetCount => _targets.Length;

    public StartCollidersMode StartMode => _startMode;

    private void Awake()
    {
        BuildCache();

        switch (_startMode)
        {
            case StartCollidersMode.EnableAll:
                SetAllInternal(true);
                break;
            case StartCollidersMode.DisableAll:
                SetAllInternal(false);
                break;
            default:
                ApplyAll();
                break;
        }
    }

    private void Update()
    {
        if (_targets == null || _targets.Length == 0) return;

        if (Input.GetKeyDown(menuKey))
            _menuActive = !_menuActive;

        if (!_menuActive) return;

        if (Input.GetKeyDown(enableAllKey))
            EnableAll();

        if (Input.GetKeyDown(disableAllKey))
            DisableAll();

        if (Input.GetKeyDown(nextTargetKey))
            _selectedIndex = (_selectedIndex + 1) % _targets.Length;

        if (Input.GetKeyDown(prevTargetKey))
            _selectedIndex = (_selectedIndex - 1 + _targets.Length) % _targets.Length;

        if (Input.GetKeyDown(toggleSelectedKey))
            SetTargetState(_selectedIndex, !_targets[_selectedIndex].collidersEnabled);

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

    // ───────────────────────── API pública (runtime) ─────────────────────────

    public void SetTargetState(int index, bool enabled)
    {
        if ((uint)index >= (uint)_targets.Length) return;
        PhysicsTarget entry = _targets[index];
        entry.collidersEnabled = enabled;
        if (_cacheReady)
            SetColliders(entry.cachedColliders, enabled);
    }

    public void EnableAll() => SetAllInternal(true);
    public void DisableAll() => SetAllInternal(false);

    public PhysicsTarget GetTarget(int index)
    {
        return (uint)index < (uint)_targets.Length ? _targets[index] : null;
    }

    /// <summary>
    /// Reconstruye la caché y reaplica el estado.
    /// Llamar si se añaden/quitan hijos en runtime.
    /// </summary>
    public void RefreshCache()
    {
        BuildCache();
        ApplyAll();
    }

    // ────────────────────── Editor: aplicar cambios al tocar Inspector ──────────────────────

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && _targets != null)
        {
            foreach (PhysicsTarget entry in _targets)
            {
                if (entry.target == null) continue;
                Collider[] cols = entry.target.GetComponentsInChildren<Collider>(true);
                SetColliders(cols, entry.collidersEnabled);
            }
        }

        if (Application.isPlaying && _cacheReady)
            ApplyAll();
    }
#endif

    // ───────────────────────── OnGUI Menu ─────────────────────────

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

        _selectedLabelStyle = new GUIStyle(_labelStyle);
        _selectedLabelStyle.normal.textColor = new Color(1f, 1f, 0.4f);
        _selectedLabelStyle.richText = true;

        _stylesInitialized = true;
    }

    private void OnGUI()
    {
        if (!_menuActive) return;

        InitStyles();

        int targetCount = _targets != null ? _targets.Length : 0;
        float lineHeight = 22f;
        float headerHeight = 220f;
        float contentHeight = headerHeight + targetCount * lineHeight;
        float boxWidth = _guiBoxWidth;
        float maxBoxHeight = Mathf.Min(contentHeight, _guiBoxHeight, Screen.height * 0.95f);
        float x = Screen.width - boxWidth - 10f;
        float y = 10f;
        Rect boxRect = new Rect(x, y, boxWidth, maxBoxHeight);

        GUI.Box(boxRect, GUIContent.none, _boxStyle);

        Rect areaRect = new Rect(x + 16, y + 12, boxWidth - 32, maxBoxHeight - 24);
        GUILayout.BeginArea(areaRect);
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        GUILayout.Label("Physics Controls", _titleStyle);
        GUILayout.Space(10);

        GUILayout.Label($"<b>[{enableAllKey}]</b>  Activar todos los colliders", _labelStyle);
        GUILayout.Label($"<b>[{disableAllKey}]</b>  Desactivar todos los colliders", _labelStyle);
        GUILayout.Label($"<b>[{toggleSelectedKey}]</b>  Alternar collider seleccionado", _labelStyle);
        GUILayout.Label($"<b>[{nextTargetKey}]</b> / <b>[{prevTargetKey}]</b>  Navegar targets", _labelStyle);
        GUILayout.Label($"<b>[{scrollUpKey}]</b> / <b>[{scrollDownKey}]</b>  Scroll menú", _labelStyle);
        GUILayout.Label($"<b>[{zoomInKey}]</b> / <b>[{zoomOutKey}]</b>  Zoom UI ({_guiFontSize}px)", _labelStyle);
        GUILayout.Label($"<b>[{uiWidthDecreaseKey}]</b> / <b>[{uiWidthIncreaseKey}]</b>  Ancho UI ({_guiBoxWidth}px)", _labelStyle);
        GUILayout.Label($"<b>[{uiHeightDecreaseKey}]</b> / <b>[{uiHeightIncreaseKey}]</b>  Alto UI ({_guiBoxHeight}px)", _labelStyle);
        GUILayout.Space(8);

        for (int i = 0; i < targetCount; i++)
        {
            PhysicsTarget entry = _targets[i];
            string targetName = entry.target != null ? entry.target.name : "(vacío)";
            string stateColor = entry.collidersEnabled ? "<color=#66FF66>ON</color>" : "<color=#FF6666>OFF</color>";
            string prefix = i == _selectedIndex ? "►  " : "    ";
            GUIStyle style = i == _selectedIndex ? _selectedLabelStyle : _labelStyle;
            GUILayout.Label($"{prefix}<b>{targetName}</b>  {stateColor}", style);
        }

        GUILayout.Space(4);
        GUILayout.Label($"<color=#888888>[{menuKey}] para cerrar</color>", _labelStyle);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // ───────────────────────── Internals ─────────────────────────

    private void BuildCache()
    {
        for (int i = 0, len = _targets.Length; i < len; i++)
        {
            PhysicsTarget entry = _targets[i];
            entry.cachedColliders = entry.target != null
                ? entry.target.GetComponentsInChildren<Collider>(true)
                : EmptyColliders;
        }
        _cacheReady = true;
    }

    private void SetAllInternal(bool enabled)
    {
        for (int i = 0, len = _targets.Length; i < len; i++)
        {
            PhysicsTarget entry = _targets[i];
            entry.collidersEnabled = enabled;
            if (_cacheReady)
                SetColliders(entry.cachedColliders, enabled);
        }
    }

    private void ApplyAll()
    {
        for (int i = 0, len = _targets.Length; i < len; i++)
            SetColliders(_targets[i].cachedColliders, _targets[i].collidersEnabled);
    }

    private static void SetColliders(Collider[] colliders, bool enabled)
    {
        for (int i = 0, len = colliders.Length; i < len; i++)
            colliders[i].enabled = enabled;
    }
}
