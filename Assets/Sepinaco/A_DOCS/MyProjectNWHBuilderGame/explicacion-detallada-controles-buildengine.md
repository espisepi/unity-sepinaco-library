# BuildEngine: explicacion detallada del sistema de controles

Este documento describe en profundidad como funciona el sistema de controles de la escena `BuildEngine` del paquete `CityEngine`.

El comportamiento no depende de un solo script, sino de la coordinacion entre:

- `CameraController` (input de camara + colocacion + confirmacion/eliminacion)
- `BuildingsMenu` (UI de categorias/edificios y seleccion de herramienta)
- `RoadGenerator` y propiedades de edificios (validaciones al construir)
- `Grid` (referencia visual de colocacion)

---

## 1. Arquitectura general del control

En la escena `BuildEngine` se instancian estos prefabs de gameplay:

- `CameraRig`
- `BuildingsMenu`
- `Controllers`
- `StartField`

El objeto `Controllers` contiene el script `CameraController` y referencias a padres de:

- edificios (`buildingsParent`)
- carreteras (`roadsParent`)
- ciudadanos (`citizensParent`)
- coches (`carsParent`)

El prefab `BuildingsMenu` contiene:

- `BuildingsMenu.cs` (menu de tipos y elementos)
- boton de abrir/cerrar menu (`ActivateMenu`)
- camara propia del menu para renderizar los iconos 3D

Por diseno, el control de usuario se reparte asi:

- **mundo/camara/confirmaciones** -> `CameraController`
- **navegacion de UI de construccion** -> `BuildingsMenu`

---

## 2. Estados funcionales del sistema

Aunque no hay una enum explicita, en la practica hay tres estados:

1. **Exploracion**
   - no hay objeto preview (`target == null`)
   - menu puede estar cerrado o abierto

2. **Seleccion en menu**
   - `activateMenu.activeSelf == true`
   - usuario explora categorias/edificios o selecciona delete tool

3. **Colocacion activa**
   - `moveTarget == true`
   - existe un `target` en preview, que sigue al raton en rejilla
   - se puede rotar (click derecho) y confirmar (doble click)

La tecla `Esc` y el boton del menu fuerzan transiciones entre estos estados.

---

## 3. Bucle de input por frame (orden real)

En `CameraController.Update()` el orden es:

1. `MouseInut()`
2. `KeyboardInput()`
3. `SetPosition()`
4. deteccion de doble click con boton izquierdo
5. manejo de `Esc`

En `LateUpdate()`:

- si `moveTarget` y `target != null`, recalcula posicion del preview sobre el plano del suelo y lo ajusta a rejilla.

### 3.1 Doble click (mecanica base)

El sistema no usa `EventSystem` para doble click; lo calcula por tiempo:

- `catchTime = 0.3f`
- si entre dos `MouseButtonDown(0)` pasan menos de `0.3s`, `doubleClick = true`.

Ese flag luego lo consumen:

- `BuildingsMenu` (para entrar en categoria o elegir edificio)
- `CameraController.OnGUI()` (para confirmar construir/eliminar)

Esto significa que gran parte de las acciones criticas dependen de doble click, no de click simple.

---

## 4. Controles de camara: comportamiento detallado

## 4.1 Movimiento horizontal (mundo)

Teclas:

- `W` / `UpArrow` avanza
- `S` / `DownArrow` retrocede
- `A` / `LeftArrow` izquierda
- `D` / `RightArrow` derecha

Velocidad:

- normal: `normalSpeed`
- rapido: manteniendo `LeftShift` usa `fastSpeed`

Importante: `toPos` siempre se actualiza con input, pero `cameraHolder.position` solo interpola cuando el menu principal no esta activo (`activateMenu.activeSelf == false`).

Consecuencia:

- con menu abierto, el movimiento de posicion queda efectivamente bloqueado
- al cerrar menu, puede reaplicarse la posicion objetivo acumulada

## 4.2 Paneo con raton (drag LMB)

Con `GetMouseButton(0)`:

- proyecta el raton sobre un plano horizontal (`Plane(Vector3.up, Vector3.zero)`)
- calcula diferencia entre punto inicial y actual
- desplaza `toPos` con esa delta

Es un paneo “sobre suelo”, no un orbit.

## 4.3 Rotacion de camara

Por teclado:

- `Q` rota en Y positivo
- `E` rota en Y negativo

Por raton:

- mantener boton central (`GetMouseButton(2)`)
- usa delta X entre posiciones para girar yaw

## 4.4 Zoom

Por rueda:

- `Input.mouseScrollDelta.y` altera `toZoom`

Por teclado:

- `R` acerca
- `F` aleja

Luego aplica clamps:

- eje Y de `toZoom`: `[-minZoom, maxZoom]` (con signo invertido en codigo)
- eje Z de `toZoom`: `[-maxZoom, minZoom]`

El zoom es interpolado suave con `Lerp`.

---

## 5. Controles de construccion: menu y seleccion

`BuildingsMenu` crea dinamicamente en runtime:

- botones de tipos (`Houses`, `Attractions`, `Industry`, `Parks`, `Infrastructure`, `Roads`)
- botones de cada edificio
- boton/herramienta de borrado (`deleteBuilding`)

## 5.1 Abrir/cerrar menu

Accion: boton UI que llama `ActivateMenu()`.

Efectos al alternar:

