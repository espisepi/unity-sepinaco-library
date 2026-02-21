# Previous video key selection in ScriptVideoclipsNew
_Exported on 21/02/2026 at 21:40:37 CET from Cursor (2.5.20)_

---

**User**

añadir en @Assets/Sepinaco/VideoclipsNew/ScriptVideoclipsNew.cs la opcion de previous video key con su correspondiente selección de tecla en el editor de unity

---

**Cursor**

Voy a añadir la opción de vídeo anterior. Como el campo es un `KeyCode` público, el inspector de Unity ya mostrará automáticamente el dropdown para seleccionar la tecla. Necesito hacer 4 cambios:

1. Añadir el campo `previousVideoKey`
2. Detectar la tecla en `Update()`
3. Crear el método `PreviousVideo()`
4. Mostrarlo en el menú OnGUI

Verifiquemos el resultado final.

Todo listo. Estos son los cambios realizados:

- **Campo `previousVideoKey`** (línea 29-30): Nuevo campo público `KeyCode` con valor por defecto `B`. Al ser un `KeyCode` público, Unity mostrará automáticamente el dropdown en el Inspector para seleccionar la tecla deseada.

- **Detección de input en `Update()`** (líneas 98-99): Cuando el menú está activo y se pulsa la tecla configurada, llama a `PreviousVideo()`.

- **Método `PreviousVideo()`** (líneas 163-167): Navega al vídeo anterior con wrap-around circular usando `(currentVideoIndex - 1 + videoClips.Length) % videoClips.Length`, de modo que al estar en el primer vídeo salta al último.

- **Menú OnGUI** (línea 259): Muestra la nueva opción `[B] Vídeo anterior` en el panel de controles, y se aumentó la altura del box de 200 a 230 para acomodar la línea extra.

