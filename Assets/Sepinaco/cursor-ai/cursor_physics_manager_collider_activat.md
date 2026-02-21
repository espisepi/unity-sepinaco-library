# Physics manager collider activation
_Exported on 21/02/2026 at 21:11:12 CET from Cursor (2.5.20)_

---

**User**

modifica el script physicsManager para decidir si por defecto al iniciar se realiza el activado o desactivado de los colliders

---

**Cursor**

Voy a añadir un enum que permita elegir el comportamiento por defecto al iniciar: usar la configuración individual de cada target, forzar todos activados, o forzar todos desactivados.

Ahora modifico el `Awake` para que aplique el modo elegido:

Verifico el resultado final del archivo.

Ahora voy a verificar si el Custom Editor necesita actualizarse para mostrar el nuevo campo. Lo leo para comprobarlo.

El Custom Editor no muestra el nuevo campo `_startMode`. Lo añado para que aparezca en el Inspector.

Ahora verifico también el archivo UI por si hace referencia al Awake o al inicio.

No hay referencias en el UI, así que no necesita cambios. Los archivos quedan así:

**`ScriptPhysicsManager.cs`** - Cambios realizados:

1. **Nuevo enum `StartCollidersMode`** con tres opciones:
   - `UseIndividualSettings` — respeta el toggle de cada target (comportamiento original).
   - `EnableAll` — fuerza todos los colliders activados al arrancar.
   - `DisableAll` — fuerza todos los colliders desactivados al arrancar.

2. **Nuevo campo serializado `_startMode`** que aparece en el Inspector bajo el header "Start Behaviour", con valor por defecto `UseIndividualSettings` (idéntico al comportamiento anterior).

3. **`Awake` modificado** para evaluar `_startMode` con un switch: si es `EnableAll` o `DisableAll` llama a `SetAllInternal`, si no aplica la configuración individual como antes.

4. **Propiedad pública `StartMode`** de solo lectura por si necesitas consultarla desde otros scripts.

**`ScriptPhysicsManagerEditor.cs`** - Se añadió el campo `_startModeProp` al Custom Editor para que el dropdown aparezca en el Inspector justo debajo del título y antes de los botones globales.

