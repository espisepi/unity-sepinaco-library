# Configuración de teclas para zoom de UI
_Exported on 22/02/2026 at 0:52:19 CET from Cursor (2.5.20)_

---

**User**

añade en los scripts @Assets/Sepinaco/ObjectsManager/ScriptObjectsManager.cs ScriptVideoclipsManager.cs ScriptPhysicsManager.cs dos nuevas teclas configurables desde el editor de unity para aumentar y disminuir el zoom de las letras de la ui. Haz que coincidan las teclas entre los scripts por defecto y que no sean ninguna de las teclas usadas anteriormente ni de las prohibidas.

---

**Cursor**

Let me read the three scripts to understand the existing key configurations and UI elements.

Let me also check the TriggerRestoreManager for any additional keys that might be in use.

Let me check if there's a list of prohibited keys documented somewhere.

Now I have a full picture. Let me compile the analysis:

**Keys currently in use:** F1, V, L, H, N, J, T, M, ], [, E, Q, G, X, B, UpArrow, DownArrow
**Prohibited keys:** A, W, S, D, C, R

I'll use `KeyCode.Equals` (`=`/`+`) for zoom in and `KeyCode.Minus` (`-`) for zoom out — universally recognized zoom keys, not used nor prohibited.

Starting with `ScriptObjectsManager.cs`. I'll add the configurable zoom fields, the zoom logic in `Update()`, apply the font size in `InitStyles()`, and show the zoom info in the OnGUI panel.

Now update `InitStyles()` to use `_guiFontSize`:

Now add the zoom info label in OnGUI, right after the scroll label line:

Now let's do `ScriptVideoclipsManager.cs`:

Add private fields after the existing GUI fields:

Add zoom handling in `Update()`:

Update `InitStyles()` to use `guiFontSize`:

Now add the zoom info in OnGUI. I need to add it after the scroll label and also rebuild the static strings that include the scroll label (since `BuildStaticGUIStrings` caches them):

Now let's do `ScriptPhysicsManager.cs`:

Add private fields:

Add zoom handling in `Update()`:

Update `InitStyles()`:

Add zoom info in OnGUI:

Now let me verify all three files look correct.

Los tres archivos están correctos. Aquí tienes el resumen de los cambios aplicados:

---

**Teclas nuevas (idénticas en los 3 scripts):**

| Acción | Tecla por defecto |
|---|---|
| Zoom In (aumentar letras) | **`=`** (`KeyCode.Equals`) |
| Zoom Out (disminuir letras) | **`-`** (`KeyCode.Minus`) |

Estas teclas no colisionan con ninguna tecla usada (F1, V, L, H, N, J, T, M, ], [, E, Q, G, X, B, UpArrow, DownArrow) ni con las prohibidas (A, W, S, D, C, R).

**Qué se añadió en cada script:**

1. **Dos campos `KeyCode` públicos** (`zoomInKey`, `zoomOutKey`) bajo un header "Zoom de la UI", configurables desde el Inspector de Unity.

2. **Lógica en `Update()`** que solo se ejecuta con el menú abierto: al pulsar la tecla de zoom in/out, el tamaño base de fuente aumenta/disminuye en pasos de 2px, con un rango de **8px a 40px**.

3. **Reconstrucción de estilos** cuando cambia el tamaño: se pone `_stylesInitialized = false` para que `InitStyles()` recree los `GUIStyle` con el nuevo `fontSize` (label = tamaño base, title = base + 4).

4. **Línea informativa en el menú OnGUI** que muestra las teclas de zoom y el tamaño actual en píxeles, por ejemplo: `[Equals] / [Minus]  Zoom UI (14px)`.

---

**User**

pon otras teclas distintas ya que quiero que sean teclas que solo tenga que pulsar esa tecla y no una combinacion

---

**Cursor**

Tienes razón, `Equals` y `Minus` pueden requerir combinaciones en algunos teclados. Voy a usar dos teclas de letra simples que no están usadas ni prohibidas:

- **I** — Zoom In (aumentar)
- **O** — Zoom Out (disminuir)

Hecho. Las teclas de zoom ahora son:

| Acción | Tecla |
|---|---|
| Zoom In (aumentar letras) | **I** |
| Zoom Out (disminuir letras) | **O** |

Son teclas de una sola pulsación, coinciden en los tres scripts, y no colisionan con ninguna tecla usada ni prohibida. Se pueden cambiar desde el Inspector de Unity como el resto de controles.

