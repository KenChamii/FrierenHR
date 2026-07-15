import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AttendanceLogDto, ClockInDto, ClockOutDto, OtComputationResultDto } from '../models/attendance.model';

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly baseUrl = `${environment.apiUrl}/api/attendance`;
  constructor(private http: HttpClient) {}

  clockIn(dto: ClockInDto) { return this.http.post<AttendanceLogDto>(`${this.baseUrl}/clock-in`, dto); }
  clockOut(dto: ClockOutDto) { return this.http.post<AttendanceLogDto>(`${this.baseUrl}/clock-out`, dto); }
  getByEmployee(employeeId: string, from?: string, to?: string) {
    const params: Record<string, string> = {};
    if (from) params['from'] = from;
    if (to) params['to'] = to;
    return this.http.get<AttendanceLogDto[]>(`${this.baseUrl}/${employeeId}`, { params });
  }
  computeOt(employeeId: string, date: string) {
    return this.http.get<OtComputationResultDto>(`${this.baseUrl}/${employeeId}/ot`, { params: { date } });
  }
}