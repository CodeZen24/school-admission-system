import { useState, type FormEvent } from 'react';
import { apiClient, ApiError } from '../../api/apiClient';
import type { AdmissionClass } from '../../api/types';
import { Alert } from '../Common/Alert';

interface CreateClassFormProps {
  onClassCreated: (newClass: AdmissionClass) => void;
}

export function CreateClassForm({ onClassCreated }: CreateClassFormProps) {
  const [name, setName] = useState('');
  const [totalSeats, setTotalSeats] = useState<string>('30');
  const [isOpen, setIsOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setFormError(null);
    setSuccessMessage(null);

    const trimmedName = name.trim();
    if (!trimmedName) {
      setFormError('Class name cannot be empty.');
      return;
    }

    const seatsNum = Number.parseInt(totalSeats, 10);
    if (Number.isNaN(seatsNum) || seatsNum <= 0) {
      setFormError('Total seats must be a positive integer greater than 0.');
      return;
    }

    setIsSubmitting(true);
    try {
      const created = await apiClient.createClass({
        name: trimmedName,
        totalSeats: seatsNum,
      });
      setName('');
      setTotalSeats('30');
      setSuccessMessage(`Class "${created.name}" created successfully with ${created.totalSeats} seats.`);
      onClassCreated(created);
      setTimeout(() => {
        setSuccessMessage(null);
        setIsOpen(false);
      }, 2000);
    } catch (err) {
      if (err instanceof ApiError) {
        setFormError(err.message);
      } else {
        setFormError('An unexpected error occurred while creating the class.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mb-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-4">
        <div>
          <h2 className="text-xl font-bold text-slate-900">Admission Classes</h2>
          <p className="text-xs text-slate-500">Live seat availability updated periodically across all classes</p>
        </div>
        <button
          type="button"
          className="inline-flex items-center justify-center px-4 py-2 text-sm font-medium rounded-lg text-white bg-indigo-600 hover:bg-indigo-700 transition-colors shadow-sm self-start sm:self-auto"
          onClick={() => {
            setIsOpen(!isOpen);
            setFormError(null);
          }}
        >
          {isOpen ? '✕ Close Form' : '+ Create New Class'}
        </button>
      </div>

      {isOpen && (
        <form
          onSubmit={handleSubmit}
          className="bg-white rounded-xl border border-indigo-100 p-6 shadow-sm mb-6 transition-all"
        >
          <h3 className="text-base font-semibold text-slate-900 mb-4">Create New Class</h3>

          {formError && (
            <div className="mb-4">
              <Alert type="error" onClose={() => setFormError(null)}>
                {formError}
              </Alert>
            </div>
          )}
          {successMessage && (
            <div className="mb-4">
              <Alert type="success" onClose={() => setSuccessMessage(null)}>
                {successMessage}
              </Alert>
            </div>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-4">
            <div className="sm:col-span-2">
              <label htmlFor="class-name-input" className="block text-xs font-semibold text-slate-700 mb-1">
                Class Name
              </label>
              <input
                id="class-name-input"
                type="text"
                className="w-full text-sm px-3.5 py-2 rounded-lg border border-slate-300 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 outline-none transition-all"
                placeholder="e.g. Grade 10 - Science Section A"
                value={name}
                onChange={e => setName(e.target.value)}
                disabled={isSubmitting}
                maxLength={200}
                required
              />
            </div>

            <div>
              <label htmlFor="total-seats-input" className="block text-xs font-semibold text-slate-700 mb-1">
                Total Seats
              </label>
              <input
                id="total-seats-input"
                type="number"
                min="1"
                step="1"
                className="w-full text-sm px-3.5 py-2 rounded-lg border border-slate-300 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 outline-none transition-all"
                placeholder="Seats"
                value={totalSeats}
                onChange={e => setTotalSeats(e.target.value)}
                disabled={isSubmitting}
                required
              />
            </div>
          </div>

          <div className="flex justify-end space-x-3">
            <button
              type="button"
              className="px-4 py-2 text-sm font-medium text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg transition-colors"
              onClick={() => setIsOpen(false)}
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="px-5 py-2 text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 rounded-lg transition-colors shadow-sm"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Creating...' : 'Save Class'}
            </button>
          </div>
        </form>
      )}
    </div>
  );
}
