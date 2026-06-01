import { useEffect, useMemo, useState } from 'react';
import { hannaClient } from './api/hannaClient';
import { AdminDropdown } from './components/AdminDropdown';
import { ChatPanel } from './components/ChatPanel';
import { SystemStatusCompact } from './components/SystemStatusCompact';
import type { HannaLogEvent, HannaStatus } from './types/hanna';

const initialStatus: HannaStatus = {
  mode: hannaClient.isDemoMode ? 'demo' : 'disconnected',
  profile: hannaClient.isDemoMode ? 'demo' : 'pendiente',
  engine: hannaClient.isDemoMode ? 'preparado' : 'pendiente',
  phase: hannaClient.isDemoMode ? 'preparada' : 'pendiente',
  telegram: hannaClient.isDemoMode ? 'preparado' : 'pendiente',
  backend: hannaClient.isDemoMode ? 'modo demo' : 'backend desconectado',
  message: hannaClient.isDemoMode ? 'Modo demo activo.' : 'Esperando backend.'
};

export default function App() {
  const [status, setStatus] = useState<HannaStatus>(initialStatus);
  const [logsOpen, setLogsOpen] = useState(false);
  const [events, setEvents] = useState<HannaLogEvent[]>([
    hannaClient.createLocalLog(hannaClient.isDemoMode ? 'UI iniciada en modo demo.' : 'UI iniciada. Backend opcional.', 'info')
  ]);

  useEffect(() => {
    let cancelled = false;
    let timer: number | undefined;

    async function refresh(force = false) {
      const next = await hannaClient.getStatus(force);
      if (cancelled) return;
      setStatus(next);
      setEvents((current) => {
        const last = current[0]?.message;
        const message = next.mode === 'connected' ? 'Backend conectado.' : next.message || 'Backend desconectado.';
        if (last === message) return current;
        return [hannaClient.createLocalLog(message, next.mode === 'connected' ? 'info' : 'warn'), ...current].slice(0, 8);
      });
      if (!hannaClient.isDemoMode) {
        timer = window.setTimeout(() => void refresh(), next.mode === 'connected' ? 60000 : hannaClient.retryDelayMs);
      }
    }

    void refresh(true);
    return () => {
      cancelled = true;
      if (timer) window.clearTimeout(timer);
    };
  }, []);

  const latestEvents = useMemo(() => events.slice(0, logsOpen ? 8 : 4), [events, logsOpen]);

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="brand">
          <span className="brand-mark">H</span>
          <strong>HANNA</strong>
        </div>
        <div className="top-status">
          <span className={`dot ${status.mode}`} />
          <span>{status.mode === 'connected' ? 'conectado' : status.mode === 'demo' ? 'demo' : 'backend desconectado'}</span>
          <span>Motor: {status.engine}</span>
          <span>Fase: {status.phase}</span>
          <span>Perfil: {status.profile}</span>
        </div>
        <AdminDropdown status={status} />
        <button className="icon-button" type="button" title="Configuración preparada">⚙</button>
      </header>

      <div className="layout">
        <SystemStatusCompact status={status} />
        <ChatPanel
          status={status}
          onLog={(event) => setEvents((current) => [event, ...current].slice(0, 8))}
        />
      </div>

      <section className="console-strip">
        <button type="button" onClick={() => setLogsOpen((value) => !value)}>
          Consola segura {logsOpen ? '▾' : '▸'}
        </button>
        <div className="console-events">
          {latestEvents.map((event) => (
            <span className={`event ${event.level}`} key={event.id}>{event.message}</span>
          ))}
        </div>
      </section>
    </div>
  );
}
