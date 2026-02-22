using UnityEngine;

namespace Sepinaco.SceneTools
{
    /// <summary>
    /// Contract for serializable target entries managed by a <see cref="BaseTargetManager"/>.
    /// Implement this interface on <c>[Serializable]</c> data classes to integrate
    /// custom target types with the shared management framework.
    /// </summary>
    /// <example>
    /// <code>
    /// [System.Serializable]
    /// public class LightTarget : ITargetEntry
    /// {
    ///     public GameObject target;
    ///     public bool lightEnabled = true;
    ///
    ///     public GameObject Target     =&gt; target;
    ///     public bool ActiveState      { get =&gt; lightEnabled; set =&gt; lightEnabled = value; }
    ///     public string DisplayName    =&gt; target != null ? target.name : "(empty)";
    /// }
    /// </code>
    /// </example>
    public interface ITargetEntry
    {
        /// <summary>The <see cref="GameObject"/> controlled by this entry.</summary>
        GameObject Target { get; }

        /// <summary>Whether the entry is in its active (enabled / visible) state.</summary>
        bool ActiveState { get; set; }

        /// <summary>Human-readable label shown in the debug menu and editor.</summary>
        string DisplayName { get; }
    }
}
