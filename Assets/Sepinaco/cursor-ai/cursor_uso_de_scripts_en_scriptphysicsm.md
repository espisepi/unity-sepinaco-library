# Uso de scripts en ScriptPhysicsManager
_Exported on 21/02/2026 at 22:21:32 CET from Cursor (2.5.20)_

---

**User**

el script @Assets/Sepinaco/PhysicsManager/ScriptPhysicsManager.cs usa algún script de los que tiene en esa misma carpeta o en las carpetas hijas recursivas?

---

**Cursor**

**No, `ScriptPhysicsManager.cs` no usa ningún otro script de esa carpeta ni de sus subcarpetas.**

El script es completamente autocontenido. Analicemos sus dependencias:

**Imports:**
- `System` (estándar .NET)
- `UnityEngine` (estándar Unity)

**Tipos definidos en el propio archivo:**
- `StartCollidersMode` (enum, línea 4)
- `PhysicsTarget` (clase, línea 17)
- `ScriptPhysicsManager` (clase principal, línea 43)

Todos los demás tipos que utiliza (`MonoBehaviour`, `GameObject`, `Collider`, `GUIStyle`, `GUI`, `GUILayout`, `Input`, `KeyCode`, etc.) son tipos estándar de Unity.

**Los otros 6 scripts de la carpeta:**

| Script | Relación |
|---|---|
| `ScriptPhysicsManagerV1.cs` | No referenciado |
| `ScriptPhysicsManagerV2.cs` | No referenciado |
| `ScriptPhysicsManagerV3.cs` | No referenciado |
| `ScriptPhysicsManagerV4.cs` | No referenciado |
| `PhysicsManagerUI.cs` | No referenciado |
| `Editor/ScriptPhysicsManagerEditor.cs` | No referenciado (aunque en el comentario XML de la línea 38 se menciona como documentación, no es una dependencia de código) |

De hecho, la relación es **inversa**: es probable que `ScriptPhysicsManagerEditor.cs` dependa de `ScriptPhysicsManager`, ya que es su Custom Editor. Pero `ScriptPhysicsManager.cs` en sí no necesita ninguno de los otros scripts para funcionar.

