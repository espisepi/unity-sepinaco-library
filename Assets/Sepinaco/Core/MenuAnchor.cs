namespace Sepinaco.SceneTools
{
    /// <summary>
    /// Screen corner where the runtime debug menu panel is anchored.
    /// Used by <see cref="BaseSceneManager"/> to position the <c>OnGUI</c> overlay.
    /// </summary>
    public enum MenuAnchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
}
