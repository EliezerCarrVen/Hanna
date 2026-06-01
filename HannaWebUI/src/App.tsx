import { useEffect, useMemo, useState } from "react";
import { getHannaStatus, sendHannaCommand, type HannaStatus } from "./api/hannaClient";
import { ArcReactor } from "./components/ArcReactor";
import { StatBar } from "./components/StatBar";
import { Waveform } from "./components/Waveform";

const initialStatus: HannaStatus = {
  connection: "pending",
  profile: "pendiente de conexión",
  engine: "pendiente de conexión",
  phase: "pendiente de conexión",
  telegram: "pendiente de conexión",
  ollama: "pendiente de conexión",
  mongo: "pendiente de conexión",
  lastError: "Backend no conectado o endpoint no implementado",
  source: "visual-demo",
};

const quickCommands = ["/status", "/diagnostico", "/motor actual", "/fase actual", "/demo"];

function now() {
  return new Date().toLocaleTimeString("es-MX", { hour12: false });
}

export default function App() {
  const [time, setTime] = useState(new Date());
  const [active, setActive] = useState(false);
  const [status, setStatus] = useState<HannaStatus>(initialStatus);
  const [consoleLines, setConsoleLines] = useState<string[]>([
    "[visual] Hanna Visual UI cargada.",
    "[visual] Diseño split-screen morado/turquesa integrado.",
    "[visual] Backend pendiente de conexión real.",
  ]);

  useEffect(() => {
    const timer = setInterval(() => setTime(new Date()), 1000);
    return () => clearInterval(timer);
  }, []);

  useEffect(() => {
    let cancelled = false;
    async function refresh() {
      const next = await getHannaStatus();
      if (!cancelled) setStatus(next);
    }
    refresh();
    const interval = setInterval(refresh, 5000);
    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, []);

  const metrics = useMemo(() => {
    const onlineBoost = status.connection === "connected" ? 16 : 0;
    return {
      perfil: status.profile.includes("telegram") ? 76 : 64 + onlineBoost,
      motor: status.engine.includes("pendiente") ? 42 : 80,
      fase: status.phase.includes("pendiente") ? 38 : 78,
      enlace: status.connection === "connected" ? 92 : 24,
    };
  }, [status]);

  async function handleCommand(command: string) {
    setActive(true);
    setConsoleLines((prev) => [`[${now()}] > ${command}`, ...prev].slice(0, 12));
    const result = await sendHannaCommand(command);
    setConsoleLines((prev) => [`[${now()}] ${result.source}: ${result.response}`, ...prev].slice(0, 12));
    setTimeout(() => setActive(false), 900);
  }

  return (
    <main className="app-shell">
      <header className="topbar">
        <div className="brand-mini"><span /> HANNA VISUAL INTERFACE</div>
        <div className="topbar-right">
          <span>{status.connection === "connected" ? "backend conectado" : "demo visual"}</span>
          <strong>{time.toLocaleTimeString("es-MX", { hour12: false })}</strong>
        </div>
      </header>

      <section className="split-layout">
        <aside className="panel left-panel">
          <div className="panel-label">Núcleo</div>
          <div className="panel-state">{active ? "Procesando" : "Sistema en línea"}</div>
          <div className="reactor-wrap"><ArcReactor active={active} /></div>

          <div className="identity-block">
            <span>Estado</span>
            <h1>HANNA</h1>
            <p>Interfaz visual opcional para diagnóstico, comandos y demostración. Conexión backend: {status.source === "backend" ? "activa" : "pendiente"}.</p>
          </div>

          <div className="status-grid">
            <div><span>Perfil</span><strong>{status.profile}</strong></div>
            <div><span>Motor</span><strong>{status.engine}</strong></div>
            <div><span>Fase</span><strong>{status.phase}</strong></div>
            <div><span>Telegram</span><strong>{status.telegram}</strong></div>
          </div>
        </aside>

        <section className="right-column">
          <div className="panel hero-panel">
            <span className="eyebrow">Asistente operativo</span>
            <h2>Panel split-screen morado/turquesa para Hanna</h2>
            <p>Diseño compatible con el proyecto C#/.NET. No reemplaza el backend; prepara una capa visual para endpoints locales de estado, diagnóstico, logs y comandos.</p>
          </div>

          <div className="panel metrics-panel">
            <div className="panel-header"><span>Sistema</span><strong>{status.connection === "connected" ? "Tiempo real" : "Pendiente de backend"}</strong></div>
            <StatBar label="Perfil" value={metrics.perfil} />
            <StatBar label="Motor" value={metrics.motor} />
            <StatBar label="Fase" value={metrics.fase} />
            <StatBar label="Enlace" value={metrics.enlace} />
          </div>

          <div className="bottom-grid">
            <div className="panel command-panel">
              <div className="panel-header"><span>Comandos rápidos</span><strong>preparado</strong></div>
              <div className="command-buttons">
                {quickCommands.map((command) => (
                  <button key={command} onClick={() => handleCommand(command)}>{command}</button>
                ))}
              </div>
              <p className="note">Si Hanna no expone POST /api/comando, estos botones quedan como demo visual.</p>
            </div>

            <div className="panel audio-panel">
              <div className="panel-header"><span>Audio</span><strong>{active ? "activo" : "reposo"}</strong></div>
              <Waveform />
              <div className="audio-footer"><span>visual</span><strong>pendiente de TTS real</strong></div>
            </div>
          </div>

          <div className="panel console-panel">
            <div className="panel-header"><span>Consola segura</span><strong>logs visuales</strong></div>
            <div className="console-lines">
              {consoleLines.map((line, index) => <p key={`${line}-${index}`}>{line}</p>)}
            </div>
          </div>
        </section>
      </section>
    </main>
  );
}
