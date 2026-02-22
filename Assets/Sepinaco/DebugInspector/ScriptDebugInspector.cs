using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// Inspector debug visual para runtime.
///
/// Muestra un menú en pantalla (OnGUI) que permite navegar y modificar
/// los campos serializados de los MonoBehaviour asignados usando el teclado.
///
/// Pantalla 1 — lista de scripts: navega con ↑/↓, selecciona con Enter.
/// Pantalla 2 — campos editables: navega con ↑/↓, modifica con ←/→.
///
/// Tipos editables: int, long, float, double, bool, string, enums,
/// Vector2/3/4, Color y Quaternion (euler).
/// Los demás tipos se muestran como solo lectura.
/// </summary>
public class ScriptDebugInspector : MonoBehaviour
{
    [Header("Scripts a inspeccionar")]
    [Tooltip("Arrastra aquí los MonoBehaviour de la escena cuyos campos quieres inspeccionar en runtime.")]
    [SerializeField] private MonoBehaviour[] _scripts = Array.Empty<MonoBehaviour>();

    [Header("Menú")]
    [Tooltip("Tecla para abrir/cerrar el menú debug")]
    public KeyCode menuKey = KeyCode.F1;

    [Header("Navegación")]
    [Tooltip("Navegar hacia arriba en la lista")]
    public KeyCode navUpKey = KeyCode.UpArrow;

    [Tooltip("Navegar hacia abajo en la lista")]
    public KeyCode navDownKey = KeyCode.DownArrow;

    [Tooltip("Seleccionar / confirmar / alternar bool")]
    public KeyCode confirmKey = KeyCode.Return;

    [Tooltip("Volver a la pantalla anterior")]
    public KeyCode backKey = KeyCode.Backspace;

    [Header("Modificación de valores")]
    [Tooltip("Aumentar valor numérico / siguiente enum")]
    public KeyCode valueUpKey = KeyCode.RightArrow;

    [Tooltip("Disminuir valor numérico / anterior enum")]
    public KeyCode valueDownKey = KeyCode.LeftArrow;

    [Tooltip("Mantener para paso rápido")]
    public KeyCode fastKey = KeyCode.LeftShift;

    [Header("Pasos de incremento")]
    [Tooltip("Paso normal para float")]
    public float floatStep = 0.1f;

    [Tooltip("Paso rápido para float (con Shift)")]
    public float floatFastStep = 1f;

    [Tooltip("Paso normal para int")]
    public int intStep = 1;

    [Tooltip("Paso rápido para int (con Shift)")]
    public int intFastStep = 10;

    [Header("Scroll del menú")]
    [Tooltip("Tecla para hacer scroll hacia arriba en el menú")]
    public KeyCode scrollUpKey = KeyCode.PageUp;

    [Tooltip("Tecla para hacer scroll hacia abajo en el menú")]
    public KeyCode scrollDownKey = KeyCode.PageDown;

    [Tooltip("Velocidad de scroll en píxeles por segundo")]
    public float scrollSpeed = 300f;

    [Header("Zoom de la UI")]
    [Tooltip("Tecla para aumentar el tamaño de las letras")]
    public KeyCode zoomInKey = KeyCode.KeypadPlus;

    [Tooltip("Tecla para disminuir el tamaño de las letras")]
    public KeyCode zoomOutKey = KeyCode.KeypadMinus;

    // ───────────────────────── State ─────────────────────────

    private bool _menuOpen;

    private enum MenuScreen { Scripts, Fields, TextEdit }
    private MenuScreen _screen;
    private int _cursor;
    private Vector2 _scroll;

    private MonoBehaviour _editTarget;
    private readonly List<FieldEntry> _entries = new List<FieldEntry>();
    private int _prevCursor = -1;

    private string _textBuf = "";
    private int _textIdx;

    // ───────────────────────── GUI ─────────────────────────

