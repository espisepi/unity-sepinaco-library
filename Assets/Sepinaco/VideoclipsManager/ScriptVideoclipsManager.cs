using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class ScriptVideoclipsManager : MonoBehaviour
{
    [Header("Debug")]
    [Tooltip("Fuerza una nueva iteración de renderers y reaplica las texturas de vídeo (solo si el reemplazo está activo). Modificable desde ScriptDebugInspector.")]
    [SerializeField] private bool forceRefreshVideoTextures;

    [Header("Audio del vídeo")]
    [Tooltip("Mutea o desmutea el audio del vídeo. Modificable desde ScriptDebugInspector.")]
    [SerializeField] private bool muteAudio;

    [Header("Texturas de vídeo")]
    [Tooltip("Activa o desactiva el reemplazo de texturas por vídeo. Modificable desde ScriptDebugInspector.")]
    [SerializeField] private bool useVideoTextures;

    [Header("Vídeo actual")]
    [Tooltip("Índice del vídeo que se reproduce actualmente. Modificable desde ScriptDebugInspector.")]
    [SerializeField] private int currentVideoIndex;

    [Header("Video Clips")]
    [Tooltip("Arrastra aquí los VideoClip desde el editor")]
    public VideoClip[] videoClips;

    [Header("Render Texture")]
    public int textureWidth = 1920;
    public int textureHeight = 1080;

    [Header("Shader")]
    [Tooltip("Shader para el material de vídeo. Si no se asigna, usa Unlit/Texture")]
    public Shader videoShader;

    [Header("Menú")]
    [Tooltip("Tecla para abrir/cerrar el menú de controles de vídeo")]
    public KeyCode menuKey = KeyCode.F1;

    [Header("Controles (solo funcionan con el menú abierto)")]
    [Tooltip("Tecla para alternar entre texturas de vídeo y texturas originales")]
    public KeyCode toggleTextureKey = KeyCode.T;

    [Tooltip("Tecla para mutear/desmutear el audio del vídeo")]
    public KeyCode toggleMuteKey = KeyCode.M;

    [Tooltip("Tecla para cambiar al siguiente vídeo")]
    public KeyCode nextVideoKey = KeyCode.RightBracket;

    [Tooltip("Tecla para cambiar al vídeo anterior")]
    public KeyCode previousVideoKey = KeyCode.LeftBracket;

    [Header("Scroll del menú (solo funcionan con el menú abierto)")]
    [Tooltip("Tecla para hacer scroll hacia arriba en el menú")]
    public KeyCode scrollUpKey = KeyCode.UpArrow;

    [Tooltip("Tecla para hacer scroll hacia abajo en el menú")]
    public KeyCode scrollDownKey = KeyCode.DownArrow;

    [Tooltip("Velocidad de scroll en píxeles por segundo")]
    public float scrollSpeed = 200f;

    [Header("Zoom de la UI (solo funcionan con el menú abierto)")]
    [Tooltip("Tecla para aumentar el tamaño de las letras de la UI")]
    public KeyCode zoomInKey = KeyCode.I;

    [Tooltip("Tecla para disminuir el tamaño de las letras de la UI")]
    public KeyCode zoomOutKey = KeyCode.O;

    [Header("Tamaño de la UI (solo funcionan con el menú abierto)")]
    [Tooltip("Tecla para aumentar el ancho de la UI")]
    public KeyCode uiWidthIncreaseKey = KeyCode.RightArrow;

    [Tooltip("Tecla para disminuir el ancho de la UI")]
    public KeyCode uiWidthDecreaseKey = KeyCode.LeftArrow;

    [Tooltip("Tecla para aumentar el alto de la UI")]
    public KeyCode uiHeightIncreaseKey = KeyCode.PageUp;

    [Tooltip("Tecla para disminuir el alto de la UI")]
    public KeyCode uiHeightDecreaseKey = KeyCode.PageDown;

    [Header("Estado inicial")]
    public bool replaceTexturesOnStart = true;
    public bool startMuted = false;

    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private RenderTexture renderTexture;
    private Material videoMaterial;

    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Dictionary<Renderer, Material[]> videoMaterialArrays = new Dictionary<Renderer, Material[]>();
    private bool hasVideoClips;
    private bool texturesReplaced;
    private bool isMuted;
    private int _lastAppliedVideoIndex = -1;
    private bool _lastAppliedUseVideoTextures;
    private bool _lastAppliedMuteAudio;
    private bool isPlayingFromUrl;
    private string urlVideoDisplayName = "";
    private bool menuActive;
    private Vector2 scrollPosition;

    private int cachedRendererCount;
    private ScriptObjectsManager objectsManager;

    private GUIStyle boxStyle;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private bool stylesInitialized;
    private int guiFontSize = 14;
    private const int GuiFontSizeMin = 8;
    private const int GuiFontSizeMax = 40;
    private const int GuiFontSizeStep = 2;

    private float guiBoxWidth = 340f;
    private float guiBoxHeight = 400f;
    private const float GuiBoxSizeStep = 20f;
    private const float GuiBoxWidthMin = 200f;
    private const float GuiBoxWidthMax = 1200f;
    private const float GuiBoxHeightMin = 100f;
    private const float GuiBoxHeightMax = 1200f;

    private string cachedLabelNext;
    private string cachedLabelPrevious;
    private string cachedLabelScroll;
    private string cachedLabelClose;
    private string cachedLabelTexture;
    private string cachedLabelMute;
    private string cachedLabelVideo;
    private bool guiStringsDirty = true;

    void Start()
    {
        if (videoClips == null || videoClips.Length == 0)
        {
            Debug.LogWarning("[ScriptVideoclipsManager No hay VideoClips asignados.");
            return;
        }

        hasVideoClips = true;

        objectsManager = FindObjectOfType<ScriptObjectsManager>();

        StoreOriginalMaterials();
        SetupRenderTexture();
        SetupVideoPlayer();
        SetupAudioSource();
        StoreRenderersFromObjectsManagerTargets();
        BuildVideoMaterialArrays();
        BuildStaticGUIStrings();

        isMuted = startMuted;
        muteAudio = isMuted;
        _lastAppliedMuteAudio = isMuted;
        audioSource.mute = isMuted;

        currentVideoIndex = 0;
        PlayVideo(currentVideoIndex);

        if (replaceTexturesOnStart || useVideoTextures)
            ReplaceSceneTextures();

        if (objectsManager != null)
            objectsManager.OnTargetStateChanged += OnObjectTargetStateChanged;
    }

    void Update()
    {
        if (!hasVideoClips) return;

        if (Input.GetKeyDown(menuKey))
            menuActive = !menuActive;

        if (!menuActive) return;

        if (Input.GetKeyDown(toggleTextureKey))
        {
            if (texturesReplaced)
                RestoreOriginalTextures();
            else
                ReplaceSceneTextures();
        }

        if (Input.GetKeyDown(toggleMuteKey))
            ToggleMute();

        if (Input.GetKeyDown(nextVideoKey))
            NextVideo();

        if (Input.GetKeyDown(previousVideoKey))
            PreviousVideo();

        if (Input.GetKey(scrollUpKey))
            scrollPosition.y -= scrollSpeed * Time.deltaTime;

        if (Input.GetKey(scrollDownKey))
            scrollPosition.y += scrollSpeed * Time.deltaTime;

        if (scrollPosition.y < 0f) scrollPosition.y = 0f;

        if (Input.GetKeyDown(zoomInKey))
        {
            guiFontSize = Mathf.Min(guiFontSize + GuiFontSizeStep, GuiFontSizeMax);
            stylesInitialized = false;
        }

        if (Input.GetKeyDown(zoomOutKey))
        {
            guiFontSize = Mathf.Max(guiFontSize - GuiFontSizeStep, GuiFontSizeMin);
            stylesInitialized = false;
        }

        if (Input.GetKeyDown(uiWidthIncreaseKey))
            guiBoxWidth = Mathf.Min(guiBoxWidth + GuiBoxSizeStep, GuiBoxWidthMax);

        if (Input.GetKeyDown(uiWidthDecreaseKey))
            guiBoxWidth = Mathf.Max(guiBoxWidth - GuiBoxSizeStep, GuiBoxWidthMin);

        if (Input.GetKeyDown(uiHeightIncreaseKey))
            guiBoxHeight = Mathf.Min(guiBoxHeight + GuiBoxSizeStep, GuiBoxHeightMax);

        if (Input.GetKeyDown(uiHeightDecreaseKey))
            guiBoxHeight = Mathf.Max(guiBoxHeight - GuiBoxSizeStep, GuiBoxHeightMin);
    }

    void StoreOriginalMaterials()
    {
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        foreach (Renderer rend in renderers)
        {
            if (rend.gameObject == gameObject) continue;

            Material[] copy = new Material[rend.sharedMaterials.Length];
            System.Array.Copy(rend.sharedMaterials, copy, copy.Length);
            originalMaterials[rend] = copy;
        }
        cachedRendererCount = renderers.Length;
    }

    void RefreshRenderersIfNeeded()
    {
        StoreRenderersFromObjectsManagerTargets();

        Renderer[] renderers = FindObjectsOfType<Renderer>();
        if (renderers.Length == cachedRendererCount) return;

        foreach (Renderer rend in renderers)
        {
            if (rend.gameObject == gameObject) continue;
            if (originalMaterials.ContainsKey(rend)) continue;

            Material[] copy = new Material[rend.sharedMaterials.Length];
            System.Array.Copy(rend.sharedMaterials, copy, copy.Length);
            originalMaterials[rend] = copy;

            Material[] mats = new Material[copy.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = videoMaterial;
            videoMaterialArrays[rend] = mats;
        }

        cachedRendererCount = renderers.Length;
    }

    void OnObjectTargetStateChanged(GameObject obj, bool active)
    {
        if (!active || obj == null || videoMaterial == null) return;

        if (texturesReplaced)
            ReplaceSceneTextures();
    }

    void StoreRenderersFromObjectsManagerTargets()
    {
        if (objectsManager == null) return;

        for (int i = 0; i < objectsManager.TargetCount; i++)
        {
            ObjectTarget t = objectsManager.GetTarget(i);
            if (t == null || t.target == null) continue;

            Renderer[] renderers = t.target.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer rend in renderers)
            {
                if (rend.gameObject == gameObject) continue;
                if (originalMaterials.ContainsKey(rend)) continue;

                Material[] copy = new Material[rend.sharedMaterials.Length];
                System.Array.Copy(rend.sharedMaterials, copy, copy.Length);
                originalMaterials[rend] = copy;

                if (videoMaterial != null)
                {
                    Material[] mats = new Material[copy.Length];
                    for (int j = 0; j < mats.Length; j++)
                        mats[j] = videoMaterial;
                    videoMaterialArrays[rend] = mats;
                }
            }
        }
    }

    void BuildVideoMaterialArrays()
    {
        foreach (var kvp in originalMaterials)
        {
            Material[] mats = new Material[kvp.Value.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = videoMaterial;
            videoMaterialArrays[kvp.Key] = mats;
        }
    }

    void BuildStaticGUIStrings()
    {
        cachedLabelNext = $"<b>[{nextVideoKey}]</b>  Siguiente vídeo";
        cachedLabelPrevious = $"<b>[{previousVideoKey}]</b>  Vídeo anterior";
        cachedLabelScroll = $"<b>[{scrollUpKey}]</b> / <b>[{scrollDownKey}]</b>  Scroll menú";
        cachedLabelClose = $"<color=#888888>[{menuKey}] para cerrar</color>";
    }

    void RebuildDynamicGUIStrings()
    {
        cachedLabelTexture = $"<b>[{toggleTextureKey}]</b>  Texturas: {(texturesReplaced ? "<color=#FF6666>Vídeo</color>" : "<color=#66FF66>Original</color>")}";
        cachedLabelMute = $"<b>[{toggleMuteKey}]</b>  Audio: {(isMuted ? "<color=#FF6666>Mute</color>" : "<color=#66FF66>On</color>")}";

        string videoName = "---";
        if (isPlayingFromUrl)
            videoName = $"<color=#4EC5F1>[Web] {urlVideoDisplayName}</color>";
        else if (videoClips != null && currentVideoIndex < videoClips.Length && videoClips[currentVideoIndex] != null)
            videoName = videoClips[currentVideoIndex].name;

        string clipInfo = videoClips != null ? $"({currentVideoIndex + 1}/{videoClips.Length})" : "";
        cachedLabelVideo = $"<b>Vídeo:</b> {videoName}  {clipInfo}";
        guiStringsDirty = false;
    }

    void SetupRenderTexture()
    {
        renderTexture = new RenderTexture(textureWidth, textureHeight, 0);
        renderTexture.Create();

        if (videoShader == null)
            videoShader = Shader.Find("Unlit/Texture");

        videoMaterial = new Material(videoShader);
        videoMaterial.mainTexture = renderTexture;
    }

    void SetupVideoPlayer()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
            videoPlayer = gameObject.AddComponent<VideoPlayer>();

        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.isLooping = true;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
    }

    void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        videoPlayer.SetTargetAudioSource(0, audioSource);
    }

    void PlayVideo(int index)
    {
        if (index < 0 || index >= videoClips.Length || videoClips[index] == null) return;

        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClips[index];
        videoPlayer.Play();
        currentVideoIndex = index;
        _lastAppliedVideoIndex = index;
        isPlayingFromUrl = false;
        guiStringsDirty = true;

        Debug.Log($"[ScriptVideoclipsManager] Reproduciendo vídeo {index}: {videoClips[index].name}");
    }

    public void RefreshTexturesForManagedObjects()
    {
        if (!hasVideoClips || videoMaterial == null) return;

        if (texturesReplaced)
            ReplaceSceneTextures();
    }

    public void OnValidate()
    {
        if (!Application.isPlaying || !hasVideoClips || videoPlayer == null) return;
        if (videoClips == null || videoClips.Length == 0) return;

        if (forceRefreshVideoTextures)
        {
            if (useVideoTextures || texturesReplaced)
                ReplaceSceneTextures();

            // One-shot trigger para poder volver a lanzarlo desde DebugInspector.
            forceRefreshVideoTextures = false;
        }

        if (muteAudio != _lastAppliedMuteAudio)
        {
            isMuted = muteAudio;
            audioSource.mute = isMuted;
            _lastAppliedMuteAudio = muteAudio;
            guiStringsDirty = true;
            Debug.Log($"[ScriptVideoclipsManager] Audio {(isMuted ? "muteado" : "activado")}.");
        }

        if (useVideoTextures != _lastAppliedUseVideoTextures)
        {
            if (useVideoTextures)
                ReplaceSceneTextures();
            else
                RestoreOriginalTextures();
        }

        int len = videoClips.Length;
        currentVideoIndex = ((currentVideoIndex % len) + len) % len;

        if (currentVideoIndex != _lastAppliedVideoIndex)
            PlayVideo(currentVideoIndex);
    }

    void NextVideo()
    {
        currentVideoIndex = (currentVideoIndex + 1) % videoClips.Length;
        PlayVideo(currentVideoIndex);
    }

    void PreviousVideo()
    {
        currentVideoIndex = (currentVideoIndex - 1 + videoClips.Length) % videoClips.Length;
        PlayVideo(currentVideoIndex);
    }

    public void PlayVideoFromUrl(string filePath, string displayName)
    {
        if (videoPlayer == null) return;

        videoPlayer.Stop();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = filePath;
        videoPlayer.Play();

        isPlayingFromUrl = true;
        urlVideoDisplayName = displayName;
        guiStringsDirty = true;

        Debug.Log($"[ScriptVideoclipsManager] Reproduciendo vídeo desde archivo: {displayName}");
    }

    public string CurrentVideoDisplayName
    {
        get
        {
            if (isPlayingFromUrl) return urlVideoDisplayName;
            if (videoClips != null && currentVideoIndex < videoClips.Length && videoClips[currentVideoIndex] != null)
                return videoClips[currentVideoIndex].name;
            return "---";
        }
    }

    void ReplaceSceneTextures()
    {
        RefreshRenderersIfNeeded();

        foreach (var kvp in videoMaterialArrays)
        {
            if (kvp.Key == null) continue;
            kvp.Key.sharedMaterials = kvp.Value;
        }

        texturesReplaced = true;
        useVideoTextures = true;
        _lastAppliedUseVideoTextures = true;
        guiStringsDirty = true;
        Debug.Log("[ScriptVideoclipsManager] Texturas reemplazadas por vídeo.");
    }

    void RestoreOriginalTextures()
    {
        RefreshRenderersIfNeeded();

        foreach (var kvp in originalMaterials)
        {
            Renderer rend = kvp.Key;
            if (rend == null) continue;
            rend.sharedMaterials = kvp.Value;
        }

        texturesReplaced = false;
        useVideoTextures = false;
        _lastAppliedUseVideoTextures = false;
        guiStringsDirty = true;
        Debug.Log("[ScriptVideoclipsManager] Texturas originales restauradas.");
    }

    void ToggleMute()
    {
        isMuted = !isMuted;
        audioSource.mute = isMuted;
        muteAudio = isMuted;
        _lastAppliedMuteAudio = isMuted;
        guiStringsDirty = true;
        Debug.Log($"[ScriptVideoclipsManager] Audio {(isMuted ? "muteado" : "activado")}.");
    }

    void InitStyles()
    {
        if (stylesInitialized) return;

        Texture2D bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
        bgTex.Apply();

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = bgTex;
        boxStyle.padding = new RectOffset(16, 16, 12, 12);

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = guiFontSize + 4;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;
        titleStyle.alignment = TextAnchor.MiddleCenter;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = guiFontSize;
        labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
        labelStyle.richText = true;

        stylesInitialized = true;
    }

    void OnGUI()
    {
        if (!menuActive) return;

        InitStyles();

        if (guiStringsDirty)
            RebuildDynamicGUIStrings();

        float boxWidth = guiBoxWidth;
        float contentHeight = 300f;
        float maxBoxHeight = Mathf.Min(contentHeight, guiBoxHeight, Screen.height * 0.95f);
        float x = 10f;
        float y = 10f;
        Rect boxRect = new Rect(x, y, boxWidth, maxBoxHeight);

        GUI.Box(boxRect, GUIContent.none, boxStyle);

        Rect areaRect = new Rect(x + 16, y + 12, boxWidth - 32, maxBoxHeight - 24);
        GUILayout.BeginArea(areaRect);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Video Controls", titleStyle);
        GUILayout.Space(10);

        GUILayout.Label(cachedLabelTexture, labelStyle);
        GUILayout.Label(cachedLabelMute, labelStyle);
        GUILayout.Label(cachedLabelNext, labelStyle);
        GUILayout.Label(cachedLabelPrevious, labelStyle);
        GUILayout.Label(cachedLabelScroll, labelStyle);
        GUILayout.Label($"<b>[{zoomInKey}]</b> / <b>[{zoomOutKey}]</b>  Zoom UI ({guiFontSize}px)", labelStyle);
        GUILayout.Label($"<b>[{uiWidthDecreaseKey}]</b> / <b>[{uiWidthIncreaseKey}]</b>  Ancho UI ({guiBoxWidth}px)", labelStyle);
        GUILayout.Label($"<b>[{uiHeightDecreaseKey}]</b> / <b>[{uiHeightIncreaseKey}]</b>  Alto UI ({guiBoxHeight}px)", labelStyle);
        GUILayout.Space(8);
        GUILayout.Label(cachedLabelVideo, labelStyle);
        GUILayout.Space(4);
        GUILayout.Label(cachedLabelClose, labelStyle);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void OnDestroy()
    {
        if (objectsManager != null)
            objectsManager.OnTargetStateChanged -= OnObjectTargetStateChanged;

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        if (videoMaterial != null)
            Destroy(videoMaterial);
    }
}
