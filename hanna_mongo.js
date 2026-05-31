use HannaDB;
// If running in the Mongo shell, uncomment the following line:
db = db.getSiblingDB('HannaDB');

db.createCollection("memorias");
db.createCollection("conversaciones");
db.createCollection("mensajes");
db.createCollection("transcripciones_audio");
db.createCollection("analisis_pantalla");
db.createCollection("acciones_agente");
db.createCollection("contexto_proyectos");
db.createCollection("codigo_generado");
db.createCollection("estado_sistema");

db.memorias.createIndex({ tipo: 1 });
db.memorias.createIndex({ importancia: -1 });
db.memorias.createIndex({ fecha: -1 });

db.conversaciones.createIndex({ origen: 1 });
db.conversaciones.createIndex({ fechaInicio: -1 });

db.mensajes.createIndex({ conversacionId: 1 });
db.mensajes.createIndex({ fecha: -1 });
db.mensajes.createIndex({ texto: "text" });

db.transcripciones_audio.createIndex({ fecha: -1 });
db.transcripciones_audio.createIndex({ origen: 1 });

db.analisis_pantalla.createIndex({ fecha: -1 });
db.analisis_pantalla.createIndex({ tipo: 1 });

db.acciones_agente.createIndex({ accion: 1 });
db.acciones_agente.createIndex({ estado: 1 });
db.acciones_agente.createIndex({ fecha: -1 });

db.contexto_proyectos.createIndex({ nombreProyecto: 1 });
db.contexto_proyectos.createIndex({ lenguaje: 1 });
db.contexto_proyectos.createIndex({ fechaActualizacion: -1 });

db.codigo_generado.createIndex({ lenguaje: 1 });
db.codigo_generado.createIndex({ fecha: -1 });

db.estado_sistema.createIndex({ clave: 1 }, { unique: true });

db.memorias.insertOne({
    tipo: "perfil_usuario",
    clave: "dueno",
    valor: {
        nombre: "Eliezer",
        preferenciaMotorPc: "OllamaLocal",
        preferenciaMotorTelegram: "Hybrid",
        vozPreferida: "es-PE-CamilaNeural"
    },
    importancia: 10,
    fecha: new Date()
});

db.estado_sistema.insertOne({
    clave: "configuracion_inicial",
    valor: {
        ollamaModelo: "qwen2.5-coder:3b",
        hotkeys: {
            vozSinVentana: "F8",
            vozConVentana: "AltGr+Enter",
            analisisPantalla: "F9"
        },
        modoPc: "OllamaLocal",
        modoTelegram: "Hybrid"
    },
    fecha: new Date()
});