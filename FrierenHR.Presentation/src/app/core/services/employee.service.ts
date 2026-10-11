import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  EmployeeDto, CreateEmployeeDto, UpdateEmployeeDto, ChangePasswordDto,
  ChangePasswordResultDto, ResetPasswordResultDto,
} from '../models/employee.model';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly baseUrl = `${environment.apiUrl}/api/employees`;
  constructor(private http: HttpClient) {}

  getByCompany(companyId: string) { return this.http.get<EmployeeDto[]>(this.baseUrl, { params: { companyId } }); }
  getById(id: string) { return this.http.get<EmployeeDto>(`${this.baseUrl}/${id}`); }
  getDirectReports(id: string) { return this.http.get<EmployeeDto[]>(`${this.baseUrl}/${id}/direct-reports`); }
  create(dto: CreateEmployeeDto) { return this.http.post<EmployeeDto>(this.baseUrl, dto); }
  update(id: string, dto: UpdateEmployeeDto) { return this.http.put<EmployeeDto>(`${this.baseUrl}/${id}`, dto); }
  /** Returns a replacement token, since the old one still says a change is required. */
  changePassword(id: string, dto: ChangePasswordDto) {
    return this.http.post<ChangePasswordResultDto>(`${this.baseUrl}/${id}/change-password`, dto);
  }

  /** HR only. Returns the temporary password once; it is not retrievable afterwards. */
  resetPassword(id: string) {
    return this.http.post<ResetPasswordResultDto>(`${this.baseUrl}/${id}/reset-password`, {});
  }
}