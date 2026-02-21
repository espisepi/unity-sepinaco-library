# Fichero gitignore para carpetas
_Exported on 21/02/2026 at 18:45:53 CET from Cursor (2.5.20)_

---

**User**

hazme un fichero gitignore que ignore todas las carpetas excepto la de Sepinaco

---

**Cursor**

Voy a revisar la estructura del proyecto para crear un `.gitignore` adecuado.

Ahora tengo claro la estructura. Voy a crear el `.gitignore` que ignore todo excepto la carpeta `Assets/Sepinaco` y los archivos esenciales de configuración de Unity para que el proyecto pueda funcionar.

El `.gitignore` está creado. Así funciona:

1. **`/*`** — Ignora todo en la raíz del proyecto.
2. **`!Assets/`** — Permite la carpeta `Assets`, pero luego **`Assets/*`** ignora todo su contenido.
3. **`!Assets/Sepinaco/`** y **`!Assets/Sepinaco.meta`** — Excluye de la ignorancia únicamente la carpeta `Sepinaco` y su archivo `.meta`.
4. También se mantienen **`ProjectSettings/`** y **`Packages/`** porque son necesarios para que Unity pueda abrir el proyecto correctamente.
5. Las carpetas temporales de Unity (`Library/`, `Temp/`, `Logs/`, etc.), los builds `.app` y los archivos de debug de Burst se ignoran explícitamente.

Si quieres que también se ignore `ProjectSettings/` y `Packages/`, dime y lo ajusto.

---

**User**

añademe al gitignore la carpeta ProjectSettings

---

**Cursor**

Listo. `ProjectSettings/` ya no está excluida, así que ahora será ignorada por la regla general `/*` del principio. Solo se mantiene `Packages/` como carpeta de configuración de Unity.

