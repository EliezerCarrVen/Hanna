import { useMemo, useState } from 'react';
import { hannaClient } from '../api/hannaClient';
import type { HannaStatus } from '../types/hanna';

interface Props {
  status: HannaStatus;
}

export function AdminDropdown({ status }: Props) {
  const [open, setOpen] = useState(false);
  const options = useMemo(() => hannaClient.getAdminOptions(status), [status]);

  return (
    <div className="admin-dropdown">
      <button className="top-button" type="button" onClick={() => setOpen((value) => !value)}>
        Admin <span>{open ? '▴' : '▾'}</span>
      </button>
      {open && (
        <div className="admin-menu" role="menu">
          <div className="admin-menu-head">
            <strong>Administración compacta</strong>
            <small>{status.mode === 'connected' ? 'backend conectado' : 'pendiente de conexión'}</small>
          </div>
          {options.map((option) => (
            <button className="admin-option" type="button" key={option.section} disabled={status.mode !== 'connected'}>
              <span>{option.name}</span>
              <small>{option.state} · {option.action}</small>
              {option.note && <em>{option.note}</em>}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
