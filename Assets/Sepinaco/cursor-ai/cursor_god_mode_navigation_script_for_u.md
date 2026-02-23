# God mode navigation script for Unity
_Exported on 23/02/2026 at 11:50:43 CET from Cursor (2.5.20)_

---

**User**

Añadir un script dentro de la carpeta sepinaco, que se asocie a un gameobject para que desactive todos los controles de la camara y del player en la escena y me añada un control en modo dios para poder navegar dentro de la escena como si lo hiciese en el editor de unity y así ir volando por el escenario en todas las direcciones. Quiero un atributo modificable desde el editor de unity para activar o desactivar el control de la camara en modo dios y cuando esté desactivado quiero que tengamos el control que teníamos previamente a activarlo. Estos atributoas también lo podremos modificar cuando añadamos el script creado al script ScriptDebugInspector para modificar esos atributos desde la ventana debug del script ScriptDebugInspector. Quiero que toda la funcionalidad definida funcione cuando haga la build y no solamente en el unity editor.


---

**Cursor**

Let me explore the project structure first to understand the existing scripts.

Now I have a complete picture of the project. Let me create the God Mode Camera script.

Ahora verifico la integración con `ScriptDebugInspector`. El script ya es 100% compatible porque:

1. **Todos los campos públicos** (`godModeEnabled`, `toggleKey`, `moveSpeed`, `lookSensitivity`, teclas, etc.) son automáticamente visibles para `ScriptDebugInspector` via reflexión.
2. **Los campos `[SerializeField] private`** (`_targetCamera`, `_scriptsToDisable`) también son detectados por el inspector.
3. **`OnValidate()`** está implementado, por lo que cuando `ScriptDebugInspector` modifique `godModeEnabled` y llame a `callOnValidateAfterChange`, el cambio se aplica inmediatamente.

La integración ya está incorporada en el diseño del script. Solo necesitas arrastrar el `ScriptGodModeCamera` al array `_scripts` del `ScriptDebugInspector` en el editor.

El script está creado en `Assets/Sepinaco/GodModeCamera/ScriptGodModeCamera.cs`. Aquí tienes el resumen de lo que hace y cómo usarlo:

---

## ScriptGodModeCamera

### Funcionalidad

- **Modo Dios activable/desactivable** con el atributo `godModeEnabled` desde el Inspector de Unity o con la tecla `F2` en runtime.
- **Al activarse**: desactiva automáticamente todos los MonoBehaviours en la cámara principal y en GameObjects con tag "Player", y toma control total de la cámara para vuelo libre.
- **Al desactivarse**: restaura todos los scripts previamente desactivados y devuelve la cámara a su posición/rotación/padre originales.
- **Funciona en builds**, no depende del editor de Unity.

### Controles de vuelo (estilo editor de Unity)

| Control | Acción |
|---|---|
| **Clic derecho + ratón** | Rotar la cámara (mirar alrededor) |
| **W / A / S / D** | Movimiento adelante/izquierda/atrás/derecha |
| **E / Q** | Subir / bajar |
| **Shift** | Movimiento rápido (x3) |
| **Ctrl** | Movimiento lento (x0.25) |
| **Rueda del ratón** | Ajustar velocidad base |
| **F2** | Activar/desactivar god mode |

Todas las teclas, velocidades, sensibilidades y opciones son configurables desde el Inspector.

### Integración con ScriptDebugInspector

El script es 100% compatible. Solo tienes que:

1. Crear un **GameObject vacío** en la escena y añadirle el componente `ScriptGodModeCamera`.
2. Arrastrar ese componente al array `_scripts` del `ScriptDebugInspector`.
3. Desde el menú debug (F1), podrás navegar y modificar todos los atributos, incluyendo `godModeEnabled` para activar/desactivar el modo dios en runtime.

### Detección automática de controles

Con `autoDetectControls = true` (por defecto), el script detecta y desactiva automáticamente:
- Todos los MonoBehaviours en el GameObject de `Camera.main`
- Todos los MonoBehaviours en GameObjects con tag "Player"

Excluye inteligentemente `ScriptDebugInspector` y a sí mismo para no romper el menú debug. También puedes añadir scripts manualmente al array `_scriptsToDisable` si necesitas desactivar algo específico.

---

**User**

modifica la tecla del ratón para que con el click izquierdo pueda rotar la camara y para activar y desactivar la funcionalidad de modo dios con la tecla 9

---

**Cursor**

Hecho. Los cambios:

- **Tecla de activación**: `F2` -> `9` (`KeyCode.Alpha9`)
- **Botón de ratón para rotar**: clic derecho (`1`) -> clic izquierdo (`0`)

