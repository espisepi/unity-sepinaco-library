using System.Collections.Generic;
using Sepinaco.SceneTools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI runtime para controlar el ScriptPhysicsManager desde cualquier plataforma.
///
/// - keyToggleUI: siempre funciona, muestra/oculta el panel.
/// - El resto de teclas solo funcionan mientras el panel esté visible.
/// - Todas las teclas son configurables desde el Inspector de Unity.
/// - Compatible con ratón, táctil y teclado.
/// </summary>
public class PhysicsManagerUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PhysicsManager physicsManager;

    [Header("Tecla Show/Hide (siempre activa)")]
    [SerializeField] private KeyCode keyToggleUI = KeyCode.F1;

    [Header("Teclas de navegación (solo con UI visible)")]
    [SerializeField] private KeyCode keyUp = KeyCode.UpArrow;
    [SerializeField] private KeyCode keyDown = KeyCode.DownArrow;
    [SerializeField] private KeyCode keyToggleItem = KeyCode.Return;
    [SerializeField] private KeyCode keyEnableAll = KeyCode.E;
    [SerializeField] private KeyCode keyDisableAll = KeyCode.D;
    [SerializeField] private KeyCode keyClose = KeyCode.Escape;

    [Header("Configuración visual")]
    [SerializeField] private int referenceWidth = 1920;
    [SerializeField] private int referenceHeight = 1080;
    [SerializeField] [Range(320, 600)] private float panelWidth = 440;

    [Header("Colores")]
    [SerializeField] private Color colorOn = new Color(0.18f, 0.75f, 0.32f);
    [SerializeField] private Color colorOff = new Color(0.85f, 0.2f, 0.2f);
    [SerializeField] private Color panelBgColor = new Color(0.1f, 0.1f, 0.12f, 0.96f);
    [SerializeField] private Color headerBgColor = new Color(0.06f, 0.06f, 0.08f, 1f);
    [SerializeField] private Color rowColorA = new Color(0.16f, 0.16f, 0.2f, 0.9f);
    [SerializeField] private Color rowColorB = new Color(0.13f, 0.13f, 0.17f, 0.9f);
    [SerializeField] private Color selectionColor = new Color(0.3f, 0.55f, 1f, 0.3f);

    private GameObject _panelRoot;
    private readonly List<RowData> _rows = new List<RowData>();
    private bool _visible;
    private int _selected = -1;
    private ScrollRect _scroll;
    private RectTransform _contentRt;
    private static Font _cachedFont;

    private struct RowData
    {
        public int targetIdx;
        public GameObject go;
        public Image bg;
        public Image dot;
        public Image btnBg;
        public Text btnText;
        public Image selOverlay;
        public Color normalBg;
    }

    // ───────────────────────── Lifecycle ─────────────────────────

    private void Start()
    {
        if (physicsManager == null)
        {
            Debug.LogError("[PhysicsManagerUI] PhysicsManager no asignado.", this);
            enabled = false;
            return;
        }

        EnsureEventSystem();
        _cachedFont = ResolveFont();
        Build();
        ShowPanel(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(keyToggleUI))
        {
            ShowPanel(!_visible);
            return;
        }

        if (!_visible) return;

        if (Input.GetKeyDown(keyClose)) ShowPanel(false);
        else if (Input.GetKeyDown(keyUp)) Navigate(-1);
        else if (Input.GetKeyDown(keyDown)) Navigate(1);
        else if (Input.GetKeyDown(keyToggleItem)) ToggleCurrent();
        else if (Input.GetKeyDown(keyEnableAll)) { physicsManager.EnableAll(); Refresh(); }
        else if (Input.GetKeyDown(keyDisableAll)) { physicsManager.DisableAll(); Refresh(); }
    }

    // ───────────────────────── Visibility ─────────────────────────

    private void ShowPanel(bool show)
    {
        _visible = show;
        _panelRoot.SetActive(show);
        if (show)
        {
            Refresh();
            if (_rows.Count > 0 && _selected < 0) Select(0);
        }
    }

    // ───────────────────────── Navigation ─────────────────────────

    private void Navigate(int dir)
    {
        if (_rows.Count == 0) return;
        int next = _selected + dir;
        if (next < 0) next = _rows.Count - 1;
        else if (next >= _rows.Count) next = 0;
        Select(next);
    }

    private void Select(int idx)
    {
        if (_rows.Count == 0) return;
        idx = Mathf.Clamp(idx, 0, _rows.Count - 1);

        if (_selected >= 0 && _selected < _rows.Count)
            _rows[_selected].selOverlay.enabled = false;

        _selected = idx;
        _rows[_selected].selOverlay.enabled = true;
        EnsureVisible(_rows[_selected].go.GetComponent<RectTransform>());
    }

    private void EnsureVisible(RectTransform target)
    {
        if (_scroll == null || _contentRt == null) return;
        float contentH = _contentRt.rect.height;
        float viewH = _scroll.viewport.rect.height;
        if (contentH <= viewH) return;

        Canvas.ForceUpdateCanvases();
        float posY = Mathf.Abs(target.anchoredPosition.y);
        float rowH = target.rect.height;
        float scrollRange = contentH - viewH;
        float top = posY - rowH;
        float bottom = posY + rowH;
        float norm = Mathf.Clamp01(1f - (top + (bottom - top) * 0.5f - viewH * 0.5f) / scrollRange);
        _scroll.verticalNormalizedPosition = norm;
    }

    private void ToggleCurrent()
    {
        if (_selected < 0 || _selected >= _rows.Count) return;
        int ti = _rows[_selected].targetIdx;
        PhysicsTarget t = physicsManager.GetTarget(ti);
        if (t == null) return;
        physicsManager.SetTargetState(ti, !t.collidersEnabled);
        Refresh();
    }

    // ───────────────────────── Refresh rows ─────────────────────────

    private void Refresh()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            RowData r = _rows[i];
            PhysicsTarget t = physicsManager.GetTarget(r.targetIdx);
            if (t == null) continue;
            bool on = t.collidersEnabled;
            Color c = on ? colorOn : colorOff;
            r.dot.color = c;
            r.btnBg.color = c;
            r.btnText.text = on ? "ENABLED" : "DISABLED";
        }
    }

    // ───────────────────────── Build UI hierarchy ─────────────────────────

    private void Build()
    {
        GameObject canvasGo = new GameObject("PhysicsManagerUI_Canvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // Panel root
        _panelRoot = MakeRect("Panel", canvasGo.transform);
        RectTransform panelRt = Rt(_panelRoot);
        panelRt.anchorMin = new Vector2(1, 0);
        panelRt.anchorMax = new Vector2(1, 1);
        panelRt.pivot = new Vector2(1, 0.5f);
        panelRt.anchoredPosition = new Vector2(-16, 0);
        panelRt.sizeDelta = new Vector2(panelWidth, -32);

        _panelRoot.AddComponent<Image>().color = panelBgColor;

        VerticalLayoutGroup panelVlg = _panelRoot.AddComponent<VerticalLayoutGroup>();
        panelVlg.childControlWidth = true;
        panelVlg.childControlHeight = true;
        panelVlg.childForceExpandWidth = true;
        panelVlg.childForceExpandHeight = false;

        BuildHeader(_panelRoot.transform);
        BuildGlobalButtons(_panelRoot.transform);
        BuildSep(_panelRoot.transform);
        BuildScrollArea(_panelRoot.transform);
        BuildSep(_panelRoot.transform);
        BuildFooter(_panelRoot.transform);
    }

    private void BuildHeader(Transform parent)
    {
        GameObject header = MakeRect("Header", parent);
        header.AddComponent<Image>().color = headerBgColor;
        AddLE(header, -1, 50);

        HorizontalLayoutGroup hlg = header.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(14, 10, 0, 0);
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childControlWidth = true;
        hlg.childControlHeight = false;

        // Title
        GameObject titleGo = MakeRect("Title", header.transform);
        Text titleTxt = titleGo.AddComponent<Text>();
        titleTxt.font = _cachedFont;
        titleTxt.text = "Physics Manager";
        titleTxt.fontSize = 20;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.color = Color.white;
        titleTxt.alignment = TextAnchor.MiddleLeft;
        LayoutElement titleLe = titleGo.AddComponent<LayoutElement>();
        titleLe.flexibleWidth = 1;
        titleLe.preferredHeight = 50;

        // Close button
        MakeButton(header.transform, "\u2716", new Color(0.65f, 0.18f, 0.18f), 30, 30,
            () => ShowPanel(false));
    }

    private void BuildGlobalButtons(Transform parent)
    {
        GameObject row = MakeRect("Globals", parent);
        row.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.5f);
        AddLE(row, -1, 48);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 6, 6);
        hlg.spacing = 8;
        hlg.childForceExpandWidth = true;
        hlg.childControlHeight = true;

        MakeFlexButton(row.transform, $"Enable All [{keyEnableAll}]", colorOn,
            () => { physicsManager.EnableAll(); Refresh(); });
        MakeFlexButton(row.transform, $"Disable All [{keyDisableAll}]", colorOff,
            () => { physicsManager.DisableAll(); Refresh(); });
    }

    private void BuildScrollArea(Transform parent)
    {
        // Viewport / ScrollRect container
        GameObject viewport = MakeRect("Viewport", parent);
        LayoutElement vpLe = viewport.AddComponent<LayoutElement>();
        vpLe.flexibleHeight = 1;
        vpLe.flexibleWidth = 1;

        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        _scroll = viewport.AddComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 40;

        // Content
        GameObject content = MakeRect("Content", viewport.transform);
        _contentRt = Rt(content);
        _contentRt.anchorMin = new Vector2(0, 1);
        _contentRt.anchorMax = new Vector2(1, 1);
        _contentRt.pivot = new Vector2(0.5f, 1);
        _contentRt.anchoredPosition = Vector2.zero;
        _contentRt.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup cvlg = content.AddComponent<VerticalLayoutGroup>();
        cvlg.padding = new RectOffset(0, 0, 2, 2);
        cvlg.spacing = 1;
        cvlg.childForceExpandWidth = true;
        cvlg.childForceExpandHeight = false;
        cvlg.childControlWidth = true;
        cvlg.childControlHeight = true;

        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scroll.content = _contentRt;
        _scroll.viewport = Rt(viewport);

        // Build a row per target
        BuildRows(content.transform);
    }

    private void BuildRows(Transform parent)
    {
        _rows.Clear();
        int count = physicsManager.TargetCount;

        for (int i = 0; i < count; i++)
        {
            PhysicsTarget pt = physicsManager.GetTarget(i);
            if (pt == null) continue;

            bool isOn = pt.collidersEnabled;
            Color bgColor = (i % 2 == 0) ? rowColorA : rowColorB;

            // ── Row ──
            GameObject row = MakeRect($"Row{i}", parent);
            Image rowBg = row.AddComponent<Image>();
            rowBg.color = bgColor;
            AddLE(row, -1, 48);

            // We use a manual layout inside the row so we have full control
            // No LayoutGroup here -- position children manually via anchors

            // Selection overlay (fills entire row)
            GameObject selGo = MakeRect("Sel", row.transform);
            Image selImg = selGo.AddComponent<Image>();
            selImg.color = selectionColor;
            selImg.raycastTarget = false;
            selImg.enabled = false;
            FillParent(Rt(selGo));

            // Index label (left side)
            GameObject idxGo = MakeRect("Idx", row.transform);
            RectTransform idxRt = Rt(idxGo);
            idxRt.anchorMin = new Vector2(0, 0);
            idxRt.anchorMax = new Vector2(0, 1);
            idxRt.pivot = new Vector2(0, 0.5f);
            idxRt.anchoredPosition = new Vector2(10, 0);
            idxRt.sizeDelta = new Vector2(28, 0);

            Image idxBg = idxGo.AddComponent<Image>();
            idxBg.color = new Color(0.22f, 0.22f, 0.3f, 0.8f);

            GameObject idxTxtGo = MakeRect("T", idxGo.transform);
            FillParent(Rt(idxTxtGo));
            Text idxTxt = idxTxtGo.AddComponent<Text>();
            idxTxt.font = _cachedFont;
            idxTxt.text = i.ToString();
            idxTxt.fontSize = 13;
            idxTxt.fontStyle = FontStyle.Bold;
            idxTxt.color = new Color(0.7f, 0.75f, 0.85f);
            idxTxt.alignment = TextAnchor.MiddleCenter;

            // Name label (middle)
            GameObject nameGo = MakeRect("Name", row.transform);
            RectTransform nameRt = Rt(nameGo);
            nameRt.anchorMin = new Vector2(0, 0);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.offsetMin = new Vector2(46, 4);
            nameRt.offsetMax = new Vector2(-130, -4);

            Text nameTxt = nameGo.AddComponent<Text>();
            nameTxt.font = _cachedFont;
            nameTxt.text = pt.target != null ? pt.target.name : "(sin asignar)";
            nameTxt.fontSize = 15;
            nameTxt.color = Color.white;
            nameTxt.alignment = TextAnchor.MiddleLeft;

            // Status dot
            GameObject dotGo = MakeRect("Dot", row.transform);
            RectTransform dotRt = Rt(dotGo);
            dotRt.anchorMin = new Vector2(1, 0.5f);
            dotRt.anchorMax = new Vector2(1, 0.5f);
            dotRt.pivot = new Vector2(1, 0.5f);
            dotRt.anchoredPosition = new Vector2(-112, 0);
            dotRt.sizeDelta = new Vector2(10, 10);

            Image dotImg = dotGo.AddComponent<Image>();
            dotImg.color = isOn ? colorOn : colorOff;

            // Toggle button with ENABLED/DISABLED label
            GameObject btnGo = MakeRect("Btn", row.transform);
            RectTransform btnRt = Rt(btnGo);
            btnRt.anchorMin = new Vector2(1, 0.5f);
            btnRt.anchorMax = new Vector2(1, 0.5f);
            btnRt.pivot = new Vector2(1, 0.5f);
            btnRt.anchoredPosition = new Vector2(-8, 0);
            btnRt.sizeDelta = new Vector2(96, 30);

            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.color = isOn ? colorOn : colorOff;

            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            GameObject btnTxtGo = MakeRect("T", btnGo.transform);
            FillParent(Rt(btnTxtGo));
            Text btnTxt = btnTxtGo.AddComponent<Text>();
            btnTxt.font = _cachedFont;
            btnTxt.text = isOn ? "ENABLED" : "DISABLED";
            btnTxt.fontSize = 12;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.color = Color.white;
            btnTxt.alignment = TextAnchor.MiddleCenter;

            int ci = i;
            int ri = _rows.Count;
            btn.onClick.AddListener(() =>
            {
                PhysicsTarget t = physicsManager.GetTarget(ci);
                if (t == null) return;
                physicsManager.SetTargetState(ci, !t.collidersEnabled);
                Select(ri);
                Refresh();
            });

            _rows.Add(new RowData
            {
                targetIdx = ci,
                go = row,
                bg = rowBg,
                dot = dotImg,
                btnBg = btnImg,
                btnText = btnTxt,
                selOverlay = selImg,
                normalBg = bgColor
            });
        }

        if (_rows.Count > 0)
            Select(0);
    }

    private void BuildFooter(Transform parent)
    {
        GameObject footer = MakeRect("Footer", parent);
        footer.AddComponent<Image>().color = headerBgColor;
        AddLE(footer, -1, 48);

        GameObject txtGo = MakeRect("Hint", footer.transform);
        FillParent(Rt(txtGo));
        RectTransform hRt = Rt(txtGo);
        hRt.offsetMin = new Vector2(8, 0);
        hRt.offsetMax = new Vector2(-8, 0);

        Text hint = txtGo.AddComponent<Text>();
        hint.font = _cachedFont;
        hint.fontSize = 11;
        hint.color = new Color(1, 1, 1, 0.45f);
        hint.alignment = TextAnchor.MiddleCenter;
        hint.text = $"[{keyToggleUI}] Show/Hide    [{keyUp}/{keyDown}] Nav    " +
                    $"[{keyToggleItem}] Toggle    [{keyClose}] Close";
    }

    private void BuildSep(Transform parent)
    {
        GameObject sep = MakeRect("Sep", parent);
        sep.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 0.5f);
        AddLE(sep, -1, 1);
    }

    // ───────────────────────── UI factory helpers ─────────────────────────

    private static GameObject MakeRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform Rt(GameObject go) => go.GetComponent<RectTransform>();

    private static void FillParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void AddLE(GameObject go, float width, float height)
    {
        LayoutElement le = go.AddComponent<LayoutElement>();
        if (width > 0) le.preferredWidth = width;
        else le.flexibleWidth = 1;
        le.preferredHeight = height;
    }

    private void MakeButton(Transform parent, string label, Color color, float w, float h,
        UnityEngine.Events.UnityAction action)
    {
        GameObject go = MakeRect("Btn", parent);
        go.AddComponent<Image>().color = color;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        btn.onClick.AddListener(action);

        GameObject txtGo = MakeRect("T", go.transform);
        FillParent(Rt(txtGo));
        Text txt = txtGo.AddComponent<Text>();
        txt.font = _cachedFont;
        txt.text = label;
        txt.fontSize = 14;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w;
        le.preferredHeight = h;
    }

    private void MakeFlexButton(Transform parent, string label, Color color,
        UnityEngine.Events.UnityAction action)
    {
        GameObject go = MakeRect("Btn", parent);
        go.AddComponent<Image>().color = color;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        btn.onClick.AddListener(action);

        GameObject txtGo = MakeRect("T", go.transform);
        FillParent(Rt(txtGo));
        Text txt = txtGo.AddComponent<Text>();
        txt.font = _cachedFont;
        txt.text = label;
        txt.fontSize = 14;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.preferredHeight = 34;
    }

    private static Font ResolveFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null) return f;
        f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (f != null) return f;
        return Font.CreateDynamicFontFromOSFont("Arial", 14);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }
}
