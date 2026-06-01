export function Waveform() {
  return (
    <div className="waveform" aria-label="Audio visual demo">
      {Array.from({ length: 42 }).map((_, i) => (
        <span key={i} style={{ animationDelay: `${i * 0.04}s`, animationDuration: `${0.6 + (i % 5) * 0.15}s`, height: `${32 + Math.sin(i * 0.5) * 28 + 24}%` }} />
      ))}
    </div>
  );
}
