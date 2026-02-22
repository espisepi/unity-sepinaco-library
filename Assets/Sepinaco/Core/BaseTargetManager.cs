using System;
using UnityEngine;

namespace Sepinaco.SceneTools
{
    /// <summary>
    /// Abstract base for managers that control an array of targets
    /// (GameObjects, colliders, lights, etc.) with activate / deactivate semantics.
    ///
    /// <para>Extends <see cref="BaseSceneManager"/> with:</para>
    /// <list type="bullet">
    ///   <item>A configurable start behaviour via <see cref="ResolveStartBehavior"/>.</item>
    ///   <item>Keyboard navigation and toggling of individual targets.</item>
    ///   <item>Automatic rendering of the target list in the debug menu.</item>
    ///   <item>A public <see cref="OnTargetStateChanged"/> event for inter-manager communication.</item>
    /// </list>
    ///
    /// <para><b>To create a new target manager:</b></para>
    /// <list type="number">
    ///   <item>Declare a <c>[SerializeField]</c> array of your <see cref="ITargetEntry"/> type.</item>
    ///   <item>Override every <c>abstract</c> member to bridge the framework to your array.</item>
    ///   <item>Optionally override the label properties
    ///         (<see cref="ActivateAllLabel"/>, <see cref="DeactivateAllLabel"/>,
    ///          <see cref="ToggleSelectedLabel"/>) for domain-specific wording.</item>
    /// </list>
    /// </summary>
    public abstract class BaseTargetManager : BaseSceneManager
    {
        #region Serialized Fields — Target Controls

        [Header("Target Controls (active while menu is open)")]
        [Tooltip("Key to activate all targets at once.")]
        [SerializeField] protected KeyCode _activateAllKey = KeyCode.V;

        [Tooltip("Key to deactivate all targets at once.")]
        [SerializeField] protected KeyCode _deactivateAllKey = KeyCode.L;

        [Tooltip("Key to toggle the currently selected target.")]
        [SerializeField] protected KeyCode _toggleSelectedKey = KeyCode.H;

        [Tooltip("Key to move selection to the next target.")]
        [SerializeField] protected KeyCode _nextTargetKey = KeyCode.N;

        [Tooltip("Key to move selection to the previous target.")]
        [SerializeField] protected KeyCode _prevTargetKey = KeyCode.J;

        #endregion

        #region State

        private int _selectedIndex;

        #endregion

        #region Events

        /// <summary>
        /// Raised whenever a target's active state changes.
        /// Parameters: the affected <see cref="GameObject"/> and its new state.
        /// Other components (e.g. <see cref="VideoclipsManager"/>) can subscribe
        /// to react to visibility or physics changes in real time.
        /// </summary>
        public event Action<GameObject, bool> OnTargetStateChanged;

        #endregion

        #region Protected Accessors

        /// <summary>Index of the target currently highlighted in the debug menu.</summary>
        protected int SelectedIndex => _selectedIndex;

        #endregion

        #region Abstract Members — Target Access

        /// <summary>Total number of targets in the serialized array.</summary>
        public abstract int TargetCount { get; }

        /// <summary>
        /// Maps the domain-specific start-mode enum to a canonical
        /// <see cref="StartBehavior"/> value.
        /// </summary>
        protected abstract StartBehavior ResolveStartBehavior();

        /// <summary>Returns the display name for the target at <paramref name="index"/>.</summary>
        protected abstract string GetTargetDisplayName(int index);

        /// <summary>Returns whether the target at <paramref name="index"/> is currently active.</summary>
        protected abstract bool GetTargetActiveState(int index);

        /// <summary>
        /// Sets the active state of the target at <paramref name="index"/>
        /// and applies the change to the scene. Implementations should call
        /// <see cref="NotifyTargetStateChanged"/> after applying.
        /// </summary>
        public abstract void SetTargetState(int index, bool active);

        /// <summary>
        /// Applies every target's serialized state to the scene without modifying
        /// the data. Called when start behaviour is
        /// <see cref="StartBehavior.UseIndividualSettings"/>.
        /// </summary>
        protected abstract void ApplyAllTargetStates();

