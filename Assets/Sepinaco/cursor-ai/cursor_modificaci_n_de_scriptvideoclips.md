# Modificación de ScriptVideoclipsManager.cs
_Exported on 22/02/2026 at 0:03:42 CET from Cursor (2.5.20)_

---

**User**

modifica el script ScriptVideoclipsManager.cs para que cuando se activen o desactiven el reemplazo de texturas, se compruebe si han cambiado el numero de gameobjects de la escena, y si han cambiado se vuelve a iterar sobre los gameobjects de la escena para reemplazar la textura en aquellos gameobjects no trackeados previamente y se actualiza la cache. Ya que actualmente tengo el script @Assets/Sepinaco/ObjectsManager/ScriptObjectsManager.cs que añade y quita gameobjects a la escena en runtime y esos gameobject no estaban previamente cacheados y no se modificaba su textura

---

**Cursor**

Let me read both files to understand the current implementation.

Now I understand the problem. The `StoreOriginalMaterials()` runs once at `Start()`, so any GameObjects added/removed at runtime by `ScriptObjectsManager` are not tracked. When toggling textures, those new objects are missed.

I'll modify `ScriptVideoclipsManager` to detect scene changes and update the cache accordingly.

Now I need to call `RefreshRenderersIfNeeded()` before replacing or restoring textures.

Let me verify the final state of the file.

Todo correcto. Estos son los cambios realizados:

**1. Nuevo campo `cachedRendererCount`** (linea 64) -- almacena el numero de `Renderer` que habia en la escena cuando se construyo la cache.

**2. Se actualiza `StoreOriginalMaterials()`** (linea 153) -- al final del escaneo inicial guarda el conteo de renderers.

**3. Nuevo metodo `RefreshRenderersIfNeeded()`** (lineas 156-177) -- su logica es:
   - Obtiene todos los `Renderer` actuales de la escena con `FindObjectsOfType<Renderer>()`
   - Si el conteo es igual al cacheado, no hace nada (salida rapida)
   - Si ha cambiado, itera sobre los renderers encontrados y para aquellos **no presentes** en `originalMaterials` (nuevos GameObjects anadidos por `ScriptObjectsManager`):
     - Guarda sus materiales originales en `originalMaterials`
     - Crea su array de video material en `videoMaterialArrays`
   - Actualiza `cachedRendererCount`

**4. Se llama a `RefreshRenderersIfNeeded()` al inicio de `ReplaceSceneTextures()` y `RestoreOriginalTextures()`** (lineas 271 y 286) -- de esta forma, cada vez que se pulsa la tecla de toggle de texturas, se comprueba si hay nuevos GameObjects en la escena y se les aplica el tratamiento correspondiente (reemplazo o restauracion).

