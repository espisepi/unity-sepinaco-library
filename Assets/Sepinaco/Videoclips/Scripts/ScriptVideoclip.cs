using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using Sepinaco.VideoApiManager; // Importa el namespace donde está VideoApiManager


public class ScriptVideoclip : MonoBehaviour
{
    public Material videoMaterial; // Asigna aquí el material del video

    // AVISO: !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // Ruta al video dentro de los Assets del proyecto
    // Ruta donde poner los videos:
    // MiNombreProyecto/Assets/StreamingAssets/Sepinaco/Videoclips/DELAOSSA - JUDAS (PROD. JMOODS).mp4
    // Ruta para poner aqui en el codigo:
    // Sepinaco/Videoclips/DELAOSSA - JUDAS (PROD. JMOODS).mp4
    public string[] videoPaths = {
        "Sepinaco/Videoclips/DELAOSSA - JUDAS (PROD. JMOODS).mp4",
        "Sepinaco/Videoclips/Soto Asa y La Zowi - Smartphone   GALLERY SESSION.mp4",
        "Sepinaco/Videoclips/SHARK - TOKYO DRIFT RMX.mp4"
    };

    private VideoPlayer videoPlayer;

    public List<string> videos;

    private bool isSearchingVideos;

    void Start()
    {

        // Searching videos mode on ;)
        isSearchingVideos = true;

        // Get Videos from API
        videos = new List<string>();

        ReplaceSceneMaterials(videoMaterial);
        StartVideoPlayer();
        // InitializeVideoLocalObjects();
    }

    // Update is called once per frame
    void Update()
    {
        if( isSearchingVideos ) {
            videos = GetVideoApiManager().GetVideos();
            if ( HasVideos(videos) ) {
                Debug.Log("LOGRADO!!" + videos.Count);
                StopSearchVideos();
                InitializeVideoApiObjects();
            } 
        }

    }

    public bool HasVideos(List<string> videos)
    {
        return videos != null && videos.Count > 0;
    }

    public void SearchVideos() {
        isSearchingVideos = true;
    }

    public void StopSearchVideos() {
        isSearchingVideos = false;
    }

    public VideoApiManager GetVideoApiManager()
    {
        return VideoApiManager.Instance;
    }

