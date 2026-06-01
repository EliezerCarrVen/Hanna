import type { ChatMessage } from '../types/hanna';

interface Props {
  message: ChatMessage;
}

export function MessageBubble({ message }: Props) {
  return (
    <article className={`message message-${message.role}`}>
      <div className="message-meta">
        <span>{message.role === 'user' ? 'Tú' : message.role === 'assistant' ? 'Hanna' : 'Sistema'}</span>
        {message.localOnly && <small>modo visual</small>}
      </div>
      <p>{message.text}</p>
    </article>
  );
}