    private GUIStyle _sBox;
    private GUIStyle _sTitle;
    private GUIStyle _sLabel;
    private GUIStyle _sSel;
    private GUIStyle _sReadOnly;
    private bool _stylesReady;
    private int _fontSize = 14;
    private const int FontMin = 8;
    private const int FontMax = 40;
    private const int FontStep = 2;
    private float _boxW = 540f;
    private float _boxH = 560f;

    private const float LineH = 22f;
    private const int HeaderLinesScripts = 7;
    private const int HeaderLinesFields = 9;

    // ───────────────────────── Supported types ─────────────────────────

    private static readonly HashSet<Type> SimpleEditable = new HashSet<Type>
    {
        typeof(int), typeof(float), typeof(double), typeof(long),
        typeof(bool), typeof(string)
    };

    private static readonly HashSet<Type> CompoundEditable = new HashSet<Type>
    {
        typeof(Vector2), typeof(Vector3), typeof(Vector4),
        typeof(Color), typeof(Quaternion)
    };

    // ═══════════════════════════════════════════════════════════
    //  FieldEntry
    // ═══════════════════════════════════════════════════════════

    private class FieldEntry
    {
        public string label;
        public FieldInfo field;
        public MonoBehaviour owner;
        public int sub; // -1 = direct, 0..3 = component index
        public bool readOnly;

        public Type ValueType => sub >= 0 ? typeof(float) : field.FieldType;

        public object Get()
        {
            object v = field.GetValue(owner);
            if (sub < 0) return v;
            if (v is Vector2 v2) return v2[sub];
            if (v is Vector3 v3) return v3[sub];
            if (v is Vector4 v4) return v4[sub];
            if (v is Color c) return c[sub];
            if (v is Quaternion q) return q.eulerAngles[sub];
            return v;
        }

