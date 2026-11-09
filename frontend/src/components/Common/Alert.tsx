import type { ReactNode } from 'react';

export type AlertType = 'info' | 'success' | 'warning' | 'error';

interface AlertProps {
  type?: AlertType;
  title?: string;
  children: ReactNode;
  onClose?: () => void;
}

const ALERT_STYLES: Record<AlertType, { container: string; title: string; close: string }> = {
  info: {
    container: 'bg-sky-50 border-sky-200 text-sky-900',
    title: 'text-sky-800',
    close: 'text-sky-600 hover:text-sky-800',
  },
  success: {
    container: 'bg-emerald-50 border-emerald-200 text-emerald-900',
    title: 'text-emerald-800',
    close: 'text-emerald-600 hover:text-emerald-800',
  },
  warning: {
    container: 'bg-amber-50 border-amber-200 text-amber-900',
    title: 'text-amber-800',
    close: 'text-amber-600 hover:text-amber-800',
  },
  error: {
    container: 'bg-rose-50 border-rose-200 text-rose-900',
    title: 'text-rose-800',
    close: 'text-rose-600 hover:text-rose-800',
  },
};

export function Alert({ type = 'info', title, children, onClose }: AlertProps) {
  const styles = ALERT_STYLES[type];

  return (
    <div
      className={`rounded-lg border p-4 flex items-start justify-between text-sm transition-all ${styles.container}`}
      role="alert"
    >
      <div className="flex-1 mr-3">
        {title && <div className={`font-semibold mb-1 ${styles.title}`}>{title}</div>}
        <div className="leading-relaxed">{children}</div>
      </div>
      {onClose && (
        <button
          type="button"
          className={`font-bold text-lg leading-none cursor-pointer p-1 rounded hover:bg-black/5 ${styles.close}`}
          onClick={onClose}
          aria-label="Dismiss alert"
        >
          ×
        </button>
      )}
    </div>
  );
}
