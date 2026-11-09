import { Component, type ReactNode, type ErrorInfo } from 'react';

interface ErrorBoundaryProps {
  children: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error?: Error;
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  public override state: ErrorBoundaryState = {
    hasError: false,
  };

  public static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  public override componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('Uncaught error caught by ErrorBoundary:', error, errorInfo);
  }

  private handleReload = () => {
    window.location.reload();
  };

  private handleReset = () => {
    this.setState({ hasError: false, error: undefined });
  };

  public override render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-screen flex items-center justify-center bg-slate-50 p-6" role="alert">
          <div className="max-w-md w-full bg-white rounded-xl border border-slate-200 p-8 shadow-sm">
            <div className="flex items-center space-x-3 text-rose-600 mb-4">
              <span className="text-2xl">⚠️</span>
              <h2 className="text-xl font-semibold">Something went wrong</h2>
            </div>
            <p className="text-sm text-slate-600 mb-6 leading-relaxed">
              {this.state.error?.message || 'An unexpected rendering error occurred in the dashboard.'}
            </p>
            <div className="flex space-x-3">
              <button
                type="button"
                className="flex-1 bg-indigo-600 hover:bg-indigo-700 text-white font-medium py-2 px-4 rounded-lg transition-colors text-sm"
                onClick={this.handleReset}
              >
                Try Again
              </button>
              <button
                type="button"
                className="flex-1 bg-slate-100 hover:bg-slate-200 text-slate-700 font-medium py-2 px-4 rounded-lg transition-colors text-sm"
                onClick={this.handleReload}
              >
                Reload Page
              </button>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
