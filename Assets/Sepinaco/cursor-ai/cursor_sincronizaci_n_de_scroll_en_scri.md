# Sincronización de scroll en ScriptDebugInspector
_Exported on 23/02/2026 at 0:00:15 CET from Cursor (2.5.20)_

---

**User**

en el script ScriptDebugInspector no se sincroniza bien el scroll con el elemento seleccionado ocurriendo ocasiones en que se realiza scroll y no veo el elemento seleccionado en pantalla. Seguramente el problema sea que los valores se calcularon a partir de un tamaño de letra más pequeño que el que tengo actualmente. Haz que esos valores se calculen dependiendo del tamaño de letra que tengo configurado para que cuando cambie el tamaño de letra siga funcionando correctamente el scroll junto con el elemento seleccionado para verlo siempre en pantalla el elemento seleccionado.

---

**Cursor**

Voy a analizar el problema. El método `AutoScroll` usa constantes hardcodeadas (`LineH = 22f`, `HeaderLinesScripts = 7`, `HeaderLinesFields = 9`) para calcular posiciones de scroll, pero esos valores no se ajustan al cambiar el tamaño de fuente. La solución es usar las posiciones reales de los elementos renderizados por Unity IMGUI en vez de estimaciones fijas.

Ahora aplico los cambios. Primero, reemplazo las constantes hardcodeadas por una lista que almacenará las posiciones reales de cada elemento renderizado.

Ahora actualizo `ApplyAutoScroll` para que no pase `headerLines` (ya no se necesita):

Ahora modifico `DrawScripts()` para capturar el rect real de cada elemento:

Ahora modifico `DrawFields()` de la misma manera:

Ahora el cambio más importante: reescribir `AutoScroll` para usar las posiciones reales en vez de estimaciones con constantes:

Ahora verifico que el archivo resultante es correcto:

Verifico las secciones clave del archivo modificado para asegurarme de que todo está correcto:

Todo está correcto. Aquí un resumen de los cambios realizados:

**Problema:** El `AutoScroll` usaba tres constantes hardcodeadas para calcular posiciones de scroll:
- `LineH = 22f` — altura fija por línea
- `HeaderLinesScripts = 7` — líneas de cabecera en pantalla de scripts
- `HeaderLinesFields = 9` — líneas de cabecera en pantalla de campos

Estos valores estaban calibrados para un tamaño de fuente específico. Al cambiar `_fontSize` con zoom, las alturas reales de los elementos crecían o se reducían, pero el cálculo de scroll seguía usando `22px * N`, provocando que el scroll no coincidiera con el elemento seleccionado.

**Solución:** En vez de estimar posiciones con constantes, ahora se capturan las **posiciones reales** (`Rect`) de cada elemento durante el evento `Repaint` de `OnGUI` usando `GUILayoutUtility.GetLastRect()`. Estos rects se almacenan en `_itemRects` y el `AutoScroll` los consulta directamente para saber exactamente dónde está cada elemento en el contenido del scroll, independientemente del tamaño de fuente.

Cambios concretos:
1. **Eliminadas** las constantes `LineH`, `HeaderLinesScripts` y `HeaderLinesFields`
2. **Añadida** `_itemRects` — lista que almacena el `Rect` renderizado de cada ítem
3. **`DrawScripts()` y `DrawFields()`** — ahora capturan el rect de cada elemento durante Repaint
4. **`AutoScroll`** — usa `_itemRects[cursor]` para obtener la posición y altura real del elemento seleccionado, y solo ajusta el scroll cuando el elemento queda fuera del área visible

