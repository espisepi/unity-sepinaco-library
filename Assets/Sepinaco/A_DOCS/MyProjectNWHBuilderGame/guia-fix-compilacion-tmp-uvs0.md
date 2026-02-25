# Guia de correccion de compilacion TMP (uvs0 Vector4 vs Vector2)

## Contexto del problema

Al actualizar TextMesh Pro, varios scripts de `Examples & Extras` quedan desfasados porque asumen que `uvs0` es `Vector2[]`, pero en versiones recientes de TMP pasa a `Vector4[]`.

Esto genera errores como:

- `CS0029: Cannot implicitly convert type 'UnityEngine.Vector4[]' to 'UnityEngine.Vector2[]'`

Se corrigieron 2 scripts:

1. `TMP_TextSelector_B.cs`
2. `VertexZoom.cs`

---

## 1) Correccion en TMP_TextSelector_B.cs

Ruta afectada:

`Assets/CityEngine/Assets/Plugins/TextMesh Pro/Examples & Extras/Scripts/TMP_TextSelector_B.cs`

### Cambio necesario

Solo cambiar el tipo de `uvs0` local de `Vector2[]` a `Vector4[]`.

### Codigo a reemplazar (copiar/pegar)

Antes:

```csharp
Vector2[] src_uv0s = m_cachedMeshInfoVertexData[materialIndex].uvs0;
Vector2[] dst_uv0s = m_TextMeshPro.textInfo.meshInfo[materialIndex].uvs0;
```

Despues:

```csharp
Vector4[] src_uv0s = m_cachedMeshInfoVertexData[materialIndex].uvs0;
Vector4[] dst_uv0s = m_TextMeshPro.textInfo.meshInfo[materialIndex].uvs0;
```

---

## 2) Correccion en VertexZoom.cs

Ruta afectada:

`Assets/CityEngine/Assets/Plugins/TextMesh Pro/Examples & Extras/Scripts/VertexZoom.cs`

Aqui hicieron falta 2 ajustes:

### 2.1 Cambiar tipos locales de UV0 a Vector4[]

Antes:

```csharp
Vector2[] sourceUVs0 = cachedMeshInfoVertexData[materialIndex].uvs0;
Vector2[] destinationUVs0 = textInfo.meshInfo[materialIndex].uvs0;
```

Despues:

```csharp
Vector4[] sourceUVs0 = cachedMeshInfoVertexData[materialIndex].uvs0;
Vector4[] destinationUVs0 = textInfo.meshInfo[materialIndex].uvs0;
```

### 2.2 Convertir uvs0 (Vector4[]) antes de asignar a mesh.uv (Vector2[])

Antes:

```csharp
textInfo.meshInfo[i].mesh.uv = textInfo.meshInfo[i].uvs0;
```

Despues:

```csharp
Vector4[] sourceUVs = textInfo.meshInfo[i].uvs0;
Vector2[] meshUVs = new Vector2[sourceUVs.Length];
for (int j = 0; j < sourceUVs.Length; j++)
    meshUVs[j] = sourceUVs[j];
textInfo.meshInfo[i].mesh.uv = meshUVs;
```

---

## Explicacion tecnica (por que funciona)

- `textInfo.meshInfo[i].uvs0` ahora devuelve `Vector4[]`.
- `mesh.uv` del `Mesh` de Unity espera `Vector2[]`.
- No existe conversion implicita de `Vector4[]` a `Vector2[]` a nivel de arreglo completo.
- Si se necesita asignar a `mesh.uv`, hay que crear un arreglo `Vector2[]` y copiar elemento por elemento.

---

## Checklist rapido para futuros errores similares

Si vuelve a aparecer `CS0029` con `Vector4[]` y `Vector2[]` en scripts de TMP:

1. Buscar variables locales tipadas como `Vector2[]` que apunten a `.uvs0`.
2. Cambiarlas a `Vector4[]`.
3. Revisar asignaciones a `mesh.uv`; si la fuente es `uvs0`, convertir primero a `Vector2[]`.
4. Compilar de nuevo en Unity y validar que desaparece el error.

---

## Nota de mantenimiento

Estos scripts pertenecen a `TextMesh Pro/Examples & Extras`.  
En futuras actualizaciones del paquete pueden sobrescribirse.  
Si eso ocurre, reaplicar estos cambios con esta hoja de referencia.
