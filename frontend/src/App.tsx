import { Activity, GraduationCap, ShieldCheck } from 'lucide-react';
import { ErrorBoundary } from './components/Common/ErrorBoundary';
import { Dashboard } from './pages/Dashboard';

export default function App() {
  return (
    <ErrorBoundary>
      <div className="min-h-screen text-[#17201e]">
        <header className="sticky top-0 z-20 border-b border-[#dfe6e2] bg-[#f9fbf9]/95 backdrop-blur-xl">
          <div className="mx-auto flex h-[4.5rem] max-w-7xl items-center justify-between gap-4 px-4 sm:px-6 lg:px-8">
            <div className="flex min-w-0 items-center gap-3">
              <div className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-[#e7f5f1] text-[#0f766e] shadow-sm ring-1 ring-[#cbe7df]">
                <GraduationCap
                  size={20}
                  strokeWidth={2.25}
                  aria-hidden="true"
                />
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
            <div className="hidden items-center gap-2 text-xs font-bold text-[#617069] md:flex">
              <ShieldCheck
                size={16}
                className="text-[#0f766e]"
                aria-hidden="true"
              />
              <span>First-come, first-served reservations</span>
            </div>
            <div className="flex shrink-0 items-center gap-2 rounded-full bg-white px-3 py-2 text-[11px] font-bold text-[#0f766e] shadow-sm ring-1 ring-[#e2e8e4]">
              <Activity size={14} aria-hidden="true" />
              <span className="hidden sm:inline">Live seat allocation</span>
              <span className="sm:hidden">Live</span>
            </div>
          </div>
        </header>
        <main>
          <Dashboard />
        </main>
        <footer className="mt-8 border-t border-[#dfe6e2] bg-white">
          <div className="mx-auto flex max-w-7xl flex-col gap-1 px-4 py-5 text-center text-[11px] font-medium text-[#75807b] sm:flex-row sm:items-center sm:justify-between sm:px-6 sm:text-left lg:px-8">
            <span>School Admission System</span>
            <span>Seat reservations are updated in real time.</span>
          </div>
        </footer>
      </div>
    </ErrorBoundary>
  );
}
