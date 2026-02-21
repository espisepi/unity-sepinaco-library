# Falta de botones en ScriptPhysicsManager
_Exported on 21/02/2026 at 22:48:14 CET from Cursor (2.5.20)_

---

**User**

por que en el script @Assets/Sepinaco/PhysicsManager/ScriptPhysicsManager.cs en el editor de unity no me aparece los botones de + y - para añadir uno por uno un nuevo target o eliminarlo

---

**Cursor**

El comentario en la línea 38 del script menciona que existe un Custom Editor (`ScriptPhysicsManagerEditor`). Cuando Unity tiene un Custom Editor para un componente, **reemplaza completamente** el Inspector por defecto, incluyendo los botones + y - que normalmente aparecen en arrays. Déjame buscar si ese editor existe o si falta.

Ya encontré el problema. Tienes un **Custom Editor** en `Assets/Sepinaco/PhysicsManager/legacy/Editor/ScriptPhysicsManagerEditor.cs` que está activo (tiene `[CustomEditor(typeof(ScriptPhysicsManager))]`). Cuando existe un Custom Editor, Unity **reemplaza completamente** el Inspector por defecto, y este editor dibuja el array de targets manualmente (líneas 72-130) pero **no incluye botones + y -** para añadir o eliminar elementos individuales. Solo tiene un campo `Size` para cambiar el tamaño del array.

Voy a añadir los botones + y - a cada elemento del array en el Custom Editor.

Listo. El problema era que el **Custom Editor** (`ScriptPhysicsManagerEditor.cs`) en la carpeta `legacy/Editor/` estaba dibujando el array de targets de forma manual y no incluía botones para añadir o eliminar elementos individualmente. Solo tenía un campo `Size` numérico.

Los cambios que he hecho:

1. **Botón `+` por cada elemento**: aparece a la derecha de cada target y al pulsarlo inserta un nuevo target vacío justo debajo.
2. **Botón `−` por cada elemento**: aparece a la derecha de cada target y al pulsarlo elimina ese target de la lista.
3. **Botón `+ Add Target` al final de la lista**: permite añadir un nuevo target vacío al final sin tener que manipular el campo Size manualmente.

Los botones + y − se procesan de forma diferida (después de dibujar todos los elementos) para evitar modificar el array mientras se itera sobre él, lo cual causaría errores en Unity.

---

**User**

evita que desde el editor le podamos dar valores negativos al array size para que no salte el error array size must be non negative. tanto escribiendo el valor con el teclado como con el raton desplazandolo

---

**Cursor**

Listo. He añadido `Mathf.Max(0, ...)` alrededor del `IntField`, de modo que cualquier valor negativo (ya sea escrito con el teclado o arrastrado con el ratón) se clampa automáticamente a 0. Así nunca llegará un valor negativo a `arraySize` y no saltará el error.

