import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { LeaveRequestDto, CreateLeaveRequestDto, DecideLeaveRequestDto, LeaveBalanceDto } from '../models/leave.model';

@Injectable({ providedIn: 'root' })
export class LeaveService {
  private readonly baseUrl = `${environment.apiUrl}/api/leave`;
  constructor(private http: HttpClient) {}

  requestLeave(dto: CreateLeaveRequestDto) { return this.http.post<LeaveRequestDto>(`${this.baseUrl}/requests`, dto); }
  decide(id: string, dto: DecideLeaveRequestDto) { return this.http.post<LeaveRequestDto>(`${this.baseUrl}/requests/${id}/decide`, dto); }
  getByEmployee(employeeId: string) { return this.http.get<LeaveRequestDto[]>(`${this.baseUrl}/requests/by-employee/${employeeId}`); }
  getPendingForApprover(approverId: string) { return this.http.get<LeaveRequestDto[]>(`${this.baseUrl}/requests/pending-for-approver/${approverId}`); }
  getBalances(employeeId: string) { return this.http.get<LeaveBalanceDto[]>(`${this.baseUrl}/balances/${employeeId}`); }
  runAccrual() { return this.http.post<{ message: string }>(`${this.baseUrl}/run-accrual`, {}); }
}