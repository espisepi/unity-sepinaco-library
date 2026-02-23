using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum SkyDomeRenderFace
{
    [Tooltip("Renderiza solo el interior (para verlo como cielo desde dentro).")]
    Inside,

    [Tooltip("Renderiza solo el exterior (renderizado estándar).")]
    Outside,

    [Tooltip("Renderiza ambas caras (interior y exterior).")]
    Both
}

[Serializable]
public class SkyDomeMeshEntry
{
    [Tooltip("Nombre descriptivo del mesh.")]
    public string name;

    [Tooltip("Referencia al Mesh.")]
    public Mesh mesh;

    [HideInInspector] public bool isBuiltIn;
}

/// <summary>
/// Gestiona un domo/cielo envolvente con mesh configurable, sin físicas.
///
/// Renderiza por defecto desde el interior para simular un cielo.
/// Permite cambiar la forma (esfera, cubo, cilindro…), aplicar rotación
/// continua, y modificar transform y visibilidad desde el Inspector.
///
/// Un Custom Editor (ScriptSkyDomeEditor) añade controles mejorados
/// para gestionar la lista de meshes disponibles (añadir/eliminar).
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ScriptSkyDome : MonoBehaviour
{
    [Header("Renderizado")]
    [Tooltip("Cara a renderizar: Interior (cielo), Exterior o Ambas. Por defecto: Interior.")]
    [SerializeField] private SkyDomeRenderFace _renderFace = SkyDomeRenderFace.Inside;

    [Header("Visibilidad")]
    [Tooltip("Mostrar u ocultar visualmente el domo en la escena.")]
    [SerializeField] private bool _isVisible = true;

    [Header("Transform del Domo")]
    [Tooltip("Posición del domo.")]
    [SerializeField] private Vector3 _domePosition = Vector3.zero;

    [Tooltip("Escala del domo. Valores grandes cubren todo el mapa.")]
    [SerializeField] private Vector3 _domeScale = Vector3.one * 1000f;

    [Tooltip("Rotación inicial del domo (ángulos Euler).")]
    [SerializeField] private Vector3 _domeRotation = Vector3.zero;

    [Header("Rotación Continua")]
    [Tooltip("Activar rotación continua del domo.")]
    [SerializeField] private bool _enableContinuousRotation;

    [Tooltip("Velocidad de rotación en grados por segundo.")]
    [SerializeField] private float _rotationSpeed = 10f;

    [Tooltip("Eje/dirección de la rotación continua.")]
    [SerializeField] private Vector3 _rotationAxis = Vector3.up;

    [Header("Forma (Mesh)")]
    [Tooltip("Índice del mesh seleccionado de la lista de meshes disponibles.")]
    [SerializeField] private int _selectedMeshIndex;

    [Tooltip("Lista de meshes disponibles. Incluye primitivas de Unity por defecto.")]
    [SerializeField] private List<SkyDomeMeshEntry> _meshEntries = new List<SkyDomeMeshEntry>();

    [Header("Material")]
    [Tooltip("Material personalizado (opcional). Si está vacío se usa el material SkyDome por defecto.")]
    [SerializeField] private Material _customMaterial;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Material _runtimeMaterial;
    private Material _trackedBaseMaterial;

    private static Dictionary<PrimitiveType, Mesh> _primitiveMeshCache;

    // ───────────────────────── Unity Lifecycle ─────────────────────────

    private void Awake()
    {
        CacheComponents();
        EnsureDefaultMeshEntries();
        RemovePhysicsComponents();
        ApplyAll();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (!_enableContinuousRotation) return;
        if (_rotationAxis == Vector3.zero) return;

        transform.Rotate(
            _rotationAxis.normalized * (_rotationSpeed * Time.deltaTime),
            Space.Self);
    }

    private void Reset()
    {
        CacheComponents();
        PopulateBuiltInMeshes();
        RemovePhysicsComponents();
        ApplyAll();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheComponents();

        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            ApplyAll();
        };
    }
