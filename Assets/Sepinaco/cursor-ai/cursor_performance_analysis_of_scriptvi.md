# Performance analysis of ScriptVideoclipsNew
_Exported on 21/02/2026 at 23:05:58 CET from Cursor (2.5.20)_

---

**User**

analizame la performance del script @Assets/Sepinaco/VideoclipsNew/ScriptVideoclipsNew.cs teniendo en cuenta que el metodo update() se ejecuta en cada frame, por si se puede mejorar sin sacrificar la calidad y la funcionalidad

---

**Cursor**

He leído el script completo. Aquí va el análisis de performance detallado:

---

## Análisis de Performance: `ScriptVideoclipsNew.cs`

### 1. `Update()` — Ya es bastante eficiente

```86:119:Assets/Sepinaco/VideoclipsNew/ScriptVideoclipsNew.cs
    void Update()
    {
        if (videoClips == null || videoClips.Length == 0) return;

        if (Input.GetKeyDown(menuKey))
            menuActive = !menuActive;

        if (!menuActive) return;

        if (Input.GetKeyDown(toggleTextureKey))
        {
            if (texturesReplaced)
                RestoreOriginalTextures();
            else
                ReplaceSceneTextures();
        }

        if (Input.GetKeyDown(toggleMuteKey))
            ToggleMute();

        if (Input.GetKeyDown(nextVideoKey))
            NextVideo();

        if (Input.GetKeyDown(previousVideoKey))
            PreviousVideo();

        if (Input.GetKey(scrollUpKey))
            scrollPosition.y -= scrollSpeed * Time.deltaTime;

        if (Input.GetKey(scrollDownKey))
            scrollPosition.y += scrollSpeed * Time.deltaTime;

        if (scrollPosition.y < 0f) scrollPosition.y = 0f;
    }
```

**Veredicto: Bien.** Las llamadas a `Input.GetKeyDown` / `Input.GetKey` son extremadamente baratas (lectura de un buffer interno de Unity). Tiene dos early returns inteligentes: uno si no hay clips, y otro si el menú no está activo. En el 99% de los frames, el método sale en las primeras 2-3 instrucciones. No hay allocations, ni búsquedas, ni iteraciones.

**Mejora menor posible:** Cachear la validez de `videoClips` en un `bool` en `Start()` para evitar el acceso a `.Length` cada frame, pero el ahorro es despreciable (nanosegundos).

---

### 2. `OnGUI()` — Este es el VERDADERO problema de performance

```252:290:Assets/Sepinaco/VideoclipsNew/ScriptVideoclipsNew.cs
    void OnGUI()
    {
        if (!menuActive) return;

        InitStyles();

        float boxWidth = 340f;
        float contentHeight = 260f;
        float maxBoxHeight = Mathf.Min(contentHeight, Screen.height * 0.8f);
        float x = 10f;
        float y = 10f;
        Rect boxRect = new Rect(x, y, boxWidth, maxBoxHeight);

        GUI.Box(boxRect, GUIContent.none, boxStyle);

        Rect areaRect = new Rect(x + 16, y + 12, boxWidth - 32, maxBoxHeight - 24);
        GUILayout.BeginArea(areaRect);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Video Controls", titleStyle);
        GUILayout.Space(10);

        string videoName = "---";
        if (videoClips != null && currentVideoIndex < videoClips.Length && videoClips[currentVideoIndex] != null)
            videoName = videoClips[currentVideoIndex].name;

        GUILayout.Label($"<b>[{toggleTextureKey}]</b>  Texturas: {(texturesReplaced ? "<color=#FF6666>Vídeo</color>" : "<color=#66FF66>Original</color>")}", labelStyle);
        GUILayout.Label($"<b>[{toggleMuteKey}]</b>  Audio: {(isMuted ? "<color=#FF6666>Mute</color>" : "<color=#66FF66>On</color>")}", labelStyle);
        GUILayout.Label($"<b>[{nextVideoKey}]</b>  Siguiente vídeo", labelStyle);
        GUILayout.Label($"<b>[{previousVideoKey}]</b>  Vídeo anterior", labelStyle);
        GUILayout.Label($"<b>[{scrollUpKey}]</b> / <b>[{scrollDownKey}]</b>  Scroll menú", labelStyle);
        GUILayout.Space(8);
        GUILayout.Label($"<b>Vídeo:</b> {videoName}  ({currentVideoIndex + 1}/{videoClips.Length})", labelStyle);
        GUILayout.Space(4);
        GUILayout.Label($"<color=#888888>[{menuKey}] para cerrar</color>", labelStyle);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
```

Aquí hay **tres problemas importantes**:

**a) `OnGUI` se llama VARIAS veces por frame** (mínimo 2: una para Layout, otra para Repaint). Cada llamada ejecuta todo el cuerpo del método.

**b) Allocations de strings cada llamada.** Cada `$"..."` (string interpolation) crea un nuevo `string` en el heap. Tienes **7 interpolaciones** que se ejecutan 2+ veces por frame. Eso son **14+ allocations de string por frame** mientras el menú está abierto, lo cual genera presión de GC constante.