        /// <summary>
        /// Forces every target to <paramref name="active"/> and applies to the scene.
        /// </summary>
        protected abstract void SetAllTargetStates(bool active);

        #endregion

        #region Virtual Labels

        /// <summary>Label for the "activate all" action in the debug menu.</summary>
        protected virtual string ActivateAllLabel => "Activate all";

        /// <summary>Label for the "deactivate all" action in the debug menu.</summary>
        protected virtual string DeactivateAllLabel => "Deactivate all";

        /// <summary>Label for the "toggle selected" action in the debug menu.</summary>
        protected virtual string ToggleSelectedLabel => "Toggle selected";

        #endregion

        #region Public API

        /// <summary>Activates every managed target.</summary>
        public void ActivateAll() => SetAllTargetStates(true);

        /// <summary>Deactivates every managed target.</summary>
        public void DeactivateAll() => SetAllTargetStates(false);

        /// <summary>
        /// Fires <see cref="OnTargetStateChanged"/>. Call from concrete
        /// implementations of <see cref="SetTargetState"/> and
        /// <see cref="SetAllTargetStates"/> after applying the new state.
        /// </summary>
        protected void NotifyTargetStateChanged(GameObject target, bool active)
        {
            OnTargetStateChanged?.Invoke(target, active);
        }

        #endregion

        #region Lifecycle Overrides

        /// <inheritdoc/>
        protected override bool CanOperate() => TargetCount > 0;

        /// <inheritdoc/>
        protected override void OnManagerInitialize()
        {
            switch (ResolveStartBehavior())
            {
                case StartBehavior.ActivateAll:
                    SetAllTargetStates(true);
                    break;
                case StartBehavior.DeactivateAll:
                    SetAllTargetStates(false);
                    break;
                default:
                    ApplyAllTargetStates();
                    break;
            }
        }

        /// <inheritdoc/>
        protected override void OnMenuInput()
        {
            if (Input.GetKeyDown(_activateAllKey))
                ActivateAll();

            if (Input.GetKeyDown(_deactivateAllKey))
                DeactivateAll();

            if (Input.GetKeyDown(_nextTargetKey))
                _selectedIndex = (_selectedIndex + 1) % TargetCount;

            if (Input.GetKeyDown(_prevTargetKey))
                _selectedIndex = (_selectedIndex - 1 + TargetCount) % TargetCount;

            if (Input.GetKeyDown(_toggleSelectedKey))
                SetTargetState(_selectedIndex, !GetTargetActiveState(_selectedIndex));
        }

        /// <inheritdoc/>
        protected override void DrawMenuHelpLines()
        {
            GUILayout.Label(
                $"<b>[{_activateAllKey}]</b>  {ActivateAllLabel}", LabelStyle);
            GUILayout.Label(
                $"<b>[{_deactivateAllKey}]</b>  {DeactivateAllLabel}", LabelStyle);
            GUILayout.Label(
                $"<b>[{_toggleSelectedKey}]</b>  {ToggleSelectedLabel}", LabelStyle);
            GUILayout.Label(
                $"<b>[{_nextTargetKey}]</b> / <b>[{_prevTargetKey}]</b>  Navigate targets",
                LabelStyle);
        }

        /// <inheritdoc/>
        protected override void DrawMenuContent()
        {
            int count = TargetCount;
            for (int i = 0; i < count; i++)
            {
                string name   = GetTargetDisplayName(i);
                bool   active = GetTargetActiveState(i);

                string stateTag = active
                    ? "<color=#66FF66>ON</color>"
                    : "<color=#FF6666>OFF</color>";

                string   prefix = i == _selectedIndex ? "►  " : "    ";
                GUIStyle style  = i == _selectedIndex ? SelectedLabelStyle : LabelStyle;

                GUILayout.Label($"{prefix}<b>{name}</b>  {stateTag}", style);
            }
        }

        /// <inheritdoc/>
        protected override float EstimateContentHeight()
        {
            return 220f + TargetCount * 22f;
        }

        #endregion
    }
}
