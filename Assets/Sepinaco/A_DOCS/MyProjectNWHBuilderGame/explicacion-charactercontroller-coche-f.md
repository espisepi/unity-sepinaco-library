# Sistema de CharacterController y cambio a coche con tecla F

Este documento explica en detalle como funciona en la escena `PrototypeScene_CharacterController` el paso de control entre:

- personaje (`SimpleCharacterController`)
- coche (`CarControllerInput` + `PlayerController`)
- camara (re-parenting entre rig de personaje y rig de coche)

## 1) Donde vive la logica en la escena

En esta escena se instancia el prefab:

- `GameController_WithEnterExitLogic`

Ese prefab contiene la configuracion de:

- `SimpleCharacterController`
- `CharacterInput`

Y ademas usa el `PlayerController` para gestionar el control del vehiculo.

No es una logica aislada en un unico script de la escena; es un flujo entre varios componentes conectados por eventos y `Initialize/Uninitialize`.

## 2) Control del personaje (antes de entrar al coche)

El movimiento del personaje se hace en:

- `SimpleCharacterController.Update()`

Puntos clave:

- Lee `Input.MoveInput` para desplazamiento.
- Usa `CharacterController.SimpleMove()` para mover.
- Rota el cuerpo con eje horizontal de vista.
- Rota la camara en vertical (clamp de angulo).

Cuando el objeto del personaje se activa/desactiva:

- `OnEnable()` activa `CharacterInput`
- `OnDisable()` desactiva `CharacterInput`

Esto es importante porque el sistema de control queda automaticamente habilitado o deshabilitado segun el estado del `GameObject`.

## 3) La tecla F en modo personaje: intento de entrar al coche

La escucha `CharacterInput`:

- `EnterExitKeyboardKey = KeyCode.F`

Cuando se pulsa `F`:

- dispara evento `OnEntrerInCar`

`SimpleCharacterController` se suscribe a ese evento y llama `TryEnterCar()`.

### Que hace `TryEnterCar()`

1. Busca o crea `PlayerController` si no existe.
2. Obtiene referencia al `CameraParentInCar` (desde `CameraController` del player).
3. Hace raycast desde la camara hacia delante (2 metros).
4. Si el collider impactado pertenece a un `CarController`, entra al coche:
   - desactiva el personaje con `gameObject.SetActive(false)`
   - llama `PlayerControllerForCar.EnterInCar(car)`
   - se suscribe a `PlayerControllerForCar.OnExitAction`
   - mueve la camara al parent de camara del coche (`CameraParentInCar`)
   - resetea `localPosition` y `localRotation`

Resultado: deja de controlarse el personaje y pasa a controlarse el vehiculo.

## 4) Como se conecta el control del vehiculo al entrar

La entrada real al coche pasa por:

- `PlayerController.EnterInCar(car)` -> `Initialize(car)`

En `PlayerController.Initialize()` se inicializan los objetos de `InitializeObjects`, entre ellos:

- `CarControllerInput`
- `CameraController`
- UI de estado del coche

La conexion del input al vehiculo ocurre en:

- `CarControllerInput.Initialize()`

Aqui asigna:

- `Car.CarControl = this`

Eso significa que el `CarController` empieza a leer este provider de control humano.

## 5) La tecla F en modo coche: salir del vehiculo

Cuando ya estas dentro del coche, `F` la procesa `CarControllerInput`:

- `EnterExitKey = KeyCode.F`

Al pulsar:

- `TryExitFromCar()` busca el `PlayerController` padre
- llama `playerController.ExitFromCar()`

## 6) Como se desconectan los controles del vehiculo al salir

`PlayerController.ExitFromCar()` hace:

1. guarda referencia al coche actual
2. llama `Uninitialize()`
3. dispara `OnExitAction(car)`

Durante `Uninitialize()`:

- se limpia estado de player sobre el coche
- se paran/ajustan elementos asociados
- y, sobre todo, cada objeto inicializado hace su `Uninitialize()`

En `CarControllerInput.Uninitialize()`:

- si el coche tenia este control asignado, lo suelta:
  - `Car.CarControl = null`

Con eso el coche deja de estar controlado por el jugador.

## 7) Cambio de camara al entrar/salir (como se conecta y desconecta)

La camara no se destruye ni crea cada vez: se reubica cambiando su parent.

### Al entrar al coche

En `SimpleCharacterController.TryEnterCar()`:

- `Camera.transform.SetParent(CameraParentInCar);`
- `localPosition = Vector3.zero`
- `localRotation = Quaternion.identity`

La vista pasa al rig de camara del vehiculo.

### Al salir del coche

En `SimpleCharacterController.OnExitFromCar()`:

- recoloca el personaje junto al coche (offset lateral + altura)
- reactiva personaje: `gameObject.SetActive(true)`
- vuelve la camara al parent del personaje:
  - `Camera.transform.SetParent(CameraParent);`
  - `localPosition = Vector3.zero`
  - `localRotation = Quaternion.identity`

Resultado: la vista vuelve al modo personaje y se restablece el control de movimiento a pie.

## 8) Resumen del flujo completo con F

### F cuando estas a pie

`CharacterInput` -> evento `OnEntrerInCar` -> `SimpleCharacterController.TryEnterCar()` -> raycast a coche -> `PlayerController.EnterInCar()` -> `Car.CarControl = CarControllerInput` -> camara al rig del coche.

### F cuando estas en el coche

`CarControllerInput` -> `TryExitFromCar()` -> `PlayerController.ExitFromCar()` -> `Uninitialize()` -> `Car.CarControl = null` -> evento `OnExitAction` -> `SimpleCharacterController.OnExitFromCar()` -> personaje activo + camara al rig del personaje.

## 9) Scripts clave para depurar o extender

- `Assets/UniversalVehicleController/Scripts/SimpleCharacterController/SimpleCharacterController.cs`
- `Assets/UniversalVehicleController/Scripts/GamePlay/Controls/CharacterInput.cs`
- `Assets/UniversalVehicleController/Scripts/GamePlay/Controls/CarControllerInput.cs`
- `Assets/UniversalVehicleController/Scripts/GamePlay/PlayerControl/PlayerController.cs`
- `Assets/UniversalVehicleController/Scripts/GamePlay/PlayerControl/CameraController.cs`

Si quieres extender el sistema (por ejemplo, animacion de entrar/salir, validaciones extra por distancia o UI contextual), el mejor punto de entrada suele ser `TryEnterCar()` y `OnExitFromCar()`, manteniendo intacta la parte de `Initialize/Uninitialize`.