- resetea scroll de tipos
- si habia preview, lo destruye
- si no habia preview, alterna visibilidad de rejilla (`grid.enabled`)
- pone `cameraController.target = null`
- pone `cameraController.moveTarget = false`
- restablece vista de categorias

La rejilla solo se enciende cuando entras al flujo de construccion y no hay preview en curso.

## 5.2 Navegacion dentro del menu

Con menu activo:

- drag horizontal con raton mueve la tira de iconos (`types.localPosition.x`)
- hay limites min/max por categoria
- hay desaceleracion/inercia cuando sueltas el raton

## 5.3 Seleccion por doble click

No usa click unico para confirmar; requiere `cameraController.doubleClick`.

- doble click en categoria -> abre su lista de edificios
- doble click en edificio -> crea preview y cierra menu

Para borrar:

- seleccionar herramienta delete crea un `target` especial con tag `DeleteTool`

---

## 6. Colocacion activa del preview en mundo

Cuando se elige un edificio/carretera/herramienta de borrado:

- `cameraController.moveTarget = true`
- `cameraController.target = instancia preview`

En `LateUpdate()`:

- raycast desde camara principal al plano del suelo
- posicion del preview se ajusta por snap a rejilla:
  - usa `Mathf.Floor((x + gridSize/2)/gridSize) * gridSize`
  - igual para `z`
- interpolacion rapida (`Lerp * 50`) para suavizar seguimiento

### Rotar preview

- click derecho (`MouseButtonDown(1)`) rota el preview +90 grados en Y.

---

## 7. Confirmar accion (construir o borrar)

La confirmacion ocurre en `CameraController.OnGUI()` cuando `doubleClick == true`.

Segun `target.tag`:

- `Road` -> `SpawnRoad(target)`
- `Building` -> `SpawnBuilding(target)`
- `DeleteTool` -> intenta eliminar edificio o carretera en esa celda

---

## 8. Reglas internas al construir

## 8.1 Carreteras

`SpawnRoad()`:

- parent a `roadsParent`
- snap a cuadricula de 10
- valida tipo/conexion con `roadGenerator.CheckRoadType()`
- si valido, desactiva vegetacion (`forestObj`) en esa posicion
- crea nueva instancia de carretera y la deja como `target` para continuar

## 8.2 Edificios

`SpawnBuilding()`:

- evita solapes con:
  - otros edificios ya construidos (`allBuildings`)
  - `additionalSpace` del edificio
  - carreteras existentes
- si pasa validacion:
  - desactiva bosque en area ocupada
  - incrementa poblacion objetivo (`carsCount`, `citizensCount`)
  - registra espacios ocupados en `allBuildings`
  - dispara animacion/proceso de construccion (`BuildConstruction.StartBuild()`)
  - apaga rejilla
  - limpia `target` y `moveTarget`

---

## 9. Sistema de borrado (DeleteTool)

Con `target.tag == DeleteTool`, en confirmacion por doble click:

- busca coincidencia por celda en `allBuildings`
- elimina building principal + espacios extra asociados
- limpia spawn points de coches/ciudadanos vinculados a ese edificio
- si encuentra carretera en esa celda:
  - la destruye
  - la saca de lista
  - llama a `roadGenerator.DeleteRoad()` para recalcular conexiones

Es un borrado “contextual por celda”, no por seleccionar objeto exacto en jerarquia.

---

## 10. Tecla Escape y cancelaciones

En `CameraController.Update()`:

- si `Esc` y menu abierto -> cierra menu (`buildingMenu.ActivateMenu()`)
- si hay `target` activo -> destruye preview

Esto funciona como cancelacion global de accion en curso.

---

## 11. Resumen operativo del usuario (flujo real)

1. Abres menu con boton de build.
2. Doble click en categoria.
3. Doble click en edificio/herramienta.
4. Se crea preview en mundo y sigue el cursor sobre rejilla.
5. Opcional: rotas con click derecho.
6. Doble click para confirmar construir/borrar.
7. `Esc` cancela o cierra menu.

---

## 12. Detalles importantes (comportamiento no obvio)

- El sistema exige **doble click** para acciones de seleccion y confirmacion; esto reduce clicks accidentales.
- El `target` preview es el centro del flujo de estados: si existe, estas en modo colocacion.
- El menu bloquea el desplazamiento de posicion de camara, pero no bloquea totalmente rotacion/zoom porque esos siguen interpolando.
- La logica de borrar tambien limpia datos de spawners para mantener consistencia de trafico/peatones.
- El snap de rejilla no usa raycast a collider del terreno, sino interseccion con plano Y fijo.

---

## 13. Scripts clave para depurar este sistema

- `Assets/CityEngine/Assets/Scripts/Camera/CameraController.cs`
- `Assets/CityEngine/Assets/Scripts/BuildingsMenu/BuildingsMenu.cs`
- `Assets/CityEngine/Assets/Scripts/RoadsGenerator/RoadGenerator.cs`
- `Assets/CityEngine/Assets/Scripts/Buildings/BuildingProperties.cs`
- `Assets/CityEngine/Assets/Scripts/Characters/Spawner.cs`

Si quieres, como siguiente paso te puedo documentar tambien un diagrama de estados (exploracion/menu/preview/confirmacion) y un plan de refactor para sustituir doble click por un sistema de input mas predecible (click para seleccionar + Enter para confirmar, por ejemplo).
