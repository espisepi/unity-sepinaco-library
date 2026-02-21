# Trigger restoration script modification
_Exported on 21/02/2026 at 23:56:46 CET from Cursor (2.5.20)_

---

**User**

modifica el script @Assets/Sepinaco/TriggerRestoreManager/TriggerRestoreManager.cs para que cuando se añada a un gameobject, se cree un trigger en el que todo gameobject de la escena que descienda por el eje Y un valor modificable desde el editor, se restaure a su posición original al empezar la escena. De esta manera cualquier objeto que se caiga por el mapa será restaurado a su posición inicial original

---

**Cursor**

Voy a leer el archivo y luego reescribirlo completamente con la funcionalidad que describes.

El archivo actualmente contiene una copia de `ScriptObjectsManager`. Lo reemplazo con un `TriggerRestoreManager` que:

1. Al iniciar la escena, registra la posición y rotación original de todos los objetos relevantes.
2. Cada frame comprueba si algún objeto ha caído por debajo de un umbral Y configurable.
3. Si lo ha hecho, lo restaura a su posición original y resetea su velocidad.

El script ha sido reescrito por completo. Aquí tienes un resumen de lo que hace:

**Funcionamiento:**
- En `Start`, registra la posición y rotación original de todos los objetos relevantes de la escena.
- En cada `Update`, comprueba si algún objeto rastreado ha caído por debajo del **umbral Y** (`_fallThresholdY`). Si es así, lo teletransporta a su posición original y le resetea la velocidad.

**Parámetros editables desde el Inspector:**

| Campo | Descripción |
|---|---|
| `Fall Threshold Y` | Valor Y por debajo del cual se restaura el objeto (por defecto `-50`) |
| `Track All Visible Objects` | Si está activo rastrea todo objeto con `Renderer`; si no, solo los que tengan `Rigidbody` (recomendado) |
| `Additional Targets` | Lista manual de GameObjects extra a rastrear |
| `Restore Rotation` | Restaurar también la rotación original |
| `Reset Velocity` | Poner a cero la velocidad del Rigidbody al restaurar |
| `Gizmo Size` | Tamaño del plano rojo que visualiza el umbral en la vista Scene |

**Extras:**
- **Gizmo visual**: dibuja un plano rojo semitransparente en la vista Scene a la altura del umbral, para que puedas ver exactamente dónde está el límite.
- **API pública**: `RegisterAtRuntime(GameObject)` para añadir objetos dinámicamente y `SetThreshold(float)` para cambiar el umbral en runtime.
- Un `Debug.Log` al inicio indica cuántos objetos se están rastreando.

---

**User**

pero la performance es muy mala no? porque se comprueba la posicion de todos los objetos en cada frame

---

**Cursor**

Tienes razón, iterar sobre todos los objetos en cada frame no es ideal. La solución correcta en Unity es usar un **trigger físico real** con `OnTriggerEnter` — así es el motor de físicas (que ya usa broadphase optimizado) el que detecta cuándo un objeto cruza el umbral, sin coste en `Update()`.

Tienes toda la razón, la versión anterior era ineficiente. Esta nueva versión **no tiene `Update()`** — la detección la hace el motor de físicas de Unity.

**Cómo funciona ahora:**

1. En `Awake`, el script crea automáticamente un `BoxCollider` marcado como trigger, posicionado con su cara superior a la altura de `_fallThresholdY` y extendiéndose hacia abajo.
2. En `Start`, registra la posición/rotación original de todos los `Rigidbody` de la escena.
3. Cuando un objeto cae y entra en el trigger, **PhysX llama a `OnTriggerEnter`** — solo se ejecuta código en ese momento, no cada frame.

**Por qué es eficiente:**
- El broadphase de PhysX (árboles AABB) descarta automáticamente el 99% de objetos que no están cerca del trigger.
- No hay ningún bucle en `Update()` — coste cero por frame mientras nada caiga.
- Solo se ejecuta `Restore()` cuando un objeto realmente cruza el umbral.

**Gizmos en la vista Scene:**
- Caja roja semitransparente: volumen del trigger.
- Línea amarilla horizontal: marca exacta del umbral Y.

---

**User**

crea un atributo booleano modificable desde el editor para activar y desactivar visualmente el trigger para verlo en modo debug visualmente

---

**Cursor**

Listo. Se ha añadido el campo `_showTriggerGizmo` bajo una sección **Debug** en el Inspector. Cuando está activado (por defecto `true`), se dibuja la caja roja y la línea amarilla en la vista Scene. Al desmarcarlo, los gizmos desaparecen completamente.

---

**User**

aparece este error: Assets/Sepinaco/TriggerRestoreManager/TriggerRestoreManager.cs(117,16): error CS1061: 'Rigidbody' does not contain a definition for 'linearVelocity' and no accessible extension method 'linearVelocity' accepting a first argument of type 'Rigidbody' could be found (are you missing a using directive or an assembly reference?)

---

**Cursor**

Corregido. `linearVelocity` se introdujo en Unity 2022.3+; en versiones anteriores la propiedad se llama `velocity`. Ahora debería compilar sin problemas.

