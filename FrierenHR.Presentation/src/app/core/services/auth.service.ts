import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { LoginDto, LoginResultDto } from '../models/auth.model';

const STORAGE_KEY = 'hr_auth';

interface StoredAuth { token: string; employeeId: string; fullName: string; role: string; companyId: string; }

@Injectable({ providedIn: 'root' })
export class AuthService {
  // sessionStorage fallback (not localStorage) — cleared per tab/session, reduces XSS token lifetime.
  private readonly _auth = signal<StoredAuth | null>(this.readFromStorage());

  readonly isAuthenticated = computed(() => this._auth() !== null);
  readonly currentRole = computed(() => this._auth()?.role ?? null);
  readonly currentEmployeeId = computed(() => this._auth()?.employeeId ?? null);
  readonly currentFullName = computed(() => this._auth()?.fullName ?? null);
  readonly currentCompanyId = computed(() => this._auth()?.companyId ?? null);

  constructor(private http: HttpClient, private router: Router) {}

  login(dto: LoginDto) {
    return this.http.post<LoginResultDto>(`${environment.apiUrl}/api/auth/login`, dto);
  }

  /**
   * Stores the session from a successful login. companyId isn't part of the
   * login response, so it defaults to '' here — call updateCompanyId() once
   * the employee's own record has been fetched (see LoginComponent.submit()).
   */
  setSession(result: LoginResultDto, companyId = ''): void {
    const stored: StoredAuth = { token: result.token, employeeId: result.employeeId, fullName: result.fullName, role: result.role, companyId };
    this._auth.set(stored);
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(stored));
  }

  /** Backfills companyId onto an already-established session. */
  updateCompanyId(companyId: string): void {
    const current = this._auth();
    if (!current) return;
    const stored: StoredAuth = { ...current, companyId };
    this._auth.set(stored);
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(stored));
  }

  token(): string | null { return this._auth()?.token ?? null; }

  logout(): void {
    this._auth.set(null);
    sessionStorage.removeItem(STORAGE_KEY);
    this.router.navigateByUrl('/login');
  }

  private readFromStorage(): StoredAuth | null {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try { return JSON.parse(raw) as StoredAuth; } catch { return null; }
  }
}
