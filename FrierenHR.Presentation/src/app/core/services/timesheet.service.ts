import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TimesheetDto, SubmitTimesheetDto, DecideTimesheetDto } from '../models/timesheet.model';

@Injectable({ providedIn: 'root' })
export class TimesheetService {
  private readonly baseUrl = `${environment.apiUrl}/api/timesheets`;
  constructor(private http: HttpClient) {}

  submit(dto: SubmitTimesheetDto) { return this.http.post<TimesheetDto>(`${this.baseUrl}/submit`, dto); }
  decide(id: string, dto: DecideTimesheetDto) { return this.http.post<TimesheetDto>(`${this.baseUrl}/${id}/decide`, dto); }
  getByEmployee(employeeId: string) { return this.http.get<TimesheetDto[]>(`${this.baseUrl}/by-employee/${employeeId}`); }
  getForWeek(employeeId: string, weekStartDate: string) {
    return this.http.get<TimesheetDto | null>(`${this.baseUrl}/by-employee/${employeeId}/week`, { params: { weekStartDate } });
  }
  getPendingForApprover(approverId: string) {
    return this.http.get<TimesheetDto[]>(`${this.baseUrl}/pending-for-approver/${approverId}`);
  }
}
