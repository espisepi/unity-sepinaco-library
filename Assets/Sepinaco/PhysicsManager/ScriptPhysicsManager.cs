using System;
using UnityEngine;

[Serializable]
public class PhysicsTarget
{
    [Tooltip("GameObject objetivo. Sus colliders y los de todos sus hijos serán gestionados.")]
    public GameObject target;

    [Tooltip("Marca/desmarca para activar o desactivar los colliders.")]
    public bool collidersEnabled = true;

    [NonSerialized] public Collider[] cachedColliders;
}

/// <summary>
/// Gestiona la activación/desactivación de colliders de múltiples GameObjects.
///
/// Los valores de "collidersEnabled" se serializan en la escena y se aplican en
/// Awake al arrancar en cualquier plataforma. No usa Update: cero coste por frame.
///
/// Para cambios en runtime desde código, usar la API pública:
///   SetTargetState(index, enabled), EnableAll(), DisableAll(), RefreshCache().
///
/// En el Editor, OnValidate aplica los cambios inmediatamente al modificar el Inspector.
/// Un Custom Editor (ScriptPhysicsManagerEditor) añade botones y toggles mejorados.
/// </summary>
public class ScriptPhysicsManager : MonoBehaviour
{
    [Header("Physics Targets")]
    [Tooltip("Arrastra aquí los GameObjects cuyas físicas quieres gestionar.")]
    [SerializeField] private PhysicsTarget[] _targets = Array.Empty<PhysicsTarget>();

    private static readonly Collider[] EmptyColliders = Array.Empty<Collider>();
    private bool _cacheReady;

    public int TargetCount => _targets.Length;

    private void Awake()
    {
        BuildCache();
        ApplyAll();
    }

    // ───────────────────────── API pública (runtime) ─────────────────────────

    public void SetTargetState(int index, bool enabled)
    {
        if ((uint)index >= (uint)_targets.Length) return;
        PhysicsTarget entry = _targets[index];
        entry.collidersEnabled = enabled;
        if (_cacheReady)
            SetColliders(entry.cachedColliders, enabled);
    }

    public void EnableAll() => SetAllInternal(true);
    public void DisableAll() => SetAllInternal(false);

    public PhysicsTarget GetTarget(int index)
    {
        return (uint)index < (uint)_targets.Length ? _targets[index] : null;
    }

    /// <summary>
    /// Reconstruye la caché y reaplica el estado.
    /// Llamar si se añaden/quitan hijos en runtime.
    /// </summary>
    public void RefreshCache()
    {
        BuildCache();
        ApplyAll();
    }

    // ────────────────────── Editor: aplicar cambios al tocar Inspector ──────────────────────

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && _targets != null)
        {
            foreach (PhysicsTarget entry in _targets)
            {
                if (entry.target == null) continue;
                Collider[] cols = entry.target.GetComponentsInChildren<Collider>(true);
                SetColliders(cols, entry.collidersEnabled);
            }
        }

        if (Application.isPlaying && _cacheReady)
            ApplyAll();
    }
#endif

    // ───────────────────────── Internals ─────────────────────────

    private void BuildCache()
    {
        for (int i = 0, len = _targets.Length; i < len; i++)
        {
            PhysicsTarget entry = _targets[i];
            entry.cachedColliders = entry.target != null
                ? entry.target.GetComponentsInChildren<Collider>(true)
                : EmptyColliders;
        }
        _cacheReady = true;
    }

    private void SetAllInternal(bool enabled)
    {
        for (int i = 0, len = _targets.Length; i < len; i++)
        {
            PhysicsTarget entry = _targets[i];
            entry.collidersEnabled = enabled;
            if (_cacheReady)
                SetColliders(entry.cachedColliders, enabled);
        }
    }

    private void ApplyAll()
    {
        for (int i = 0, len = _targets.Length; i < len; i++)
            SetColliders(_targets[i].cachedColliders, _targets[i].collidersEnabled);
    }

    private static void SetColliders(Collider[] colliders, bool enabled)
    {
        for (int i = 0, len = colliders.Length; i < len; i++)
            colliders[i].enabled = enabled;
    }
}
