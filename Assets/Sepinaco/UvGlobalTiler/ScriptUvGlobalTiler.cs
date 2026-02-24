using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Recorre todos los renderers de la escena y aplica un tiling UV global
/// sobre propiedades de textura configurables del material.
/// </summary>
public class ScriptUvGlobalTiler : MonoBehaviour
{
    private struct UvSnapshot
    {
        public Material material;
        public string propertyName;
        public Vector2 scale;
        public Vector2 offset;
    }

    [Header("Aplicacion")]
    [Tooltip("Aplica el tiling automaticamente al iniciar la escena.")]
    [SerializeField] private bool applyOnStart = true;

    [Tooltip("Incluye GameObjects inactivos al buscar renderers.")]
    [SerializeField] private bool includeInactive = true;

    [Tooltip("Modifica sharedMaterials (afecta todos los objetos que compartan material).")]
    [SerializeField] private bool useSharedMaterials = true;

    [Header("UV / Tiling")]
    [Tooltip("Valor de tiling que se aplicara. Alto por defecto para ver mosaicos.")]
    [SerializeField] private Vector2 uvTiling = new Vector2(20f, 20f);

    [Tooltip("Offset UV que se aplicara a las propiedades configuradas.")]
    [SerializeField] private Vector2 uvOffset = Vector2.zero;

    [Tooltip("Nombres de propiedades de textura del shader a modificar.")]
    [SerializeField] private string[] texturePropertyNames =
    {
        "_MainTex",
        "_BaseMap",
        "_BaseColorMap"
    };

    [Tooltip("Forzar WrapMode.Repeat para que el tiling se vea en mosaico y no estirado por clamping.")]
    [SerializeField] private bool forceRepeatWrapMode = true;

    [Header("Acciones desde inspectors")]
    [Tooltip("Poner en True para aplicar UV desde ScriptDebugInspector. Se auto-resetea.")]
    [SerializeField] private bool triggerApply = false;

    [Tooltip("Poner en True para restaurar UV originales desde ScriptDebugInspector. Se auto-resetea.")]
    [SerializeField] private bool triggerRestore = false;

    private readonly Dictionary<string, UvSnapshot> _originalUv = new Dictionary<string, UvSnapshot>();

    private void Start()
    {
        if (!applyOnStart)
            return;

        ApplyUvToScene();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        if (triggerApply)
        {
            triggerApply = false;
            ApplyUvToScene();
        }

        if (triggerRestore)
        {
            triggerRestore = false;
            RestoreOriginalUv();
        }
    }

    [ContextMenu("Apply UV Tiling To Scene")]
    public void ApplyUvToScene()
    {
        Renderer[] renderers = FindObjectsOfType<Renderer>(includeInactive);
        int changedMaterials = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererRef = renderers[i];
            Material[] materials = useSharedMaterials ? rendererRef.sharedMaterials : rendererRef.materials;

            for (int m = 0; m < materials.Length; m++)
            {
                Material materialRef = materials[m];
                if (materialRef == null)
                    continue;

                bool materialChanged = false;

                for (int p = 0; p < texturePropertyNames.Length; p++)
                {
                    string propertyName = texturePropertyNames[p];
                    if (string.IsNullOrWhiteSpace(propertyName))
                        continue;

                    if (!materialRef.HasProperty(propertyName))
                        continue;

                    Texture textureRef = materialRef.GetTexture(propertyName);
                    if (forceRepeatWrapMode && textureRef != null)
                    {
                        textureRef.wrapMode = TextureWrapMode.Repeat;
                    }

                    string key = BuildKey(materialRef, propertyName);
                    if (!_originalUv.ContainsKey(key))
                    {
                        _originalUv[key] = new UvSnapshot
                        {
                            material = materialRef,
                            propertyName = propertyName,
                            scale = materialRef.GetTextureScale(propertyName),
                            offset = materialRef.GetTextureOffset(propertyName)
                        };
                    }

                    materialRef.SetTextureScale(propertyName, uvTiling);
                    materialRef.SetTextureOffset(propertyName, uvOffset);
                    materialChanged = true;
                }

                if (materialChanged)
                    changedMaterials++;
            }
        }

        Debug.Log(
            $"[ScriptUvGlobalTiler] UV tiling aplicado. Renderers encontrados: {renderers.Length}. Materiales modificados: {changedMaterials}.",
            this
        );
    }

    [ContextMenu("Restore Original UV")]
    public void RestoreOriginalUv()
    {
        int restoredCount = 0;

        foreach (var kv in _originalUv)
        {
            UvSnapshot snapshot = kv.Value;
            if (snapshot.material == null)
                continue;
            if (!snapshot.material.HasProperty(snapshot.propertyName))
                continue;

            snapshot.material.SetTextureScale(snapshot.propertyName, snapshot.scale);
            snapshot.material.SetTextureOffset(snapshot.propertyName, snapshot.offset);
            restoredCount++;
        }

        Debug.Log(
            $"[ScriptUvGlobalTiler] UV restaurado. Propiedades restauradas: {restoredCount}.",
            this
        );
    }

    private static string BuildKey(Material materialRef, string propertyName)
    {
        return materialRef.GetInstanceID() + "|" + propertyName;
    }
}
