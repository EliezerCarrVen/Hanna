const commands = [
  '/help','/status','/doctor','/diagnostico','/selftest','/deps',
  '/memoria prueba','/memoria guardar TEXTO','/memoria buscar TEXTO','/memoria ultimos','/memoria estado',
  '/codigo prueba','/codigo buscar TEXTO','/codigo listar','/codigo estado',
  '/summary','/summary regenerar','/indexar','/indice estado',
  '/motor actual','/motor estado','/motor cambiar NOMBRE','/fase actual','/fase estado','/fase cambiar NOMBRE',
  '/vault estado','/vault crear NOMBRE','/vault listar','/vault importar RUTA','/vault verificar',
  '/auditoria','/auditoria verificar','/modulos','/zeroleak TEXTO','/intencion TEXTO',
  '/nas estado','/nas indexar','/nas buscar TEXTO','/mqtt estado','/mqtt publicar TOPIC MENSAJE',
  '/spotify estado','/spotify auth estado','/spotify reproducir TEXTO','/spotify pausar','/spotify siguiente','/spotify anterior','/spotify buscar TEXTO',
  '/wol estado','/wol probar MAC','/wol enviar MAC','/clamav estado','/clamav escanear RUTA',
  '/docker estado','/nodered estado','/nodered ping','/serverless estado','/sistema doctor',
  '/ntp estado','/ip estado','/json COMANDO','/salir'
];
module.exports = { commands };
