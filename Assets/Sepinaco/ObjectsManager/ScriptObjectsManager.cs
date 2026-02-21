using System;
using UnityEngine;

public enum StartObjectsMode
{
    [Tooltip("Usa el estado individual (isActive) de cada target.")]
    UseIndividualSettings,

    [Tooltip("Fuerza todos los objetos visibles al iniciar.")]
    ShowAll,

    [Tooltip("Fuerza todos los objetos ocultos al iniciar.")]
    HideAll
}

[Serializable]
public class ObjectTarget
{
    [Tooltip("GameObject objetivo. Se activará/desactivará en la escena.")]
    public GameObject target;

    [Tooltip("Marca/desmarca para mostrar u ocultar el objeto.")]
    public bool isActive = true;
}

/// <summary>
/// Gestiona la activación/desactivación de GameObjects en la escena.
///
/// Los valores de "isActive" se serializan en la escena y se aplican en
/// Awake al arrancar en cualquier plataforma.
///
/// Para cambios en runtime desde código, usar la API pública:
///   SetTargetState(index, active), ShowAll(), HideAll().
///
/// En el Editor, OnValidate aplica los cambios inmediatamente al modificar el Inspector.
///
/// En runtime, pulsa F1 para abrir un panel OnGUI (abajo-izquierda) con controles
/// interactivos para mostrar/ocultar cada target.
/// </summary>
public class ScriptObjectsManager : MonoBehaviour
{
    [Header("Start Behaviour")]
    [Tooltip("Qué hacer con los objetos al arrancar la escena.")]
    [SerializeField] private StartObjectsMode _startMode = StartObjectsMode.UseIndividualSettings;

    [Header("Object Targets")]
    [Tooltip("Arrastra aquí los GameObjects que quieres mostrar/ocultar.")]
    [SerializeField] private ObjectTarget[] _targets = Array.Empty<ObjectTarget>();

    [Header("Menú")]
    [Tooltip("Tecla para abrir/cerrar el menú de controles de objetos")]
    public KeyCode menuKey = KeyCode.F1;

    [Header("Controles (solo funcionan con el menú abierto)")]
    [Tooltip("Tecla para mostrar todos los objetos")]
    public KeyCode showAllKey = KeyCode.V;

    [Tooltip("Tecla para ocultar todos los objetos")]
    public KeyCode hideAllKey = KeyCode.L;

    [Tooltip("Tecla para alternar la visibilidad del objeto seleccionado")]
    public KeyCode toggleSelectedKey = KeyCode.H;

    [Tooltip("Tecla para seleccionar el siguiente target")]
    public KeyCode nextTargetKey = KeyCode.N;

    [Tooltip("Tecla para seleccionar el target anterior")]
    public KeyCode prevTargetKey = KeyCode.J;

    [Header("Scroll del menú (solo funcionan con el menú abierto)")]
    [Tooltip("Tecla para hacer scroll hacia arriba en el menú")]
    public KeyCode scrollUpKey = KeyCode.UpArrow;

    [Tooltip("Tecla para hacer scroll hacia abajo en el menú")]
    public KeyCode scrollDownKey = KeyCode.DownArrow;

    [Tooltip("Velocidad de scroll en píxeles por segundo")]
    public float scrollSpeed = 200f;

    private bool _menuActive;
    private int _selectedIndex;
    private Vector2 _scrollPosition;

    private GUIStyle _boxStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _selectedLabelStyle;
    private bool _stylesInitialized;

    public int TargetCount => _targets.Length;

    public StartObjectsMode StartMode => _startMode;

