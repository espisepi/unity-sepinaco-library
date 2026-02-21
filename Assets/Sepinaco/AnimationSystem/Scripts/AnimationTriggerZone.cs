using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Sepinaco.AnimationSystem
{
    /// <summary>
    /// Zona trigger que detecta objetos y dispara animaciones en los receptores configurados.
    /// Colocar en un GameObject con un Collider marcado como "Is Trigger".
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AnimationTriggerZone : MonoBehaviour
    {
        [Header("Filtro de detección")]
        [Tooltip("Tag del objeto que puede activar este trigger. Dejar vacío para aceptar cualquiera.")]
        public string requiredTag = "Player";

        [Tooltip("Layer mask de los objetos que pueden activar este trigger.")]
        public LayerMask detectionMask = ~0;

        [Header("Animaciones al Entrar")]
        [Tooltip("Nombre del trigger del Animator que se activa al entrar en la zona.")]
        public string enterTriggerName = "";

        [Tooltip("Nombre del parámetro bool del Animator que se pone a true al entrar.")]
        public string enterBoolName = "";

        [Header("Animaciones al Salir")]
        [Tooltip("Nombre del trigger del Animator que se activa al salir de la zona.")]
        public string exitTriggerName = "";

        [Tooltip("Nombre del parámetro bool del Animator que se pone a false al salir.")]
        public string exitBoolName = "";

        [Header("Receptores de Animación")]
        [Tooltip("Receptores que ejecutarán las animaciones. Si está vacío, busca en este mismo GameObject.")]
        public List<TriggerAnimationReceiver> receivers = new List<TriggerAnimationReceiver>();

        [Header("Opciones")]
        [Tooltip("Si es true, el trigger solo se activa una vez y luego se desactiva.")]
        public bool triggerOnce = false;

        [Tooltip("Retardo en segundos antes de ejecutar la animación de entrada.")]
        public float enterDelay = 0f;

        [Tooltip("Retardo en segundos antes de ejecutar la animación de salida.")]
        public float exitDelay = 0f;

        [Header("Eventos")]
        public UnityEvent<Collider> onTriggerActivated;
        public UnityEvent<Collider> onTriggerDeactivated;

        private bool hasTriggered = false;
        private int objectsInside = 0;

        private void Start()
        {
            // Auto-detectar receptores si no se asignaron manualmente
            if (receivers.Count == 0)
            {
                var receiver = GetComponent<TriggerAnimationReceiver>();
                if (receiver != null)
                {
                    receivers.Add(receiver);
                }
            }

            // Verificar que el collider es trigger
            var col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning($"[AnimationTriggerZone] El Collider en '{gameObject.name}' no está marcado como Trigger. Activándolo automáticamente.", this);
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsValidTarget(other)) return;
            if (triggerOnce && hasTriggered) return;

            objectsInside++;

            // Solo activar si es el primer objeto que entra
            if (objectsInside == 1)
            {
                hasTriggered = true;

                if (enterDelay > 0f)
                {
                    StartCoroutine(DelayedEnter(other));
                }
                else
                {
                    ExecuteEnter(other);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsValidTarget(other)) return;
            if (triggerOnce && hasTriggered && objectsInside <= 0) return;

            objectsInside = Mathf.Max(0, objectsInside - 1);

            // Solo desactivar cuando salen todos los objetos
            if (objectsInside == 0)
            {
                if (exitDelay > 0f)
                {
                    StartCoroutine(DelayedExit(other));
                }
                else
                {
                    ExecuteExit(other);
                }
            }
        }

        private void ExecuteEnter(Collider other)
        {
            foreach (var receiver in receivers)
            {
                if (receiver == null) continue;

                if (!string.IsNullOrEmpty(enterTriggerName))
                    receiver.FireTrigger(enterTriggerName);

                if (!string.IsNullOrEmpty(enterBoolName))
                    receiver.SetBool(enterBoolName, true);
            }

            onTriggerActivated?.Invoke(other);
        }

        private void ExecuteExit(Collider other)
        {
            foreach (var receiver in receivers)
            {
                if (receiver == null) continue;

                if (!string.IsNullOrEmpty(exitTriggerName))
                    receiver.FireTrigger(exitTriggerName);

                if (!string.IsNullOrEmpty(exitBoolName))
                    receiver.SetBool(exitBoolName, false);
            }

            onTriggerDeactivated?.Invoke(other);
        }

        private bool IsValidTarget(Collider other)
        {
            // Verificar layer
            if ((detectionMask.value & (1 << other.gameObject.layer)) == 0)
                return false;

            // Verificar tag (si se especificó)
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
                return false;

            return true;
        }

        private System.Collections.IEnumerator DelayedEnter(Collider other)
        {
            yield return new WaitForSeconds(enterDelay);
            ExecuteEnter(other);
        }

        private System.Collections.IEnumerator DelayedExit(Collider other)
        {
            yield return new WaitForSeconds(exitDelay);
            ExecuteExit(other);
        }

        /// <summary>
        /// Resetea el trigger para que pueda activarse de nuevo (útil si triggerOnce = true).
        /// </summary>
        public void ResetTrigger()
        {
            hasTriggered = false;
            objectsInside = 0;
        }

        private void OnDrawGizmos()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = hasTriggered ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 1f, 0f, 0.3f);

            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
            }
        }
    }
}
