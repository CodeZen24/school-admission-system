import { ErrorBoundary } from './components/Common/ErrorBoundary';
import { Dashboard } from './pages/Dashboard';

export default function App() {
  return (
    <ErrorBoundary>
      <div className="min-h-screen bg-slate-50 flex flex-col justify-between font-sans text-slate-900">
        <nav className="bg-white border-b border-slate-200">
          <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <span className="text-xl">🎓</span>
              <span className="font-extrabold text-base tracking-tight text-slate-900">
                School Admission System
              </span>
              <span className="hidden sm:inline-block text-xs bg-slate-100 text-slate-600 font-mono px-2 py-0.5 rounded">
                .NET 8 + PostgreSQL + React
              </span>
            </div>
            <div className="flex items-center space-x-4 text-xs font-medium text-slate-500">
              <span className="hidden md:inline">FCFS Seat Reservation</span>
              <span className="w-1.5 h-1.5 rounded-full bg-slate-300 hidden md:inline" />
              <span className="hidden md:inline">Concurrency-Safe</span>
            </div>
          </div>
        </nav>

        <main className="flex-1">
          <Dashboard />
        </main>

        <footer className="border-t border-slate-200 bg-white py-6 text-center text-xs text-slate-400">
          <div className="max-w-6xl mx-auto px-4 flex flex-col sm:flex-row items-center justify-between gap-2">
            <span>OnnoRokom Projukti Limited — Assistant Software Engineer (.NET) Project</span>
            <span>PostgreSQL Row Locking &bull; Derived Seats &bull; 17-digit Student ID</span>
          </div>
        </footer>
      </div>
    </ErrorBoundary>
  );
}
