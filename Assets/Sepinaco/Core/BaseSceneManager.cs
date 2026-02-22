using UnityEngine;

namespace Sepinaco.SceneTools
{
    /// <summary>
    /// Abstract foundation for all Sepinaco scene-management components.
    /// Provides a fully configurable runtime debug menu rendered via <c>OnGUI</c>
    /// with scroll, zoom, and panel-resize controls — all driven by remappable keys.
    ///
    /// <para><b>Design pattern — Template Method:</b> the base class owns the
    /// lifecycle (<c>Awake</c>, <c>Update</c>, <c>OnGUI</c>) and delegates
    /// domain-specific behaviour to abstract / virtual hooks that subclasses override:
    /// <see cref="MenuTitle"/>, <see cref="OnMenuInput"/>,
    /// <see cref="DrawMenuHelpLines"/>, and <see cref="DrawMenuContent"/>.</para>
    ///
    /// <para><b>Extending:</b> inherit from this class (or from
    /// <see cref="BaseTargetManager"/> for target-array managers) and override
    /// the abstract members to get the debug-menu infrastructure for free.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class BaseSceneManager : MonoBehaviour
    {
        #region Serialized Fields — Debug Menu

        [Header("Debug Menu")]
        [Tooltip("Key to open / close the runtime debug menu.")]
        [SerializeField] private KeyCode _menuKey = KeyCode.F1;

        [Tooltip("Screen corner where the menu panel is anchored.")]
        [SerializeField] protected MenuAnchor _menuAnchor = MenuAnchor.BottomLeft;

        [Header("Menu Scroll (active while menu is open)")]
        [Tooltip("Key to scroll the menu upward.")]
        [SerializeField] private KeyCode _scrollUpKey = KeyCode.UpArrow;

        [Tooltip("Key to scroll the menu downward.")]
        [SerializeField] private KeyCode _scrollDownKey = KeyCode.DownArrow;

        [Tooltip("Scroll speed in pixels per second.")]
        [SerializeField] private float _scrollSpeed = 200f;

        [Header("Menu Zoom (active while menu is open)")]
        [Tooltip("Key to increase the menu font size.")]
        [SerializeField] private KeyCode _zoomInKey = KeyCode.I;

        [Tooltip("Key to decrease the menu font size.")]
        [SerializeField] private KeyCode _zoomOutKey = KeyCode.O;

        [Header("Menu Panel Size (active while menu is open)")]
        [Tooltip("Key to increase the panel width.")]
        [SerializeField] private KeyCode _panelWidthIncreaseKey = KeyCode.RightArrow;

        [Tooltip("Key to decrease the panel width.")]
        [SerializeField] private KeyCode _panelWidthDecreaseKey = KeyCode.LeftArrow;

        [Tooltip("Key to increase the panel height.")]
        [SerializeField] private KeyCode _panelHeightIncreaseKey = KeyCode.PageUp;

        [Tooltip("Key to decrease the panel height.")]
        [SerializeField] private KeyCode _panelHeightDecreaseKey = KeyCode.PageDown;

        #endregion

        #region GUI Constants

        private const int FontSizeMin  = 8;
        private const int FontSizeMax  = 40;
        private const int FontSizeStep = 2;

        private const float PanelSizeStep = 20f;
        private const float PanelWidthMin  = 200f;
        private const float PanelWidthMax  = 1200f;
        private const float PanelHeightMin = 100f;
        private const float PanelHeightMax = 1200f;
        private const float PanelMargin    = 10f;

        #endregion

        #region GUI Runtime State

        private bool    _menuActive;
        private Vector2 _scrollPosition;

        private GUIStyle _boxStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _selectedLabelStyle;
        private bool     _stylesInitialized;

        private int   _fontSize    = 14;
        private float _panelWidth  = 380f;
        private float _panelHeight = 400f;

        #endregion

        #region Protected Accessors (for subclasses)

        /// <summary>Whether the debug menu is currently visible.</summary>
        protected bool IsMenuActive => _menuActive;

        /// <summary>Reusable style for the semi-transparent background box.</summary>
        protected GUIStyle BoxStyle => _boxStyle;

        /// <summary>Reusable style for the menu title label.</summary>
        protected GUIStyle TitleStyle => _titleStyle;

        /// <summary>Reusable style for normal-text labels (supports rich text).</summary>
        protected GUIStyle LabelStyle => _labelStyle;

        /// <summary>Reusable style for the currently-selected item label.</summary>
        protected GUIStyle SelectedLabelStyle => _selectedLabelStyle;

        /// <summary>Current font size used by the menu labels.</summary>
        protected int FontSize => _fontSize;

        /// <summary>Current panel width in pixels.</summary>
        protected float CurrentPanelWidth => _panelWidth;

        /// <summary>Current panel height in pixels.</summary>
        protected float CurrentPanelHeight => _panelHeight;

        #endregion

        #region Abstract / Virtual Hooks

        /// <summary>Title displayed at the top of the debug menu panel.</summary>
        protected abstract string MenuTitle { get; }

        /// <summary>
        /// Called once during <c>Awake</c> after base initialisation.
        /// Use for domain-specific setup such as caching, applying initial state, etc.
        /// </summary>
        protected virtual void OnManagerInitialize() { }

        /// <summary>
        /// Called every frame when the debug menu is open.
        /// Handle manager-specific key bindings here.
        /// </summary>
        protected abstract void OnMenuInput();

        /// <summary>
        /// Draw the manager-specific key-binding legend inside the menu scroll view,
        /// above the main content area.
        /// </summary>
        protected abstract void DrawMenuHelpLines();

        /// <summary>
        /// Draw the manager-specific main content inside the menu scroll view,
        /// below the help lines.
        /// </summary>
        protected abstract void DrawMenuContent();

        /// <summary>
        /// Return <c>false</c> to disable the entire <c>Update</c> / <c>OnGUI</c>
        /// cycle (e.g. when the target list is empty). Defaults to <c>true</c>.
        /// </summary>
        protected virtual bool CanOperate() => true;

        /// <summary>
        /// Estimated total pixel height of the menu content.
        /// Override to provide a more accurate value based on actual content size.
        /// </summary>
        protected virtual float EstimateContentHeight() => 500f;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            OnManagerInitialize();
        }

