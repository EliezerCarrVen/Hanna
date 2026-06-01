import type { HannaStatus } from '../types/hanna';

interface Props {
  status: HannaStatus;
}

export function SystemStatusCompact({ status }: Props) {
  const items = [
    ['Perfil', status.profile],
    ['Motor', status.engine],
    ['Fase', status.phase],
    ['Telegram', status.telegram],
    ['Backend', status.backend]
  ];

  return (
    <aside className="status-card" aria-label="Estado mínimo de Hanna">
      <div className="orb" aria-hidden="true"><span /></div>
      <h2>Núcleo</h2>
      <div className="status-list">
        {items.map(([label, value]) => (
          <div className="status-row" key={label}>
            <span>{label}</span>
            <strong>{value}</strong>
          </div>
        ))}
      </div>
    </aside>
  );
}
