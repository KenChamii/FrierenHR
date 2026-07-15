import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { EmployeeDto, CreateEmployeeDto, UpdateEmployeeDto } from '../models/employee.model';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly baseUrl = `${environment.apiUrl}/api/employees`;
  constructor(private http: HttpClient) {}

  getByCompany(companyId: string) { return this.http.get<EmployeeDto[]>(this.baseUrl, { params: { companyId } }); }
  getById(id: string) { return this.http.get<EmployeeDto>(`${this.baseUrl}/${id}`); }
  getDirectReports(id: string) { return this.http.get<EmployeeDto[]>(`${this.baseUrl}/${id}/direct-reports`); }
  create(dto: CreateEmployeeDto) { return this.http.post<EmployeeDto>(this.baseUrl, dto); }
  update(id: string, dto: UpdateEmployeeDto) { return this.http.put<EmployeeDto>(`${this.baseUrl}/${id}`, dto); }
}