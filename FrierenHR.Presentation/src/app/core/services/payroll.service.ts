import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  PayrollDeductionDto, PayslipDto, PayslipRunDto, PayslipSummaryDto, SavePayrollDeductionDto,
} from '../models/payroll.model';

@Injectable({ providedIn: 'root' })
export class PayslipService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/payslips`;

  mine() { return this.http.get<PayslipSummaryDto[]>(`${this.baseUrl}/mine`); }

  byEmployee(employeeId: string) {
    return this.http.get<PayslipSummaryDto[]>(`${this.baseUrl}/by-employee/${employeeId}`);
  }

  getById(id: string) { return this.http.get<PayslipDto>(`${this.baseUrl}/${id}`); }

  forPeriod(from: string, to: string) {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<PayslipSummaryDto[]>(`${this.baseUrl}/period`, { params });
  }

  generate(from: string, to: string) {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.post<PayslipRunDto>(`${this.baseUrl}/generate`, {}, { params });
  }
}

@Injectable({ providedIn: 'root' })
export class PayrollDeductionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/payroll-deductions`;

  getAll() { return this.http.get<PayrollDeductionDto[]>(this.baseUrl); }
  save(dto: SavePayrollDeductionDto) { return this.http.post<PayrollDeductionDto>(this.baseUrl, dto); }
  delete(id: string) { return this.http.delete<void>(`${this.baseUrl}/${id}`); }
}
