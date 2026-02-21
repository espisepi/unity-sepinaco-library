// using System;
// using UnityEngine;

// [Serializable]
// public class PhysicsTarget
// {
//     public GameObject target;
    
//     public bool collidersEnabled;

//     [HideInInspector]
//     public bool previousState;
// }

// public class ScriptPhysicsManagerV1 : MonoBehaviour
// {
//     [Header("Physics Manager")]
//     [Tooltip("Arrastra aquí los GameObjects cuyas físicas quieres gestionar")]
//     [SerializeField] private PhysicsTarget[] _targets = Array.Empty<PhysicsTarget>();

//     [Header("Control Global")]
//     [Tooltip("Activa o desactiva todos los colliders a la vez")]
//     [SerializeField] private bool _enableAll;
//     [SerializeField] private bool _disableAll;

//     private void Start()
//     {
//         SyncPreviousStates();
//         ApplyAll();
//     }

//     private void Update()
//     {
//         HandleGlobalToggles();
//         HandleIndividualToggles();
//     }

//     private void HandleGlobalToggles()
//     {
//         if (_enableAll)
//         {
//             _enableAll = false;
//             SetAll(true);
//         }

//         if (_disableAll)
//         {
//             _disableAll = false;
//             SetAll(false);
//         }
//     }

//     private void HandleIndividualToggles()
//     {
//         foreach (var entry in _targets)
//         {
//             if (entry.target == null || entry.collidersEnabled == entry.previousState)
//                 continue;

//             entry.previousState = entry.collidersEnabled;
//             SetCollidersRecursive(entry.target, entry.collidersEnabled);
//         }
//     }

//     private void SetAll(bool enabled)
//     {
//         foreach (var entry in _targets)
//         {
//             if (entry.target == null)
//                 continue;

//             entry.collidersEnabled = enabled;
//             entry.previousState = enabled;
//             SetCollidersRecursive(entry.target, enabled);
//         }
//     }

//     private void ApplyAll()
//     {
//         foreach (var entry in _targets)
//         {
//             if (entry.target != null)
//                 SetCollidersRecursive(entry.target, entry.collidersEnabled);
//         }
//     }

//     private void SyncPreviousStates()
//     {
//         foreach (var entry in _targets)
//             entry.previousState = entry.collidersEnabled;
//     }

//     private static void SetCollidersRecursive(GameObject obj, bool enabled)
//     {
//         foreach (var col in obj.GetComponents<Collider>())
//             col.enabled = enabled;

//         foreach (Transform child in obj.transform)
//             SetCollidersRecursive(child.gameObject, enabled);
//     }
// }