        public void Set(object value)
        {
            if (readOnly) return;
            if (sub < 0) { field.SetValue(owner, value); return; }

            float f = Convert.ToSingle(value);
            object parent = field.GetValue(owner);

            if (parent is Vector2 a) { a[sub] = f; field.SetValue(owner, a); }
            else if (parent is Vector3 b) { b[sub] = f; field.SetValue(owner, b); }
            else if (parent is Vector4 c) { c[sub] = f; field.SetValue(owner, c); }
            else if (parent is Color d) { d[sub] = Mathf.Clamp01(f); field.SetValue(owner, d); }
            else if (parent is Quaternion e)
            {
                Vector3 euler = e.eulerAngles;
                euler[sub] = f;
                field.SetValue(owner, Quaternion.Euler(euler));
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Update — input
    // ═══════════════════════════════════════════════════════════

    private void Update()
    {
        if (Input.GetKeyDown(menuKey))
        {
            _menuOpen = !_menuOpen;
            if (_menuOpen)
            {
                _screen = MenuScreen.Scripts;
                _cursor = 0;
                _scroll = Vector2.zero;
                _prevCursor = -1;
            }
        }

        if (!_menuOpen) return;
        if (_screen == MenuScreen.TextEdit) return;

        if (Input.GetKeyDown(zoomInKey))
        {
            _fontSize = Mathf.Min(_fontSize + FontStep, FontMax);
            _stylesReady = false;
        }
        if (Input.GetKeyDown(zoomOutKey))
        {
            _fontSize = Mathf.Max(_fontSize - FontStep, FontMin);
            _stylesReady = false;
        }

        if (Input.GetKey(scrollUpKey))
            _scroll.y -= scrollSpeed * Time.deltaTime;
        if (Input.GetKey(scrollDownKey))
            _scroll.y += scrollSpeed * Time.deltaTime;
        if (_scroll.y < 0f) _scroll.y = 0f;

        switch (_screen)
        {
            case MenuScreen.Scripts: InputScripts(); break;
            case MenuScreen.Fields:  InputFields();  break;
        }
    }

    private void InputScripts()
    {
        int count = _scripts != null ? _scripts.Length : 0;
        if (count == 0) return;

        if (Input.GetKeyDown(navDownKey))
            _cursor = (_cursor + 1) % count;
        if (Input.GetKeyDown(navUpKey))
            _cursor = (_cursor - 1 + count) % count;

        if (Input.GetKeyDown(confirmKey))
        {
            MonoBehaviour target = _scripts[_cursor];
            if (target != null)
            {
                _editTarget = target;
                RebuildEntries(target);
                _screen = MenuScreen.Fields;
                _cursor = 0;
                _scroll = Vector2.zero;
                _prevCursor = -1;
            }
        }
    }

    private void InputFields()
    {
        if (Input.GetKeyDown(backKey))
        {
            _screen = MenuScreen.Scripts;
            _cursor = _scripts != null ? Mathf.Max(0, Array.IndexOf(_scripts, _editTarget)) : 0;
            _scroll = Vector2.zero;
            _prevCursor = -1;
            return;
        }

        int count = _entries.Count;
        if (count == 0) return;

        if (Input.GetKeyDown(navDownKey))
            _cursor = (_cursor + 1) % count;
        if (Input.GetKeyDown(navUpKey))
            _cursor = (_cursor - 1 + count) % count;

        FieldEntry fe = _entries[_cursor];
        if (fe.readOnly) return;

        bool fast = Input.GetKey(fastKey);
        Type vt = fe.ValueType;

        if (vt == typeof(bool))
        {
            if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(valueUpKey) || Input.GetKeyDown(valueDownKey))
                fe.Set(!(bool)fe.Get());
        }
        else if (vt == typeof(string))
        {
            if (Input.GetKeyDown(confirmKey))
            {
                _textBuf = (string)fe.Get() ?? "";
                _textIdx = _cursor;
                _screen = MenuScreen.TextEdit;
            }
        }
        else if (vt == typeof(int))
        {
            int s = fast ? intFastStep : intStep;
            if (Input.GetKeyDown(valueUpKey))   fe.Set((int)fe.Get() + s);
            if (Input.GetKeyDown(valueDownKey)) fe.Set((int)fe.Get() - s);
        }
        else if (vt == typeof(long))
        {
            long s = fast ? intFastStep : intStep;
            if (Input.GetKeyDown(valueUpKey))   fe.Set((long)fe.Get() + s);
            if (Input.GetKeyDown(valueDownKey)) fe.Set((long)fe.Get() - s);
        }
        else if (vt == typeof(float))
        {
            float s = fast ? floatFastStep : floatStep;
            if (Input.GetKeyDown(valueUpKey))   fe.Set((float)fe.Get() + s);
            if (Input.GetKeyDown(valueDownKey)) fe.Set((float)fe.Get() - s);
        }
        else if (vt == typeof(double))
        {
            double s = fast ? floatFastStep : floatStep;
            if (Input.GetKeyDown(valueUpKey))   fe.Set((double)fe.Get() + s);
            if (Input.GetKeyDown(valueDownKey)) fe.Set((double)fe.Get() - s);
        }
        else if (vt.IsEnum)
        {
            Array vals = Enum.GetValues(vt);
            int idx = Array.IndexOf(vals, fe.Get());
            if (Input.GetKeyDown(valueUpKey) || Input.GetKeyDown(confirmKey))
                fe.Set(vals.GetValue((idx + 1) % vals.Length));
            if (Input.GetKeyDown(valueDownKey))
                fe.Set(vals.GetValue((idx - 1 + vals.Length) % vals.Length));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Reflection
    // ═══════════════════════════════════════════════════════════

    private void RebuildEntries(MonoBehaviour target)
    {
        _entries.Clear();

        var hierarchy = new List<Type>();
        for (Type t = target.GetType();
             t != null && t != typeof(MonoBehaviour);
             t = t.BaseType)
            hierarchy.Add(t);
        hierarchy.Reverse();

        foreach (Type t in hierarchy)
        {
            FieldInfo[] fields = t.GetFields(
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            foreach (FieldInfo fi in fields)
            {
                if (fi.IsStatic) continue;
                if (Attribute.IsDefined(fi, typeof(HideInInspector))) continue;
                if (Attribute.IsDefined(fi, typeof(NonSerializedAttribute))) continue;

                bool isPublic = fi.IsPublic;
                bool hasSer = Attribute.IsDefined(fi, typeof(SerializeField));
                if (!isPublic && !hasSer) continue;

                Type ft = fi.FieldType;

                if (CompoundEditable.Contains(ft))
                {
                    AddCompound(fi, target, ft);
                    continue;
                }

                bool editable = SimpleEditable.Contains(ft) || ft.IsEnum;
                _entries.Add(new FieldEntry
                {
                    label = Nicify(fi.Name),
                    field = fi,
                    owner = target,
                    sub = -1,
                    readOnly = !editable
                });
            }
        }
    }

    private void AddCompound(FieldInfo fi, MonoBehaviour target, Type ft)
    {
        string[] labels;
        if      (ft == typeof(Vector2))    labels = new[] { "x", "y" };
        else if (ft == typeof(Vector3))    labels = new[] { "x", "y", "z" };
        else if (ft == typeof(Vector4))    labels = new[] { "x", "y", "z", "w" };
        else if (ft == typeof(Color))      labels = new[] { "r", "g", "b", "a" };
        else                               labels = new[] { "x°", "y°", "z°" };

        for (int i = 0; i < labels.Length; i++)
        {
            _entries.Add(new FieldEntry
            {
                label = $"{Nicify(fi.Name)}.{labels[i]}",
                field = fi,
                owner = target,
                sub = i,
                readOnly = false
            });
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  OnGUI — rendering
    // ═══════════════════════════════════════════════════════════

    private void InitStyles()
    {
        if (_stylesReady) return;

        Texture2D bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.80f));
        bgTex.Apply();

        _sBox = new GUIStyle(GUI.skin.box);
        _sBox.normal.background = bgTex;
        _sBox.padding = new RectOffset(16, 16, 12, 12);

        _sTitle = new GUIStyle(GUI.skin.label);
        _sTitle.fontSize = _fontSize + 4;
        _sTitle.fontStyle = FontStyle.Bold;
        _sTitle.normal.textColor = Color.white;
        _sTitle.alignment = TextAnchor.MiddleCenter;

        _sLabel = new GUIStyle(GUI.skin.label);
        _sLabel.fontSize = _fontSize;
        _sLabel.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
        _sLabel.richText = true;

        _sSel = new GUIStyle(_sLabel);
        _sSel.normal.textColor = new Color(1f, 1f, 0.4f);
        _sSel.richText = true;

        _sReadOnly = new GUIStyle(_sLabel);
        _sReadOnly.normal.textColor = new Color(0.55f, 0.55f, 0.55f);
        _sReadOnly.richText = true;

        _stylesReady = true;
    }

    private void OnGUI()
    {
        if (!_menuOpen) return;

        InitStyles();

        if ((_screen == MenuScreen.Fields || _screen == MenuScreen.TextEdit) && _editTarget == null)
        {
            _screen = MenuScreen.Scripts;
            _cursor = 0;
        }

        float w = _boxW;
        float h = Mathf.Min(_boxH, Screen.height * 0.92f);
        float x = 10f;
        float y = (Screen.height - h) * 0.5f;
        Rect box = new Rect(x, y, w, h);

        GUI.Box(box, GUIContent.none, _sBox);

        Rect area = new Rect(x + 16, y + 12, w - 32, h - 24);
        GUILayout.BeginArea(area);
        _scroll = GUILayout.BeginScrollView(_scroll);

        switch (_screen)
        {
            case MenuScreen.Scripts:  DrawScripts();  break;
            case MenuScreen.Fields:   DrawFields();   break;
            case MenuScreen.TextEdit: DrawTextEdit(); break;
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // ── Screen: Script list ────────────────────────────────

    private void DrawScripts()
    {
        GUILayout.Label("Debug Inspector", _sTitle);
        GUILayout.Space(8);

        GUILayout.Label($"<b>[{navUpKey}]</b> / <b>[{navDownKey}]</b>  Navegar", _sLabel);
        GUILayout.Label($"<b>[{confirmKey}]</b>  Seleccionar script", _sLabel);
        GUILayout.Label($"<b>[{scrollUpKey}]</b> / <b>[{scrollDownKey}]</b>  Scroll menú", _sLabel);
        GUILayout.Label($"<b>[{zoomInKey}]</b> / <b>[{zoomOutKey}]</b>  Zoom UI ({_fontSize}px)", _sLabel);
        GUILayout.Label($"<b>[{menuKey}]</b>  Cerrar menú", _sLabel);
        GUILayout.Space(10);

        int count = _scripts != null ? _scripts.Length : 0;

        if (count == 0)
        {
            GUILayout.Label("<color=#FF6666>No hay scripts asignados.</color>", _sLabel);
        }
        else
        {
            AutoScroll(_cursor, HeaderLinesScripts, count);

            for (int i = 0; i < count; i++)
            {
                MonoBehaviour s = _scripts[i];
                string name = s != null ? s.GetType().Name : "<color=#FF6666>(vacío)</color>";
                string prefix = i == _cursor ? "►  " : "    ";
                GUIStyle style = i == _cursor ? _sSel : _sLabel;
                GUILayout.Label($"{prefix}<b>{name}</b>", style);
            }
        }

        GUILayout.Space(6);
        DrawFooterHints();
    }

    // ── Screen: Field editor ───────────────────────────────

    private void DrawFields()
    {
        string typeName = _editTarget != null ? _editTarget.GetType().Name : "?";
        GUILayout.Label($"◄ {typeName}", _sTitle);
        GUILayout.Space(8);

        GUILayout.Label($"<b>[{navUpKey}]</b> / <b>[{navDownKey}]</b>  Navegar campos", _sLabel);
        GUILayout.Label($"<b>[{valueDownKey}]</b> / <b>[{valueUpKey}]</b>  Modificar valor", _sLabel);
        GUILayout.Label($"<b>[{confirmKey}]</b>  Toggle bool / Editar string / Ciclar enum", _sLabel);
        GUILayout.Label($"<b>[{fastKey}]</b>  Paso rápido    <b>[{backKey}]</b>  Volver", _sLabel);
        GUILayout.Label($"<b>[{scrollUpKey}]</b> / <b>[{scrollDownKey}]</b>  Scroll menú", _sLabel);
        GUILayout.Label($"<b>[{zoomInKey}]</b> / <b>[{zoomOutKey}]</b>  Zoom UI ({_fontSize}px)", _sLabel);
        GUILayout.Space(10);

        int count = _entries.Count;

        if (count == 0)
        {
            GUILayout.Label("<color=#FF6666>Sin campos editables.</color>", _sLabel);
        }
        else
        {
            AutoScroll(_cursor, HeaderLinesFields, count);

            for (int i = 0; i < count; i++)
            {
                FieldEntry fe = _entries[i];
                string prefix = i == _cursor ? "►  " : "    ";
                string val = FormatValue(fe);

                GUIStyle style;
                if (fe.readOnly) style = _sReadOnly;
                else if (i == _cursor) style = _sSel;
                else style = _sLabel;

                string readOnlyTag = fe.readOnly ? " <color=#666666>[ro]</color>" : "";
                GUILayout.Label($"{prefix}<b>{fe.label}</b>  {val}{readOnlyTag}", style);
            }
        }

        GUILayout.Space(6);
        DrawFooterHints();
    }

    // ── Screen: Text edit ──────────────────────────────────

    private void DrawTextEdit()
    {
        string fieldName = _textIdx >= 0 && _textIdx < _entries.Count
            ? _entries[_textIdx].label : "?";

        GUILayout.Label($"Editando: {fieldName}", _sTitle);
        GUILayout.Space(10);

        GUILayout.Label($"<b>[{KeyCode.Return}]</b>  Confirmar    <b>[{KeyCode.Escape}]</b>  Cancelar", _sLabel);
        GUILayout.Space(8);

        bool blink = (int)(Time.unscaledTime * 2f) % 2 == 0;
        string cursor = blink ? "<color=#FFCC00>|</color>" : " ";
        GUILayout.Label($"  > <color=#AADDFF>{EscapeRich(_textBuf)}</color>{cursor}", _sSel);

        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                _entries[_textIdx].Set(_textBuf);
                _screen = MenuScreen.Fields;
                _cursor = _textIdx;
                e.Use();
            }
            else if (e.keyCode == KeyCode.Escape)
            {
                _screen = MenuScreen.Fields;
                _cursor = _textIdx;
                e.Use();
            }
            else if (e.keyCode == KeyCode.Backspace)
            {
                if (_textBuf.Length > 0)
                    _textBuf = _textBuf.Substring(0, _textBuf.Length - 1);
                e.Use();
            }
            else if (e.character != '\0' && !char.IsControl(e.character))
            {
                _textBuf += e.character;
                e.Use();
            }
        }
    }

    // ── Shared UI helpers ──────────────────────────────────

    private void DrawFooterHints()
    {
        GUILayout.Label(
            $"<color=#666666>[{scrollUpKey}/{scrollDownKey}] Scroll" +
            $"   [{zoomInKey}/{zoomOutKey}] Zoom ({_fontSize}px)" +
            $"   [{menuKey}] Cerrar</color>",
            _sLabel);
    }

    private void AutoScroll(int cursor, int headerLines, int itemCount)
    {
        if (cursor == _prevCursor) return;
        _prevCursor = cursor;

        float viewH = _boxH - 48;
        float cursorY = (headerLines + cursor) * LineH;
        float headerH = headerLines * LineH;

        if (cursorY < _scroll.y + headerH)
            _scroll.y = Mathf.Max(0f, cursorY - headerH);
        else if (cursorY + LineH > _scroll.y + viewH)
            _scroll.y = cursorY + LineH - viewH;
    }

    // ═══════════════════════════════════════════════════════════
    //  Utility
    // ═══════════════════════════════════════════════════════════

    private static string FormatValue(FieldEntry fe)
    {
        object val;
        try { val = fe.Get(); }
        catch { return "<color=#FF6666><error></color>"; }

        if (val == null) return "<color=#888888><null></color>";

        Type t = fe.ValueType;

        if (t == typeof(bool))
        {
            bool b = (bool)val;
            return b
                ? "<color=#66FF66>True</color>"
                : "<color=#FF6666>False</color>";
        }
        if (t == typeof(int) || t == typeof(long))
            return $"<color=#FFCC00>{val}</color>";
        if (t == typeof(float))
            return $"<color=#FFCC00>{((float)val):F3}</color>";
        if (t == typeof(double))
            return $"<color=#FFCC00>{((double)val):F3}</color>";
        if (t == typeof(string))
            return $"<color=#AADDFF>\"{EscapeRich(val.ToString())}\"</color>";
        if (t.IsEnum)
            return $"<color=#CC99FF>{val}</color>";

        if (val is UnityEngine.Object uObj)
            return uObj != null
                ? $"<color=#888888>{uObj.name}</color>"
                : "<color=#888888><None></color>";
        if (t.IsArray)
        {
            Array arr = val as Array;
            return arr != null
                ? $"<color=#888888>({t.GetElementType()?.Name}[{arr.Length}])</color>"
                : "<color=#888888><null></color>";
        }
        if (val is IList list)
            return $"<color=#888888>(List [{list.Count}])</color>";

        return $"<color=#888888>{val}</color>";
    }

    private static string Nicify(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        int start = 0;
        while (start < name.Length && name[start] == '_') start++;
        if (start >= name.Length) return name;

        var sb = new StringBuilder(name.Length + 8);
        sb.Append(char.ToUpper(name[start]));

        for (int i = start + 1; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c))
            {
                bool prevLower = char.IsLower(name[i - 1]);
                bool nextLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                if (prevLower || nextLower)
                    sb.Append(' ');
            }
            sb.Append(c);
        }

        return sb.ToString();
    }

    private static string EscapeRich(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text
            .Replace("<", "‹")
            .Replace(">", "›");
    }
}