        protected virtual void Update()
        {
            if (!CanOperate()) return;

            if (Input.GetKeyDown(_menuKey))
                _menuActive = !_menuActive;

            if (!_menuActive) return;

            OnMenuInput();
            ProcessScrollInput();
            ProcessZoomInput();
            ProcessPanelSizeInput();
        }

        private void OnGUI()
        {
            if (!_menuActive || !CanOperate()) return;

            EnsureStyles();
            RenderMenuPanel();
        }

        #endregion

        #region Input Processors

        private void ProcessScrollInput()
        {
            if (Input.GetKey(_scrollUpKey))
                _scrollPosition.y -= _scrollSpeed * Time.deltaTime;

            if (Input.GetKey(_scrollDownKey))
                _scrollPosition.y += _scrollSpeed * Time.deltaTime;

            if (_scrollPosition.y < 0f)
                _scrollPosition.y = 0f;
        }

        private void ProcessZoomInput()
        {
            if (Input.GetKeyDown(_zoomInKey))
            {
                _fontSize = Mathf.Min(_fontSize + FontSizeStep, FontSizeMax);
                _stylesInitialized = false;
            }

            if (Input.GetKeyDown(_zoomOutKey))
            {
                _fontSize = Mathf.Max(_fontSize - FontSizeStep, FontSizeMin);
                _stylesInitialized = false;
            }
        }

