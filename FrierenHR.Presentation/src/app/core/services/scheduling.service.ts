import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AttendanceCorrectionDto, AuditPageDto, CreateAttendanceCorrectionDto,
  DecideAttendanceCorrectionDto, HolidayDto, HolidayImportResultDto, ResolvedShiftDto,
  SaveHolidayDto, SaveShiftDto, ShiftDto, WorkingDaysDto,
} from '../models/scheduling.model';

@Injectable({ providedIn: 'root' })
export class HolidayService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/holidays`;

  getForYear(year: number) {
    return this.http.get<HolidayDto[]>(this.baseUrl, { params: new HttpParams().set('year', year) });
  }

  save(dto: SaveHolidayDto) { return this.http.post<HolidayDto>(this.baseUrl, dto); }

  import(holidays: SaveHolidayDto[]) {
    return this.http.post<HolidayImportResultDto>(`${this.baseUrl}/import`, holidays);
  }

  delete(id: string) { return this.http.delete<void>(`${this.baseUrl}/${id}`); }

  /** How many days a leave request would actually deduct, and which days it skips. */
  countWorkingDays(from: string, to: string, employeeId?: string) {
    let params = new HttpParams().set('from', from).set('to', to);
    if (employeeId) params = params.set('employeeId', employeeId);
    return this.http.get<WorkingDaysDto>(`${this.baseUrl}/working-days`, { params });
  }
}

@Injectable({ providedIn: 'root' })
export class ShiftService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/shifts`;

  getMine() { return this.http.get<ShiftDto[]>(this.baseUrl); }
  save(dto: SaveShiftDto) { return this.http.post<ShiftDto>(this.baseUrl, dto); }
  delete(id: string) { return this.http.delete<void>(`${this.baseUrl}/${id}`); }

  forEmployee(employeeId: string) {
    return this.http.get<ResolvedShiftDto>(`${this.baseUrl}/for-employee/${employeeId}`);
  }
}

@Injectable({ providedIn: 'root' })
export class AttendanceCorrectionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/attendance-corrections`;

  request(dto: CreateAttendanceCorrectionDto) {
    return this.http.post<AttendanceCorrectionDto>(this.baseUrl, dto);
  }

  mine() { return this.http.get<AttendanceCorrectionDto[]>(`${this.baseUrl}/mine`); }

  pending() { return this.http.get<AttendanceCorrectionDto[]>(`${this.baseUrl}/pending`); }

  decide(id: string, dto: DecideAttendanceCorrectionDto) {
    return this.http.post<AttendanceCorrectionDto>(`${this.baseUrl}/${id}/decide`, dto);
  }
}

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/audit`;

  getPage(options: { entityName?: string; entityId?: string; skip?: number; take?: number } = {}) {
    let params = new HttpParams()
      .set('skip', options.skip ?? 0)
      .set('take', options.take ?? 50);
    if (options.entityName) params = params.set('entityName', options.entityName);
    if (options.entityId) params = params.set('entityId', options.entityId);
    return this.http.get<AuditPageDto>(this.baseUrl, { params });
  }
}
