using System;
using UnityEngine;

/// <summary>
/// Representa un GameObject cuyas físicas (colliders) se pueden activar/desactivar
/// desde el Inspector. Los colliders se cachean en Awake para evitar allocations en runtime.
/// </summary>
[Serializable]
public class PhysicsTarget
{
    [Tooltip("GameObject objetivo. Sus colliders y los de todos sus hijos serán gestionados.")]
    public GameObject target;

    [Tooltip("Marca/desmarca para activar o desactivar los colliders en runtime.")]
    public bool collidersEnabled;

    /// <summary>Estado del frame anterior para detectar cambios sin eventos.</summary>
    [HideInInspector] public bool previousState;

    /// <summary>
    /// Array plano con todos los Collider del target y sus hijos, cacheado en Awake.
    /// Evita llamadas a GetComponents y recursión en cada frame.
    /// </summary>
    [NonSerialized] public Collider[] cachedColliders;
}

/// <summary>
/// Gestiona la activación/desactivación de colliders de múltiples GameObjects de la escena.
/// 
/// Uso:
///   1. Añadir este componente a un GameObject vacío.
///   2. Arrastrar GameObjects al array "Targets" en el Inspector.
///   3. Togglear "collidersEnabled" por entrada, o usar "Enable All" / "Disable All".
///   4. Si se modifican hijos en runtime, llamar a RefreshCache().
///
/// Performance:
///   - Los colliders se cachean en Awake (una sola allocation por target).
///   - El Update solo compara bools; no genera GC ni hace recursión.
///   - SetColliders itera un array plano pre-cacheado.
/// </summary>
public class ScriptPhysicsManager : MonoBehaviour
{
    [Header("Physics Manager")]
    [Tooltip("Arrastra aquí los GameObjects cuyas físicas quieres gestionar")]
    [SerializeField] private PhysicsTarget[] _targets = Array.Empty<PhysicsTarget>();

    [Header("Control Global")]
    [Tooltip("Pulsa para activar todos los colliders de golpe (se auto-resetea)")]
    [SerializeField] private bool _enableAll;

    [Tooltip("Pulsa para desactivar todos los colliders de golpe (se auto-resetea)")]
    [SerializeField] private bool _disableAll;

    /// <summary>Array vacío compartido para targets sin GameObject asignado. Evita allocations.</summary>
    private static readonly Collider[] EmptyColliders = Array.Empty<Collider>();

    /// <summary>
    /// Cachea todos los colliders de cada target en un array plano usando
    /// GetComponentsInChildren (incluye inactivos). Se ejecuta antes que Start
    /// para garantizar que la caché esté lista antes de cualquier lógica.
    /// </summary>
    private void Awake()
    {
        CacheColliders();
    }

    /// <summary>
    /// Sincroniza el estado anterior con el actual y aplica el estado inicial
    /// de cada entrada. Se ejecuta después de Awake.
    /// </summary>
    private void Start()
    {
        SyncPreviousStates();
        ApplyAll();
    }

    /// <summary>
    /// Polling ligero por frame:
    ///   - Primero comprueba los toggles globales (Enable All / Disable All).
    ///   - Luego recorre _targets comparando bools para detectar cambios individuales.
    /// Coste sin cambios: N comparaciones de bools (~nanosegundos).
    /// Coste con cambio: itera el array plano de colliders cacheados (sin GC).
    /// </summary>
    private void Update()
    {
        if (_enableAll)
        {
            _enableAll = false;
            SetAll(true);
            return;
        }

        if (_disableAll)
        {
            _disableAll = false;
            SetAll(false);
            return;
        }

        for (int i = 0, len = _targets.Length; i < len; i++)
        {
            PhysicsTarget entry = _targets[i];
            if (entry.collidersEnabled == entry.previousState)
                continue;

            entry.previousState = entry.collidersEnabled;
            SetColliders(entry.cachedColliders, entry.collidersEnabled);
        }
    }

    /// <summary>
    /// Reconstruye la caché de colliders y reaplica el estado actual.
    /// Llamar desde otro script cuando se añadan/quiten hijos en runtime:
    /// <code>GetComponent&lt;ScriptPhysicsManager&gt;().RefreshCache();</code>
    /// </summary>
    public void RefreshCache()
    {
        CacheColliders();
        SyncPreviousStates();
        ApplyAll();
    }

    /// <summary>
    /// Recorre cada target y cachea sus colliders (incluidos hijos inactivos)
    /// en un array plano. Targets nulos reciben un array vacío compartido.
    /// </summary>
    private void CacheColliders()
    {
        for (int i = 0, len = _targets.Length; i < len; i++)
        {
            PhysicsTarget entry = _targets[i];
            entry.cachedColliders = entry.target != null
                ? entry.target.GetComponentsInChildren<Collider>(true)
                : EmptyColliders;
        }
    }

    /// <summary>
    /// Fuerza un estado uniforme en todos los targets a la vez.
    /// Actualiza tanto el valor visible en el Inspector como el estado interno.
    /// </summary>
    private void SetAll(bool enabled)
    {
        for (int i = 0, len = _targets.Length; i < len; i++)
        {
            PhysicsTarget entry = _targets[i];
            entry.collidersEnabled = enabled;
            entry.previousState = enabled;
            SetColliders(entry.cachedColliders, enabled);
        }
    }

    /// <summary>Aplica el estado actual de cada entrada sobre sus colliders cacheados.</summary>
    private void ApplyAll()
    {
        for (int i = 0, len = _targets.Length; i < len; i++)
            SetColliders(_targets[i].cachedColliders, _targets[i].collidersEnabled);
    }

    /// <summary>Copia el estado actual al estado anterior para evitar falsos positivos en el primer frame.</summary>
    private void SyncPreviousStates()
    {
        for (int i = 0, len = _targets.Length; i < len; i++)
            _targets[i].previousState = _targets[i].collidersEnabled;
    }

    /// <summary>
    /// Itera un array plano de colliders y los activa/desactiva.
    /// Static para evitar captura de this y dejar claro que no accede a estado de instancia.
    /// </summary>
    private static void SetColliders(Collider[] colliders, bool enabled)
    {
        for (int i = 0, len = colliders.Length; i < len; i++)
            colliders[i].enabled = enabled;
    }
}