    void ReplaceSceneMaterials(Material newMaterial)
    {
        Renderer[] renderers = FindObjectsOfType<Renderer>(); // Encuentra todos los objetos con componente Renderer en la escena
        foreach (Renderer renderer in renderers)
        {
            Material[] mats = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = newMaterial; // Reemplaza cada material por el material de video
            }
            renderer.sharedMaterials = mats; // Asigna el nuevo arreglo de materiales al renderer
        }
    }

    void StartVideoPlayer() {
        // Obtén el componente VideoPlayer ya existente en este GameObject
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            Debug.LogError("No se encontró el componente VideoPlayer en el GameObject.");
            return;
        }

        // Configura el VideoPlayer
        // videoPlayer.playOnAwake = false;
        // videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
        // videoPlayer.targetMaterialRenderer = GetComponent<Renderer>();
        // videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        // videoPlayer.SetTargetAudioSource(0, GetComponent<AudioSource>());

        // Establece el clip de video
        SetVideoLocal(videoPaths[2]);

        // Opcional: Reproducir automáticamente
        videoPlayer.Play();
    }

    void SetVideoLocal(string path)
    {
        // https://docs.unity3d.com/Manual/StreamingAssets.html
        videoPlayer.url = System.IO.Path.Combine(Application.streamingAssetsPath, path);
    }

    void SetVideoApi(string urlVideo) {
        // string urlVideoApi = Uri.EscapeDataString(GetVideoApiManager().ConvertVideoUrlToMyApiServer(urlVideo));
        string urlVideoApi = GetVideoApiManager().ConvertVideoUrlToMyApiServer(urlVideo);
        videoPlayer.url = urlVideoApi;
        // videoPlayer.url = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";
        Debug.Log("CAMBIANDO A VIDEO: " + urlVideoApi);

    }

    void InitializeVideoApiObjects() {
        MeshRenderer parentRenderer = GetComponent<MeshRenderer>();
        MeshFilter parentFilter = GetComponent<MeshFilter>();
        for (int i = 0; i < videos.Count; i++)
        {
            // Debug.Log("Creando el gameobject para el video: " + videos[i]);
            // Crea un nuevo GameObject para cada video
            GameObject videoObject = new GameObject(videos[i]);
            videoObject.transform.SetParent(transform); // Establece el GameObject padre
            videoObject.transform.localPosition = Vector3.right * i * 2.0f; // Ajusta la posición relativa según sea necesario

            // Copia los componentes de renderizado del padre al hijo
            if (parentRenderer != null && parentFilter != null)
            {
                MeshRenderer childRenderer = videoObject.AddComponent<MeshRenderer>();
                MeshFilter childFilter = videoObject.AddComponent<MeshFilter>();
                childRenderer.material = parentRenderer.material;
                childFilter.mesh = parentFilter.mesh;
            }

            // Añade un Collider para detectar colisiones
            BoxCollider collider = videoObject.AddComponent<BoxCollider>();
            collider.isTrigger = true; // Hacer el collider un trigger si quieres evitar físicas reales

            // Añade el script de manejo de trigger a cada hijo
            videoObject.AddComponent<ChildTriggerHandler>();

        }
    }

    void InitializeVideoLocalObjects() {
        MeshRenderer parentRenderer = GetComponent<MeshRenderer>();
        MeshFilter parentFilter = GetComponent<MeshFilter>();
        for (int i = 0; i < videoPaths.Length; i++)
        {
            // Crea un nuevo GameObject para cada video
            GameObject videoObject = new GameObject(videoPaths[i]);
            videoObject.transform.SetParent(transform); // Establece el GameObject padre
            videoObject.transform.localPosition = Vector3.right * i * 2.0f; // Ajusta la posición relativa según sea necesario

            // Copia los componentes de renderizado del padre al hijo
            if (parentRenderer != null && parentFilter != null)
            {
                MeshRenderer childRenderer = videoObject.AddComponent<MeshRenderer>();
                MeshFilter childFilter = videoObject.AddComponent<MeshFilter>();
                childRenderer.material = parentRenderer.material;
                childFilter.mesh = parentFilter.mesh;
            }

            // Añade un Collider para detectar colisiones
            BoxCollider collider = videoObject.AddComponent<BoxCollider>();
            collider.isTrigger = true; // Hacer el collider un trigger si quieres evitar físicas reales

            // Añade el script de manejo de trigger a cada hijo
            videoObject.AddComponent<ChildTriggerHandler>();

            // Añade y configura el script VideoTrigger
            // VideoTrigger trigger = videoObject.AddComponent<VideoTrigger>();
            // trigger.videoPath = videoPaths[i];
            // trigger.videoPlayer = videoPlayer;
        }
    }

     // Esta función será llamada cuando ocurra una colisión con un trigger
    private void OnTriggerEnter(Collider other)
    {
        // string urlVideo = other.name;
        // SetVideoLocal(urlVideo);
        // Debug.Log("Colisiono con el objeto: " + urlVideo);
        // VideoTrigger trigger = other.GetComponent<VideoTrigger>();
        // if (trigger != null)
        // {
        //     trigger.PlayVideo();
        // }
    }

    // Este método será llamado por los hijos cuando detecten una colisión
    public void ChildCollided(GameObject child, Collider other)
    {
        Debug.Log(child.name + " colisionó con " + other.gameObject.name);
        string urlVideo = child.name;
        SetVideoApi(urlVideo);
        // Aquí puedes añadir más lógica basada en la colisión
    }


}




// OLD CODE
/*
    // public RenderTexture videoTexture; // Asigna aquí tu Render Texture


    // Start is called before the first frame update
    void Start()
    {
        ReplaceSceneMaterials(videoMaterial);
        // Material videoMaterial = new Material(Shader.Find("Unlit/Texture"));
        // videoMaterial.mainTexture = videoTexture; // Usa la Render Texture como la textura principal del material
        // ReplaceSceneMaterials(videoMaterial);
    }
*/