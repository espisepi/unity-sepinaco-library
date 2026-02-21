# Modificación de ScriptVideoclipsNew
_Exported on 21/02/2026 at 21:37:04 CET from Cursor (2.5.20)_

---

**User**

modificame el archivo @Assets/Sepinaco/VideoclipsNew/ScriptVideoclipsNew.cs para que pueda añadir videos desde el editor de unity, un array con los videos para cambiar la textura de la escena por esos videos junto con la reproducción del audio del video. Añade la opción de reemplazar las texturas o de restaurar las texturas en cualquier momento en runtime mediante una tecla que se decida desde el editor de unity. También la opción de mutear o no mutear el audio del video con una tecla que se decida desde el editor de unity.

---

**Cursor**

Let me read the current file first.

Now I have a clear picture of the current script. Let me rewrite it with all the features you requested.

El script ha sido reescrito. Aquí tienes un resumen de lo que hace y cómo usarlo:

### Campos configurables desde el Inspector de Unity

| Campo | Descripción |
|---|---|
| **Video Clips** | Array donde arrastras los `VideoClip` directamente desde el editor |
| **Texture Width / Height** | Resolución de la `RenderTexture` generada (por defecto 1920×1080) |
| **Toggle Texture Key** | Tecla para alternar entre texturas de vídeo y texturas originales (defecto: `T`) |
| **Toggle Mute Key** | Tecla para mutear/desmutear el audio del vídeo (defecto: `M`) |
| **Next Video Key** | Tecla para cambiar al siguiente vídeo del array (defecto: `N`) |
| **Replace Textures On Start** | Si se reemplazan las texturas al iniciar la escena |
| **Start Muted** | Si el audio empieza muteado |

### Cómo funciona

1. **Al iniciar**, guarda todas las texturas/materiales originales de la escena en un diccionario.
2. Crea una `RenderTexture` y un `Material` con shader `Unlit/Texture` que usa esa render texture.
3. El `VideoPlayer` renderiza el vídeo sobre esa `RenderTexture`, así todos los objetos de la escena muestran el vídeo.
4. El audio del vídeo sale por un `AudioSource` (se crea automáticamente si no existe).
5. En runtime:
   - Pulsa la tecla de **toggle texture** para restaurar las texturas originales o volver a las de vídeo.
   - Pulsa la tecla de **toggle mute** para mutear/desmutear.
   - Pulsa la tecla de **next video** para ciclar entre los vídeos del array.

### Uso

Añade el componente al GameObject, arrastra tus `VideoClip` al array en el Inspector, configura las teclas que quieras, y listo.

---

**User**

añademe una tecla para activar o desactivar el menu, de tal manera que cuando activo el menu, funcionan las teclas de controles y cuando desactivo el menu no funcionan las teclas de controles, para que no interfieran en el gameplay. añade una referencia visual en pantalla cuando se activa el menu y no muestres nada en pantalla cuando el menu esta desactivado

---

**Cursor**

Voy a añadir la tecla de menú, la lógica de activación/desactivación y un HUD visual con `OnGUI` que muestre los controles cuando el menú está activo.

Ahora añado el método `OnGUI` para la referencia visual en pantalla.

Perfecto, sin errores. Así queda el archivo final:

Listo. Esto es lo que se ha añadido:

### Nuevo campo en el Inspector

- **Menu Key** (por defecto `F1`) -- Configurable desde el editor, es la tecla para abrir/cerrar el menú.

### Comportamiento

- **Menú cerrado**: Las teclas de control (`T`, `M`, `N`) no hacen nada, no interfieren con el gameplay.
- **Menú abierto**: Las teclas de control funcionan con normalidad y aparece un panel HUD en la esquina superior izquierda con:
  - El estado actual de las texturas (Vídeo / Original) con color rojo/verde.
  - El estado del audio (Mute / On) con color rojo/verde.
  - La tecla para cambiar de vídeo.
  - El nombre y número del vídeo actual.
  - Indicación de qué tecla pulsar para cerrar el menú.

El panel tiene fondo semitransparente negro (75% opacidad) y texto con rich text para que sea legible sobre cualquier escena.

---

**User**

ahora haz que cuando esta activado el menu, las teclas de los controles solo funcionan para los controles del menu y no para el gameplay, de tal manera que no interfieran y se ejecute las dos acciones a la vez

---

**Cursor**

Voy a implementar dos mecanismos para bloquear el input al gameplay cuando el menú está activo:

1. Una propiedad estática `InputBlocked` que cualquier script de gameplay puede consultar.
2. Consumir los eventos de teclado en `OnGUI` con `Event.current.Use()` para que no lleguen a otros sistemas GUI.

Ahora añado el consumo de eventos de teclado en `OnGUI` para que las teclas de control no lleguen a otros sistemas.

