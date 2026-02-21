using UnityEngine;
using UnityEngine.Events;

namespace Sepinaco.AnimationSystem
{
    /// <summary>
    /// Componente receptor de animaciones. Se coloca en el GameObject que tiene el Animator
    /// y recibe comandos de AnimationTriggerZone para ejecutar animaciones.
    /// Soporta Animator (Mecanim) y Animation (Legacy).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class TriggerAnimationReceiver : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("Animator del objeto. Se auto-detecta si no se asigna.")]
        public Animator animator;

        [Tooltip("Velocidad de reproducción de la animación (1 = normal).")]
        [Range(0.1f, 5f)]
        public float animationSpeed = 1f;

        [Header("Parámetros adicionales opcionales")]
        [Tooltip("Si es true, la animación se ejecuta incluso si el objeto está desactivado.")]
        public bool activateOnDisabled = false;

        [Header("Eventos")]
        public UnityEvent onAnimationTriggered;
        public UnityEvent onBoolSetTrue;
        public UnityEvent onBoolSetFalse;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                Debug.LogError($"[TriggerAnimationReceiver] No se encontró Animator en '{gameObject.name}'.", this);
            }
        }

        /// <summary>
        /// Dispara un trigger en el Animator.
        /// </summary>
        /// <param name="triggerName">Nombre del parámetro trigger en el Animator Controller.</param>
        public void FireTrigger(string triggerName)
        {
            if (!CanPlay()) return;

            animator.speed = animationSpeed;
            animator.SetTrigger(triggerName);
            onAnimationTriggered?.Invoke();

            Debug.Log($"[TriggerAnimationReceiver] Trigger '{triggerName}' activado en '{gameObject.name}'.");
        }

        /// <summary>
        /// Establece un parámetro bool en el Animator.
        /// </summary>
        /// <param name="boolName">Nombre del parámetro bool.</param>
        /// <param name="value">Valor a establecer.</param>
        public void SetBool(string boolName, bool value)
        {
            if (!CanPlay()) return;

            animator.speed = animationSpeed;
            animator.SetBool(boolName, value);

            if (value)
                onBoolSetTrue?.Invoke();
            else
                onBoolSetFalse?.Invoke();

            Debug.Log($"[TriggerAnimationReceiver] Bool '{boolName}' = {value} en '{gameObject.name}'.");
        }

        /// <summary>
        /// Establece un parámetro float en el Animator.
        /// </summary>
        /// <param name="floatName">Nombre del parámetro float.</param>
        /// <param name="value">Valor a establecer.</param>
        public void SetFloat(string floatName, float value)
        {
            if (!CanPlay()) return;

            animator.SetFloat(floatName, value);
        }

        /// <summary>
        /// Establece un parámetro int en el Animator.
        /// </summary>
        /// <param name="intName">Nombre del parámetro int.</param>
        /// <param name="value">Valor a establecer.</param>
        public void SetInteger(string intName, int value)
        {
            if (!CanPlay()) return;

            animator.SetInteger(intName, value);
        }

        /// <summary>
        /// Reproduce un estado de animación directamente por nombre.
        /// </summary>
        /// <param name="stateName">Nombre del estado en el Animator Controller.</param>
        /// <param name="layer">Capa del Animator (por defecto 0).</param>
        public void PlayState(string stateName, int layer = 0)
        {
            if (!CanPlay()) return;

            animator.speed = animationSpeed;
            animator.Play(stateName, layer);
            onAnimationTriggered?.Invoke();
        }

        /// <summary>
        /// Reproduce un estado con transición suave (crossfade).
        /// </summary>
        /// <param name="stateName">Nombre del estado destino.</param>
        /// <param name="transitionDuration">Duración de la transición en segundos.</param>
        /// <param name="layer">Capa del Animator.</param>
        public void CrossFadeState(string stateName, float transitionDuration = 0.25f, int layer = 0)
        {
            if (!CanPlay()) return;

            animator.speed = animationSpeed;
            animator.CrossFadeInFixedTime(stateName, transitionDuration, layer);
            onAnimationTriggered?.Invoke();
        }

        /// <summary>
        /// Resetea todos los triggers del Animator.
        /// </summary>
        public void ResetAllTriggers()
        {
            if (animator == null) return;

            foreach (var param in animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.ResetTrigger(param.name);
                }
            }
        }

        /// <summary>
        /// Verifica si el Animator puede reproducir animaciones.
        /// </summary>
        private bool CanPlay()
        {
            if (animator == null)
            {
                Debug.LogWarning($"[TriggerAnimationReceiver] Animator es null en '{gameObject.name}'.", this);
                return false;
            }

            if (!gameObject.activeInHierarchy && !activateOnDisabled)
            {
                Debug.LogWarning($"[TriggerAnimationReceiver] '{gameObject.name}' está desactivado. Habilita 'activateOnDisabled' para permitir animaciones.", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Devuelve información del estado actual de la animación.
        /// </summary>
        /// <param name="layer">Capa del Animator.</param>
        /// <returns>Info del estado actual.</returns>
        public AnimatorStateInfo GetCurrentStateInfo(int layer = 0)
        {
            return animator.GetCurrentAnimatorStateInfo(layer);
        }

        /// <summary>
        /// Comprueba si una animación específica se está reproduciendo.
        /// </summary>
        /// <param name="stateName">Nombre del estado.</param>
        /// <param name="layer">Capa del Animator.</param>
        /// <returns>True si el estado está activo.</returns>
        public bool IsPlayingState(string stateName, int layer = 0)
        {
            if (animator == null) return false;
            return animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName);
        }
    }
}