#endif

    private void OnDestroy()
    {
        CleanupRuntimeMaterial();
    }

    // ───────────────────────── Initialization ─────────────────────────

    private void CacheComponents()
    {
        if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
        if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void RemovePhysicsComponents()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (Application.isPlaying) Destroy(rb);
            else DestroyImmediate(rb);
        }
    }

    private void EnsureDefaultMeshEntries()
    {
        if (_meshEntries != null && _meshEntries.Count > 0) return;
        PopulateBuiltInMeshes();
    }

    /// <summary>
    /// Rellena la lista con las primitivas por defecto de Unity.
    /// Borra cualquier entrada existente.
    /// </summary>
    public void PopulateBuiltInMeshes()
    {
        if (_meshEntries == null)
            _meshEntries = new List<SkyDomeMeshEntry>();

        _meshEntries.Clear();

        var defaults = new (PrimitiveType type, string label)[]
        {
            (PrimitiveType.Sphere,   "Esfera"),
            (PrimitiveType.Cube,     "Cubo"),
            (PrimitiveType.Cylinder, "Cilindro"),
            (PrimitiveType.Capsule,  "Cápsula"),
            (PrimitiveType.Plane,    "Plano"),
            (PrimitiveType.Quad,     "Quad"),
        };

        foreach (var (type, label) in defaults)
        {
            _meshEntries.Add(new SkyDomeMeshEntry
            {
                name      = label,
                mesh      = GetPrimitiveMesh(type),
                isBuiltIn = true,
            });
        }

        _selectedMeshIndex = 0;
    }

    /// <summary>
    /// Obtiene (y cachea) el mesh de una primitiva de Unity.
    /// </summary>
    public static Mesh GetPrimitiveMesh(PrimitiveType type)
    {
        if (_primitiveMeshCache == null)
            _primitiveMeshCache = new Dictionary<PrimitiveType, Mesh>();

        if (_primitiveMeshCache.TryGetValue(type, out Mesh cached) && cached != null)
            return cached;

        GameObject tmp = GameObject.CreatePrimitive(type);
        tmp.hideFlags = HideFlags.HideAndDontSave;
        Mesh mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
        _primitiveMeshCache[type] = mesh;

        if (Application.isPlaying) Destroy(tmp);
        else DestroyImmediate(tmp);

        return mesh;
    }

    // ───────────────────────── Apply Configuration ─────────────────────────

    /// <summary>Aplica todos los ajustes serializados al objeto.</summary>
    public void ApplyAll()
    {
        CacheComponents();
        ApplyTransform();
        ApplyMesh();
        ApplyMaterial();
        ApplyVisibility();
    }

    private void ApplyTransform()
    {
        transform.localPosition    = _domePosition;
        transform.localScale       = _domeScale;
        transform.localEulerAngles = _domeRotation;
    }

    private void ApplyMesh()
    {
        if (_meshFilter == null) return;

        if (_meshEntries == null || _meshEntries.Count == 0)
        {
            PopulateBuiltInMeshes();
            if (_meshEntries.Count == 0) return;
        }

        _selectedMeshIndex = Mathf.Clamp(_selectedMeshIndex, 0, _meshEntries.Count - 1);

        Mesh m = _meshEntries[_selectedMeshIndex].mesh;

        if (m == null && _meshEntries[_selectedMeshIndex].isBuiltIn)
        {
            PopulateBuiltInMeshes();
            _selectedMeshIndex = Mathf.Clamp(_selectedMeshIndex, 0, _meshEntries.Count - 1);
            m = _meshEntries[_selectedMeshIndex].mesh;
        }

        if (m != null)
            _meshFilter.sharedMesh = m;
    }

    private void ApplyMaterial()
    {
        if (_meshRenderer == null) return;

        Material baseMat = _customMaterial;

        if (_runtimeMaterial == null || _trackedBaseMaterial != baseMat)
        {
            CleanupRuntimeMaterial();

            if (baseMat != null)
            {
                _runtimeMaterial = new Material(baseMat);
            }
            else
            {
                Shader shader = Shader.Find("Sepinaco/SkyDome");
                if (shader == null)
                {
                    Debug.LogWarning(
                        "[ScriptSkyDome] Shader 'Sepinaco/SkyDome' no encontrado. " +
                        "Usando 'Sprites/Default' como fallback (Cull Off fijo).");
                    shader = Shader.Find("Sprites/Default");
                }
                if (shader == null)
                {
                    Debug.LogError("[ScriptSkyDome] No se encontró ningún shader compatible.");
                    return;
                }

                _runtimeMaterial = new Material(shader);
                _runtimeMaterial.color = new Color(0.53f, 0.72f, 0.96f);
            }

            _runtimeMaterial.name      = "SkyDome_Runtime";
            _runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
            _trackedBaseMaterial       = baseMat;
        }

        _meshRenderer.sharedMaterial = _runtimeMaterial;
        ApplyCullMode(_runtimeMaterial);
    }

    private void ApplyCullMode(Material mat)
    {
        if (mat == null || !mat.HasProperty("_Cull")) return;

        float cull;
        switch (_renderFace)
        {
            case SkyDomeRenderFace.Inside:  cull = 1f; break;
            case SkyDomeRenderFace.Outside: cull = 2f; break;
            case SkyDomeRenderFace.Both:    cull = 0f; break;
            default:                        cull = 1f; break;
        }

        mat.SetFloat("_Cull", cull);
    }

    private void ApplyVisibility()
    {
        if (_meshRenderer != null)
            _meshRenderer.enabled = _isVisible;
    }

    private void CleanupRuntimeMaterial()
    {
        if (_runtimeMaterial == null) return;

        if (Application.isPlaying) Destroy(_runtimeMaterial);
        else DestroyImmediate(_runtimeMaterial);

        _runtimeMaterial     = null;
        _trackedBaseMaterial = null;
    }

    // ───────────────────────── Public API ─────────────────────────

    public SkyDomeRenderFace RenderFace
    {
        get => _renderFace;
        set { _renderFace = value; ApplyMaterial(); }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set { _isVisible = value; ApplyVisibility(); }
    }

    public bool EnableContinuousRotation
    {
        get => _enableContinuousRotation;
        set => _enableContinuousRotation = value;
    }

    public float RotationSpeed
    {
        get => _rotationSpeed;
        set => _rotationSpeed = value;
    }

    public Vector3 RotationAxisDirection
    {
        get => _rotationAxis;
        set => _rotationAxis = value;
    }

    public int SelectedMeshIndex
    {
        get => _selectedMeshIndex;
        set { _selectedMeshIndex = value; ApplyMesh(); }
    }

    public List<SkyDomeMeshEntry> MeshEntries => _meshEntries;

    public void AddMeshEntry(string entryName, Mesh mesh)
    {
        if (_meshEntries == null)
            _meshEntries = new List<SkyDomeMeshEntry>();

        _meshEntries.Add(new SkyDomeMeshEntry
        {
            name      = entryName,
            mesh      = mesh,
            isBuiltIn = false,
        });
    }

    public bool RemoveMeshEntry(int index)
    {
        if (_meshEntries == null) return false;
        if ((uint)index >= (uint)_meshEntries.Count) return false;
        if (_meshEntries[index].isBuiltIn) return false;

        _meshEntries.RemoveAt(index);

        if (_selectedMeshIndex >= _meshEntries.Count)
            _selectedMeshIndex = Mathf.Max(0, _meshEntries.Count - 1);

        ApplyMesh();
        return true;
    }
}
