using UnityEngine;

namespace Sepinaco.SceneTools
{
    /// <summary>
    /// Determines how <see cref="ObjectsManager"/> initialises target
    /// visibility when the scene starts.
    /// </summary>
    public enum StartObjectsMode
    {
        [Tooltip("Keep each target's individual isActive setting.")]
        UseIndividualSettings,

        [Tooltip("Force every target visible on start.")]
        ShowAll,

        [Tooltip("Force every target hidden on start.")]
        HideAll
    }
}
