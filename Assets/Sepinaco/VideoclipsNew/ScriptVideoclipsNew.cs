using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class ScriptVideoclipsNew : MonoBehaviour
{
    [Header("Video Clips")]
    [Tooltip("Arrastra aquí los VideoClip desde el editor")]
    public VideoClip[] videoClips;

    [Header("Render Texture")]
    public int textureWidth = 1920;
    public int textureHeight = 1080;

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

    [Header("Estado inicial")]
    public bool replaceTexturesOnStart = true;
    public bool startMuted = false;

    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private RenderTexture renderTexture;
    private Material videoMaterial;

    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private bool texturesReplaced;
    private bool isMuted;
    private int currentVideoIndex;
    private bool menuActive;
    private Vector2 scrollPosition;

    private GUIStyle boxStyle;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private bool stylesInitialized;

    void Start()
    {
        if (videoClips == null || videoClips.Length == 0)
        {
            Debug.LogWarning("[ScriptVideoclipsNew] No hay VideoClips asignados.");
            return;
        }

        StoreOriginalMaterials();
        SetupRenderTexture();
        SetupVideoPlayer();
        SetupAudioSource();

        isMuted = startMuted;
        audioSource.mute = isMuted;

        currentVideoIndex = 0;
        PlayVideo(currentVideoIndex);

        if (replaceTexturesOnStart)
            ReplaceSceneTextures();
    }

    void Update()
    {
        if (videoClips == null || videoClips.Length == 0) return;

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
    }

    void SetupRenderTexture()
    {
        renderTexture = new RenderTexture(textureWidth, textureHeight, 0);
        renderTexture.Create();

        videoMaterial = new Material(Shader.Find("Unlit/Texture"));
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

        videoPlayer.clip = videoClips[index];
        videoPlayer.Play();
        currentVideoIndex = index;

        Debug.Log($"[ScriptVideoclipsNew] Reproduciendo vídeo {index}: {videoClips[index].name}");
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

    void ReplaceSceneTextures()
    {
        foreach (var kvp in originalMaterials)
        {
            Renderer rend = kvp.Key;
            if (rend == null) continue;

            Material[] mats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = videoMaterial;

            rend.sharedMaterials = mats;
        }

        texturesReplaced = true;
        Debug.Log("[ScriptVideoclipsNew] Texturas reemplazadas por vídeo.");
    }

    void RestoreOriginalTextures()
    {
        foreach (var kvp in originalMaterials)
        {
            Renderer rend = kvp.Key;
            if (rend == null) continue;
            rend.sharedMaterials = kvp.Value;
        }

        texturesReplaced = false;
        Debug.Log("[ScriptVideoclipsNew] Texturas originales restauradas.");
    }

    void ToggleMute()
    {
        isMuted = !isMuted;
        audioSource.mute = isMuted;
        Debug.Log($"[ScriptVideoclipsNew] Audio {(isMuted ? "muteado" : "activado")}.");
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
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;
        titleStyle.alignment = TextAnchor.MiddleCenter;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14;
        labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
        labelStyle.richText = true;

        stylesInitialized = true;
    }

    void OnGUI()
    {
        if (!menuActive) return;

        InitStyles();

        float boxWidth = 340f;
        float contentHeight = 260f;
        float maxBoxHeight = Mathf.Min(contentHeight, Screen.height * 0.8f);
        float x = 10f;
        float y = 10f;
        Rect boxRect = new Rect(x, y, boxWidth, maxBoxHeight);

        GUI.Box(boxRect, GUIContent.none, boxStyle);

        Rect areaRect = new Rect(x + 16, y + 12, boxWidth - 32, maxBoxHeight - 24);
        GUILayout.BeginArea(areaRect);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Video Controls", titleStyle);
        GUILayout.Space(10);

        string videoName = "---";
        if (videoClips != null && currentVideoIndex < videoClips.Length && videoClips[currentVideoIndex] != null)
            videoName = videoClips[currentVideoIndex].name;

        GUILayout.Label($"<b>[{toggleTextureKey}]</b>  Texturas: {(texturesReplaced ? "<color=#FF6666>Vídeo</color>" : "<color=#66FF66>Original</color>")}", labelStyle);
        GUILayout.Label($"<b>[{toggleMuteKey}]</b>  Audio: {(isMuted ? "<color=#FF6666>Mute</color>" : "<color=#66FF66>On</color>")}", labelStyle);
        GUILayout.Label($"<b>[{nextVideoKey}]</b>  Siguiente vídeo", labelStyle);
        GUILayout.Label($"<b>[{previousVideoKey}]</b>  Vídeo anterior", labelStyle);
        GUILayout.Label($"<b>[{scrollUpKey}]</b> / <b>[{scrollDownKey}]</b>  Scroll menú", labelStyle);
        GUILayout.Space(8);
        GUILayout.Label($"<b>Vídeo:</b> {videoName}  ({currentVideoIndex + 1}/{videoClips.Length})", labelStyle);
        GUILayout.Space(4);
        GUILayout.Label($"<color=#888888>[{menuKey}] para cerrar</color>", labelStyle);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        if (videoMaterial != null)
            Destroy(videoMaterial);
    }
}
