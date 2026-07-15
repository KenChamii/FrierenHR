import { RuleType } from './enums.model';

export interface RuleConditionDto { field: string; op: string; value: any; }
export interface RuleActionDto { type: string; amount?: number; cap?: number; extraParams?: Record<string, any>; }
export interface RuleDefinitionDto { ruleType: string; conditions: RuleConditionDto[]; action: RuleActionDto; }

export interface RuleConfigDto {
  id: string; companyId: string; ruleType: RuleType; ruleJson: string; isActive: boolean; priority: number;
}
export interface SaveRuleConfigDto {
  id?: string; companyId: string; ruleType: RuleType; ruleJson: string; isActive: boolean; priority: number;
}
export interface TestEvaluateRequestDto { companyId: string; ruleType: RuleType; facts: Record<string, any>; }
export interface RuleEvaluationResultDto {
  matched: boolean; success: boolean; resultValue?: number; message: string; actionType?: string;
}