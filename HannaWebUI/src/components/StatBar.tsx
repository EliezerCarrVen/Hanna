export function StatBar({ label, value, unit = "%" }: { label: string; value: number; unit?: string }) {
  return (
    <div className="stat-bar">
      <div className="stat-row">
        <span>{label}</span>
        <strong>{value}{unit}</strong>
      </div>
      <div className="stat-track"><span style={{ width: `${Math.min(Math.max(value, 0), 100)}%` }} /></div>
    </div>
  );
}
