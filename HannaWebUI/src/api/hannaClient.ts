import type { AdminOption, HannaLogEvent, HannaStatus, SendChatResult } from '../types/hanna';

const configuredBaseUrl = (import.meta.env.VITE_HANNA_API_BASE_URL || 'http://127.0.0.1:8790').replace(/\/$/, '');
const demoMode = String(import.meta.env.VITE_HANNA_DEMO_MODE || 'true').toLowerCase() === 'true';
const timeoutMs = Number(import.meta.env.VITE_HANNA_TIMEOUT_MS || 1800);
const retryDelayMs = Number(import.meta.env.VITE_HANNA_RETRY_DELAY_MS || 30000);

let offlineUntil = 0;
let lastOfflineReason = 'backend desconectado';

function nowIso() {
  return new Date().toISOString();
}

function demoStatus(): HannaStatus {
  return {
    mode: 'demo',
    profile: 'demo',
    engine: 'preparado',
    phase: 'preparada',
    telegram: 'preparado',
    backend: 'modo demo',
    message: 'Modo demo activo: no se llama al backend.'
  };
}

function disconnectedStatus(message = lastOfflineReason): HannaStatus {
  return {
    mode: 'disconnected',
    profile: 'pendiente',
    engine: 'pendiente',
    phase: 'pendiente',
    telegram: 'pendiente',
    backend: 'backend desconectado',
    message
  };
}

function normalizeStatus(raw: unknown): HannaStatus {
  const value = (raw || {}) as Record<string, unknown>;
  const settings = (value.settings || {}) as Record<string, unknown>;
  return {
    mode: 'connected',
    profile: String(value.hannaMode || value.mode || settings.hannaMode || 'conectado'),
    engine: String(value.engine || value.model || settings.ollamaModel || 'conectado'),
    phase: String(value.phase || value.activePhase || ((value.status as Record<string, unknown> | undefined)?.activePhase) || 'conectada'),
    telegram: String(value.telegram || (value.pairingConfigured ? 'configurado' : 'pendiente')),
    backend: 'conectado',
    message: 'Backend conectado.'
  };
}

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  if (demoMode) {
    throw new Error('demo-mode');
  }

  const controller = new AbortController();
  const timer = window.setTimeout(() => controller.abort(), timeoutMs);
  try {
    const response = await fetch(`${configuredBaseUrl}${path}`, {
      ...init,
      signal: controller.signal,
      headers: {
        'Content-Type': 'application/json',
        ...(init?.headers || {})
      }
    });
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }
    return (await response.json()) as T;
  } finally {
    window.clearTimeout(timer);
  }
}

function markOffline(error: unknown) {
  const reason = error instanceof Error && error.message !== 'demo-mode' ? error.message : 'backend desconectado';
  lastOfflineReason = reason.includes('abort') ? 'timeout consultando backend' : reason;
  offlineUntil = Date.now() + retryDelayMs;
}

export const hannaClient = {
  isDemoMode: demoMode,
  apiBaseUrl: configuredBaseUrl,
  retryDelayMs,

  async getStatus(force = false): Promise<HannaStatus> {
    if (demoMode) return demoStatus();
    if (!force && Date.now() < offlineUntil) return disconnectedStatus();

    try {
      try {
        const state = await requestJson<unknown>('/api/status');
        return normalizeStatus(state);
      } catch {
        try {
          const adminState = await requestJson<unknown>('/api/state');
          return normalizeStatus(adminState);
        } catch {
          const mobileState = await requestJson<unknown>('/api/mobile/state');
          return normalizeStatus(mobileState);
        }
      }
    } catch (error) {
      markOffline(error);
      return disconnectedStatus();
    }
  },

  async getDiagnostico(): Promise<string> {
    if (demoMode) return 'Diagnóstico preparado en modo demo.';
    try {
      const result = await requestJson<unknown>('/api/diagnostico');
      return JSON.stringify(result, null, 2);
    } catch (error) {
      markOffline(error);
      return 'Backend desconectado. Diagnóstico preparado, no implementado sin backend.';
    }
  },

  async getLogs(): Promise<HannaLogEvent[]> {
    if (demoMode) return [];
    try {
      const result = await requestJson<{ events?: HannaLogEvent[] }>('/api/logs');
      return result.events || [];
    } catch (error) {
      markOffline(error);
      return [];
    }
  },

  async sendCommand(command: string): Promise<SendChatResult> {
    if (demoMode) return { ok: false, offline: true, preparedOnly: true, error: 'Modo demo activo.' };
    try {
      const result = await requestJson<{ response?: string; text?: string; ok?: boolean }>('/api/comando', {
        method: 'POST',
        body: JSON.stringify({ command })
      });
      return { ok: result.ok !== false, response: result.response || result.text || 'Comando enviado.' };
    } catch (error) {
      markOffline(error);
      return { ok: false, offline: true, error: 'Backend desconectado.' };
    }
  },

  async sendChatMessage(message: string, files?: File[]): Promise<SendChatResult> {
    if (demoMode) return { ok: false, offline: true, preparedOnly: true, error: 'Modo demo activo.' };
    if (files?.length) {
      return { ok: false, preparedOnly: true, error: 'Subida de archivos preparada, no implementada sin endpoint estable.' };
    }

    try {
      try {
        const chat = await requestJson<{ response?: string; text?: string; ok?: boolean }>('/api/chat', {
          method: 'POST',
          body: JSON.stringify({ message })
        });
        return { ok: chat.ok !== false, response: chat.response || chat.text || 'Mensaje enviado.' };
      } catch {
        const mobile = await requestJson<{ response?: string; ok?: boolean }>('/api/mobile/message', {
          method: 'POST',
          body: JSON.stringify({ text: message })
        });
        return { ok: mobile.ok !== false, response: mobile.response || 'Mensaje enviado.' };
      }
    } catch (error) {
      markOffline(error);
      return { ok: false, offline: true, error: 'Backend desconectado.' };
    }
  },

  async uploadFiles(files: File[]): Promise<SendChatResult> {
    if (demoMode) return { ok: false, preparedOnly: true, error: 'Subida preparada en modo demo.' };
    if (!files.length) return { ok: true, response: 'Sin archivos.' };
    return { ok: false, preparedOnly: true, error: 'Endpoint POST /api/files/upload preparado, no implementado en backend estable.' };
  },

  getAdminOptions(status: HannaStatus): AdminOption[] {
    const connected = status.mode === 'connected';
    const state = connected ? 'conectado' : 'pendiente';
    const note = connected ? 'Disponible si el endpoint existe.' : 'pendiente de conexión';
    return [
      ['Estado', 'Resumen de Hanna'],
      ['Motores', '/motor actual / cambio de motor'],
      ['Fases', '/fase actual / cambio de fase'],
      ['Memoria', 'búsqueda segura'],
      ['Logs', 'logs recientes'],
      ['Errores', 'último error'],
      ['Archivos', 'revisión/subida preparada'],
      ['Voz / TTS', 'estado de voz'],
      ['Telegram', 'estado de canal'],
      ['Spotify', 'estado de integración'],
      ['Pantalla', 'análisis local'],
      ['API móvil', 'Mobile API 8790'],
      ['WebChat', 'chat web'],
      ['Costos / tokens', 'presupuesto y tokens'],
      ['Configuración', 'variables y perfil']
    ].map(([section, action]) => ({ section, name: section, action, state: connected ? state : 'pendiente', note }));
  },

  createLocalLog(message: string, level: HannaLogEvent['level'] = 'info'): HannaLogEvent {
    return { id: crypto.randomUUID(), level, message, at: nowIso() };
  }
};
