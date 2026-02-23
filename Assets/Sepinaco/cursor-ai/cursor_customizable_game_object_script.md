# Customizable game object script in Unity
_Exported on 23/02/2026 at 1:02:59 CET from Cursor (2.5.20)_

---

**User**

Crear script que añada un gameobject de una esfera
que ocupe todo el mapa sin fisicas para verla como si fuese un cielo desde la escena (para eso la esfera tiene que renderizarse para verlo desde dentro, quiero crear un atributo en el editor de unity para poder modificar si quiero que la esfera se renderice por dentro, por fuera o por ambas partes y que por defecto esté seleccionado por dentro). La esfera tendra de atributos configurables desde el editor de unity la opción de que haga una rotación continua o no la haga junto con los valores modificables de velocidad y dirección de la rotación. También tendrá otro atributo modificable desde el editor de unity para mostrar o no mostrar visualmente la esfera en la escena. También tendrá atributos para modificar la posición, escala y rotación de la esfera. Y tendrá un atributo para poder cambiar la esfera por un cubo o cualquier otro objeto por defecto de unity o un mesh u objeto añadido al array de todos los elementos que hay para reemplazar la esfera. Teniendo definidos algunos por defecto, que son los que vienen por defecto en unity y dando la posibilidad de añadir más o eliminarlos respectivamente desde el menu del editor de unity. Creame el script dentro de la carpeta Sepinaco y crea una nueva carpeta en donde añadas el fichero de ese script

---

**Cursor**

Let me check an existing script to follow the project's coding style.

Voy a crear el script con todas las funcionalidades que pides. Necesito crear 3 archivos: el script principal, un editor personalizado para el Inspector, y un shader que soporte el renderizado configurable por caras.

Los 3 archivos están creados. Verifiquemos la estructura final.

Perfecto. Se han creado los 3 archivos en la nueva carpeta `Assets/Sepinaco/SkyDome/`. Aquí tienes un resumen de lo que hace cada uno:

---

### Estructura creada

```
Assets/Sepinaco/SkyDome/
├── ScriptSkyDome.cs              ← Script principal (MonoBehaviour)
├── Editor/
│   └── ScriptSkyDomeEditor.cs    ← Custom Editor para el Inspector
└── Shaders/
    └── SkyDomeCullable.shader    ← Shader con Cull configurable
```

---

### `ScriptSkyDome.cs` - Funcionalidades

**Renderizado configurable** (`SkyDomeRenderFace`):
- **Inside** (por defecto) - Renderiza solo el interior, para ver como cielo desde dentro
- **Outside** - Renderizado normal desde fuera
- **Both** - Ambas caras

**Visibilidad**: Toggle para mostrar/ocultar el domo en la escena.

**Transform del Domo**: Posición, escala (por defecto 1000x1000x1000 para cubrir todo el mapa) y rotación, todo configurable desde el Inspector.

**Rotación Continua**: Toggle para activar/desactivar, con velocidad (°/s) y eje de rotación configurables. Los controles de velocidad/eje solo aparecen cuando la rotación está activada.

**Forma (Mesh)**: Lista de meshes con las 6 primitivas de Unity por defecto (Esfera, Cubo, Cilindro, Cápsula, Plano, Quad). Dropdown para seleccionar el mesh activo. Botones para:
- **+ Añadir Mesh Personalizado** - Crea una entrada donde puedes arrastrar cualquier mesh externo
- **Restaurar Primitivas** - Regenera la lista con los valores por defecto
- **✕** - Eliminar entradas personalizadas (las built-in no se pueden borrar individualmente)

**Material**: Campo opcional para material personalizado. Si se deja vacío, usa el shader `Sepinaco/SkyDome` con un color cielo azul por defecto.

**Sin Físicas**: El script elimina automáticamente cualquier Collider y Rigidbody del GameObject al añadirlo.

---

### Cómo usarlo en Unity

1. Crea un **GameObject vacío** en la escena
2. Añade el componente **ScriptSkyDome**
3. Se crearán automáticamente el `MeshFilter` y `MeshRenderer`, se cargará la esfera por defecto y se eliminará cualquier collider
4. Configura los atributos desde el Inspector a tu gusto

---

**User**

no veo la esfera cuando ejecuto la escena, puede ser porque está a mucha distancia y unity no la renderiza por performance?

---

**Cursor**

Sí, es muy probable. El problema está en el shader: por defecto, Unity recorta (clip) los vértices que exceden el **far clipping plane** de la cámara. Con una escala de 1000, los vértices de la esfera están a ~500 unidades, y si el far clip de la cámara es menor, no se renderiza nada.

La solución correcta para un sky dome es hacer lo mismo que Unity hace internamente con su skybox: **proyectar los vértices al far plane** en el vertex shader, de forma que nunca se recorten sin importar la escala. Además voy a mejorar el fallback del script por si el shader no se encuentra.

Veo los dos problemas. Voy a corregirlos:

1. **Shader**: Los vértices se proyectan en clip-space normal, y si superan el far clip de la cámara, se recortan. La solución es la misma que usa Unity internamente en su skybox: forzar la profundidad al far plane en el vertex shader.

2. **Script**: Si el shader `Sepinaco/SkyDome` no se encuentra (error de compilación, URP, etc.), el fallback a `Unlit/Color` no soporta la propiedad `_Cull`, así que el Cull Back por defecto hace la esfera invisible desde dentro.

Ahora arreglo el fallback del shader en el script y añado diagnóstico.

Ahora añado recuperación automática de meshes perdidos en `ApplyMesh`.

Verifico que ambos archivos queden bien.

Todo limpio. Aquí el resumen de los cambios:

---

### Cambios realizados

**Shader (`SkyDomeCullable.shader`)** - Fix principal:

```50:57:Assets/Sepinaco/SkyDome/Shaders/SkyDomeCullable.shader
                // Force depth to the far clip plane so the dome is never
                // clipped regardless of its scale or camera far-clip distance.
                // Same technique Unity uses internally for the built-in Skybox.
                #if defined(UNITY_REVERSED_Z)
                o.vertex.z = 1.0e-6;
                #else
                o.vertex.z = o.vertex.w - 1.0e-6;
                #endif
```

Tras transformar el vértice a clip-space, se fuerza la coordenada Z al **far clip plane**. Es la misma tecnica que usa Unity internamente en su Skybox. Esto garantiza que:
- Los vértices **nunca se recorten** por el far clipping plane, sin importar la escala del domo ni la configuracion de la cámara
- La profundidad queda al fondo de la escena (combinado con `ZWrite Off` y `Queue=Background+1`), así toda la geometría se renderiza por encima

**Script (`ScriptSkyDome.cs`)** - Mejoras de robustez:

1. **Mejor fallback de shader**: Si `Sepinaco/SkyDome` no se encuentra (p.ej. si usas URP/HDRP donde shaders built-in no compilan), ahora usa `Sprites/Default` como fallback (que tiene `Cull Off` nativo) en vez de `Unlit/Color` (que no soporta `_Cull` y dejaba la esfera invisible desde dentro). Ademas muestra un warning en consola para ayudar a diagnosticar.

2. **Recuperación automática de meshes**: Si la referencia a un mesh built-in se pierde durante serialización/domain reload, se regenera automáticamente en vez de dejar el MeshFilter vacío.

