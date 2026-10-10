import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AvailabilityDto, LeaveForecastDto, LiabilityDto, PayrollPeriodDto,
} from '../models/reporting.model';

@Injectable({ providedIn: 'root' })
export class ReportingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/reports`;

  availability(from: string, to: string, departmentId?: string | null) {
    let params = new HttpParams().set('from', from).set('to', to);
    if (departmentId) params = params.set('departmentId', departmentId);
    return this.http.get<AvailabilityDto>(`${this.baseUrl}/availability`, { params });
  }

  /** Omitting employeeId forecasts the signed-in employee's own balance. */
  leaveForecast(employeeId?: string, horizon?: string) {
    let params = new HttpParams();
    if (employeeId) params = params.set('employeeId', employeeId);
    if (horizon) params = params.set('horizon', horizon);
    return this.http.get<LeaveForecastDto>(`${this.baseUrl}/leave-forecast`, { params });
  }

  payrollPeriod(from: string, to: string) {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<PayrollPeriodDto>(`${this.baseUrl}/payroll-period`, { params });
  }

  leaveLiability(dailyRate: number) {
    const params = new HttpParams().set('dailyRate', dailyRate);
    return this.http.get<LiabilityDto>(`${this.baseUrl}/leave-liability`, { params });
  }

  /**
   * CSV comes back as a blob rather than text, so the browser saves the bytes the API sent,
   * byte order mark included. Re-encoding a string here is what breaks accented names.
   */
  payrollPeriodCsv(from: string, to: string) {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get(`${this.baseUrl}/payroll-period.csv`, { params, responseType: 'blob' });
  }

  leaveLiabilityCsv(dailyRate: number) {
    const params = new HttpParams().set('dailyRate', dailyRate);
    return this.http.get(`${this.baseUrl}/leave-liability.csv`, { params, responseType: 'blob' });
  }
}
