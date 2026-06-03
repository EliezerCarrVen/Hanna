# Hanna

Hanna mantiene tres líneas de ejecución separadas para no romper compatibilidad entre equipos modernos y hardware legacy:

- `Hanna/`: versión principal C#/.NET para PC moderna, con el proyecto original intacto.
- `Hanna.Lightweight/`: experimento lightweight en .NET para validar módulos y documentación.
- `Hanna.NodeLightweight/`: versión portable JavaScript/Node.js para HP Mini 110 con Debian 12 i386/x86.

## Por qué existe Hanna.NodeLightweight

La HP Mini 110 usa CPU x86/i386 de 32 bits. .NET moderno no es un runtime principal viable en Linux i386, mientras que Debian 12 todavía ofrece paquetes Node.js/npm utilizables para JavaScript directo. Por eso la versión para HP Mini pivota a Node.js sin reemplazar el Hanna C#.

## Hanna.NodeLightweight

`Hanna.NodeLightweight/` no requiere .NET, no usa TypeScript obligatorio y evita dependencias pesadas. Usa archivos locales en `HannaData/`, JSONL, Markdown, `crypto` nativo de Node y adaptadores opcionales para herramientas externas como ripgrep, MQTT, ClamAV, Docker y Node-RED.

Node-RED es opcional: puede actuar como orquestador visual, pero Hanna.NodeLightweight arranca aunque Node-RED no esté instalado.

## Comandos rápidos

```bash
cd Hanna.NodeLightweight
npm install
npm start
npm run self-test
npm run once -- "/status"
npm run once -- "/doctor"
npm run deps
npm test
```

## Datos locales privados

La carpeta `HannaData/` se crea automáticamente y permanece ignorada por git. No se deben subir tokens, contraseñas, `.env`, prompts internos ni datos runtime privados.
