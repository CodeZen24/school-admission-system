import { useState, type FormEvent } from 'react';
import { apiClient, ApiError } from '../../api/apiClient';
import type { AdmissionClass } from '../../api/types';
import { Alert } from '../Common/Alert';

interface CreateClassFormProps {
  onClassCreated: (newClass: AdmissionClass) => void;
}

export function CreateClassForm({ onClassCreated }: CreateClassFormProps) {
  const [name, setName] = useState('');
  const [totalSeats, setTotalSeats] = useState('30');
  const [isOpen, setIsOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setFormError(null);
    const trimmedName = name.trim();
    const seats = Number.parseInt(totalSeats, 10);
    if (!trimmedName) {
      setFormError('Class name cannot be empty.');
      return;
    }
    if (Number.isNaN(seats) || seats <= 0) {
      setFormError('Total seats must be a positive whole number.');
      return;
    }
    setIsSubmitting(true);
    try {
      const created = await apiClient.createClass({
        name: trimmedName,
        totalSeats: seats,
      });
      setName('');
      setTotalSeats('30');
      setIsOpen(false);
      onClassCreated(created);
    } catch (error) {
      setFormError(
        error instanceof ApiError
          ? error.message
          : 'The class could not be created. Please try again.',
      );
    } finally {
      setIsSubmitting(false);
    }
  };
  return (
    <section className="mb-8" aria-labelledby="classes-heading">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="mb-1 text-[10px] font-extrabold uppercase tracking-[0.08em] text-[#0f766e]">
            Seat availability
          </p>
          <h2
            id="classes-heading"
            className="text-2xl font-extrabold tracking-tight text-[#17201e]"
          >
            Admission classes
          </h2>
          <p className="mt-1 text-xs font-medium text-[#718078]">
            Availability refreshes automatically while this page is open.
          </p>
        </div>
        <button
          type="button"
          className="rounded-xl bg-[#17201e] px-4 py-3 text-sm font-extrabold text-white shadow-sm transition hover:bg-[#2d3a35]"
          onClick={() => {
            setIsOpen((open) => !open);
            setFormError(null);
          }}
        >
          {isOpen ? 'Close form' : '+ Create new class'}
        </button>
      </div>
      {isOpen && (
        <form
          onSubmit={handleSubmit}
          className="mt-5 rounded-2xl bg-white p-5 shadow-[0_2px_16px_rgba(28,44,38,0.06)] ring-1 ring-[#e2e9e5] sm:p-6"
        >
          <div className="mb-5 flex items-center justify-between">
            <div>
              <h3 className="font-extrabold text-[#17201e]">
                Create an admission class
              </h3>
              <p className="mt-1 text-xs text-[#718078]">
                Set a name and the approved seat capacity.
              </p>
            </div>
            <span className="rounded-lg bg-[#e7f5f1] px-2 py-1 text-[10px] font-bold text-[#0f766e]">
              Administrator action
            </span>
          </div>
          {formError && (
            <div className="mb-4">
              <Alert type="error" onClose={() => setFormError(null)}>
                {formError}
              </Alert>
            </div>
          )}
          <div className="grid gap-4 sm:grid-cols-[1fr_11rem]">
            <label className="block text-xs font-bold text-[#44514c]">
              Class name
              <input
                type="text"
                className="mt-2 w-full rounded-xl border border-[#d5dfda] bg-[#f9fbfa] px-3.5 py-3 text-sm text-[#17201e] outline-none transition focus:border-[#0f766e]"
                placeholder="e.g. Grade 10 - Science Section A"
                value={name}
                onChange={(event) => setName(event.target.value)}
                disabled={isSubmitting}
                maxLength={200}
                required
              />
            </label>
            <label className="block text-xs font-bold text-[#44514c]">
              Total seats
              <input
                type="number"
                min="1"
                step="1"
                className="mt-2 w-full rounded-xl border border-[#d5dfda] bg-[#f9fbfa] px-3.5 py-3 text-sm text-[#17201e] outline-none transition focus:border-[#0f766e]"
                value={totalSeats}
                onChange={(event) => setTotalSeats(event.target.value)}
                disabled={isSubmitting}
                required
              />
            </label>
          </div>
          <div className="mt-5 flex justify-end gap-3">
            <button
              type="button"
              className="rounded-xl px-4 py-2.5 text-sm font-bold text-[#59665f] hover:bg-[#edf1ee]"
              onClick={() => setIsOpen(false)}
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="rounded-xl bg-[#0f766e] px-5 py-2.5 text-sm font-extrabold text-white shadow-sm transition hover:bg-[#0b625b] disabled:opacity-60"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Creating…' : 'Create class'}
            </button>
          </div>
        </form>
      )}
    </section>
  );
}
