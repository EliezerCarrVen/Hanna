# Corrección V6.3 - Panel integrado

Esta versión corrige la separación del chat online.

## Cambios

- El chat online queda integrado dentro del panel principal `http://127.0.0.1:8787/`.
- El puerto `8789` queda como modo opcional/legacy.
- `HANNA_WEBCHAT_ENABLED=false` por defecto para evitar procesos Node separados.
- El panel principal ahora tiene pestaña **Chat integrado**.
- El chat llama directamente a la API móvil `8790` usando `HANNA_MOBILE_API_PAIRING_TOKEN`.
- Se corrigió la lectura del cuerpo HTTP en `MobileApiServerService` para evitar JSON incompleto.

## Uso

1. Inicia Hanna con `dotnet run`.
2. Abre `http://127.0.0.1:8787/`.
3. Entra a **Chat integrado**.
4. Pega tu Chat ID y pairing token.
5. Selecciona motor/fase y envía mensajes.

## Nota

El panel web anterior no estaba actualizado porque el chat se había agregado como módulo externo para no tocar el HTML grande del panel. En V6.3 ya queda integrado en ese panel.
