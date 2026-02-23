# Ajuste de texto y ancho de ventana en ScriptDebugInspector.cs
_Exported on 22/02/2026 at 23:43:49 CET from Cursor (2.5.20)_

---

**User**

aumenta por defecto el texto de la ui del script ScriptDebugInspector.cs y el ancho de la ventana de debug para que se vea correctamente al realizar y ejecutar la build en mi mac, que actualmente se ve muy pequeño todo

---

**Cursor**

Voy a aumentar los valores por defecto de tamaño de fuente, ancho y alto de la ventana para que se vea bien en builds de Mac (especialmente con pantallas Retina).

Los cambios realizados:

- **`_fontSize`**: de `14` a `22` -- el texto de la UI será notablemente más grande por defecto, ideal para pantallas Retina de Mac donde el IMGUI de Unity tiende a verse diminuto.
- **`FontMax`**: de `40` a `48` -- mayor margen de zoom por si necesitas agrandar aún más en runtime.
- **`_boxW`**: de `540` a `780` -- la ventana es más ancha para acomodar el texto más grande sin cortes.
- **`_boxH`**: de `560` a `720` -- la ventana es más alta para mostrar más contenido visible.

Seguirás pudiendo ajustar el tamaño en runtime con las teclas de zoom (`KeypadPlus` / `KeypadMinus`), pero ahora el punto de partida será mucho más legible en tu Mac.

