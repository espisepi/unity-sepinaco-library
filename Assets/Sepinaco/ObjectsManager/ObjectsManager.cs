using System;
using UnityEngine;

namespace Sepinaco.SceneTools
{
    /// <summary>
    /// Manages the activation and deactivation of GameObjects in the scene.
    ///
    /// <para><b>Inspector workflow:</b> drag GameObjects into the
    /// <c>Targets</c> array and tick / untick <c>isActive</c> to set initial
    /// visibility. Changes apply immediately in the Editor via <c>OnValidate</c>.</para>
    ///
    /// <para><b>Runtime debug menu:</b> press the menu key (default <c>F1</c>)
    /// to open an on-screen panel with keyboard controls for
    /// showing / hiding individual objects or all at once.</para>
    ///
    /// <para><b>Scripting API:</b></para>
    /// <list type="bullet">
    ///   <item><see cref="SetTargetState"/>  — set one target's visibility.</item>
    ///   <item><see cref="ShowAll"/> / <see cref="HideAll"/> — bulk operations.</item>
    ///   <item><see cref="BaseTargetManager.OnTargetStateChanged"/> — subscribe to changes.</item>
    ///   <item><see cref="GetTarget"/> — retrieve the <see cref="ObjectTarget"/> at an index.</item>
    /// </list>
    /// </summary>
    [AddComponentMenu("Sepinaco/Scene Tools/Objects Manager")]
    [HelpURL("https://github.com/sepinaco/scene-tools")]
    public class ObjectsManager : BaseTargetManager
    {
        #region Serialized Fields

        [Header("Start Behaviour")]
        [Tooltip("How targets are initialised when the scene starts.")]
        [SerializeField] private StartObjectsMode _startMode = StartObjectsMode.UseIndividualSettings;

        [Header("Object Targets")]
        [Tooltip("GameObjects whose visibility this manager controls.")]
        [SerializeField] private ObjectTarget[] _targets = Array.Empty<ObjectTarget>();

        #endregion

        #region BaseSceneManager Overrides

        /// <inheritdoc/>
        protected override string MenuTitle => "Objects Controls";

        /// <inheritdoc/>
        protected override string ActivateAllLabel => "Show all objects";

        /// <inheritdoc/>
        protected override string DeactivateAllLabel => "Hide all objects";

        /// <inheritdoc/>
        protected override string ToggleSelectedLabel => "Toggle selected object";

        #endregion

        #region BaseTargetManager Overrides

        /// <inheritdoc/>
        public override int TargetCount => _targets != null ? _targets.Length : 0;

        /// <inheritdoc/>
        protected override StartBehavior ResolveStartBehavior()
        {
            switch (_startMode)
            {
                case StartObjectsMode.ShowAll:  return StartBehavior.ActivateAll;
                case StartObjectsMode.HideAll:  return StartBehavior.DeactivateAll;
                default:                        return StartBehavior.UseIndividualSettings;
            }
        }

        /// <inheritdoc/>
        protected override string GetTargetDisplayName(int index) =>
            _targets[index].DisplayName;

        /// <inheritdoc/>
        protected override bool GetTargetActiveState(int index) =>
            _targets[index].isActive;

        /// <inheritdoc/>
        public override void SetTargetState(int index, bool active)
        {
            if ((uint)index >= (uint)_targets.Length) return;

            ObjectTarget entry = _targets[index];
            entry.isActive = active;

            if (entry.target != null)
            {
                entry.target.SetActive(active);
                NotifyTargetStateChanged(entry.target, active);
            }
        }

        /// <inheritdoc/>
        protected override void ApplyAllTargetStates()
        {
            for (int i = 0, len = _targets.Length; i < len; i++)
            {
                ObjectTarget entry = _targets[i];
                if (entry.target != null)
                {
                    entry.target.SetActive(entry.isActive);
                    NotifyTargetStateChanged(entry.target, entry.isActive);
                }
            }
        }

        /// <inheritdoc/>
        protected override void SetAllTargetStates(bool active)
        {
            for (int i = 0, len = _targets.Length; i < len; i++)
            {
                ObjectTarget entry = _targets[i];
                entry.isActive = active;

                if (entry.target != null)
                {
                    entry.target.SetActive(active);
                    NotifyTargetStateChanged(entry.target, active);
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>The start mode configured in the Inspector.</summary>
        public StartObjectsMode StartMode => _startMode;

        /// <summary>
        /// Returns the <see cref="ObjectTarget"/> at <paramref name="index"/>,
        /// or <c>null</c> if the index is out of range.
        /// </summary>
        public ObjectTarget GetTarget(int index) =>
            (uint)index < (uint)_targets.Length ? _targets[index] : null;

        /// <summary>Convenience wrapper — activates (shows) every target.</summary>
        public void ShowAll() => ActivateAll();

        /// <summary>Convenience wrapper — deactivates (hides) every target.</summary>
        public void HideAll() => DeactivateAll();

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_targets == null) return;

            foreach (ObjectTarget entry in _targets)
            {
                if (entry.target != null)
                    entry.target.SetActive(entry.isActive);
            }
        }
#endif

        #endregion
    }
}
