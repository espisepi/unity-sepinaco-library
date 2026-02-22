using UnityEngine;

namespace Sepinaco.SceneTools
{
    /// <summary>
    /// Determines how <see cref="PhysicsManager"/> initialises collider
    /// state when the scene starts.
    /// </summary>
    public enum StartCollidersMode
    {
        [Tooltip("Keep each target's individual collidersEnabled setting.")]
        UseIndividualSettings,

        [Tooltip("Force all colliders enabled on start.")]
        EnableAll,

        [Tooltip("Force all colliders disabled on start.")]
        DisableAll
    }
}
