# Actualización de texturas en gameobjects
_Exported on 22/02/2026 at 0:35:51 CET from Cursor (2.5.20)_

---

**User**

Añade la funcionalidad al script ScriptVideoclipsManager.cs que actualice las texturas de todos los gameobjects que aparecen en la escena cuando se añaden en el script ScriptObjectsManager.cs

---

**Cursor**

Let me start by reading both scripts to understand their current structure.

Now I have a clear picture of both scripts. The approach will be:

1. **`ScriptObjectsManager`**: Add an event that fires when a target's visibility state changes.
2. **`ScriptVideoclipsManager`**: Subscribe to that event and, when a GameObject is activated, register its renderers and apply video textures if they're currently active.

Let me implement this.

First, I'll add the event to `ScriptObjectsManager` and fire it wherever a target's state changes.

Now fire the event in `SetTargetState`:

Now fire it in `SetAllInternal`:

And in `ApplyAll`:

Now I'll modify `ScriptVideoclipsManager` to subscribe to the event and handle newly activated objects.

Now add the subscription in `Start` and the handler method. I'll add the subscription right after the existing setup, and also add the handler method and update `OnDestroy`:

Now add the handler method. I'll place it right after `RefreshRenderersIfNeeded`:

Finally, unsubscribe in `OnDestroy` to avoid memory leaks:

Now let me verify both files look correct.

Ambos archivos están correctos. Aquí tienes un resumen de los cambios realizados:

---

### `ScriptObjectsManager.cs` - Evento de notificación

Se añadió un evento público que se dispara cada vez que un target cambia de estado:

```90:90:Assets/Sepinaco/ObjectsManager/ScriptObjectsManager.cs
    public event Action<GameObject, bool> OnTargetStateChanged;
```

Se invoca en los tres lugares donde se cambia el estado de los objetos:
- **`SetTargetState`** (toggle individual)
- **`SetAllInternal`** (ShowAll / HideAll)
- **`ApplyAll`** (al iniciar con UseIndividualSettings)

---

### `ScriptVideoclipsManager.cs` - Suscripción y handler

1. **Suscripción al evento** al final de `Start()`:

```107:109:Assets/Sepinaco/VideoclipsManager/ScriptVideoclipsManager.cs
        objectsManager = FindObjectOfType<ScriptObjectsManager>();
        if (objectsManager != null)
            objectsManager.OnTargetStateChanged += OnObjectTargetStateChanged;
```

2. **Handler `OnObjectTargetStateChanged`** que se ejecuta cuando un objeto se activa:

```184:207:Assets/Sepinaco/VideoclipsManager/ScriptVideoclipsManager.cs
    void OnObjectTargetStateChanged(GameObject obj, bool active)
    {
        if (!active || obj == null || videoMaterial == null) return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            if (originalMaterials.ContainsKey(rend)) continue;

            Material[] copy = new Material[rend.sharedMaterials.Length];
            System.Array.Copy(rend.sharedMaterials, copy, copy.Length);
            originalMaterials[rend] = copy;

            Material[] mats = new Material[copy.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = videoMaterial;
            videoMaterialArrays[rend] = mats;

            if (texturesReplaced)
                rend.sharedMaterials = mats;
        }

        cachedRendererCount = FindObjectsOfType<Renderer>().Length;
    }
```

3. **Desuscripción en `OnDestroy`** para evitar memory leaks:

```405:406:Assets/Sepinaco/VideoclipsManager/ScriptVideoclipsManager.cs
        if (objectsManager != null)
            objectsManager.OnTargetStateChanged -= OnObjectTargetStateChanged;
```

---

### Flujo de funcionamiento

1. Cuando `ScriptObjectsManager` activa un GameObject (ShowAll, toggle, o al arrancar), dispara `OnTargetStateChanged`.
2. `ScriptVideoclipsManager` recibe la notificación y, si el objeto se activó (`active == true`):
   - Busca todos los `Renderer` del objeto (incluidos hijos).
   - Guarda sus materiales originales (si no estaban ya registrados).
   - Crea los arrays de material de vídeo correspondientes.
   - Si las texturas de vídeo están activas (`texturesReplaced`), aplica el material de vídeo inmediatamente al nuevo objeto.

