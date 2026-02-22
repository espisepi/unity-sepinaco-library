using System;
using UnityEngine;

namespace Sepinaco.SceneTools
{
    /// <summary>
    /// Serializable entry that pairs a <see cref="GameObject"/> with a
    /// visibility flag. Used by <see cref="ObjectsManager"/> to control
    /// activation / deactivation of scene objects at edit-time and runtime.
    /// </summary>
    [Serializable]
    public class ObjectTarget : ITargetEntry
    {
        [Tooltip("The GameObject to show or hide.")]
        public GameObject target;

        [Tooltip("Check to show the object, uncheck to hide it.")]
        public bool isActive = true;

        /// <inheritdoc/>
        public GameObject Target => target;

        /// <inheritdoc/>
        public bool ActiveState
        {
            get => isActive;
            set => isActive = value;
        }

        /// <inheritdoc/>
        public string DisplayName => target != null ? target.name : "(empty)";
    }
}
