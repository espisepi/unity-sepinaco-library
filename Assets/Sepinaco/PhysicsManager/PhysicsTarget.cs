using System;
using UnityEngine;

namespace Sepinaco.SceneTools
{
    /// <summary>
    /// Serializable entry that pairs a <see cref="GameObject"/> with a
    /// collider-enabled flag. At runtime, <see cref="PhysicsManager"/>
    /// caches all <see cref="Collider"/> components found in the target's
    /// hierarchy for zero-allocation state changes.
    /// </summary>
    [Serializable]
    public class PhysicsTarget : ITargetEntry
    {
        [Tooltip("The GameObject whose colliders (including children) will be managed.")]
        public GameObject target;

        [Tooltip("Check to enable colliders, uncheck to disable them.")]
        public bool collidersEnabled = true;

        /// <summary>
        /// Runtime cache populated by <see cref="PhysicsManager"/> during Awake
        /// via <c>GetComponentsInChildren&lt;Collider&gt;</c>.
        /// Not serialized — rebuilt every time the scene loads.
        /// </summary>
        [NonSerialized] public Collider[] cachedColliders;

        /// <inheritdoc/>
        public GameObject Target => target;

        /// <inheritdoc/>
        public bool ActiveState
        {
            get => collidersEnabled;
            set => collidersEnabled = value;
        }

        /// <inheritdoc/>
        public string DisplayName => target != null ? target.name : "(empty)";
    }
}
