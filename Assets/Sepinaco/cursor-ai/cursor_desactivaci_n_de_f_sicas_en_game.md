# Desactivación de físicas en GameObjects
_Exported on 21/02/2026 at 5:19:19 CET from Cursor (2.5.20)_

---

**User**

añademe a este script de unity ScriptPhysicsDesactivated la funcionalidad de desactivar las fisicas del gameobject asociado y todos los hijos recursivos que tenga el gameobject. Comprueba que tenga fisicas, porque habra algunos gameobjects que no tengan fisicas asociadas

---

**Cursor**

El script ya está actualizado. Esto es lo que hace:

1. **En `Start()`** se llama a `DesactivarFisicasRecursivo` pasando el propio `gameObject` como punto de partida.

2. **`DesactivarFisicasRecursivo`** recorre el GameObject y todos sus hijos de forma recursiva, y para cada uno:
   - **Rigidbody**: comprueba si existe (`!= null`). Si lo tiene, lo pone en `isKinematic = true` y resetea la velocidad lineal y angular a cero para que quede completamente parado.
   - **Colliders**: obtiene todos los `Collider` del objeto (puede tener más de uno) y los desactiva con `enabled = false`.
   - **Hijos**: itera sobre cada `Transform` hijo y llama recursivamente a la misma función.

Si un GameObject no tiene `Rigidbody` o no tiene `Collider`, simplemente se salta esa parte sin errores, gracias a las comprobaciones de nulidad y al uso de arrays.

---

**User**

con desactivar fisicas del gameobject me refiero a desactivar las colisiones con cualquier gameobject de la escena solamente

---

**Cursor**

Listo. Ahora el script solo desactiva los `Collider` (que son los responsables de las colisiones) en el GameObject y todos sus hijos recursivamente. Si algún objeto no tiene `Collider`, simplemente lo salta sin error. Los `Rigidbody` se dejan intactos para que la gravedad y el movimiento físico sigan funcionando con normalidad.

