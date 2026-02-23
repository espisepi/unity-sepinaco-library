# Cambio de teclas en ScriptPostProcessingPixelPs1
_Exported on 22/02/2026 at 2:29:35 CET from Cursor (2.5.20)_

---

**User**

cambia las teclas del script ScriptPostProcessingPixelPs1 para que no use F3 ni las teclas alpha ni ninguna tecla que sea combinacion de teclas

---

**Cursor**

Voy a cambiar todas las teclas Alpha (0-9) y la tecla F3 por teclas individuales simples que no requieran combinaciones. Aquí está el plan de reasignación:

- **Pixel Size**: Alpha1/Alpha2 → `Q` / `W`
- **Color Depth**: Alpha3/Alpha4 → `A` / `S`
- **Jitter Intensity**: Alpha5/Alpha6 → `Z` / `X`
- **Jitter Speed**: Alpha7/Alpha8 → `R` / `T`
- **Dither Intensity**: Alpha9/Alpha0 → `C` / `V`
- **Preset retro sutil**: F3 → `F4`

Listo. Estos son los cambios realizados en las teclas:

| Control | Antes | Ahora |
|---|---|---|
| Pixel Size -/+ | `Alpha1` / `Alpha2` | `Q` / `W` |
| Color Depth -/+ | `Alpha3` / `Alpha4` | `A` / `S` |
| Jitter Intensity -/+ | `Alpha5` / `Alpha6` | `Z` / `X` |
| Jitter Speed -/+ | `Alpha7` / `Alpha8` | `R` / `T` |
| Dither Intensity -/+ | `Alpha9` / `Alpha0` | `C` / `V` |
| Preset retro sutil | `F3` | `F4` |

Todas las demás teclas se mantienen igual (`F1` menú, `E` toggle efecto, `F2` preset auténtico, flechas, `I`/`O` zoom, `PageUp`/`PageDown`). Ninguna tecla requiere combinación y se eliminaron todos los Alpha y el F3.

---

**User**

no uses ninguna tecla prohibida ni que hayamos usado en otros scripts

---

**Cursor**

Voy a revisar los otros scripts para ver qué teclas ya están en uso y evitar conflictos.

Ahora tengo el mapa completo de teclas de todos los scripts del proyecto. Hay varios conflictos con las teclas que asigné antes. Aquí están las teclas ocupadas por **otros scripts**:

- **ScriptObjectsManager**: `V`, `L`, `H`, `N`, `J`
- **ScriptVideoclipsManager**: `T`, `M`
- **ScriptPhysicsManager**: `E`, `Q`, `G`, `X`, `B`
- **PhysicsManagerUI (legacy)**: `D`
- **Compartidas (UI)**: `F1`, `I`, `O`, flechas, `PageUp`, `PageDown`

Voy a reasignar las teclas del PostProcessing usando solo letras libres y F-keys que no estén en uso en ningún otro script:

| Control | Nuevo |
|---|---|
| Toggle efecto | `F5` |
| Pixel Size -/+ | `A` / `S` |
| Color Depth -/+ | `W` / `R` |
| Jitter Intensity -/+ | `Z` / `C` |
| Jitter Speed -/+ | `F` / `K` |
| Dither Intensity -/+ | `Y` / `U` |
| Preset auténtico | `F2` (sin cambio) |
| Preset retro sutil | `F4` (sin cambio) |

Ahora aplico los cambios. Reemplazo el bloque completo de teclas de control:

Listo. Aquí está el resumen final de todas las teclas del script, sin conflictos con ningún otro script del proyecto:

| Control | Tecla | Notas |
|---|---|---|
| Menú on/off | `F1` | Compartida (intencionalmente) con todos los scripts |
| Toggle efecto | `F5` | Libre |
| Pixel Size -/+ | `A` / `S` | Libres |
| Color Depth -/+ | `W` / `R` | Libres |
| Jitter Intensity -/+ | `Z` / `C` | Libres |
| Jitter Speed -/+ | `F` / `K` | Libres |
| Dither Intensity -/+ | `Y` / `U` | Libres |
| Preset auténtico | `F2` | Libre |
| Preset retro sutil | `F4` | Libre |
| Scroll menú | `UpArrow` / `DownArrow` | Compartidas (UI) |
| Zoom UI | `I` / `O` | Compartidas (UI) |
| Ancho UI | `LeftArrow` / `RightArrow` | Compartidas (UI) |
| Alto UI | `PageUp` / `PageDown` | Compartidas (UI) |

Ninguna tecla es Alpha (0-9), ninguna es F3, ninguna requiere combinación, y ninguna colisiona con `ScriptObjectsManager`, `ScriptVideoclipsManager`, `ScriptPhysicsManager` ni `PhysicsManagerUI`.

---

**User**

no uses las teclas F1, F2...F9,F0

---

**Cursor**

Las teclas F que necesito reemplazar en este script son: `F1` (menú), `F5` (toggle efecto), `F2` (preset auténtico), `F4` (preset sutil).

Teclas libres disponibles que no colisionan con ningún script: `P`, `BackQuote` (tecla `), `Home`, `End`.

Ahora actualizo también el comentario de documentación del inicio del archivo que mencionaba F1.

Listo. Aquí está el mapa final completo de teclas del script, sin ninguna tecla F, sin Alpha, sin combinaciones, y sin conflictos con otros scripts:

| Control | Tecla |
|---|---|
| Menú on/off | `` ` `` (BackQuote) |
| Toggle efecto | `P` |
| Pixel Size -/+ | `A` / `S` |
| Color Depth -/+ | `W` / `R` |
| Jitter Intensity -/+ | `Z` / `C` |
| Jitter Speed -/+ | `F` / `K` |
| Dither Intensity -/+ | `Y` / `U` |
| Preset auténtico | `Home` |
| Preset retro sutil | `End` |
| Scroll menú | `UpArrow` / `DownArrow` |
| Zoom UI | `I` / `O` |
| Ancho UI | `LeftArrow` / `RightArrow` |
| Alto UI | `PageUp` / `PageDown` |

---

**User**

añade las teclas de los numeros 1,2,3..9,0 consecutivamente a cada atributo que requiera keycode en el script ScriptPostProcessingPixelPs1.cs

---

**Cursor**

Hay 22 atributos KeyCode en el script pero solo 10 teclas numéricas (1-9, 0). Asignaré las teclas numéricas consecutivamente a los primeros 10 atributos y los 12 restantes conservarán sus teclas actuales.

También actualizo el comentario de documentación.

Listo. Asignación final:

| # | Atributo | Tecla |
|---|---|---|
| 1 | menuKey | `1` |
| 2 | toggleEffectKey | `2` |
| 3 | pixelSizeUpKey | `3` |
| 4 | pixelSizeDownKey | `4` |
| 5 | colorDepthUpKey | `5` |
| 6 | colorDepthDownKey | `6` |
| 7 | jitterIntensityUpKey | `7` |
| 8 | jitterIntensityDownKey | `8` |
| 9 | jitterSpeedUpKey | `9` |
| 10 | jitterSpeedDownKey | `0` |
| 11 | ditherIntensityUpKey | `U` |
| 12 | ditherIntensityDownKey | `Y` |
| 13 | presetAuthenticKey | `Home` |
| 14 | presetSubtleKey | `End` |
| 15-22 | scrollUp/Down, zoom, UI size | Sin cambio (flechas, I/O, PageUp/Down) |

Los primeros 10 atributos usan las teclas numéricas del 1 al 0 consecutivamente. Los 12 restantes conservan sus teclas anteriores.

