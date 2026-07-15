import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApprovalChainDto, CreateApprovalChainDto, ApprovalInstanceDto } from '../models/approval.model';

@Injectable({ providedIn: 'root' })
export class ApprovalService {
  private readonly baseUrl = `${environment.apiUrl}/api/approval-chains`;
  constructor(private http: HttpClient) {}

  create(dto: CreateApprovalChainDto) { return this.http.post<ApprovalChainDto>(this.baseUrl, dto); }
  getForCompany(companyId: string) { return this.http.get<ApprovalChainDto | null>(`${this.baseUrl}/by-company/${companyId}`); }
  advance(instanceId: string, approve: boolean) {
    return this.http.post<ApprovalInstanceDto>(`${this.baseUrl}/instances/${instanceId}/advance`, {}, { params: { approve: String(approve) } });
  }
}