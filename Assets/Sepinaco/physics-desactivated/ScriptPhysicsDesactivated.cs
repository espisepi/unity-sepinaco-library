using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptPhysicsDesactivated : MonoBehaviour
{
    [Header("Configuración de Físicas")]
    [Tooltip("Activar o desactivar las físicas (colliders) del objeto y sus hijos")]
    public bool fisicasActivas = false;

    private bool _estadoAnterior;

    void Start()
    {
        _estadoAnterior = fisicasActivas;
        AplicarEstadoFisicas(gameObject, fisicasActivas);
    }

    void Update()
    {
        if (fisicasActivas != _estadoAnterior)
        {
            _estadoAnterior = fisicasActivas;
            AplicarEstadoFisicas(gameObject, fisicasActivas);
        }
    }

    private void AplicarEstadoFisicas(GameObject obj, bool activas)
    {
        Collider[] colliders = obj.GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = activas;
        }

        foreach (Transform hijo in obj.transform)
        {
            AplicarEstadoFisicas(hijo.gameObject, activas);
        }
    }
}