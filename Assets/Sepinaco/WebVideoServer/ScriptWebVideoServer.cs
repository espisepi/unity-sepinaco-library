using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Video;

public class ScriptWebVideoServer : MonoBehaviour
{
    [Header("Información de conexión")]
    [Tooltip("URL del servidor (se rellena automáticamente al iniciar)")]
    public string serverURL = "";

    [Header("Servidor Web")]
    [Tooltip("Puerto del servidor web local")]
    [SerializeField] private int port = 8080;

    [Tooltip("Mostrar información del servidor en pantalla (OnGUI)")]
    [SerializeField] private bool showServerInfo = true;

    [Header("Almacenamiento")]
    [Tooltip("Carpeta donde se guardan los vídeos subidos (relativa a persistentDataPath)")]
    [SerializeField] private string uploadFolder = "UploadedVideos";

    [Header("Búsqueda de VideoPlayer")]
    [Tooltip("Buscar VideoPlayer/ScriptVideoclipsManager automáticamente si no se encuentra al inicio")]
    public bool searchVideoPlayer = true;

    [Tooltip("Intervalo en segundos entre cada intento de búsqueda")]
    public float searchInterval = 1f;

    private float searchTimer;

    private HttpListener httpListener;
    private Thread listenerThread;
    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    private ScriptVideoclipsManager videoclipsManager;
    private VideoPlayer videoPlayer;

    private string[] localIPs = Array.Empty<string>();
    private bool serverRunning;
    private string uploadPath;

    private volatile string cachedVideoName = "---";
    private volatile string serverStatusText = "Iniciando...";

    private GUIStyle guiBoxStyle;
    private GUIStyle guiTitleStyle;
    private GUIStyle guiLabelStyle;
    private bool guiStylesInit;

    void Start()
    {
        uploadPath = Path.Combine(Application.persistentDataPath, uploadFolder);
        if (!Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        localIPs = GetLocalIPAddresses();
        StartServer();
        TryFindVideoPlayer();
    }

    void Update()
    {
        while (mainThreadActions.TryDequeue(out Action action))
        {
            try { action?.Invoke(); }
            catch (Exception e) { Debug.LogError($"[WebVideoServer] Main thread action error: {e.Message}"); }
        }

        if (videoPlayer == null && searchVideoPlayer)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchInterval)
            {
                searchTimer = 0f;
                TryFindVideoPlayer();
            }
        }

