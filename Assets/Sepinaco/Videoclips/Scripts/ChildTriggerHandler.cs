using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildTriggerHandler : MonoBehaviour
{
    private ScriptVideoclip parentScript;

    private void Start()
    {
        // Obtiene una referencia al script del padre
        parentScript = GetComponentInParent<ScriptVideoclip>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Notifica al padre sobre la colisión
        if (parentScript != null)
        {
            parentScript.ChildCollided(this.gameObject, other);
        }
    }
}
