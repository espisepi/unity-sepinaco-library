// using System;
// using UnityEngine;

// [Serializable]
// public class PhysicsTarget
// {
//     public GameObject target;
//     public bool collidersEnabled;

//     [HideInInspector] public bool previousState;
//     [NonSerialized] public Collider[] cachedColliders;
// }

// public class ScriptPhysicsManagerV2 : MonoBehaviour
// {
//     [Header("Physics Manager")]
//     [Tooltip("Arrastra aquí los GameObjects cuyas físicas quieres gestionar")]
//     [SerializeField] private PhysicsTarget[] _targets = Array.Empty<PhysicsTarget>();

//     [Header("Control Global")]
//     [SerializeField] private bool _enableAll;
//     [SerializeField] private bool _disableAll;

//     private static readonly Collider[] EmptyColliders = Array.Empty<Collider>();

//     private void Awake()
//     {
//         CacheColliders();
//     }

//     private void Start()
//     {
//         SyncPreviousStates();
//         ApplyAll();
//     }

//     private void Update()
//     {
//         if (_enableAll)
//         {
//             _enableAll = false;
//             SetAll(true);
//             return;
//         }

//         if (_disableAll)
//         {
//             _disableAll = false;
//             SetAll(false);
//             return;
//         }

//         for (int i = 0, len = _targets.Length; i < len; i++)
//         {
//             PhysicsTarget entry = _targets[i];
//             if (entry.collidersEnabled == entry.previousState)
//                 continue;

//             entry.previousState = entry.collidersEnabled;
//             SetColliders(entry.cachedColliders, entry.collidersEnabled);
//         }
//     }

//     private void CacheColliders()
//     {
//         for (int i = 0, len = _targets.Length; i < len; i++)
//         {
//             PhysicsTarget entry = _targets[i];
//             entry.cachedColliders = entry.target != null
//                 ? entry.target.GetComponentsInChildren<Collider>(true)
//                 : EmptyColliders;
//         }
//     }

//     private void SetAll(bool enabled)
//     {
//         for (int i = 0, len = _targets.Length; i < len; i++)
//         {
//             PhysicsTarget entry = _targets[i];
//             entry.collidersEnabled = enabled;
//             entry.previousState = enabled;
//             SetColliders(entry.cachedColliders, enabled);
//         }
//     }

//     private void ApplyAll()
//     {
//         for (int i = 0, len = _targets.Length; i < len; i++)
//             SetColliders(_targets[i].cachedColliders, _targets[i].collidersEnabled);
//     }

//     private void SyncPreviousStates()
//     {
//         for (int i = 0, len = _targets.Length; i < len; i++)
//             _targets[i].previousState = _targets[i].collidersEnabled;
//     }

//     private static void SetColliders(Collider[] colliders, bool enabled)
//     {
//         for (int i = 0, len = colliders.Length; i < len; i++)
//             colliders[i].enabled = enabled;
//     }
// }
