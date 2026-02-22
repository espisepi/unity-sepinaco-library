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
/// Vector2/3/4, Color, Quaternion (euler), arrays, listas,
/// clases/structs [Serializable] (expandidos recursivamente)
/// y referencias a UnityEngine.Object (solo lectura).
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

    [Header("Callbacks")]
    [Tooltip("Llamar a OnValidate() del script objetivo tras modificar un campo.\n" +
             "Permite que scripts como ScriptPhysicsManager apliquen los cambios automáticamente.")]
    public bool callOnValidateAfterChange = true;

    // ───────────────────────── State ─────────────────────────

    private bool _menuOpen;

    private enum MenuScreen { Scripts, Fields, TextEdit }
    private MenuScreen _screen;
    private int _cursor;
    private Vector2 _scroll;

    private MonoBehaviour _editTarget;
    private readonly List<FieldEntry> _entries = new List<FieldEntry>();
    private int _prevCursor = -1;
    private float _lastItemContentBottom;
    private MethodInfo _cachedOnValidate;

    private string _textBuf = "";
    private int _textIdx;

    // ───────────────────────── GUI ─────────────────────────

    private GUIStyle _sBox;
    private GUIStyle _sTitle;
    private GUIStyle _sLabel;
    private GUIStyle _sSel;
    private GUIStyle _sReadOnly;
    private GUIStyle _sHeader;
    private bool _stylesReady;
    private int _fontSize = 14;
    private const int FontMin = 8;
    private const int FontMax = 40;
    private const int FontStep = 2;
    private float _boxW = 540f;
    private float _boxH = 560f;
    private float _renderedH;

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

    private const int MaxNestDepth = 5;
    private const int MaxArrayDisplay = 50;

    // ═══════════════════════════════════════════════════════════
    //  FieldEntry
    // ═══════════════════════════════════════════════════════════

    private class FieldEntry
    {
        public string label;
        public int indent;
        public bool readOnly;
        public bool isHeader;
        public Type valueType;

        public Func<object> getter;
        public Action<object> setter;

        public object Get()
        {
            try { return getter != null ? getter() : null; }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DebugInspector] Get '{label}': {ex.Message}");
                return null;
            }
        }

        public void Set(object value)
        {
            if (readOnly || setter == null) return;
            try { setter(value); }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DebugInspector] Set '{label}': {ex.Message}");
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

        ApplyAutoScroll();
    }

    private void ApplyAutoScroll()
    {
        int headerLines, itemCount;
        switch (_screen)
        {
            case MenuScreen.Scripts:
                headerLines = HeaderLinesScripts;
                itemCount = _scripts != null ? _scripts.Length : 0;
                break;
            case MenuScreen.Fields:
                headerLines = HeaderLinesFields;
                itemCount = _entries.Count;
                break;
            default: return;
        }
        if (itemCount > 0)
            AutoScroll(_cursor, headerLines, itemCount);
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
                _cachedOnValidate = target.GetType().GetMethod("OnValidate",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                RebuildEntries(target);
                _screen = MenuScreen.Fields;
                _cursor = 0;
                _scroll = Vector2.zero;
                _prevCursor = -1;
            }
        }
    }

    private void ApplySet(FieldEntry fe, object value)
    {
        fe.Set(value);
        NotifyFieldChanged();
    }

    private void NotifyFieldChanged()
    {
        if (!callOnValidateAfterChange || _editTarget == null || _cachedOnValidate == null) return;
        try { _cachedOnValidate.Invoke(_editTarget, null); }
        catch (Exception ex) { Debug.LogWarning($"[DebugInspector] OnValidate: {ex.Message}"); }
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
        Type vt = fe.valueType;
        object cur = fe.Get();

        if (vt == typeof(bool))
        {
            if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(valueUpKey) || Input.GetKeyDown(valueDownKey))
            {
                if (cur is bool b) ApplySet(fe, !b);
            }
        }
        else if (vt == typeof(string))
        {
            if (Input.GetKeyDown(confirmKey))
            {
                _textBuf = cur as string ?? "";
                _textIdx = _cursor;
                _screen = MenuScreen.TextEdit;
            }
        }
        else if (vt == typeof(int))
        {
            if (cur is int iv)
            {
                int s = fast ? intFastStep : intStep;
                if (Input.GetKeyDown(valueUpKey))   ApplySet(fe, iv + s);
                if (Input.GetKeyDown(valueDownKey)) ApplySet(fe, iv - s);
            }
        }
        else if (vt == typeof(long))
        {
            if (cur is long lv)
            {
                long s = fast ? intFastStep : intStep;
                if (Input.GetKeyDown(valueUpKey))   ApplySet(fe, lv + s);
                if (Input.GetKeyDown(valueDownKey)) ApplySet(fe, lv - s);
            }
        }
        else if (vt == typeof(float))
        {
            if (cur is float fv)
            {
                float s = fast ? floatFastStep : floatStep;
                if (Input.GetKeyDown(valueUpKey))   ApplySet(fe, fv + s);
                if (Input.GetKeyDown(valueDownKey)) ApplySet(fe, fv - s);
            }
        }
        else if (vt == typeof(double))
        {
            if (cur is double dv)
            {
                double s = fast ? floatFastStep : floatStep;
                if (Input.GetKeyDown(valueUpKey))   ApplySet(fe, dv + s);
                if (Input.GetKeyDown(valueDownKey)) ApplySet(fe, dv - s);
            }
        }
        else if (vt.IsEnum)
        {
            Array vals = Enum.GetValues(vt);
            int idx = cur != null ? Array.IndexOf(vals, cur) : -1;
            if (idx < 0) idx = 0;
            if (Input.GetKeyDown(valueUpKey) || Input.GetKeyDown(confirmKey))
                ApplySet(fe, vals.GetValue((idx + 1) % vals.Length));
            if (Input.GetKeyDown(valueDownKey))
                ApplySet(fe, vals.GetValue((idx - 1 + vals.Length) % vals.Length));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Reflection — entry building
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

                FieldInfo captured = fi;
                Func<object> getter = () => captured.GetValue(target);
                Action<object> setter = v => captured.SetValue(target, v);

                AddFieldRecursive(getter, setter, captured.FieldType, Nicify(captured.Name), 0, 0);
            }
        }
    }

    // ── Recursive field expansion ───────────────────────────

    private void AddFieldRecursive(Func<object> getter, Action<object> setter,
                                    Type ft, string label, int indent, int depth)
    {
        if (depth > MaxNestDepth)
        {
            _entries.Add(new FieldEntry
            {
                label = label, indent = indent, readOnly = true,
                valueType = ft, getter = getter
            });
            return;
        }

        // 1. Compound vector/color types
        if (CompoundEditable.Contains(ft))
        {
            AddCompoundEntries(getter, setter, ft, label, indent);
            return;
        }

        // 2. Simple editable + enums
        if (SimpleEditable.Contains(ft) || ft.IsEnum)
        {
            _entries.Add(new FieldEntry
            {
                label = label, indent = indent, readOnly = false,
                valueType = ft, getter = getter, setter = setter
            });
            return;
        }

        // 3. Arrays
        if (ft.IsArray)
        {
            AddArrayEntries(getter, setter, ft, label, indent, depth);
            return;
        }

        // 4. Generic List<T>
        if (typeof(IList).IsAssignableFrom(ft) && ft.IsGenericType)
        {
            AddListEntries(getter, setter, ft, label, indent, depth);
            return;
        }

        // 5. UnityEngine.Object references (read-only)
        if (typeof(UnityEngine.Object).IsAssignableFrom(ft))
        {
            _entries.Add(new FieldEntry
            {
                label = label, indent = indent, readOnly = true,
                valueType = ft, getter = getter
            });
            return;
        }

        // 6. [Serializable] classes / structs — expand recursively
        if (IsSerializableExpandable(ft))
        {
            AddSerializableEntries(getter, setter, ft, label, indent, depth);
            return;
        }

        // 7. Fallback — read only
        _entries.Add(new FieldEntry
        {
            label = label, indent = indent, readOnly = true,
            valueType = ft, getter = getter
        });
    }

    // ── Compound types (Vector2/3/4, Color, Quaternion) ─────

    private void AddCompoundEntries(Func<object> parentGet, Action<object> parentSet,
                                     Type ft, string label, int indent)
    {
        string[] labels;
        bool isColor = ft == typeof(Color);
        bool isQuat  = ft == typeof(Quaternion);

        if      (ft == typeof(Vector2))    labels = new[] { "x", "y" };
        else if (ft == typeof(Vector3))    labels = new[] { "x", "y", "z" };
        else if (ft == typeof(Vector4))    labels = new[] { "x", "y", "z", "w" };
        else if (isColor)                  labels = new[] { "r", "g", "b", "a" };
        else                               labels = new[] { "x°", "y°", "z°" };

        for (int i = 0; i < labels.Length; i++)
        {
            int comp = i;

            Func<object> cGet = () =>
            {
                object v = parentGet();
                if (v is Vector2 v2) return v2[comp];
                if (v is Vector3 v3) return v3[comp];
                if (v is Vector4 v4) return v4[comp];
                if (v is Color c)    return c[comp];
                if (v is Quaternion q) return q.eulerAngles[comp];
                return 0f;
            };

            Action<object> cSet = val =>
            {
                float f = Convert.ToSingle(val);
                object parent = parentGet();

                if (parent is Vector2 a) { a[comp] = f; parentSet(a); }
                else if (parent is Vector3 b) { b[comp] = f; parentSet(b); }
                else if (parent is Vector4 c) { c[comp] = f; parentSet(c); }
                else if (parent is Color d) { d[comp] = Mathf.Clamp01(f); parentSet(d); }
                else if (parent is Quaternion e)
                {
                    Vector3 euler = e.eulerAngles;
                    euler[comp] = f;
                    parentSet(Quaternion.Euler(euler));
                }
            };

            _entries.Add(new FieldEntry
            {
                label = $"{label}.{labels[i]}",
                indent = indent,
                readOnly = false,
                valueType = typeof(float),
                getter = cGet,
                setter = cSet
            });
        }
    }

    // ── Arrays ──────────────────────────────────────────────

    private void AddArrayEntries(Func<object> arrGet, Action<object> arrSet,
                                  Type ft, string label, int indent, int depth)
    {
        Type elemType = ft.GetElementType();

        _entries.Add(new FieldEntry
        {
            label = label, indent = indent, readOnly = true,
            isHeader = true, valueType = ft, getter = arrGet
        });

        object arrObj;
        try { arrObj = arrGet(); } catch { return; }
        if (!(arrObj is Array arr)) return;

        int count = Mathf.Min(arr.Length, MaxArrayDisplay);
        for (int i = 0; i < count; i++)
        {
            int idx = i;

            Func<object> elemGet = () =>
            {
                object a = arrGet();
                if (a is Array ar && idx < ar.Length) return ar.GetValue(idx);
                return null;
            };

            Action<object> elemSet = v =>
            {
                object a = arrGet();
                if (a is Array ar && idx < ar.Length) ar.SetValue(v, idx);
            };

            AddElementEntry(elemGet, elemSet, elemType, $"{label}[{idx}]", indent + 1, depth + 1);
        }

        if (arr.Length > MaxArrayDisplay)
        {
            _entries.Add(new FieldEntry
            {
                label = $"… ({arr.Length - MaxArrayDisplay} más)",
                indent = indent + 1, readOnly = true, isHeader = true,
                valueType = typeof(void), getter = () => null
            });
        }
    }

    // ── Generic List<T> ─────────────────────────────────────

    private void AddListEntries(Func<object> listGet, Action<object> listSet,
                                 Type ft, string label, int indent, int depth)
    {
        Type elemType = ft.IsGenericType ? ft.GetGenericArguments()[0] : typeof(object);

        _entries.Add(new FieldEntry
        {
            label = label, indent = indent, readOnly = true,
            isHeader = true, valueType = ft, getter = listGet
        });

        object listObj;
        try { listObj = listGet(); } catch { return; }
        if (!(listObj is IList list)) return;

        int count = Mathf.Min(list.Count, MaxArrayDisplay);
        for (int i = 0; i < count; i++)
        {
            int idx = i;

            Func<object> elemGet = () =>
            {
                object l = listGet();
                if (l is IList ls && idx < ls.Count) return ls[idx];
                return null;
            };

            Action<object> elemSet = v =>
            {
                object l = listGet();
                if (l is IList ls && idx < ls.Count) ls[idx] = v;
            };

            AddElementEntry(elemGet, elemSet, elemType, $"{label}[{idx}]", indent + 1, depth + 1);
        }

        if (list.Count > MaxArrayDisplay)
        {
            _entries.Add(new FieldEntry
            {
                label = $"… ({list.Count - MaxArrayDisplay} más)",
                indent = indent + 1, readOnly = true, isHeader = true,
                valueType = typeof(void), getter = () => null
            });
        }
    }

    // ── Element dispatch (array / list element) ─────────────

    private void AddElementEntry(Func<object> elemGet, Action<object> elemSet,
                                  Type elemType, string label, int indent, int depth)
    {
        if (CompoundEditable.Contains(elemType) ||
            SimpleEditable.Contains(elemType) ||
            elemType.IsEnum)
        {
            AddFieldRecursive(elemGet, elemSet, elemType, label, indent, depth);
            return;
        }

        if (typeof(UnityEngine.Object).IsAssignableFrom(elemType))
        {
            _entries.Add(new FieldEntry
            {
                label = label, indent = indent, readOnly = true,
                valueType = elemType, getter = elemGet
            });
            return;
        }

        if (IsSerializableExpandable(elemType))
        {
            _entries.Add(new FieldEntry
            {
                label = label, indent = indent, readOnly = true,
                isHeader = true, valueType = elemType, getter = elemGet
            });
            ExpandSerializableFields(elemGet, elemSet, elemType, indent + 1, depth);
            return;
        }

        AddFieldRecursive(elemGet, elemSet, elemType, label, indent, depth);
    }

    // ── [Serializable] class / struct ───────────────────────

    private void AddSerializableEntries(Func<object> getter, Action<object> setter,
                                         Type ft, string label, int indent, int depth)
    {
        _entries.Add(new FieldEntry
        {
            label = label, indent = indent, readOnly = true,
            isHeader = true, valueType = ft, getter = getter
        });

        object current;
        try { current = getter(); } catch { return; }
        if (current == null && !ft.IsValueType) return;

        ExpandSerializableFields(getter, setter, ft, indent + 1, depth + 1);
    }

    private void ExpandSerializableFields(Func<object> parentGet, Action<object> parentSet,
                                           Type ft, int indent, int depth)
    {
        var hierarchy = new List<Type>();
        Type stopType = ft.IsValueType ? typeof(ValueType) : typeof(object);
        for (Type t = ft; t != null && t != stopType; t = t.BaseType)
            hierarchy.Add(t);
        hierarchy.Reverse();

        bool parentIsValueType = ft.IsValueType;

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

                FieldInfo captured = fi;

                Func<object> fieldGet = () =>
                {
                    object parent = parentGet();
                    return parent != null ? captured.GetValue(parent) : null;
                };

                Action<object> fieldSet = v =>
                {
                    object parent = parentGet();
                    if (parent == null) return;
                    captured.SetValue(parent, v);
                    if (parentIsValueType && parentSet != null)
                        parentSet(parent);
                };

                AddFieldRecursive(fieldGet, fieldSet, captured.FieldType,
                                  Nicify(captured.Name), indent, depth);
            }
        }
    }

    private static bool IsSerializableExpandable(Type t)
    {
        if (t.IsPrimitive || t == typeof(string) || t == typeof(decimal)) return false;
        if (t.IsEnum) return false;
        if (SimpleEditable.Contains(t) || CompoundEditable.Contains(t)) return false;
        if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return false;
        if (t.IsArray) return false;
        if (typeof(IList).IsAssignableFrom(t)) return false;

        return Attribute.IsDefined(t, typeof(SerializableAttribute));
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

        _sHeader = new GUIStyle(_sLabel);
        _sHeader.normal.textColor = new Color(0.6f, 0.85f, 1f);
        _sHeader.richText = true;

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
        _renderedH = h;
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
            for (int i = 0; i < count; i++)
            {
                MonoBehaviour s = _scripts[i];
                string name = s != null ? s.GetType().Name : "<color=#FF6666>(vacío)</color>";
                string prefix = i == _cursor ? "►  " : "    ";
                GUIStyle style = i == _cursor ? _sSel : _sLabel;
                GUILayout.Label($"{prefix}<b>{name}</b>", style);
                if (i == count - 1 && Event.current.type == EventType.Repaint)
                    _lastItemContentBottom = GUILayoutUtility.GetLastRect().yMax;
            }
        }

        GUILayout.Space(_renderedH > 0f ? _renderedH : _boxH);
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
            for (int i = 0; i < count; i++)
            {
                FieldEntry fe = _entries[i];
                string pad = fe.indent > 0 ? new string(' ', fe.indent * 3) : "";
                string prefix = i == _cursor ? "► " : "   ";
                string val = FormatValue(fe);

                GUIStyle style;
                if (fe.isHeader)
                    style = i == _cursor ? _sSel : _sHeader;
                else if (fe.readOnly)
                    style = i == _cursor ? _sSel : _sReadOnly;
                else
                    style = i == _cursor ? _sSel : _sLabel;

                string tag = (!fe.isHeader && fe.readOnly) ? " <color=#666666>[ro]</color>" : "";
                string headerMark = fe.isHeader ? "▸ " : "";
                GUILayout.Label($"{prefix}{pad}{headerMark}<b>{fe.label}</b>  {val}{tag}", style);
                if (i == count - 1 && Event.current.type == EventType.Repaint)
                    _lastItemContentBottom = GUILayoutUtility.GetLastRect().yMax;
            }
        }

        GUILayout.Space(_renderedH > 0f ? _renderedH : _boxH);
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
                ApplySet(_entries[_textIdx], _textBuf);
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
        int prev = _prevCursor;
        _prevCursor = cursor;
        if (cursor == prev || itemCount == 0) return;

        float viewH = _renderedH > 0f ? _renderedH - 24f : _boxH - 24f;
        float cursorY = (headerLines + cursor) * LineH;

        bool goingToLast = cursor == itemCount - 1;
        bool goingToFirst = cursor == 0;
        bool wrappedDown = prev >= 0 && prev == itemCount - 1 && goingToFirst;
        bool wrappedUp   = prev >= 0 && prev == 0 && goingToLast;
        bool isFirstMove = prev < 0;

        if (wrappedDown || (isFirstMove && goingToFirst))
        {
            _scroll.y = 0f;
        }
        else if (wrappedUp || (isFirstMove && goingToLast))
        {
            float targetY = _lastItemContentBottom > 0f
                ? _lastItemContentBottom
                : cursorY + LineH;
            _scroll.y = Mathf.Max(0f, targetY - viewH + LineH);
        }
        else if (isFirstMove || cursor > prev)
        {
            _scroll.y = Mathf.Max(0f, cursorY - viewH * 0.3f);
        }
        else
        {
            _scroll.y -= LineH;
            if (_scroll.y < 0f) _scroll.y = 0f;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Utility
    // ═══════════════════════════════════════════════════════════

    private static string FormatValue(FieldEntry fe)
    {
        if (fe.isHeader)
            return FormatHeaderValue(fe);

        object val;
        try { val = fe.Get(); }
        catch { return "<color=#FF6666><error></color>"; }

        if (val == null) return "<color=#888888><null></color>";

        Type t = fe.valueType;

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
                ? $"<color=#88CCFF>{uObj.name}</color>"
                : "<color=#888888><None></color>";

        return $"<color=#888888>{val}</color>";
    }

    private static string FormatHeaderValue(FieldEntry fe)
    {
        object val;
        try { val = fe.Get(); }
        catch { return "<color=#FF6666><error></color>"; }

        Type t = fe.valueType;

        if (val == null)
            return $"<color=#888888>({t?.Name ?? "?"}) <null></color>";

        if (t != null && t.IsArray)
        {
            Array arr = val as Array;
            string elemName = t.GetElementType()?.Name ?? "?";
            int len = arr?.Length ?? 0;
            return $"<color=#999999>({elemName}[{len}])</color>";
        }

        if (val is IList list)
        {
            Type elemType = t != null && t.IsGenericType ? t.GetGenericArguments()[0] : null;
            string elemName = elemType?.Name ?? "?";
            return $"<color=#999999>(List‹{elemName}› [{list.Count}])</color>";
        }

        if (t == typeof(void))
            return "";

        return $"<color=#999999>({t?.Name ?? "?"})</color>";
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
