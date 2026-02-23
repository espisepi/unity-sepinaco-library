# Modificación de teclas de zoom en ScriptDebugInspector
_Exported on 23/02/2026 at 11:25:35 CET from Cursor (2.5.20)_

---

**User**

modifica las teclas de zoom de texto en el script ScriptDebugInspector para que disminuyamos el tamaño de las letras con la tecla 8 y lo aumentemos con la tecla 9

---

**Cursor**

Ahora cambio las teclas de zoom. Actualmente `zoomInKey` está asignada a `KeypadPlus` y `zoomOutKey` a `KeypadMinus`. Las cambiaré a `Alpha9` (tecla 9) y `Alpha8` (tecla 8) respectivamente.

Listo. Los cambios realizados son:

- **Tecla 8** (`KeyCode.Alpha8`): disminuye el tamaño de las letras (zoom out).
- **Tecla 9** (`KeyCode.Alpha9`): aumenta el tamaño de las letras (zoom in).

Ambas teclas siguen siendo configurables desde el Inspector de Unity, así que puedes reasignarlas en cualquier momento si lo necesitas.

