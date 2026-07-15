import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { CompanyDto, CreateCompanyDto, DepartmentDto, CreateDepartmentDto } from '../models/company.model';

@Injectable({ providedIn: 'root' })
export class CompanyService {
  private readonly baseUrl = `${environment.apiUrl}/api/companies`;
  constructor(private http: HttpClient) {}

  getAll() { return this.http.get<CompanyDto[]>(this.baseUrl); }
  getById(id: string) { return this.http.get<CompanyDto>(`${this.baseUrl}/${id}`); }
  create(dto: CreateCompanyDto) { return this.http.post<CompanyDto>(this.baseUrl, dto); }
  getDepartments(companyId: string) { return this.http.get<DepartmentDto[]>(`${this.baseUrl}/${companyId}/departments`); }
  createDepartment(companyId: string, dto: CreateDepartmentDto) {
    return this.http.post<DepartmentDto>(`${this.baseUrl}/${companyId}/departments`, dto);
  }
}