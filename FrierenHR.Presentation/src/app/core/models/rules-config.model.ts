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

// --- Schema served by GET /api/rules-config/schema --- Mirrors FrierenHR.Core.RulesEngine.Schema.

export type RuleValueKind = 'Integer' | 'Decimal' | 'Text' | 'Enum' | 'Boolean';

/** A Boolean field is a two-option dropdown rather than a checkbox, so it reads as a value. */
export const BOOLEAN_OPTIONS = [
  { label: 'Yes', value: true },
  { label: 'No', value: false },
];

export interface RuleFieldSchema {
  name: string;
  label: string;
  kind: RuleValueKind;
  operators: string[];
  unit?: string | null;
  options?: string[] | null;
}

export interface RuleActionParamSchema {
  name: 'amount' | 'cap' | string;
  label: string;
  kind: RuleValueKind;
  required: boolean;
  unit?: string | null;
}

export interface RuleActionSchema {
  type: string;
  label: string;
  /** Sentence fragment with {amount} and {cap} placeholders, used for the preview line. */
  summary: string;
  parameters: RuleActionParamSchema[];
}

export interface RuleTypeSchema {
  ruleType: string;
  label: string;
  description: string;
  fields: RuleFieldSchema[];
  actions: RuleActionSchema[];
  /** False when the engine supports the type but nothing calls it yet. */
  wired: boolean;
  starter: RuleDefinitionDto;
}

export type RuleSchemaMap = Record<RuleType, RuleTypeSchema>;

/** Reads as the operator does, for the preview line and the operator dropdown. */
export const OPERATOR_LABELS: Record<string, string> = {
  '>=': 'is at least',
  '<=': 'is at most',
  '==': 'is',
  '!=': 'is not',
  'in': 'is any of',
};
