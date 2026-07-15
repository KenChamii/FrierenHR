import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { LoginDto, LoginResultDto } from '../models/auth.model';

const STORAGE_KEY = 'hr_auth';

interface StoredAuth { token: string; employeeId: string; fullName: string; role: string; }

@Injectable({ providedIn: 'root' })
export class AuthService {
  // sessionStorage fallback (not localStorage) — cleared per tab/session, reduces XSS token lifetime.
  private readonly _auth = signal<StoredAuth | null>(this.readFromStorage());

  readonly isAuthenticated = computed(() => this._auth() !== null);
  readonly currentRole = computed(() => this._auth()?.role ?? null);
  readonly currentEmployeeId = computed(() => this._auth()?.employeeId ?? null);
  readonly currentFullName = computed(() => this._auth()?.fullName ?? null);

  constructor(private http: HttpClient, private router: Router) {}

  login(dto: LoginDto) {
    return this.http.post<LoginResultDto>(`${environment.apiUrl}/api/auth/login`, dto);
  }

  setSession(result: LoginResultDto): void {
    const stored: StoredAuth = { token: result.token, employeeId: result.employeeId, fullName: result.fullName, role: result.role };
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