using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace Sepinaco.SceneTools
{
    /// <summary>
    /// Manages video playback and real-time material replacement across
    /// every <see cref="Renderer"/> in the scene.
    ///
    /// <para>Replaces all renderer materials with a shared video-textured material
    /// and provides runtime controls to cycle through clips, toggle between
    /// video and original textures, and mute / unmute audio.</para>
    ///
    /// <para><b>Integration with <see cref="ObjectsManager"/>:</b>
    /// automatically detects an <see cref="ObjectsManager"/> in the scene and
    /// subscribes to <see cref="BaseTargetManager.OnTargetStateChanged"/>, so
    /// newly activated objects receive the video material in real time.</para>
    ///
    /// <para><b>Scripting API:</b></para>
    /// <list type="bullet">
    ///   <item><see cref="ReplaceSceneTextures"/> / <see cref="RestoreOriginalTextures"/>
    ///         — toggle video materials.</item>
    ///   <item><see cref="CycleVideo"/> — advance or rewind through the clip list.</item>
    ///   <item><see cref="ToggleMute"/> — mute / unmute the audio source.</item>
    /// </list>
    /// </summary>
    [AddComponentMenu("Sepinaco/Scene Tools/Videoclips Manager")]
    [HelpURL("https://github.com/sepinaco/scene-tools")]
    public class VideoclipsManager : BaseSceneManager
    {
        #region Serialized Fields

        [Header("Video Clips")]
        [Tooltip("VideoClip assets to cycle through at runtime.")]
        [SerializeField] private VideoClip[] _videoClips;

        [Header("Render Texture")]
        [Tooltip("Width of the internal render texture in pixels.")]
        [SerializeField] private int _textureWidth = 1920;

        [Tooltip("Height of the internal render texture in pixels.")]
        [SerializeField] private int _textureHeight = 1080;

        [Header("Shader")]
        [Tooltip("Shader for the video material. Falls back to Unlit/Texture if empty.")]
        [SerializeField] private Shader _videoShader;

        [Header("Video Controls (active while menu is open)")]
        [Tooltip("Key to toggle between video and original textures.")]
        [SerializeField] private KeyCode _toggleTextureKey = KeyCode.T;

        [Tooltip("Key to mute / unmute video audio.")]
        [SerializeField] private KeyCode _toggleMuteKey = KeyCode.M;

        [Tooltip("Key to switch to the next video clip.")]
        [SerializeField] private KeyCode _nextVideoKey = KeyCode.RightBracket;

        [Tooltip("Key to switch to the previous video clip.")]
        [SerializeField] private KeyCode _previousVideoKey = KeyCode.LeftBracket;

        [Header("Initial State")]
        [Tooltip("Replace all scene textures with video on start.")]
        [SerializeField] private bool _replaceTexturesOnStart = true;

        [Tooltip("Start with audio muted.")]
        [SerializeField] private bool _startMuted;

        #endregion

        #region Runtime State

        private VideoPlayer  _videoPlayer;
        private AudioSource  _audioSource;
        private RenderTexture _renderTexture;
        private Material     _videoMaterial;

        private readonly Dictionary<Renderer, Material[]> _originalMaterials =
            new Dictionary<Renderer, Material[]>();

        private readonly Dictionary<Renderer, Material[]> _videoMaterialArrays =
            new Dictionary<Renderer, Material[]>();

        private bool _hasVideoClips;
        private bool _texturesReplaced;
        private bool _isMuted;
        private int  _currentVideoIndex;
        private int  _cachedRendererCount;

        private ObjectsManager _objectsManager;

        private string _labelTexture;
        private string _labelMute;
        private string _labelVideo;
        private bool   _guiStringsDirty = true;

        #endregion

        #region Editor Defaults

#if UNITY_EDITOR
        private void Reset()
        {
            _menuAnchor = MenuAnchor.TopLeft;
        }
#endif

        #endregion

        #region BaseSceneManager Overrides

        /// <inheritdoc/>
        protected override string MenuTitle => "Video Controls";

        /// <inheritdoc/>
        protected override bool CanOperate() => _hasVideoClips;

        /// <inheritdoc/>
        protected override void OnMenuInput()
        {
            if (Input.GetKeyDown(_toggleTextureKey))
            {
                if (_texturesReplaced) RestoreOriginalTextures();
                else ReplaceSceneTextures();
            }

            if (Input.GetKeyDown(_toggleMuteKey))
                ToggleMute();

            if (Input.GetKeyDown(_nextVideoKey))
                CycleVideo(1);

            if (Input.GetKeyDown(_previousVideoKey))
                CycleVideo(-1);
        }

        /// <inheritdoc/>
        protected override void DrawMenuHelpLines()
        {
            if (_guiStringsDirty) RebuildDynamicLabels();

            GUILayout.Label(_labelTexture, LabelStyle);
            GUILayout.Label(_labelMute, LabelStyle);
            GUILayout.Label(
                $"<b>[{_nextVideoKey}]</b>  Next video", LabelStyle);
            GUILayout.Label(
                $"<b>[{_previousVideoKey}]</b>  Previous video", LabelStyle);
        }

        /// <inheritdoc/>
        protected override void DrawMenuContent()
        {
            if (_guiStringsDirty) RebuildDynamicLabels();
            GUILayout.Label(_labelVideo, LabelStyle);
        }

        /// <inheritdoc/>
        protected override float EstimateContentHeight() => 300f;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (_videoClips == null || _videoClips.Length == 0)
            {
                Debug.LogWarning($"[{nameof(VideoclipsManager)}] No VideoClips assigned.");
                return;
            }

            _hasVideoClips = true;

            StoreOriginalMaterials();
            SetupRenderTexture();
            SetupVideoPlayer();
            SetupAudioSource();
            BuildVideoMaterialArrays();

            _isMuted = _startMuted;
            _audioSource.mute = _isMuted;

            _currentVideoIndex = 0;
            PlayVideo(_currentVideoIndex);

            if (_replaceTexturesOnStart)
                ReplaceSceneTextures();

            _objectsManager = FindObjectOfType<ObjectsManager>();
            if (_objectsManager != null)
                _objectsManager.OnTargetStateChanged += HandleObjectTargetStateChanged;
        }

        private void OnDestroy()
        {
            if (_objectsManager != null)
                _objectsManager.OnTargetStateChanged -= HandleObjectTargetStateChanged;

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }

            if (_videoMaterial != null)
                Destroy(_videoMaterial);
        }

        #endregion

        #region Setup

        private void SetupRenderTexture()
        {
            _renderTexture = new RenderTexture(_textureWidth, _textureHeight, 0);
            _renderTexture.Create();

            if (_videoShader == null)
                _videoShader = Shader.Find("Unlit/Texture");

            _videoMaterial = new Material(_videoShader);
            _videoMaterial.mainTexture = _renderTexture;
        }

        private void SetupVideoPlayer()
        {
            _videoPlayer = GetComponent<VideoPlayer>();
            if (_videoPlayer == null)
                _videoPlayer = gameObject.AddComponent<VideoPlayer>();

            _videoPlayer.playOnAwake = false;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _renderTexture;
            _videoPlayer.isLooping = true;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        }

        private void SetupAudioSource()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _videoPlayer.SetTargetAudioSource(0, _audioSource);
        }

        #endregion

        #region Material Management

        private void StoreOriginalMaterials()
        {
            Renderer[] renderers = FindObjectsOfType<Renderer>();
            foreach (Renderer rend in renderers)
            {
                if (rend.gameObject == gameObject) continue;

                Material[] copy = new Material[rend.sharedMaterials.Length];
                System.Array.Copy(rend.sharedMaterials, copy, copy.Length);
                _originalMaterials[rend] = copy;
            }
            _cachedRendererCount = renderers.Length;
        }

        private void BuildVideoMaterialArrays()
        {
            foreach (var kvp in _originalMaterials)
            {
                Material[] mats = new Material[kvp.Value.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = _videoMaterial;
                _videoMaterialArrays[kvp.Key] = mats;
            }
        }

        private void RefreshRenderersIfNeeded()
        {
            Renderer[] renderers = FindObjectsOfType<Renderer>();
            if (renderers.Length == _cachedRendererCount) return;

            foreach (Renderer rend in renderers)
            {
                if (rend.gameObject == gameObject) continue;
                if (_originalMaterials.ContainsKey(rend)) continue;

                Material[] copy = new Material[rend.sharedMaterials.Length];
                System.Array.Copy(rend.sharedMaterials, copy, copy.Length);
                _originalMaterials[rend] = copy;

                Material[] mats = new Material[copy.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = _videoMaterial;
                _videoMaterialArrays[rend] = mats;
            }

            _cachedRendererCount = renderers.Length;
        }

        /// <summary>
        /// Replaces every tracked renderer's materials with the video material.
        /// Automatically refreshes the renderer cache if new renderers appeared.
        /// </summary>
        public void ReplaceSceneTextures()
        {
            RefreshRenderersIfNeeded();

            foreach (var kvp in _videoMaterialArrays)
            {
                if (kvp.Key != null)
                    kvp.Key.sharedMaterials = kvp.Value;
            }

            _texturesReplaced = true;
            _guiStringsDirty = true;
        }

        /// <summary>
        /// Restores every tracked renderer's original materials.
        /// </summary>
        public void RestoreOriginalTextures()
        {
            RefreshRenderersIfNeeded();

            foreach (var kvp in _originalMaterials)
            {
                if (kvp.Key != null)
                    kvp.Key.sharedMaterials = kvp.Value;
            }

            _texturesReplaced = false;
            _guiStringsDirty = true;
        }

        #endregion

        #region Video Playback

        private void PlayVideo(int index)
        {
            if (index < 0 || index >= _videoClips.Length || _videoClips[index] == null) return;

            _videoPlayer.clip = _videoClips[index];
            _videoPlayer.Play();
            _currentVideoIndex = index;
            _guiStringsDirty = true;
        }

        /// <summary>
        /// Advances or rewinds through the clip list by <paramref name="direction"/>
        /// (positive = forward, negative = backward). Wraps around.
        /// </summary>
        public void CycleVideo(int direction)
        {
            _currentVideoIndex =
                (_currentVideoIndex + direction + _videoClips.Length) % _videoClips.Length;
            PlayVideo(_currentVideoIndex);
        }

        /// <summary>Toggles audio mute on the video's <see cref="AudioSource"/>.</summary>
        public void ToggleMute()
        {
            _isMuted = !_isMuted;
            _audioSource.mute = _isMuted;
            _guiStringsDirty = true;
        }

        #endregion

        #region Inter-Manager Communication

        private void HandleObjectTargetStateChanged(GameObject obj, bool active)
        {
            if (!active || obj == null || _videoMaterial == null) return;

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer rend in renderers)
            {
                if (_originalMaterials.ContainsKey(rend)) continue;

                Material[] copy = new Material[rend.sharedMaterials.Length];
                System.Array.Copy(rend.sharedMaterials, copy, copy.Length);
                _originalMaterials[rend] = copy;

                Material[] mats = new Material[copy.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = _videoMaterial;
                _videoMaterialArrays[rend] = mats;

                if (_texturesReplaced)
                    rend.sharedMaterials = mats;
            }

            _cachedRendererCount = FindObjectsOfType<Renderer>().Length;
        }

        #endregion

        #region GUI Labels

        private void RebuildDynamicLabels()
        {
            string texState = _texturesReplaced
                ? "<color=#FF6666>Video</color>"
                : "<color=#66FF66>Original</color>";
            _labelTexture = $"<b>[{_toggleTextureKey}]</b>  Textures: {texState}";

            string muteState = _isMuted
                ? "<color=#FF6666>Muted</color>"
                : "<color=#66FF66>On</color>";
            _labelMute = $"<b>[{_toggleMuteKey}]</b>  Audio: {muteState}";

            string videoName = "---";
            if (_videoClips != null
                && _currentVideoIndex < _videoClips.Length
                && _videoClips[_currentVideoIndex] != null)
            {
                videoName = _videoClips[_currentVideoIndex].name;
            }

            _labelVideo = $"<b>Video:</b> {videoName}  ({_currentVideoIndex + 1}/{_videoClips.Length})";

            _guiStringsDirty = false;
        }

        #endregion
    }
}
