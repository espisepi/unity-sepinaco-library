# PostProcessingPixelPs1 script creation
_Exported on 22/02/2026 at 2:08:45 CET from Cursor (2.5.20)_

---

**User**

añademe una nueva carpeta dentro de la carpeta sepinaco que se llame PostProcessingPixelPs1 y que contenga un script llamado ScriptPostProcessingPixelPs1 que consista en añadir un efecto de postprocesamiento visual de la escena con un efecto de pixel de la consola ps1 junto con los temblores de las texturas propio de la consola

---

**Cursor**

Let me first look at the project structure and an existing script to understand the conventions used.

Voy a revisar la estructura de carpetas existente y luego crear el script con el efecto PS1.

Perfecto. Voy a crear la carpeta con el script C# principal y un shader para el efecto de post-procesado PS1. El efecto PS1 clásico requiere tanto un script como un shader para funcionar correctamente.

Todo listo. He creado la carpeta `PostProcessingPixelPs1` dentro de `Assets/Sepinaco/` con la siguiente estructura:

```
PostProcessingPixelPs1/
├── ScriptPostProcessingPixelPs1.cs    ← MonoBehaviour para la cámara
└── Shaders/
    └── PS1PostProcess.shader          ← Shader de post-procesado
```

### Qué hace el efecto

El script emula las 4 limitaciones visuales clave de la PlayStation 1:

1. **Pixelación** — Reduce la resolución interna de la imagen, como los 320x240 de la PS1. Configurable con `_pixelSize` (1-16).

2. **Reducción de profundidad de color** — Posteriza a 32 niveles por canal (5 bits), simulando el framebuffer de 15-bit de la PS1. Configurable con `_colorDepth`.

3. **Temblor de texturas (Jitter)** — Desplazamiento UV pseudo-aleatorio por "píxel" que cambia N veces por segundo, imitando la falta de corrección de perspectiva y el vertex snapping de la PS1. Configurable con `_jitterIntensity` y `_jitterSpeed`.

4. **Dithering ordenado (Bayer 4x4)** — Patrón de tramado como el que usaba la PS1 para suavizar gradientes en escenas oscuras. Configurable con `_ditherIntensity`.

### Cómo usarlo

1. Selecciona tu **cámara principal** en la escena.
2. Haz clic en **Add Component** y busca `ScriptPostProcessingPixelPs1`.
3. Ajusta los parámetros desde el Inspector a tu gusto.

El script también incluye dos presets accesibles por código:
- `ApplyAuthenticPS1Preset()` — valores fieles a la consola original.
- `ApplySubtleRetroPreset()` — versión más suave para un toque retro sin ser extremo.

---

**User**

añade al script la busqueda de la camara principal de la escena para no tener que seleccionarla. Quiero que la manera de usarse sea añadiendo este script a cualquier gameobject de la escena para que realice el postprocesamiento

---

**Cursor**

Voy a reestructurar el script para que se pueda colocar en cualquier GameObject. El truco es que `OnRenderImage` solo funciona en scripts adjuntos a la cámara, así que el script añadirá automáticamente un componente auxiliar ligero a la cámara principal.

Ahora voy a añadir la clase auxiliar `PS1CameraHook` al final del archivo, que es el componente ligero que se inyecta en la cámara.

Verifiquemos que el archivo final queda bien.

Todo correcto, sin errores. Aquí el resumen de los cambios:

### Cambios realizados

1. **Eliminado `[RequireComponent(typeof(Camera))]`** — Ya no es necesario que el script esté en la cámara.

2. **Nuevo campo `_targetCamera`** (línea 55) — Campo opcional en el Inspector. Si lo dejas vacío, el script busca automáticamente `Camera.main`. Si necesitas apuntar a una cámara distinta, la arrastras ahí.

3. **Sistema de hook automático** — El script principal (`ScriptPostProcessingPixelPs1`) ya no hace `OnRenderImage` directamente. En su lugar:
   - En `OnEnable` busca la cámara y le inyecta un componente auxiliar invisible (`PS1CameraHook`).
   - En `Update` empuja los parámetros actuales al hook (para que los cambios en el Inspector se reflejen en tiempo real).
   - En `OnDisable` limpia el hook de la cámara.

4. **`PS1CameraHook`** — Componente ligero y oculto (`HideFlags.HideAndDontSave`, sin entrada en `AddComponentMenu`) que vive en la cámara y ejecuta el `OnRenderImage` real.

### Cómo usarlo ahora

1. Crea un **GameObject vacío** en la escena (o usa uno existente).
2. Añádele el componente `ScriptPostProcessingPixelPs1`.
3. Listo. El efecto PS1 se aplica automáticamente sobre `Camera.main`.

---

**User**

tengo estos errore: Shader error in 'Sepinaco/PS1PostProcess': syntax error: unexpected token '[' at line 65 (on metal) Shader error in 'Sepinaco/PS1PostProcess': syntax error: unexpected token 'matrix' at line 59 (on metal)

Compiling Subshader: 0, Pass: <Unnamed Pass 0>, Vertex program with <no keywords>
Platform defines: SHADER_API_DESKTOP UNITY_COLORSPACE_GAMMA UNITY_ENABLE_DETAIL_NORMALMAP UNITY_ENABLE_REFLECTION_BUFFERS UNITY_FRAMEBUFFER_FETCH_AVAILABLE UNITY_LIGHTMAP_FULL_HDR UNITY_LIGHT_PROBE_PROXY_VOLUME UNITY_NEEDS_RENDERPASS_FBFETCH_FALLBACK UNITY_PBS_USE_BRDF1 UNITY_SPECCUBE_BLENDING UNITY_SPECCUBE_BOX_PROJECTION UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS
Disabled keywords: SHADER_API_GLES30 


---

**Cursor**

El problema es que `matrix` es una palabra reservada en HLSL/Metal, y la inicialización de arrays locales con `const float[16] = {...}` no es compatible con Metal. Voy a reescribir la función de dithering con un enfoque matemático que funciona en todas las plataformas.

Hmm, mi fórmula matemática no reproduce correctamente la matriz Bayer. Voy a usar un enfoque con `dot` products y `float4` que es totalmente compatible con Metal y produce los valores exactos.

Listo. Los dos problemas eran:

1. **`matrix` es palabra reservada** en HLSL/Metal — ya no se usa.
2. **`const float[16] = {...}` no compila en Metal** — los arrays locales con inicialización estilo C no están soportados en el backend Metal de Unity.

La solución usa `float4` como vectores constantes y `dot()` con máscaras de selección (`step`) para indexar la matriz Bayer de forma **branchless** y **100% compatible con Metal, OpenGL y Vulkan**. El resultado numérico es idéntico al original.

