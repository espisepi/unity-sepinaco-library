# Atributos para postprocesado en ScriptPostProcessingPixelPs1Manager
_Exported on 22/02/2026 at 22:36:25 CET from Cursor (2.5.20)_

---

**User**

añade un atributo en ScriptPostProcessingPixelPs1Manager para modificar en runtime el activar o desactivar el efecto de postprocesado que es lo que se hace con la tecla Alpha2 y otro atributo para restaurar los valores por defecto a la ps1 que es lo que se hace con la tecla Alpha1, para poder modificar esos atributos desde el menu debug del script ScriptDebugInspector y pon esos atributos en primer lugar

---

**Cursor**

Voy a leer ambos archivos completamente y luego hacer los cambios necesarios.

Ahora entiendo la estructura. Necesito:

1. Añadir dos campos serializados al inicio de `ScriptPostProcessingPixelPs1`: uno para toggle del efecto y otro para restaurar defaults
2. Sincronizarlos con la lógica existente de Alpha2 y Alpha1
3. Añadir `OnValidate` para que el `ScriptDebugInspector` pueda disparar los cambios

Primero, añado los dos campos serializados como los primeros atributos del script, antes de la sección de Pixelación.

Ahora sincronizo `_debugToggleEffect` con `_effectEnabled` en `Start()`.

Ahora sincronizo cuando se pulsa Alpha2 (toggle del efecto).

Ahora sincronizo en ambos presets.

Ahora añado el método `OnValidate()` para que cuando el `ScriptDebugInspector` modifique estos campos, surtan efecto.

Verifico el resultado final de la zona modificada.

Todo listo. Aquí un resumen de los cambios realizados en `ScriptPostProcessingPixelPs1.cs`:

**Nuevos campos serializados** (posicionados como los primeros atributos del script):

```24:29:Assets/Sepinaco/PostProcessingPixelPs1/ScriptPostProcessingPixelPs1.cs
    [Header("Control desde Debug Inspector")]
    [Tooltip("Activa/desactiva el efecto de post-procesado PS1 en runtime (equivalente a la tecla de toggle).")]
    [SerializeField] private bool _debugToggleEffect = true;

    [Tooltip("Ponlo en True para restaurar los valores por defecto del efecto PS1 (preset auténtico). Se desactiva solo tras aplicar.")]
    [SerializeField] private bool _debugRestoreDefaults;
```

**Nuevo `OnValidate()`** para que el `ScriptDebugInspector` dispare los cambios:

```202:212:Assets/Sepinaco/PostProcessingPixelPs1/ScriptPostProcessingPixelPs1.cs
    private void OnValidate()
    {
        if (_debugRestoreDefaults)
        {
            _debugRestoreDefaults = false;
            ApplyAuthenticPS1Preset();
            return;
        }

        _effectEnabled = _debugToggleEffect;
    }
```

**Cómo funciona:**

- **`_debugToggleEffect`** (bool): Al ser el primer campo serializado, aparece primero en el menú del Debug Inspector. Al cambiarlo a `false`/`true`, activa/desactiva el efecto PS1 (mismo comportamiento que la tecla Alpha2). Se mantiene sincronizado bidireccionalmente: si se usa la tecla Alpha2, el campo se actualiza; si se cambia desde el Debug Inspector, `OnValidate` sincroniza `_effectEnabled`.

- **`_debugRestoreDefaults`** (bool): Al ponerlo en `true` desde el Debug Inspector, `OnValidate` ejecuta `ApplyAuthenticPS1Preset()` (mismo comportamiento que la tecla Alpha1) y automáticamente se resetea a `false`.