        if (videoclipsManager != null)
            cachedVideoName = videoclipsManager.CurrentVideoDisplayName;
    }

    void TryFindVideoPlayer()
    {
        videoclipsManager = FindObjectOfType<ScriptVideoclipsManager>();
        if (videoclipsManager != null)
            videoPlayer = videoclipsManager.GetComponent<VideoPlayer>();

        if (videoPlayer == null)
        {
            videoPlayer = FindObjectOfType<VideoPlayer>();
            if (videoPlayer != null)
                Debug.LogWarning("[WebVideoServer] No se encontró ScriptVideoclipsManager. Usando VideoPlayer de la escena.");
        }

        if (videoPlayer != null)
        {
            Debug.Log("[WebVideoServer] VideoPlayer encontrado.");
            serverStatusText = "Activo";
        }
    }

    void OnDestroy()
    {
        StopServer();
    }

    void OnApplicationQuit()
    {
        StopServer();
    }

    #region Server Lifecycle

    void StartServer()
    {
        try
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://*:{port}/");
            httpListener.Start();

            serverRunning = true;
            serverStatusText = "Activo";

            listenerThread = new Thread(ListenLoop) { IsBackground = true, Name = "WebVideoServer" };
            listenerThread.Start();

            serverURL = localIPs.Length > 0 ? $"http://{localIPs[0]}:{port}" : $"http://localhost:{port}";

            Debug.Log($"[WebVideoServer] ============================");
            Debug.Log($"[WebVideoServer] Servidor web iniciado en puerto {port}");
            Debug.Log($"[WebVideoServer] ----------------------------");
            foreach (string ip in localIPs)
                Debug.Log($"[WebVideoServer]   http://{ip}:{port}");
            Debug.Log($"[WebVideoServer] ----------------------------");
            Debug.Log($"[WebVideoServer] Carpeta de vídeos: {uploadPath}");
            Debug.Log($"[WebVideoServer] ============================");
        }
        catch (Exception e)
        {
            serverStatusText = $"Error: {e.Message}";
            Debug.LogError($"[WebVideoServer] Error al iniciar servidor: {e.Message}");
            Debug.LogError("[WebVideoServer] En macOS/Linux, asegúrate de que el puerto no esté en uso. En Windows, puede requerir permisos de administrador.");
        }
    }

    void StopServer()
    {
        serverRunning = false;

        if (httpListener != null)
        {
            try { httpListener.Stop(); } catch { }
            try { httpListener.Close(); } catch { }
            httpListener = null;
        }

        if (listenerThread != null && listenerThread.IsAlive)
        {
            listenerThread.Join(2000);
            listenerThread = null;
        }

        Debug.Log("[WebVideoServer] Servidor detenido.");
    }

    void ListenLoop()
    {
        while (serverRunning && httpListener != null && httpListener.IsListening)
        {
            try
            {
                HttpListenerContext ctx = httpListener.GetContext();
                ThreadPool.QueueUserWorkItem(_ => ProcessRequest(ctx));
            }
            catch (HttpListenerException) { }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }
    }

    #endregion

    #region Request Routing

    void ProcessRequest(HttpListenerContext ctx)
    {
        try
        {
            string path = ctx.Request.Url.AbsolutePath;
            string method = ctx.Request.HttpMethod;

            if (method == "GET" && path == "/")
                ServeHtmlPage(ctx);
            else if (method == "GET" && path == "/api/status")
                ServeStatus(ctx);
            else if (method == "GET" && path == "/api/videos")
                ServeVideoList(ctx);
            else if (method == "POST" && path == "/api/upload")
                HandleUpload(ctx);
            else if (method == "POST" && path == "/api/play")
                HandlePlay(ctx);
            else
                SendJson(ctx, 404, "{\"error\":\"Not found\"}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebVideoServer] Request error: {e.Message}");
            try { SendJson(ctx, 500, "{\"error\":\"Internal server error\"}"); } catch { }
        }
    }

    #endregion

    #region Endpoints

    void ServeHtmlPage(HttpListenerContext ctx)
    {
        string html = BuildHtmlPage();
        byte[] data = Encoding.UTF8.GetBytes(html);
        ctx.Response.ContentType = "text/html; charset=utf-8";
        ctx.Response.ContentLength64 = data.Length;
        ctx.Response.OutputStream.Write(data, 0, data.Length);
        ctx.Response.Close();
    }

    void ServeStatus(HttpListenerContext ctx)
    {
        string json = "{\"currentVideo\":\"" + JsonEscape(cachedVideoName) + "\",\"status\":\"" + JsonEscape(serverStatusText) + "\"}";
        SendJson(ctx, 200, json);
    }

    void ServeVideoList(HttpListenerContext ctx)
    {
        var sb = new StringBuilder("{\"videos\":[");
        try
        {
            if (Directory.Exists(uploadPath))
            {
                string[] files = Directory.GetFiles(uploadPath);
                Array.Sort(files);
                for (int i = 0; i < files.Length; i++)
                {
                    string name = Path.GetFileName(files[i]);
                    if (i > 0) sb.Append(",");
                    sb.Append("\"").Append(JsonEscape(name)).Append("\"");
                }
            }
        }
        catch { }
        sb.Append("]}");
        SendJson(ctx, 200, sb.ToString());
    }

    void HandleUpload(HttpListenerContext ctx)
    {
        string rawFilename = ctx.Request.Headers["X-Filename"];
        if (string.IsNullOrEmpty(rawFilename))
        {
            SendJson(ctx, 400, "{\"success\":false,\"message\":\"Falta el nombre del archivo (header X-Filename)\"}");
            return;
        }

        string filename = Uri.UnescapeDataString(rawFilename);
        filename = SanitizeFilename(filename);

        if (string.IsNullOrEmpty(filename))
        {
            SendJson(ctx, 400, "{\"success\":false,\"message\":\"Nombre de archivo inválido\"}");
            return;
        }

        string filePath = Path.Combine(uploadPath, filename);

        try
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = new byte[65536];
                int bytesRead;
                while ((bytesRead = ctx.Request.InputStream.Read(buffer, 0, buffer.Length)) > 0)
                    fs.Write(buffer, 0, bytesRead);
            }

            Debug.Log($"[WebVideoServer] Vídeo recibido: {filename} ({new FileInfo(filePath).Length} bytes)");

            mainThreadActions.Enqueue(() => PlayUploadedVideo(filePath, filename));

            SendJson(ctx, 200, "{\"success\":true,\"message\":\"Vídeo subido y reproduciéndose\",\"filename\":\"" + JsonEscape(filename) + "\"}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebVideoServer] Error guardando archivo: {e.Message}");
            SendJson(ctx, 500, "{\"success\":false,\"message\":\"Error al guardar el archivo\"}");
        }
    }

    void HandlePlay(HttpListenerContext ctx)
    {
        string filename = ctx.Request.QueryString["filename"];
        if (string.IsNullOrEmpty(filename))
        {
            SendJson(ctx, 400, "{\"success\":false,\"message\":\"Falta el parámetro filename\"}");
            return;
        }

        filename = Uri.UnescapeDataString(filename);
        string filePath = Path.Combine(uploadPath, SanitizeFilename(filename));

        if (!File.Exists(filePath))
        {
            SendJson(ctx, 404, "{\"success\":false,\"message\":\"Archivo no encontrado\"}");
            return;
        }

        mainThreadActions.Enqueue(() => PlayUploadedVideo(filePath, Path.GetFileName(filePath)));

        SendJson(ctx, 200, "{\"success\":true,\"filename\":\"" + JsonEscape(Path.GetFileName(filePath)) + "\"}");
    }

    #endregion

    #region Video Playback (Main Thread)

    void PlayUploadedVideo(string filePath, string displayName)
    {
        if (videoclipsManager != null)
        {
            videoclipsManager.PlayVideoFromUrl(filePath, displayName);
        }
        else if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = filePath;
            videoPlayer.Play();
            Debug.Log($"[WebVideoServer] Reproduciendo vídeo: {displayName}");
        }
    }

    #endregion

    #region Helpers

    void SendJson(HttpListenerContext ctx, int statusCode, string json)
    {
        byte[] data = Encoding.UTF8.GetBytes(json);
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
        ctx.Response.ContentLength64 = data.Length;
        ctx.Response.OutputStream.Write(data, 0, data.Length);
        ctx.Response.Close();
    }

    static string JsonEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    static string SanitizeFilename(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        char[] invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (char c in name)
        {
            if (Array.IndexOf(invalid, c) < 0)
                sb.Append(c);
            else
                sb.Append('_');
        }
        return sb.ToString().Trim();
    }

    string[] GetLocalIPAddresses()
    {
        var ips = new List<string>();
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        ips.Add(addr.Address.ToString());
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[WebVideoServer] Error obteniendo IPs: {e.Message}");
        }

        if (ips.Count == 0)
            ips.Add("127.0.0.1");

        return ips.ToArray();
    }

    #endregion

    #region OnGUI

    void OnGUI()
    {
        if (!showServerInfo || !serverRunning) return;

        InitGuiStyles();

        float w = 320f;
        float lineH = 22f;
        int lines = 3 + localIPs.Length;
        float h = 16f + lines * lineH + 12f;
        Rect box = new Rect(Screen.width - w - 10f, Screen.height - h - 10f, w, h);

        GUI.Box(box, GUIContent.none, guiBoxStyle);

        float y = box.y + 8f;
        float x = box.x + 12f;
        float lw = w - 24f;

        GUI.Label(new Rect(x, y, lw, lineH), "Web Video Server", guiTitleStyle);
        y += lineH;

        GUI.Label(new Rect(x, y, lw, lineH), $"Estado: {serverStatusText}  |  Puerto: {port}", guiLabelStyle);
        y += lineH;

        foreach (string ip in localIPs)
        {
            GUI.Label(new Rect(x, y, lw, lineH), $"  http://{ip}:{port}", guiLabelStyle);
            y += lineH;
        }

        GUI.Label(new Rect(x, y, lw, lineH), $"Video: {cachedVideoName}", guiLabelStyle);
    }

    void InitGuiStyles()
    {
        if (guiStylesInit) return;

        Texture2D bg = new Texture2D(1, 1);
        bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.7f));
        bg.Apply();

        guiBoxStyle = new GUIStyle(GUI.skin.box);
        guiBoxStyle.normal.background = bg;

        guiTitleStyle = new GUIStyle(GUI.skin.label);
        guiTitleStyle.fontSize = 13;
        guiTitleStyle.fontStyle = FontStyle.Bold;
        guiTitleStyle.normal.textColor = new Color(0.3f, 0.8f, 0.65f);

        guiLabelStyle = new GUIStyle(GUI.skin.label);
        guiLabelStyle.fontSize = 11;
        guiLabelStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

        guiStylesInit = true;
    }

    #endregion

    #region HTML Page

    string BuildHtmlPage()
    {
        var connHtml = new StringBuilder();
        for (int i = 0; i < localIPs.Length; i++)
        {
            string url = $"http://{localIPs[i]}:{port}";
            connHtml.Append("<div class=\"info-item\">");
            connHtml.Append("<span class=\"info-label\">").Append(i == 0 ? "IP Principal" : $"IP {i + 1}").Append("</span>");
            connHtml.Append("<span class=\"info-value\">").Append(url).Append("</span>");
            connHtml.Append("</div>");
        }

        return HTML_TEMPLATE
            .Replace("{{CONNECTION_INFO}}", connHtml.ToString())
            .Replace("{{PORT}}", port.ToString());
    }

    private const string HTML_TEMPLATE = @"<!DOCTYPE html>
