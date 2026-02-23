# Efecto de postprocesamiento en build
_Exported on 22/02/2026 at 2:56:14 CET from Cursor (2.5.20)_

---

**User**

el efecto de postprocessing del script ScriptPostProcessingPixelPs1.cs no funciona al hacer la build y ejecutarla en mi mac m2 pro, haciendo la build para mac intel y mac apple

---

**Cursor**

Let me start by reading the post-processing script to understand how it works and identify potential build issues.

Ahora necesito ver el shader para identificar el problema.

Ya encontré el problema. Es un problema clásico de Unity: **`Shader.Find()` no funciona en builds** para shaders que no están referenciados directamente por ningún material en escenas del proyecto.

En el editor funciona porque Unity tiene acceso a todos los assets, pero al hacer la build, Unity "strippea" (elimina) los shaders que no detecta como necesarios. Como tu shader solo se carga dinámicamente con `Shader.Find("Sepinaco/PS1PostProcess")` en la línea 555, Unity no lo incluye en la build.

La solución más robusta es usar una referencia serializada al shader en lugar de `Shader.Find()`. Así Unity sabe que debe incluirlo en la build.

Ahora paso la referencia del shader al hook en `AttachHook()`:

Ahora actualizo `PS1CameraHook` para recibir y usar la referencia directa al shader, con fallback a `Shader.Find()`:

Ahora actualizo `EnsureMaterial()` para usar la referencia directa con fallback:

Todo está correcto. Aquí va el resumen de lo que pasaba y lo que he hecho:

---

## El problema

`Shader.Find("Sepinaco/PS1PostProcess")` **funciona en el editor pero falla en builds**. Unity "strippea" (elimina) de la build los shaders que no detecta como referenciados por ningún material asignado a objetos en las escenas. Como tu shader solo se cargaba dinámicamente en runtime con `Shader.Find()`, Unity no lo incluía en la build, y el efecto desaparecía silenciosamente.

## La solución

He añadido un campo serializado `_ps1Shader` al componente `ScriptPostProcessingPixelPs1`:

```67:70:/Users/joseangel/Proyectos/Myproject/Assets/Sepinaco/PostProcessingPixelPs1/ScriptPostProcessingPixelPs1.cs
    [Header("Shader")]
    [Tooltip("Referencia directa al shader PS1PostProcess. Necesaria para que funcione en builds. " +
             "Si se deja vacío se intenta Shader.Find como fallback (solo funciona en el editor).")]
    [SerializeField] private Shader _ps1Shader;
```

Esta referencia se pasa al `PS1CameraHook`, que ahora usa primero la referencia directa y solo recurre a `Shader.Find()` como fallback:

```562:565:/Users/joseangel/Proyectos/Myproject/Assets/Sepinaco/PostProcessingPixelPs1/ScriptPostProcessingPixelPs1.cs
        Shader shader = _shaderRef;

        if (shader == null)
            shader = Shader.Find(ShaderName);
```

## Lo que tienes que hacer en Unity

1. Selecciona el **GameObject** que tiene el componente `ScriptPostProcessingPixelPs1`
2. En el Inspector verás un nuevo campo **"Ps1 Shader"** bajo la sección "Shader"
3. Arrastra ahí el archivo `PS1PostProcess.shader` (que está en `Assets/Sepinaco/PostProcessingPixelPs1/Shaders/`)
4. Guarda la escena y haz la build de nuevo

Con la referencia directa, Unity detecta que el shader es necesario y lo incluye en la build tanto para Mac Intel como Apple Silicon.