    private void Awake()
    {
        switch (_startMode)
        {
            case StartObjectsMode.ShowAll:
                SetAllInternal(true);
                break;
            case StartObjectsMode.HideAll:
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

        if (Input.GetKeyDown(showAllKey))
            ShowAll();

        if (Input.GetKeyDown(hideAllKey))
            HideAll();

        if (Input.GetKeyDown(nextTargetKey))
            _selectedIndex = (_selectedIndex + 1) % _targets.Length;

        if (Input.GetKeyDown(prevTargetKey))
            _selectedIndex = (_selectedIndex - 1 + _targets.Length) % _targets.Length;

        if (Input.GetKeyDown(toggleSelectedKey))
            SetTargetState(_selectedIndex, !_targets[_selectedIndex].isActive);

        if (Input.GetKey(scrollUpKey))
            _scrollPosition.y -= scrollSpeed * Time.deltaTime;

        if (Input.GetKey(scrollDownKey))
            _scrollPosition.y += scrollSpeed * Time.deltaTime;

        if (_scrollPosition.y < 0f) _scrollPosition.y = 0f;
    }

    // ───────────────────────── API pública (runtime) ─────────────────────────

    public void SetTargetState(int index, bool active)
    {
        if ((uint)index >= (uint)_targets.Length) return;
        ObjectTarget entry = _targets[index];
        entry.isActive = active;
        if (entry.target != null)
            entry.target.SetActive(active);
    }

    public void ShowAll() => SetAllInternal(true);
    public void HideAll() => SetAllInternal(false);

    public ObjectTarget GetTarget(int index)
    {
        return (uint)index < (uint)_targets.Length ? _targets[index] : null;
    }

    // ────────────────────── Editor: aplicar cambios al tocar Inspector ──────────────────────

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_targets == null) return;

        foreach (ObjectTarget entry in _targets)
        {
            if (entry.target == null) continue;
            entry.target.SetActive(entry.isActive);
        }
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
        _titleStyle.fontSize = 18;
        _titleStyle.fontStyle = FontStyle.Bold;
        _titleStyle.normal.textColor = Color.white;
        _titleStyle.alignment = TextAnchor.MiddleCenter;

        _labelStyle = new GUIStyle(GUI.skin.label);
        _labelStyle.fontSize = 14;
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
        float headerHeight = 190f;
        float boxWidth = 380f;
        float contentHeight = headerHeight + targetCount * lineHeight;
        float maxBoxHeight = Mathf.Min(contentHeight, Screen.height * 0.8f);
        float x = 10f;
        float y = Screen.height - maxBoxHeight - 10f;
        Rect boxRect = new Rect(x, y, boxWidth, maxBoxHeight);

        GUI.Box(boxRect, GUIContent.none, _boxStyle);

        Rect areaRect = new Rect(x + 16, y + 12, boxWidth - 32, maxBoxHeight - 24);
        GUILayout.BeginArea(areaRect);
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        GUILayout.Label("Objects Controls", _titleStyle);
        GUILayout.Space(10);

        GUILayout.Label($"<b>[{showAllKey}]</b>  Mostrar todos los objetos", _labelStyle);
        GUILayout.Label($"<b>[{hideAllKey}]</b>  Ocultar todos los objetos", _labelStyle);
        GUILayout.Label($"<b>[{toggleSelectedKey}]</b>  Alternar objeto seleccionado", _labelStyle);
        GUILayout.Label($"<b>[{nextTargetKey}]</b> / <b>[{prevTargetKey}]</b>  Navegar targets", _labelStyle);
        GUILayout.Label($"<b>[{scrollUpKey}]</b> / <b>[{scrollDownKey}]</b>  Scroll menú", _labelStyle);
        GUILayout.Space(8);

        for (int i = 0; i < targetCount; i++)
        {
            ObjectTarget entry = _targets[i];
            string targetName = entry.target != null ? entry.target.name : "(vacío)";
            string stateColor = entry.isActive ? "<color=#66FF66>ON</color>" : "<color=#FF6666>OFF</color>";
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

    private void SetAllInternal(bool active)
    {
        for (int i = 0, len = _targets.Length; i < len; i++)
        {
            ObjectTarget entry = _targets[i];
            entry.isActive = active;
            if (entry.target != null)
                entry.target.SetActive(active);
        }
    }

    private void ApplyAll()
    {
        for (int i = 0, len = _targets.Length; i < len; i++)
        {
            if (_targets[i].target != null)
                _targets[i].target.SetActive(_targets[i].isActive);
        }
    }
}