        private void ProcessPanelSizeInput()
        {
            if (Input.GetKeyDown(_panelWidthIncreaseKey))
                _panelWidth = Mathf.Min(_panelWidth + PanelSizeStep, PanelWidthMax);

            if (Input.GetKeyDown(_panelWidthDecreaseKey))
                _panelWidth = Mathf.Max(_panelWidth - PanelSizeStep, PanelWidthMin);

            if (Input.GetKeyDown(_panelHeightIncreaseKey))
                _panelHeight = Mathf.Min(_panelHeight + PanelSizeStep, PanelHeightMax);

            if (Input.GetKeyDown(_panelHeightDecreaseKey))
                _panelHeight = Mathf.Max(_panelHeight - PanelSizeStep, PanelHeightMin);
        }

        #endregion

        #region GUI Rendering

        private void EnsureStyles()
        {
            if (_stylesInitialized) return;

            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
            bgTex.Apply();

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = bgTex;
            _boxStyle.padding = new RectOffset(16, 16, 12, 12);

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontSize = _fontSize + 4;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = Color.white;
            _titleStyle.alignment = TextAnchor.MiddleCenter;

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = _fontSize;
            _labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            _labelStyle.richText = true;

            _selectedLabelStyle = new GUIStyle(_labelStyle);
            _selectedLabelStyle.normal.textColor = new Color(1f, 1f, 0.4f);

            _stylesInitialized = true;
        }

        private void RenderMenuPanel()
        {
            float contentHeight = EstimateContentHeight();
            float clampedHeight = Mathf.Min(contentHeight, _panelHeight, Screen.height * 0.95f);

            Vector2 origin = ComputeAnchorOrigin(_panelWidth, clampedHeight);
            Rect boxRect = new Rect(origin.x, origin.y, _panelWidth, clampedHeight);

            GUI.Box(boxRect, GUIContent.none, _boxStyle);

            Rect areaRect = new Rect(
                origin.x + 16f, origin.y + 12f,
                _panelWidth - 32f, clampedHeight - 24f);

            GUILayout.BeginArea(areaRect);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Label(MenuTitle, _titleStyle);
            GUILayout.Space(10);

            DrawMenuHelpLines();
            DrawCommonHelpLines();
            GUILayout.Space(8);

            DrawMenuContent();

            GUILayout.Space(4);
            GUILayout.Label($"<color=#888888>[{_menuKey}] to close</color>", _labelStyle);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>Renders the help lines for scroll, zoom, and panel resize shared by every manager.</summary>
        private void DrawCommonHelpLines()
        {
            GUILayout.Label(
                $"<b>[{_scrollUpKey}]</b> / <b>[{_scrollDownKey}]</b>  Scroll",
                _labelStyle);
            GUILayout.Label(
                $"<b>[{_zoomInKey}]</b> / <b>[{_zoomOutKey}]</b>  Zoom ({_fontSize}px)",
                _labelStyle);
            GUILayout.Label(
                $"<b>[{_panelWidthDecreaseKey}]</b> / <b>[{_panelWidthIncreaseKey}]</b>  Width ({_panelWidth}px)",
                _labelStyle);
            GUILayout.Label(
                $"<b>[{_panelHeightDecreaseKey}]</b> / <b>[{_panelHeightIncreaseKey}]</b>  Height ({_panelHeight}px)",
                _labelStyle);
        }

        private Vector2 ComputeAnchorOrigin(float width, float height)
        {
            switch (_menuAnchor)
            {
                case MenuAnchor.TopRight:
                    return new Vector2(Screen.width - width - PanelMargin, PanelMargin);
                case MenuAnchor.BottomLeft:
                    return new Vector2(PanelMargin, Screen.height - height - PanelMargin);
                case MenuAnchor.BottomRight:
                    return new Vector2(Screen.width - width - PanelMargin,
                                       Screen.height - height - PanelMargin);
                case MenuAnchor.TopLeft:
                default:
                    return new Vector2(PanelMargin, PanelMargin);
            }
        }

        #endregion
    }
}
