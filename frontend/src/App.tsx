import { ErrorBoundary } from './components/Common/ErrorBoundary';
import { Dashboard } from './pages/Dashboard';

export default function App() {
  return (
    <ErrorBoundary>
      <div className="min-h-screen text-[#17201e]">
        <header className="sticky top-0 z-20 border-b border-[#dfe6e2]/90 bg-[#f9fbf9]/85 backdrop-blur-xl">
          <div className="mx-auto flex h-[4.5rem] max-w-7xl items-center justify-between gap-4 px-4 sm:px-6 lg:px-8">
            <div className="flex min-w-0 items-center gap-3">
              <div
                className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-[#e7f5f1] text-lg font-extrabold text-[#0f766e] shadow-sm ring-1 ring-[#cbe7df]"
                aria-hidden="true"
              >
                S
              </div>
              <div className="min-w-0">
                <p className="truncate text-sm font-extrabold tracking-tight text-[#17201e]">
                  School Admission System
                </p>
                <p className="hidden text-[11px] font-medium text-[#65716c] sm:block">
                  Portal &amp; seat allocation
                </p>
              </div>
            </div>
            <div className="hidden items-center rounded-xl bg-[#edf1ee] p-1 text-xs font-semibold text-[#68736f] md:flex">
              <span className="rounded-lg bg-white px-3 py-1.5 text-[#0f766e] shadow-sm">
                Seat allocation
              </span>
              <span className="px-3 py-1.5">Live capacity</span>
            </div>
            <div className="flex shrink-0 items-center gap-2 rounded-full bg-white px-3 py-2 text-[11px] font-bold text-[#0f766e] shadow-sm ring-1 ring-[#e2e8e4]">
              <span className="relative flex h-2 w-2">
                <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-[#14b8a6] opacity-50" />
                <span className="relative inline-flex h-2 w-2 rounded-full bg-[#0f9b86]" />
              </span>
              <span className="hidden sm:inline">Live seat allocation</span>
              <span className="sm:hidden">Live</span>
            </div>
          </div>
        </header>
        <main>
          <Dashboard />
        </main>
        <footer className="mt-8 border-t border-[#dfe6e2] bg-white/70">
          <div className="mx-auto flex max-w-7xl flex-col gap-1 px-4 py-5 text-center text-[11px] font-medium text-[#75807b] sm:flex-row sm:items-center sm:justify-between sm:px-6 sm:text-left lg:px-8">
            <span>School admission portal</span>
            <span>
              Live availability · PostgreSQL-backed reservations · 17-digit
              student ID
            </span>
          </div>
        </footer>
      </div>
    </ErrorBoundary>
  );
}
