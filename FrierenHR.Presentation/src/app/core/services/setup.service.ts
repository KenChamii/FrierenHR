import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { SetupProgressDto, SetupStepDto } from '../models/setup.model';
import { AuthService } from './auth.service';

const DISMISSED_KEY = 'hr_setup_dismissed';
const ACKNOWLEDGED_KEY = 'hr_setup_acknowledged';

/** Tracks how far a new company has got with setup. */
@Injectable({ providedIn: 'root' })
export class SetupService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  private readonly raw = signal<SetupProgressDto | null>(null);

  /** Steps marked done by hand. */
  readonly acknowledged = signal<string[]>(this.readAcknowledged());

  readonly dismissed = signal(this.readDismissed());

  /** True when the deployment offers sample data and this company is still empty. */
  readonly sampleDataAvailable = signal(false);
  readonly loadingSampleData = signal(false);

  /** Progress with manual acknowledgements folded in. */
  readonly progress = computed<SetupProgressDto | null>(() => {
    const progress = this.raw();
    if (!progress) return null;

    const acknowledged = this.acknowledged();
    const steps = progress.steps.map(step =>
      step.done || !acknowledged.includes(step.key) ? step : { ...step, done: true });

    const required = steps.filter(s => !s.optional);
    const completed = required.filter(s => s.done).length;

    return {
      ...progress,
      steps,
      completed,
      isComplete: completed === required.length,
      nextRoute: steps.find(s => !s.done && !s.optional)?.route ?? null,
    };
  });

  /** The step to do next, or null when setup is finished or hidden. */
  readonly nextStep = computed<SetupStepDto | null>(() => {
    if (this.dismissed()) return null;
    const progress = this.progress();
    if (!progress || progress.isComplete) return null;
    return progress.steps.find(s => !s.done && !s.optional) ?? null;
  });

  readonly nextRoute = computed(() => this.nextStep()?.route ?? null);

  readonly showChecklist = computed(() => {
    const progress = this.progress();
    return !!progress && !progress.isComplete && !this.dismissed();
  });

  /** Only HR can read this, and only HR can act on it, so nobody else asks. */
  refresh(): void {
    if (this.auth.currentRole() !== 'HRAdmin') return;

    this.http.get<SetupProgressDto>(`${environment.apiUrl}/api/setup/progress`).subscribe({
      next: progress => this.raw.set(progress),
      // Silent.
      error: () => this.raw.set(null),
    });

    this.http.get<{ available: boolean }>(`${environment.apiUrl}/api/setup/sample-data`).subscribe({
      next: status => this.sampleDataAvailable.set(status.available),
      error: () => this.sampleDataAvailable.set(false),
    });
  }

  /** Fills the company with sample staff, leave and attendance, then reloads so every screen shows it. */
  loadSampleData(): void {
    this.loadingSampleData.set(true);
    this.http.post(`${environment.apiUrl}/api/setup/sample-data`, {}).subscribe({
      next: () => window.location.reload(),
      error: () => { this.loadingSampleData.set(false); this.refresh(); },
    });
  }

  acknowledge(key: string): void {
    const next = [...new Set([...this.acknowledged(), key])];
    this.acknowledged.set(next);
    this.write(ACKNOWLEDGED_KEY, JSON.stringify(next));
  }

  dismiss(): void {
    this.dismissed.set(true);
    this.write(DISMISSED_KEY, 'true');
  }

  private readDismissed(): boolean {
    try {
      return localStorage.getItem(DISMISSED_KEY) === 'true';
    } catch {
      return false;
    }
  }

  private readAcknowledged(): string[] {
    try {
      const stored = localStorage.getItem(ACKNOWLEDGED_KEY);
      return stored ? (JSON.parse(stored) as string[]) : [];
    } catch {
      return [];
    }
  }

  private write(key: string, value: string): void {
    try {
      localStorage.setItem(key, value);
    } catch {
      // Private browsing, or storage refused.
    }
  }
}
