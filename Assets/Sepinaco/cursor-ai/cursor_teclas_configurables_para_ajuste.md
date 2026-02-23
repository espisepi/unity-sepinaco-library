# Teclas configurables para ajuste de UI
_Exported on 22/02/2026 at 1:54:46 CET from Cursor (2.5.20)_

---

**User**

añademe en los scripts ScriptPhysicsManager.cs ScriptVideoclips.cs ScriptObjectManager.cs 4 nuevas teclas configurables desde el editor para aumentar y disminuir el ancho y alto de la ui de cada script independientemente. Las teclas por defecto son las mismas para cada script para que se modifiquen a la vez

---

**Cursor**

Let me read the three scripts first to understand their current structure.

Voy a añadir a los tres scripts 4 teclas configurables para controlar el ancho y alto de la UI. Las teclas por defecto serán las mismas en los tres: `LeftArrow` / `RightArrow` para el ancho y `PageDown` / `PageUp` para el alto.

Empiezo con `ScriptPhysicsManager.cs`:

Now add the key handling in Update and update the OnGUI for Physics:

Now update the OnGUI to use the dynamic sizes and show the new controls:

Ahora `ScriptVideoclipsManager.cs`:

Now add key handling in Update for Videoclips:

Now update OnGUI for Videoclips to use dynamic sizes and show controls:

Ahora `ScriptObjectsManager.cs`:

Now update OnGUI for Objects to use dynamic sizes and show controls:

Verifico que los archivos finales estén correctos.

Los tres scripts han sido actualizados. Aquí un resumen de los cambios en cada uno:

**4 nuevas teclas configurables** añadidas bajo el header `[Header("Tamaño de la UI")]`:

| Acción | Tecla por defecto | Campo |
|---|---|---|
| Aumentar ancho | `RightArrow` | `uiWidthIncreaseKey` |
| Disminuir ancho | `LeftArrow` | `uiWidthDecreaseKey` |
| Aumentar alto | `PageUp` | `uiHeightIncreaseKey` |
| Disminuir alto | `PageDown` | `uiHeightDecreaseKey` |

**Comportamiento:**
- Las teclas son **configurables desde el Inspector de Unity** independientemente para cada script.
- Por defecto, las mismas teclas (`LeftArrow`, `RightArrow`, `PageUp`, `PageDown`) están asignadas en los tres scripts, por lo que al pulsarlas **se redimensionan las tres UIs a la vez**.
- Si cambias las teclas en el Inspector de un script concreto, ese script se redimensionará con sus propias teclas de forma independiente.
- Cada pulsación modifica el tamaño en **20px**, con un rango de **200-1200px** tanto para ancho como para alto.
- Solo funcionan con el menú abierto, como el resto de controles.
- Los controles se muestran en el propio menú OnGUI con los valores actuales (ej: `[LeftArrow] / [RightArrow] Ancho UI (380px)`).

