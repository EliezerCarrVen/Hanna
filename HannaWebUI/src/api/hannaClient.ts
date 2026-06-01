export type ConnectionState = "connected" | "pending" | "error";

export interface HannaStatus {
  connection: ConnectionState;
  profile: string;
  engine: string;
  phase: string;
  telegram: string;
  ollama: string;
  mongo: string;
  lastError: string;
  source: "backend" | "visual-demo";
}

export interface HannaCommandResult {
  ok: boolean;
  command: string;
  response: string;
  source: "backend" | "visual-demo";
}

const envBase = (import.meta.env.VITE_HANNA_API_BASE_URL as string | undefined)?.replace(/\/$/, "");
const apiBase = envBase || "";

async function safeJson<T>(url: string, init?: RequestInit): Promise<T | null> {
  try {
    const response = await fetch(url, {
      ...init,
      headers: {
        "Content-Type": "application/json",
        ...(init?.headers ?? {}),
      },
    });

    if (!response.ok) return null;
    return (await response.json()) as T;
  } catch {
    return null;
  }
}

export async function getHannaStatus(): Promise<HannaStatus> {
  const backend = await safeJson<Partial<HannaStatus>>(`${apiBase}/api/status`);

  if (backend) {
    return {
      connection: "connected",
      profile: backend.profile ?? "pendiente de backend",
      engine: backend.engine ?? "pendiente de backend",
      phase: backend.phase ?? "pendiente de backend",
      telegram: backend.telegram ?? "pendiente de backend",
      ollama: backend.ollama ?? "pendiente de backend",
      mongo: backend.mongo ?? "pendiente de backend",
      lastError: backend.lastError ?? "sin dato del backend",
      source: "backend",
    };
  }

  return {
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
}

export async function sendHannaCommand(command: string): Promise<HannaCommandResult> {
  const backend = await safeJson<{ response?: string; text?: string; message?: string }>(`${apiBase}/api/comando`, {
    method: "POST",
    body: JSON.stringify({ command }),
  });

  if (backend) {
    return {
      ok: true,
      command,
      response: backend.response ?? backend.text ?? backend.message ?? "Comando enviado al backend.",
      source: "backend",
    };
  }

  return {
    ok: false,
    command,
    response: "Preparado, no implementado: falta conectar POST /api/comando en Hanna.",
    source: "visual-demo",
  };
}
