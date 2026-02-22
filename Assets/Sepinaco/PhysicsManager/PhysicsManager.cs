using System;
using UnityEngine;

namespace Sepinaco.SceneTools
{
    /// <summary>
    /// Manages the activation and deactivation of colliders across multiple
    /// GameObjects, including their entire child hierarchies.
    ///
    /// <para>Each target's <see cref="Collider"/> components are cached on
    /// <c>Awake</c> for efficient toggling without per-frame allocations.
    /// Call <see cref="RefreshCache"/> if the hierarchy changes at runtime.</para>
    ///
    /// <para><b>Inspector workflow:</b> drag GameObjects into the
    /// <c>Targets</c> array and tick / untick <c>collidersEnabled</c>.
    /// Changes apply in the Editor via <c>OnValidate</c>.</para>
    ///
    /// <para><b>Scripting API:</b></para>
    /// <list type="bullet">
    ///   <item><see cref="SetTargetState"/> — enable / disable one target's colliders.</item>
    ///   <item><see cref="EnableAll"/> / <see cref="DisableAll"/> — bulk operations.</item>
    ///   <item><see cref="RefreshCache"/> — rebuild collider cache after hierarchy changes.</item>
    ///   <item><see cref="BaseTargetManager.OnTargetStateChanged"/> — subscribe to changes.</item>
    /// </list>
    /// </summary>
    [AddComponentMenu("Sepinaco/Scene Tools/Physics Manager")]
    [HelpURL("https://github.com/sepinaco/scene-tools")]
    public class PhysicsManager : BaseTargetManager
    {
        #region Serialized Fields

        [Header("Start Behaviour")]
        [Tooltip("How colliders are initialised when the scene starts.")]
        [SerializeField] private StartCollidersMode _startMode = StartCollidersMode.UseIndividualSettings;

        [Header("Physics Targets")]
        [Tooltip("GameObjects whose collider hierarchies this manager controls.")]
        [SerializeField] private PhysicsTarget[] _targets = Array.Empty<PhysicsTarget>();

        #endregion

        #region Internal State

        private static readonly Collider[] EmptyColliders = Array.Empty<Collider>();
        private bool _cacheReady;

        #endregion

        #region BaseSceneManager Overrides

        /// <inheritdoc/>
        protected override string MenuTitle => "Physics Controls";

        /// <inheritdoc/>
        protected override string ActivateAllLabel => "Enable all colliders";

        /// <inheritdoc/>
        protected override string DeactivateAllLabel => "Disable all colliders";

        /// <inheritdoc/>
        protected override string ToggleSelectedLabel => "Toggle selected collider";

        #endregion

        #region BaseTargetManager Overrides

        /// <inheritdoc/>
        public override int TargetCount => _targets != null ? _targets.Length : 0;

        /// <inheritdoc/>
        protected override void OnManagerInitialize()
        {
            BuildCache();
            base.OnManagerInitialize();
        }

        /// <inheritdoc/>
        protected override StartBehavior ResolveStartBehavior()
        {
            switch (_startMode)
            {
                case StartCollidersMode.EnableAll:  return StartBehavior.ActivateAll;
                case StartCollidersMode.DisableAll: return StartBehavior.DeactivateAll;
                default:                            return StartBehavior.UseIndividualSettings;
            }
        }

        /// <inheritdoc/>
        protected override string GetTargetDisplayName(int index) =>
            _targets[index].DisplayName;

        /// <inheritdoc/>
        protected override bool GetTargetActiveState(int index) =>
            _targets[index].collidersEnabled;

        /// <inheritdoc/>
        public override void SetTargetState(int index, bool enabled)
        {
            if ((uint)index >= (uint)_targets.Length) return;

            PhysicsTarget entry = _targets[index];
            entry.collidersEnabled = enabled;

            if (_cacheReady)
                SetColliders(entry.cachedColliders, enabled);
        }

        /// <inheritdoc/>
        protected override void ApplyAllTargetStates()
        {
            for (int i = 0, len = _targets.Length; i < len; i++)
                SetColliders(_targets[i].cachedColliders, _targets[i].collidersEnabled);
        }

        /// <inheritdoc/>
        protected override void SetAllTargetStates(bool enabled)
        {
            for (int i = 0, len = _targets.Length; i < len; i++)
            {
                PhysicsTarget entry = _targets[i];
                entry.collidersEnabled = enabled;

                if (_cacheReady)
                    SetColliders(entry.cachedColliders, enabled);
            }
        }

        #endregion

        #region Public API

        /// <summary>The start mode configured in the Inspector.</summary>
        public StartCollidersMode StartMode => _startMode;

        /// <summary>
        /// Returns the <see cref="PhysicsTarget"/> at <paramref name="index"/>,
        /// or <c>null</c> if the index is out of range.
        /// </summary>
        public PhysicsTarget GetTarget(int index) =>
            (uint)index < (uint)_targets.Length ? _targets[index] : null;

        /// <summary>Convenience wrapper — enables every target's colliders.</summary>
        public void EnableAll() => ActivateAll();

        /// <summary>Convenience wrapper — disables every target's colliders.</summary>
        public void DisableAll() => DeactivateAll();

        /// <summary>
        /// Rebuilds the collider cache and reapplies all states.
        /// Call after adding or removing child GameObjects at runtime.
        /// </summary>
        public void RefreshCache()
        {
            BuildCache();
            ApplyAllTargetStates();
        }

        #endregion

        #region Editor Defaults

#if UNITY_EDITOR
        /// <summary>
        /// Called by Unity when the component is first added to a GameObject.
        /// Sets sensible default key bindings and menu anchor for this manager.
        /// </summary>
        private void Reset()
        {
            _menuAnchor       = MenuAnchor.TopRight;
            _activateAllKey   = KeyCode.E;
            _deactivateAllKey = KeyCode.Q;
            _toggleSelectedKey = KeyCode.G;
            _nextTargetKey    = KeyCode.X;
            _prevTargetKey    = KeyCode.B;
        }
#endif

        #endregion

        #region Editor Validation

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_targets == null) return;

            if (!Application.isPlaying)
            {
                foreach (PhysicsTarget entry in _targets)
                {
                    if (entry.target == null) continue;
                    Collider[] cols = entry.target.GetComponentsInChildren<Collider>(true);
                    SetColliders(cols, entry.collidersEnabled);
                }
            }
            else if (_cacheReady)
            {
                ApplyAllTargetStates();
            }
        }
#endif

        #endregion

        #region Internals

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

        private static void SetColliders(Collider[] colliders, bool enabled)
        {
            for (int i = 0, len = colliders.Length; i < len; i++)
                colliders[i].enabled = enabled;
        }

        #endregion
    }
}