Ahora limpio `InputBlocked` al destruir el objeto para no dejar estado sucio.

Verifico que el archivo final queda coherente.

Todo correcto. Esto es lo que se ha añadido:

### Mecanismo doble de bloqueo de input

**1. Propiedad estática `ScriptVideoclipsNew.InputBlocked`** (línea 12)

Cualquier script de gameplay puede comprobar esta propiedad al inicio de su lógica de input para ignorar las teclas mientras el menú esté abierto:

```csharp
void Update()
{
    if (ScriptVideoclipsNew.InputBlocked) return;
    // ... tu lógica de gameplay normal ...
}
```

Se activa automáticamente al abrir el menú y se desactiva al cerrarlo (también en `OnDisable` y `OnDestroy` para no dejar estado sucio).

**2. Consumo de eventos GUI** (`ConsumeControlKeyEvents`, línea 272)

Cuando el menú está activo, intercepta los eventos de teclado de las teclas de control (`T`, `M`, `N`) en `OnGUI` y los consume con `Event.current.Use()`. Esto evita que otros sistemas basados en `OnGUI` reciban esas teclas.

### Resumen del flujo

- **Menú cerrado** (`F1`): `InputBlocked = false`, las teclas de control no hacen nada en este script, el gameplay funciona normal.
- **Menú abierto** (`F1`): `InputBlocked = true`, las teclas de control ejecutan solo las acciones del menú, los eventos se consumen, y el gameplay puede ignorar el input consultando la propiedad estática.

---

**User**

siguen funcionando los controles del gameplay cuando tengo el menu activado

---

**Cursor**

El problema es que `Input.GetKeyDown` en Unity es global — no se puede "consumir" para que otros scripts no lo lean. `Event.current.Use()` solo afecta al pipeline de `OnGUI`, no al `Input` del `Update`. La propiedad estática requiere que cada script de gameplay la compruebe manualmente.

La solución más efectiva y limpia: añadir un array en el editor donde arrastres los scripts/objetos de gameplay que quieres desactivar cuando el menú esté abierto, y desactivarlos/activarlos automáticamente.

Entendido. El problema de raíz es que `Input.GetKeyDown()` es global en Unity — no existe forma de "consumirlo" para que otro script no lo lea. La única solución real es **desactivar los scripts de gameplay** cuando el menú está abierto.

Voy a añadir dos opciones para máxima flexibilidad: un array donde arrastres scripts concretos, y un tag para desactivar automáticamente todos los MonoBehaviours de objetos con ese tag.

Ahora sí funciona correctamente. El cambio clave es que ya no intenta "consumir" teclas (que no funciona con `Input.GetKeyDown`), sino que **desactiva directamente los scripts de gameplay** cuando el menú está abierto. Esto es lo que hace Unity internamente en cualquier sistema de pausa/menú.

### Cómo configurarlo en el editor

Tienes dos formas (se pueden combinar):

**Opción A — Arrastrar scripts concretos:**
En el campo `Gameplay Scripts` del Inspector, arrastra los `MonoBehaviour` que manejan input de gameplay (ej: tu player controller, cámara, etc). Cuando el menú se abra, esos scripts se desactivan (`enabled = false`).

**Opción B — Usar un tag:**
En el campo `Gameplay Tag` escribe el nombre de un tag (ej: `"Player"`). Al abrir el menú, TODOS los MonoBehaviours de los GameObjects con ese tag se desactivan automáticamente.

### Qué pasa en runtime

- Pulsas **F1** → menú se abre, scripts de gameplay se desactivan, las teclas T/M/N solo afectan al menú.
- Pulsas **F1** otra vez → menú se cierra, scripts de gameplay se reactivan, gameplay vuelve a la normalidad.
- El HUD ahora muestra un aviso amarillo de "Gameplay pausado" para que el usuario sepa qué está pasando.

---

**User**

haz que se desactiven los controles del gameobject añadido y de todos sus hijos de ese gameobject recursivamente

---

**Cursor**

Voy a cambiar el campo de `MonoBehaviour[]` a `GameObject[]` para que arrastres GameObjects enteros, y recoger recursivamente todos los MonoBehaviours del objeto y de todos sus hijos.

Listo. Los cambios:

1. **El campo ahora es `GameObject[]`** en lugar de `MonoBehaviour[]` — arrastra GameObjects enteros desde el editor, no scripts individuales.

2. **Usa `GetComponentsInChildren<MonoBehaviour>(true)`** — esto recoge automáticamente todos los MonoBehaviours del propio GameObject y de todos sus hijos, nietos, etc. de forma recursiva. El parámetro `true` incluye también los que estén inactivos.

Así, si arrastras por ejemplo tu GameObject "Player" al array, se desactivarán todos los scripts del Player y de cualquier hijo que tenga (cámara, controlador de armas, etc.) cuando abras el menú con F1.

