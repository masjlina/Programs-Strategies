import { useEffect, useState } from "react";

function easeOutCubic(t: number): number {
  return 1 - Math.pow(1 - t, 3);
}

interface UseCountUpOptions {
  duration?: number;
  decimals?: number;
  enabled?: boolean;
}

export function useCountUp(
  target: number,
  { duration = 1200, decimals = 0, enabled = true }: UseCountUpOptions = {},
): number {
  const [value, setValue] = useState(0);

  useEffect(() => {
    if (!enabled) {
      setValue(0);
      return;
    }

    if (target === 0) {
      setValue(0);
      return;
    }

    let startTime: number | null = null;
    let frameId = 0;

    const tick = (timestamp: number) => {
      if (startTime === null) startTime = timestamp;
      const progress = Math.min((timestamp - startTime) / duration, 1);
      const next = target * easeOutCubic(progress);
      setValue(next);
      if (progress < 1) {
        frameId = requestAnimationFrame(tick);
      }
    };

    frameId = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frameId);
  }, [target, duration, decimals, enabled]);

  if (decimals > 0) {
    return Number(value.toFixed(decimals));
  }

  return Math.round(value);
}
