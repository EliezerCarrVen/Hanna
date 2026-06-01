export type BackendMode = 'demo' | 'connected' | 'disconnected';
export type AdminOptionState = 'conectado' | 'pendiente' | 'preparado';

export interface HannaStatus {
  mode: BackendMode;
  profile: string;
  engine: string;
  phase: string;
  telegram: string;
  backend: string;
  message?: string;
}

export interface HannaLogEvent {
  id: string;
  level: 'info' | 'warn' | 'error';
  message: string;
  at: string;
}

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  text: string;
  createdAt: string;
  localOnly?: boolean;
}

export interface PendingFile {
  id: string;
  file: File;
  status: 'pendiente de envío' | 'preparado';
}

export interface AdminOption {
  section: string;
  name: string;
  state: AdminOptionState;
  action?: string;
  note?: string;
}

export interface SendChatResult {
  ok: boolean;
  response?: string;
  offline?: boolean;
  preparedOnly?: boolean;
  error?: string;
}