<html lang=""es"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width,initial-scale=1.0,maximum-scale=1.0,user-scalable=no"">
<title>Sepinaco - Video Controller</title>
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background:#0f0f1a;color:#e0e0e0;min-height:100vh;padding:16px}
.container{max-width:560px;margin:0 auto}
header{text-align:center;margin-bottom:20px}
header h1{font-size:1.4rem;color:#fff;margin-bottom:8px;letter-spacing:-0.5px}
.status-badge{display:inline-block;padding:4px 14px;border-radius:20px;font-size:.75rem;font-weight:600}
.status-ok{background:rgba(78,204,163,.12);color:#4ecca3;border:1px solid rgba(78,204,163,.3)}
.status-err{background:rgba(233,69,96,.12);color:#e94560;border:1px solid rgba(233,69,96,.3)}
.card{background:#1a1a2e;border-radius:12px;padding:18px;margin-bottom:14px;border:1px solid #2a2a3e}
.card h2{font-size:.95rem;color:#fff;margin-bottom:10px}
.info-item{display:flex;justify-content:space-between;align-items:center;padding:7px 0;border-bottom:1px solid #252540}
.info-item:last-child{border-bottom:none}
.info-label{color:#888;font-size:.82rem}
.info-value{font-family:'SF Mono','Fira Code',monospace;color:#4ecca3;font-size:.82rem;word-break:break-all}
.current-video{text-align:center;padding:14px;background:rgba(233,69,96,.08);border-radius:8px;border:1px solid rgba(233,69,96,.18)}
.current-video .label{color:#888;font-size:.78rem}
.current-video .name{color:#e94560;font-size:1.05rem;font-weight:600;margin-top:4px;word-break:break-all}
.upload-zone{border:2px dashed #3a3a5e;border-radius:12px;padding:36px 16px;text-align:center;cursor:pointer;transition:all .25s}
.upload-zone:hover,.upload-zone.dragover{border-color:#e94560;background:rgba(233,69,96,.04)}
.upload-zone .icon{font-size:2.2rem;margin-bottom:8px}
.upload-zone p{color:#777;font-size:.88rem}
.file-info{display:flex;justify-content:space-between;padding:10px 12px;background:#16213e;border-radius:8px;margin-top:10px;font-size:.82rem}
.file-name{color:#fff;font-weight:500;word-break:break-all}
.file-size{color:#888;white-space:nowrap;margin-left:12px}
.btn{width:100%;padding:13px;border:none;border-radius:8px;font-size:.95rem;font-weight:600;cursor:pointer;margin-top:10px;transition:all .25s}
.btn-primary{background:#e94560;color:#fff}
.btn-primary:hover:not(:disabled){background:#d63851;transform:translateY(-1px)}
.btn-primary:disabled{background:#2a2a3e;color:#555;cursor:not-allowed}
.btn-secondary{background:#16213e;color:#4ecca3;border:1px solid #2a2a3e}
.btn-secondary:hover{background:#1a2744}
.progress-wrap{margin-top:10px;background:#16213e;border-radius:8px;overflow:hidden;position:relative;height:34px}
.progress-bar{height:100%;background:linear-gradient(90deg,#e94560,#0f3460);width:0;transition:width .3s;border-radius:8px}
.progress-text{position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);font-size:.82rem;font-weight:600;color:#fff}
.msg{margin-top:10px;padding:10px;border-radius:8px;font-size:.82rem;text-align:center}
.msg.ok{background:rgba(78,204,163,.12);color:#4ecca3;border:1px solid rgba(78,204,163,.25)}
.msg.err{background:rgba(233,69,96,.12);color:#e94560;border:1px solid rgba(233,69,96,.25)}
.vlist{list-style:none}
.vlist li{display:flex;justify-content:space-between;align-items:center;padding:9px 12px;background:#16213e;border-radius:8px;margin-bottom:6px;transition:all .2s}
.vlist li:hover{background:#1a2744}
.vlist .vname{color:#ddd;font-size:.85rem;word-break:break-all;flex:1;margin-right:10px}
.vlist .play-btn{background:#e94560;color:#fff;border:none;padding:5px 14px;border-radius:6px;font-size:.78rem;cursor:pointer;white-space:nowrap}
.vlist .play-btn:hover{background:#d63851}
</style>
</head>
<body>
<div class=""container"">
<header>
<h1>Sepinaco Video Controller</h1>
<div class=""status-badge status-ok"" id=""statusBadge"">Conectado</div>
</header>

<div class=""card"">
<h2>Conexi&oacute;n</h2>
{{CONNECTION_INFO}}
<div class=""info-item"">
<span class=""info-label"">Puerto</span>
<span class=""info-value"">{{PORT}}</span>
</div>
</div>

<div class=""card"">
<h2>V&iacute;deo Actual</h2>
<div class=""current-video"">
<div class=""label"">Reproduciendo ahora</div>
<div class=""name"" id=""curVideo"">Cargando...</div>
</div>
</div>

<div class=""card"">
<h2>Subir V&iacute;deo</h2>
<div class=""upload-zone"" id=""dropZone"">
<div class=""icon"">&#127909;</div>
<p>Arrastra un v&iacute;deo aqu&iacute; o pulsa para seleccionar</p>
</div>
<input type=""file"" id=""fileIn"" accept=""video/*"" style=""display:none"">
<div class=""file-info"" id=""fileInfo"" style=""display:none"">
<span class=""file-name"" id=""fName""></span>
<span class=""file-size"" id=""fSize""></span>
</div>
<button class=""btn btn-primary"" id=""uploadBtn"" disabled>Subir y Reproducir</button>
<div class=""progress-wrap"" id=""progWrap"" style=""display:none"">
<div class=""progress-bar"" id=""progBar""></div>
<span class=""progress-text"" id=""progText"">0%</span>
</div>
<div id=""msg""></div>
</div>

<div class=""card"" id=""histCard"" style=""display:none"">
<h2>V&iacute;deos Subidos</h2>
<ul class=""vlist"" id=""vList""></ul>
</div>
</div>

<script>
(function(){
var dropZone=document.getElementById('dropZone'),
    fileIn=document.getElementById('fileIn'),
    fileInfo=document.getElementById('fileInfo'),
    fName=document.getElementById('fName'),
    fSize=document.getElementById('fSize'),
    uploadBtn=document.getElementById('uploadBtn'),
    progWrap=document.getElementById('progWrap'),
    progBar=document.getElementById('progBar'),
    progText=document.getElementById('progText'),
    msgDiv=document.getElementById('msg'),
    curVideo=document.getElementById('curVideo'),
    histCard=document.getElementById('histCard'),
    vList=document.getElementById('vList'),
    statusBadge=document.getElementById('statusBadge'),
    selFile=null;

dropZone.addEventListener('click',function(){fileIn.click()});
dropZone.addEventListener('dragover',function(e){e.preventDefault();dropZone.classList.add('dragover')});
dropZone.addEventListener('dragleave',function(){dropZone.classList.remove('dragover')});
dropZone.addEventListener('drop',function(e){
    e.preventDefault();dropZone.classList.remove('dragover');
    if(e.dataTransfer.files.length>0)pickFile(e.dataTransfer.files[0]);
});
fileIn.addEventListener('change',function(e){if(e.target.files.length>0)pickFile(e.target.files[0])});

function pickFile(f){
    selFile=f;
    fName.textContent=f.name;
    fSize.textContent=fmtBytes(f.size);
    fileInfo.style.display='flex';
    uploadBtn.disabled=false;
    msgDiv.innerHTML='';
}

uploadBtn.addEventListener('click',doUpload);

function doUpload(){
    if(!selFile)return;
    uploadBtn.disabled=true;
    progWrap.style.display='block';
    msgDiv.innerHTML='';
    var xhr=new XMLHttpRequest();
    xhr.upload.onprogress=function(e){
        if(e.lengthComputable){
            var p=Math.round(e.loaded/e.total*100);
            progBar.style.width=p+'%';
            progText.textContent=p+'%';
        }
    };
    xhr.onload=function(){
        if(xhr.status===200){
            try{var r=JSON.parse(xhr.responseText);showMsg('ok',r.message||'Subido')}
            catch(ex){showMsg('ok','Subido')}
            poll();loadVideos();
        }else{showMsg('err','Error al subir')}
        resetUI();
    };
    xhr.onerror=function(){showMsg('err','Error de conexi\u00f3n');resetUI()};
    xhr.open('POST','/api/upload');
    xhr.setRequestHeader('X-Filename',encodeURIComponent(selFile.name));
    xhr.send(selFile);
}

function resetUI(){
    setTimeout(function(){
        progWrap.style.display='none';
        progBar.style.width='0%';
        progText.textContent='0%';
        fileInfo.style.display='none';
        selFile=null;
        uploadBtn.disabled=true;
    },800);
}

function showMsg(t,m){msgDiv.innerHTML='<div class=""msg '+t+'"">'+m+'</div>'}

function fmtBytes(b){
    if(b===0)return '0 B';
    var k=1024,s=['B','KB','MB','GB'],i=Math.floor(Math.log(b)/Math.log(k));
    return parseFloat((b/Math.pow(k,i)).toFixed(1))+' '+s[i];
}

function poll(){
    fetch('/api/status').then(function(r){return r.json()}).then(function(d){
        curVideo.textContent=d.currentVideo||'---';
        statusBadge.textContent='Conectado';
        statusBadge.className='status-badge status-ok';
    }).catch(function(){
        statusBadge.textContent='Desconectado';
        statusBadge.className='status-badge status-err';
    });
}

function loadVideos(){
    fetch('/api/videos').then(function(r){return r.json()}).then(function(d){
        if(d.videos&&d.videos.length>0){
            histCard.style.display='block';
            vList.innerHTML=d.videos.map(function(v){
                return '<li><span class=""vname"">'+esc(v)+'</span><button class=""play-btn"" onclick=""playV(\''+encodeURIComponent(v)+'\')"">Reproducir</button></li>';
            }).join('');
        }else{histCard.style.display='none'}
    }).catch(function(){});
}

window.playV=function(fn){
    fetch('/api/play?filename='+fn,{method:'POST'}).then(function(r){return r.json()}).then(function(d){
        if(d.success){showMsg('ok','Reproduciendo: '+decodeURIComponent(fn));poll()}
        else{showMsg('err',d.message||'Error')}
    }).catch(function(){showMsg('err','Error de conexi\u00f3n')});
};

function esc(s){var d=document.createElement('div');d.textContent=s;return d.innerHTML}

poll();loadVideos();
setInterval(poll,3000);
})();
</script>
</body>
</html>";

    #endregion
}
