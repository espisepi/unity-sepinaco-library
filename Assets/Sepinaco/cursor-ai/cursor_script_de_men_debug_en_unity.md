# Script de menú debug en Unity
_Exported on 22/02/2026 at 20:31:18 CET from Cursor (2.5.20)_

---

**User**

quiero crear un script que lo asocie a un gameobject y le incluya al script creado un array de scripts. Los scripts del array que le incluyo son scripts que aparecen en la escena. Quiero que el script creado me muestre un menu debug visual en el que puedo modificar los valores de los atributos que se muestran en el editor de unity de los scripts añadidos respectivamente. Dentro del menu visual del script creado, aparecerá primero una pantalla con el listado de scripts del array en la que puedo navegar uno por uno entre ellos y seleccionarlo. Y una vez seleccionado el script aparecerá otra pantalla con los valores modificables de los atributos del script y puedo navegar entre ellos y modificarlo. Todo lo puedo hacer usando el teclado. Quiero que para abrir y cerrar el menu debug del script que me has creado tenga que pulsar la tecla F1.

---

**Cursor**

Voy a revisar la estructura del proyecto y los scripts existentes para crear el ScriptDebugInspector siguiendo el mismo estilo visual y convenciones del proyecto.

Ahora tengo claro el estilo del proyecto. Voy a crear el script `ScriptDebugInspector` usando el mismo patrón visual de OnGUI y las mismas convenciones.

El script se ha creado correctamente. Te explico cómo funciona:

---

**Archivo creado:** `Assets/Sepinaco/DebugInspector/ScriptDebugInspector.cs`

### Cómo usarlo

1. Arrastra el script a cualquier GameObject de la escena
2. En el Inspector, arrastra al array `Scripts` los MonoBehaviour que quieras inspeccionar
3. En runtime, pulsa **F1** para abrir/cerrar el menú debug

### Pantalla 1 — Lista de scripts

| Tecla | Acción |
|---|---|
| `↑` / `↓` | Navegar entre scripts |
| `Enter` | Seleccionar script y ver sus campos |
| `F1` | Cerrar menú |

### Pantalla 2 — Campos editables del script seleccionado

| Tecla | Acción |
|---|---|
| `↑` / `↓` | Navegar entre campos |
| `←` / `→` | Disminuir / aumentar valor numérico, ciclar enum |
| `Enter` | Toggle bool / editar string / ciclar enum |
| `Shift` + `←`/`→` | Paso rápido (x10 para int, x10 para float) |
| `Backspace` | Volver a la lista de scripts |

### Pantalla 3 — Edición de texto (strings)

| Tecla | Acción |
|---|---|
| Teclado | Escribir caracteres |
| `Backspace` | Borrar último carácter |
| `Enter` | Confirmar |
| `Escape` | Cancelar |

### Tipos soportados para edición

- **Numéricos**: `int`, `long`, `float`, `double` (incremento/decremento con paso configurable)
- **Bool**: toggle con Enter o flechas
- **String**: edición de texto completa
- **Enums** (incluido `KeyCode`): ciclar valores con flechas
- **Vector2/3/4**: cada componente (x, y, z, w) se muestra y edita individualmente
- **Color**: componentes r, g, b, a editables (clamped a 0-1)
- **Quaternion**: editado como ángulos Euler (x°, y°, z°)

Los tipos no soportados (arrays, referencias a objetos, clases custom) se muestran como **solo lectura** en gris con la etiqueta `[ro]`.

El menú usa **reflexión** para descubrir automáticamente todos los campos que Unity muestra en el Inspector (campos públicos y campos privados con `[SerializeField]`), exactamente los mismos atributos que ves en el editor.

---

**User**

añade al ScriptDebugInspector las teclas y funcionalidad para poder hacer scroll hacia arriba y hacia abajo dentro de la ui

---

**Cursor**

El script ya tiene las variables `scrollUpKey`/`scrollDownKey` y el código en `Update`, pero hay dos problemas: las hints de scroll no se muestran de forma visible en cada pantalla, y el `AutoScroll` automático sobreescribe el scroll manual en cada frame. Voy a arreglar ambos.

Now I'll update the `DrawScripts` help section to show the scroll hints prominently.

Now update the header line counts since we added 2 more lines to each screen.

Now fix the `AutoScroll` to only trigger when the cursor changes, so manual scroll isn't overridden.

Reset `_prevCursor` on screen transitions so AutoScroll fires correctly when entering a new screen.

Listo. Los cambios realizados:

1. **Hints de scroll visibles en cada pantalla** — Ahora tanto la pantalla de lista de scripts como la de campos editables muestran `[PageUp] / [PageDown] Scroll menú` y `[+] / [-] Zoom UI` en la sección de controles, bien visibles junto al resto de teclas.

2. **AutoScroll corregido** — Antes, el auto-scroll que sigue al cursor se ejecutaba en cada frame de OnGUI, lo que sobreescribía cualquier scroll manual con `PageUp`/`PageDown`. Ahora solo se activa cuando el cursor cambia de posición (`_prevCursor`), permitiendo que el scroll manual funcione libremente sin que el auto-scroll lo anule.

3. **Reset de estado en transiciones** — Al cambiar de pantalla (abrir menú, seleccionar script, volver atrás), se resetea `_prevCursor` para que el auto-scroll recalcule correctamente al entrar en la nueva pantalla.

