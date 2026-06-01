import { useRef, useState } from 'react';
import { hannaClient } from '../api/hannaClient';
import type { ChatMessage, HannaLogEvent, HannaStatus, PendingFile } from '../types/hanna';
import { FileDropzone } from './FileDropzone';
import { MessageBubble } from './MessageBubble';

interface Props {
  status: HannaStatus;
  onLog: (event: HannaLogEvent) => void;
}

function createMessage(role: ChatMessage['role'], text: string, localOnly = false): ChatMessage {
  return { id: crypto.randomUUID(), role, text, createdAt: new Date().toISOString(), localOnly };
}

export function ChatPanel({ status, onLog }: Props) {
  const [messages, setMessages] = useState<ChatMessage[]>([
    createMessage('system', 'Chat operativo de Hanna listo. Si el backend está desconectado, los mensajes quedan solo en modo visual.', true)
  ]);
  const [text, setText] = useState('');
  const [files, setFiles] = useState<PendingFile[]>([]);
  const [sending, setSending] = useState(false);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);

  function addFiles(nextFiles: File[]) {
    const prepared = nextFiles.map((file) => ({ id: crypto.randomUUID(), file, status: 'preparado' as const }));
    setFiles((current) => [...current, ...prepared]);
  }

  async function send() {
    const trimmed = text.trim();
    if (!trimmed && files.length === 0) return;

    const outgoingText = trimmed || `Archivos preparados: ${files.map((item) => item.file.name).join(', ')}`;
    setMessages((current) => [...current, createMessage('user', outgoingText, status.mode !== 'connected')]);
    setText('');
    setSending(true);

    if (status.mode !== 'connected') {
      const offline = 'Backend desconectado. No se envió a Hanna real.';
      setMessages((current) => [...current, createMessage('system', offline, true)]);
      onLog(hannaClient.createLocalLog(offline, 'warn'));
      setSending(false);
      return;
    }

    const result = await hannaClient.sendChatMessage(outgoingText, files.map((item) => item.file));
    if (result.ok && result.response) {
      setMessages((current) => [...current, createMessage('assistant', result.response)]);
      onLog(hannaClient.createLocalLog('Mensaje enviado al backend.', 'info'));
    } else {
      const fallback = result.preparedOnly
        ? 'Función preparada, no implementada en backend estable.'
        : 'Backend desconectado. No se envió a Hanna real.';
      setMessages((current) => [...current, createMessage('system', result.error || fallback, true)]);
      onLog(hannaClient.createLocalLog(result.error || fallback, result.offline ? 'warn' : 'info'));
    }
    setFiles([]);
    setSending(false);
  }

  return (
    <main className="chat-panel">
      <div className="chat-header">
        <div>
          <h1>Chat online</h1>
          <p>{status.mode === 'connected' ? 'Conectado a Hanna.' : 'Backend no conectado. El mensaje queda en modo visual y no se envía a Hanna real.'}</p>
        </div>
        <span className={`status-chip ${status.mode}`}>{status.backend}</span>
      </div>

      <section className="message-list" aria-live="polite">
        {messages.map((message) => <MessageBubble message={message} key={message.id} />)}
      </section>

      <FileDropzone files={files} onAdd={addFiles} onRemove={(id) => setFiles((current) => current.filter((item) => item.id !== id))} />

      <footer className="composer">
        <textarea
          ref={textareaRef}
          value={text}
          placeholder="Escribe a Hanna..."
          onChange={(event) => setText(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter' && !event.shiftKey) {
              event.preventDefault();
              void send();
            }
          }}
        />
        <button type="button" onClick={() => void send()} disabled={sending}>{sending ? 'Enviando' : 'Enviar'}</button>
      </footer>
    </main>
  );
}
