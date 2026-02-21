using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Crea un trigger físico invisible a una altura Y configurable.
/// Cuando un Rigidbody de la escena cae y entra en el trigger,
/// se restaura automáticamente a su posición y rotación originales.
///
/// Rendimiento: usa OnTriggerEnter (broadphase de PhysX), sin
/// comprobaciones por frame — solo se ejecuta código cuando un
/// objeto realmente cruza el umbral.
/// </summary>
public class TriggerRestoreManager : MonoBehaviour
{
    [Header("Umbral de caída")]
    [Tooltip("Posición en el eje Y por debajo de la cual un objeto será restaurado.")]
    [SerializeField] private float _fallThresholdY = -50f;

    [Header("Tamaño del trigger")]
    [Tooltip("Extensión del trigger en cada eje. " +
             "X/Z deben cubrir todo el mapa; Y es el grosor (más grueso = menos riesgo de tunneling).")]
    [SerializeField] private Vector3 _triggerSize = new Vector3(500f, 50f, 500f);

    [Header("Opciones de restauración")]
    [Tooltip("Restaurar también la rotación original del objeto.")]
    [SerializeField] private bool _restoreRotation = true;

    [Tooltip("Poner a cero la velocidad lineal y angular del Rigidbody al restaurar.")]
    [SerializeField] private bool _resetVelocity = true;

    [Header("Debug")]
    [Tooltip("Muestra el volumen del trigger y la línea del umbral en la vista Scene.")]
    [SerializeField] private bool _showTriggerGizmo = true;

    private struct OriginalPose
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private readonly Dictionary<GameObject, OriginalPose> _tracked = new Dictionary<GameObject, OriginalPose>();
    private BoxCollider _trigger;

    private void Awake()
    {
        ConfigureTrigger();
    }

    private void Start()
    {
        RegisterSceneRigidbodies();
        Debug.Log($"[TriggerRestoreManager] Rastreando {_tracked.Count} Rigidbodies · Umbral Y = {_fallThresholdY}");
    }

    private void ConfigureTrigger()
    {
        _trigger = GetComponent<BoxCollider>();
        if (_trigger == null)
            _trigger = gameObject.AddComponent<BoxCollider>();

        _trigger.isTrigger = true;
        SyncTriggerPosition();
    }

    private void SyncTriggerPosition()
    {
        if (_trigger == null) return;

        _trigger.size = _triggerSize;

        Vector3 worldCenter = new Vector3(
            transform.position.x,
            _fallThresholdY - _triggerSize.y * 0.5f,
            transform.position.z);

        _trigger.center = transform.InverseTransformPoint(worldCenter);
    }

    private void RegisterSceneRigidbodies()
    {
        foreach (Rigidbody rb in FindObjectsOfType<Rigidbody>())
        {
            if (rb.gameObject != gameObject)
                Register(rb.gameObject);
        }
    }

    private void Register(GameObject obj)
    {
        if (_tracked.ContainsKey(obj)) return;

        _tracked[obj] = new OriginalPose
        {
            Position = obj.transform.position,
            Rotation = obj.transform.rotation
        };
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        GameObject target = rb != null ? rb.gameObject : other.gameObject;

        if (_tracked.TryGetValue(target, out OriginalPose pose))
            Restore(target, rb, pose);
    }

    private void Restore(GameObject obj, Rigidbody rb, OriginalPose pose)
    {
        obj.transform.position = pose.Position;

        if (_restoreRotation)
            obj.transform.rotation = pose.Rotation;

        if (_resetVelocity && rb != null)
        {
            // unity updated: rb.linearVelocity = Vector3.zero;
            rb.velocity = Vector3.zero;
            
            rb.angularVelocity = Vector3.zero;
        }
    }

    // ──────────────── API pública ────────────────

    /// <summary>Registra un objeto en runtime para que sea rastreado.</summary>
    public void RegisterAtRuntime(GameObject obj)
    {
        if (obj != null && obj != gameObject)
            Register(obj);
    }

    /// <summary>Cambia el umbral Y en runtime y reposiciona el trigger.</summary>
    public void SetThreshold(float newY)
    {
        _fallThresholdY = newY;
        SyncTriggerPosition();
    }

    // ──────────────── Editor ────────────────

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_trigger != null)
            SyncTriggerPosition();
    }
#endif

    private void OnDrawGizmos()
    {
        if (!_showTriggerGizmo) return;

        Vector3 center = new Vector3(
            transform.position.x,
            _fallThresholdY - _triggerSize.y * 0.5f,
            transform.position.z);

        Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.18f);
        Gizmos.DrawCube(center, _triggerSize);

        Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.7f);
        Gizmos.DrawWireCube(center, _triggerSize);

        Gizmos.color = Color.yellow;
        Vector3 lineCenter = new Vector3(transform.position.x, _fallThresholdY, transform.position.z);
        float half = _triggerSize.x * 0.5f;
        Gizmos.DrawLine(lineCenter + Vector3.left * half, lineCenter + Vector3.right * half);
    }
}
