using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections.Generic;
using Sepinaco.Singleton;

// Este script aka manager singletone es usado internamente en ScriptVideoclip.cs

// TODO: Cambiar la palabra Manager por Services
namespace Sepinaco.VideoApiManager
{
public class VideoApiManager : Singleton<VideoApiManager>
{

    public string pathMyServer = "https://sepinaco.com:3000";

    public string apiUrl = "https://sepinaco.com:3000/media-list";

    // Lista de strings
    private List<string> videos;

    void Start()
    {
        // Inicialización de la lista de strings
        videos = new List<string>();
        StartCoroutine(GetVideoDataCoroutine());
    }

    IEnumerator GetVideoDataCoroutine()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching data: " + request.error);
            }
            else
            {
                ProcessVideos(request.downloadHandler.text);
            }
        }
    }

    void ProcessVideos(string jsonData)
    {
        VideoData videoData = JsonUtility.FromJson<VideoData>(jsonData);
        foreach (string videoFile in videoData.mediaFiles)
        {
            // Debug.Log("OYEEEEEEEEEE" + videoFile);
            videos.Add(videoFile);
        }
    }


    [System.Serializable]
    public class VideoData
    {
        public string[] mediaFiles;
    }

    public List<string> GetVideos() {
        return videos;
    }

    public string ConvertVideoUrlToMyApiServer(string url)
    {
        return pathMyServer + "/media/" + url;
    }

}

}