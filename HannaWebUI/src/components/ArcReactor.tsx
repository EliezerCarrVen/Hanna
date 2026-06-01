import { useEffect, useState } from "react";

interface Props {
  intensity?: number;
  active?: boolean;
}

export function ArcReactor({ intensity, active = false }: Props) {
  const [idle, setIdle] = useState(0.52);

  useEffect(() => {
    if (intensity !== undefined) return;
    let raf = 0;
    const start = performance.now();
    const tick = (time: number) => {
      const seconds = (time - start) / 1000;
      const value = 0.45 + Math.sin(seconds * 1.4) * 0.12 + Math.sin(seconds * 3.7) * 0.08;
      setIdle(Math.max(0.25, Math.min(1, value)));
      raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [intensity]);

  const level = intensity ?? idle;
  const coreScale = 1 + level * 0.25;
  const glowOpacity = 0.35 + level * 0.5;
  const ringDash = 60 + level * 120;

  return (
    <div className="reactor" aria-label="Núcleo visual Hanna">
      <svg viewBox="0 0 200 200" className="reactor-layer spin-slow">
        <polygon points="100,4 170,42 170,158 100,196 30,158 30,42" fill="none" stroke="var(--hud)" strokeWidth="0.6" opacity="0.52" strokeDasharray="3 3" />
        <circle cx="100" cy="100" r="95" fill="none" stroke="var(--hud)" strokeWidth="0.4" strokeDasharray="1 6" opacity="0.6" />
        {Array.from({ length: 60 }).map((_, i) => (
          <line
            key={i}
            x1="100"
            y1="6"
            x2="100"
            y2={i % 5 === 0 ? 16 : i % 2 === 0 ? 12 : 9}
            stroke="var(--hud)"
            strokeWidth={i % 5 === 0 ? 1.4 : 0.5}
            opacity={i % 5 === 0 ? 1 : 0.35}
            transform={`rotate(${i * 6} 100 100)`}
          />
        ))}
        {[0, 90, 180, 270].map((r) => (
          <g key={r} transform={`rotate(${r} 100 100)`}>
            <path d="M 100 0 L 88 0 L 88 6" fill="none" stroke="var(--accent)" strokeWidth="1.5" />
          </g>
        ))}
      </svg>

      <svg viewBox="0 0 200 200" className="reactor-layer inset spin-reverse">
        <circle cx="100" cy="100" r="86" fill="none" stroke="var(--hud-dim)" strokeWidth="0.6" />
        <circle cx="100" cy="100" r="86" fill="none" stroke="var(--hud)" strokeWidth="2.5" strokeDasharray="50 200" strokeLinecap="round" />
        <circle cx="100" cy="100" r="86" fill="none" stroke="var(--hud)" strokeWidth="1.2" strokeDasharray="8 14" opacity="0.6" />
        {Array.from({ length: 12 }).map((_, i) => {
          const a = (i / 12) * Math.PI * 2 - Math.PI / 2;
          const x = 100 + Math.cos(a) * 74;
          const y = 100 + Math.sin(a) * 74;
          return <text key={i} x={x} y={y} fontSize="5" fill="var(--hud)" textAnchor="middle" dominantBaseline="middle" opacity="0.7">{String(i * 30).padStart(3, "0")}</text>;
        })}
      </svg>

      <svg viewBox="0 0 200 200" className="reactor-layer">
        {Array.from({ length: 48 }).map((_, i) => {
          const a = (i / 48) * Math.PI * 2;
          const seed = Math.sin(i * 1.3) * 0.5 + 0.5;
          const height = 4 + level * 14 * (0.4 + seed * 0.8);
          const r1 = 64;
          const r2 = r1 + height;
          const x1 = 100 + Math.cos(a) * r1;
          const y1 = 100 + Math.sin(a) * r1;
          const x2 = 100 + Math.cos(a) * r2;
          const y2 = 100 + Math.sin(a) * r2;
          return <line key={i} x1={x1} y1={y1} x2={x2} y2={y2} stroke="var(--hud)" strokeWidth="1.4" strokeLinecap="round" opacity={0.4 + level * 0.6} className="svg-glow" />;
        })}
      </svg>

      <div className="radar-sweep"><span /></div>
      <div className="reactor-halo" style={{ width: `${30 + level * 30}%`, height: `${30 + level * 30}%`, opacity: glowOpacity }} />

      <svg viewBox="0 0 200 200" className="reactor-layer">
        <circle cx="100" cy="100" r="42" fill="none" stroke="var(--hud)" strokeWidth="2" strokeDasharray={`${ringDash} 999`} className="svg-glow" />
        <circle cx="100" cy="100" r="36" fill="none" stroke="var(--hud-dim)" strokeWidth="0.5" strokeDasharray="2 3" />
      </svg>

      <div className="reactor-core" style={{ transform: `scale(${coreScale})` }}>
        <div className="reactor-core-inner"><span /></div>
      </div>

      {active && <div className="transmitting">▲ Transmitiendo</div>}
    </div>
  );
}
