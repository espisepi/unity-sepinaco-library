# Guia rapida - Fix CS1061 con CharacterController.height

## Error observado

En `SimpleCharacterController.cs` aparece:

`error CS1061: 'CharacterController' does not contain a definition for 'height' ...`

## Causa raiz

En el proyecto existe una clase propia llamada `CharacterController`:

- `Assets/CityEngine/Assets/Scripts/Characters/CharacterController.cs`

Ese nombre entra en conflicto con `UnityEngine.CharacterController`.
Cuando el compilador resuelve el tipo equivocado, `height` no existe y falla.

## Solucion aplicada

Archivo corregido:

- `Assets/UniversalVehicleController/Scripts/SimpleCharacterController/SimpleCharacterController.cs`

Cambio realizado: usar el tipo completamente calificado de Unity para evitar ambiguedad.

- `[RequireComponent(typeof(UnityEngine.CharacterController))]`
- `UnityEngine.CharacterController CharacterController;`
- `GetComponent<UnityEngine.CharacterController>()`

## Como corregirlo de nuevo si vuelve a pasar

1. Buscar el error CS1061 que mencione `CharacterController.height`.
2. Revisar si existe una clase local llamada `CharacterController` en el proyecto.
3. En el script con error, reemplazar referencias ambiguas por `UnityEngine.CharacterController`.
4. Recompilar y confirmar que desaparece el error.

## Recomendacion preventiva

Para evitar este problema en otros scripts:

- Renombrar la clase personalizada a algo especifico, por ejemplo `NPCCharacterController`.
- O mover clases propias a un namespace dedicado y usar `using` de forma explicita.
