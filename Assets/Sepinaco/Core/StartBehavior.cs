namespace Sepinaco.SceneTools
{
    /// <summary>
    /// Canonical initialization strategy for target-based managers.
    /// Concrete managers map their domain-specific enums
    /// (e.g. <c>StartObjectsMode</c>, <c>StartCollidersMode</c>) to these values
    /// via <see cref="BaseTargetManager.ResolveStartBehavior"/>.
    /// </summary>
    public enum StartBehavior
    {
        /// <summary>Each target retains its individually serialized state.</summary>
        UseIndividualSettings,

        /// <summary>All targets are forced to their active/enabled state on Awake.</summary>
        ActivateAll,

        /// <summary>All targets are forced to their inactive/disabled state on Awake.</summary>
        DeactivateAll
    }
}