**c) El sistema IMGUI (OnGUI) en general** es ineficiente comparado con Unity UI (Canvas). Cada `GUILayout.Label` internamente calcula layout, genera meshes de texto, etc.

---

### 3. `ReplaceSceneTextures()` — Allocation innecesaria

```188:204:Assets/Sepinaco/VideoclipsNew/ScriptVideoclipsNew.cs
    void ReplaceSceneTextures()
    {
        foreach (var kvp in originalMaterials)
        {
            Renderer rend = kvp.Key;
            if (rend == null) continue;

            Material[] mats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = videoMaterial;

            rend.sharedMaterials = mats;
        }
        // ...
    }
```

Cada llamada crea un `new Material[]` por cada renderer. No es per-frame, pero si el usuario alterna texturas frecuentemente, genera garbage innecesario. Se podría cachear un array reutilizable.

---

### 4. `Shader.Find` en runtime

```139:139:Assets/Sepinaco/VideoclipsNew/ScriptVideoclipsNew.cs
        videoMaterial = new Material(Shader.Find("Unlit/Texture"));
```

`Shader.Find` busca por nombre en todos los shaders cargados. Se ejecuta solo una vez en `Start()`, así que no es crítico, pero si el shader no está incluido en el build, fallará silenciosamente. Es mejor referenciar el shader como campo serializado.

---

## Recomendaciones concretas

Te propongo aplicar las optimizaciones que realmente impactan, sin cambiar funcionalidad ni aspecto visual. Las ordeno por impacto:

| Prioridad | Problema | Impacto |
|-----------|----------|---------|
| **Alta** | Strings en `OnGUI` generan GC cada frame | Cachear strings, regenerar solo cuando cambia el estado |
| **Media** | `Shader.Find` por nombre | Usar campo serializado `[SerializeField] Shader` |
| **Media** | `Material[]` allocation en toggle | Cachear arrays por renderer |
| **Baja** | Check de `videoClips` en Update | Cachear como `bool` |

La optimización de mayor impacto real (migrar a Canvas UI) sería un cambio arquitectónico grande. Lo que sí puedo hacer ahora es **cachear todos los strings del OnGUI** para eliminar las allocations per-frame, que es el cambio con mejor relación esfuerzo/impacto.

Quieres que aplique estas optimizaciones al script?

---

**User**

hazme las mejoras de cacheo

---

**Cursor**

Voy a aplicar todas las optimizaciones de cacheo. Son cambios en prácticamente todos los métodos, así que voy a reorganizar el archivo completo.

Listo. Aquí tienes el resumen de todas las optimizaciones aplicadas:

---

### 1. Strings de OnGUI cacheados (mayor impacto)

Se eliminaron las **14+ allocations de string por frame** cuando el menú está abierto. Ahora hay dos niveles de caché:

- **Strings estáticos** (`BuildStaticGUIStrings`): Los 4 labels que nunca cambian (siguiente, anterior, scroll, cerrar) se construyen una sola vez en `Start()`.

- **Strings dinámicos** (`RebuildDynamicGUIStrings`): Los 3 labels que dependen del estado (textura, mute, vídeo actual) solo se reconstruyen cuando `guiStringsDirty = true`, es decir, cuando el usuario realmente cambia algo. En `OnGUI` solo se pasan las referencias cacheadas.

### 2. Material[] arrays pre-construidos

`BuildVideoMaterialArrays()` crea los arrays de materiales de vídeo una sola vez en `Start()`. Antes, `ReplaceSceneTextures()` creaba un `new Material[]` por cada renderer en cada toggle. Ahora simplemente asigna los arrays ya existentes del diccionario `videoMaterialArrays`.

### 3. Shader serializado con fallback

Nuevo campo público `videoShader` en el Inspector. Si lo asignas desde el editor, evitas `Shader.Find` en runtime completamente. Si no lo asignas, hace fallback a `Shader.Find("Unlit/Texture")` como antes, manteniendo compatibilidad.

### 4. `hasVideoClips` cacheado en Update

Se reemplazó `if (videoClips == null || videoClips.Length == 0)` por un simple `if (!hasVideoClips)` - una lectura de bool en vez de null check + acceso a propiedad de array cada frame.

### 5. Dirty flag en cada punto de cambio

Se marca `guiStringsDirty = true` en los 4 puntos exactos donde cambia el estado visible del menú:
- `PlayVideo()` (cambia el nombre/índice del vídeo)
- `ReplaceSceneTextures()` (cambia el estado de texturas)
- `RestoreOriginalTextures()` (cambia el estado de texturas)
- `ToggleMute()` (cambia el estado de audio)

---

**Resultado neto:** En frames normales (menú cerrado), el `Update()` hace una lectura de bool y sale. Con el menú abierto, `OnGUI` pasa strings pre-cacheados sin hacer ninguna allocation de memoria, eliminando la presión sobre el Garbage Collector. La funcionalidad y el aspecto visual son idénticos.

