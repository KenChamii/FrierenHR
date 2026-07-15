import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { SaveRuleConfigDto, RuleConfigDto, TestEvaluateRequestDto, RuleEvaluationResultDto } from '../models/rules-config.model';

@Injectable({ providedIn: 'root' })
export class RulesConfigService {
  private readonly baseUrl = `${environment.apiUrl}/api/rules-config`;
  constructor(private http: HttpClient) {}

  save(dto: SaveRuleConfigDto) { return this.http.post<RuleConfigDto>(this.baseUrl, dto); }
  testEvaluate(dto: TestEvaluateRequestDto) { return this.http.post<RuleEvaluationResultDto>(`${this.baseUrl}/test-evaluate`, dto); }
}