import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { DropdownModule } from 'primeng/dropdown';
import { MultiSelectModule } from 'primeng/multiselect';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { TextareaModule } from 'primeng/textarea';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { PanelModule } from 'primeng/panel';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { RulesConfigService } from '../../../core/services/rules-config.service';
import { AuthService } from '../../../core/services/auth.service';
import { RuleType } from '../../../core/models/enums.model';
import {
  OPERATOR_LABELS, RuleActionSchema, RuleConfigDto, RuleDefinitionDto, RuleEvaluationResultDto,
  RuleFieldSchema, RuleSchemaMap, RuleTypeSchema,
} from '../../../core/models/rules-config.model';

interface Option<T = string> { label: string; value: T; }

@Component({
  selector: 'app-rule-config',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule, DropdownModule, MultiSelectModule,
    InputNumberModule, InputTextModule, ToggleSwitchModule, TextareaModule, ButtonModule,
    MessageModule, PanelModule, TableModule, TagModule,
  ],
  templateUrl: './rule-config.component.html',
  styleUrl: './rule-config.component.scss',
})
export class RuleConfigComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly rulesConfigService = inject(RulesConfigService);
  private readonly authService = inject(AuthService);

  readonly schema = signal<RuleSchemaMap | null>(null);
  readonly configs = signal<RuleConfigDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly seeding = signal(false);
  readonly testing = signal(false);
  readonly saveError = signal<string | null>(null);
  readonly saveSuccess = signal(false);
  readonly schemaError = signal<string | null>(null);
  readonly testResult = signal<RuleEvaluationResultDto | null>(null);
  readonly testError = signal<string | null>(null);
  readonly editingId = signal<string | null>(null);
  readonly showJson = signal(false);

  // Mirrors the form so the preview and the dropdowns can be computed. The form itself is not
  // a signal, so every change pushes into this counter.
  private readonly revision = signal(0);
  private readonly ruleTypeSig = signal<RuleType>('LeaveAccrual');

  form = this.fb.group({
    ruleType: this.fb.nonNullable.control<RuleType>('LeaveAccrual', Validators.required),
    conditions: this.fb.array<FormGroup>([]),
    actionType: this.fb.nonNullable.control('', Validators.required),
    amount: this.fb.control<number | null>(null),
    cap: this.fb.control<number | null>(null),
    isActive: this.fb.nonNullable.control(true),
    priority: this.fb.nonNullable.control(0),
  });

  /** One control per schema field, filled in by the administrator to try a rule out. */
  testForm = this.fb.group({});

  get conditions(): FormArray<FormGroup> { return this.form.controls.conditions; }

  readonly ruleTypeOptions = computed<Option<RuleType>[]>(() => {
    const schema = this.schema();
    if (!schema) return [];
    return (Object.keys(schema) as RuleType[]).map(key => ({ label: schema[key].label, value: key }));
  });

  readonly currentSchema = computed<RuleTypeSchema | null>(() => this.schema()?.[this.ruleTypeSig()] ?? null);

  readonly fieldOptions = computed<Option[]>(() =>
    (this.currentSchema()?.fields ?? []).map(f => ({ label: f.label, value: f.name })));

  readonly actionOptions = computed<Option[]>(() =>
    (this.currentSchema()?.actions ?? []).map(a => ({ label: a.label, value: a.type })));

  readonly actionSchema = computed<RuleActionSchema | null>(() => {
    this.revision();
    const type = this.form.controls.actionType.value;
    return this.currentSchema()?.actions.find(a => a.type === type) ?? null;
  });

  /** Rules of the same type, in the order the engine will try them. */
  readonly stackedRules = computed<RuleConfigDto[]>(() => {
    const ruleType = this.ruleTypeSig();
    return this.configs()
      .filter(c => c.ruleType === ruleType)
      .sort((a, b) => a.priority - b.priority);
  });

  readonly previewSentence = computed(() => {
    this.revision();
    const schema = this.currentSchema();
    const action = this.actionSchema();
    if (!schema || !action) return '';

    const parts = this.conditions.controls
      .map(group => {
        const field = schema.fields.find(f => f.name === group.value.field);
        const value = group.value.value;
        if (!field || value === null || value === undefined || value === '') return null;
        const operator = OPERATOR_LABELS[group.value.op] ?? group.value.op;
        const rendered = Array.isArray(value) ? value.join(' or ') : value;
        const unit = field.unit && field.kind !== 'Enum' ? ` ${field.unit}` : '';
        return `${field.label.toLowerCase()} ${operator} ${rendered}${unit}`;
      })
      .filter((part): part is string => part !== null);

    const outcome = action.summary
      .replace('{amount}', this.format(this.form.controls.amount.value))
      .replace('{cap}', this.format(this.form.controls.cap.value));

    const capParam = action.parameters.find(p => p.name === 'cap');
    const capClause = capParam && this.form.controls.cap.value != null
      ? `, up to a ${capParam.label.toLowerCase()} of ${this.format(this.form.controls.cap.value)}${capParam.unit ? ' ' + capParam.unit : ''}`
      : '';

    const when = parts.length ? `When ${parts.join(' and ')}, ` : 'For every employee, ';
    return `${when}${outcome}${capClause}.`;
  });

  /** The rule as it will be stored. Shown read-only under Advanced. */
  readonly ruleJson = computed(() => {
    this.revision();
    return JSON.stringify(this.buildRule(), null, 2);
  });

  ngOnInit(): void {
    this.form.valueChanges.subscribe(() => this.revision.update(v => v + 1));
    this.form.controls.ruleType.valueChanges.subscribe(ruleType => {
      if (ruleType === this.ruleTypeSig()) return;
      this.ruleTypeSig.set(ruleType);
      this.resetFromStarter();
    });

    this.rulesConfigService.getSchema().subscribe({
      next: schema => {
        this.schema.set(schema);
        this.resetFromStarter();
        this.loadConfigs();
      },
      error: () => {
        this.schemaError.set('Could not load the rule schema, so the form cannot be built. Check that the API is running.');
        this.loading.set(false);
      },
    });
  }

  private loadConfigs(): void {
    this.loading.set(true);
    this.rulesConfigService.getMine().subscribe({
      next: list => { this.configs.set(list); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  // --- conditions ---

  addCondition(fieldName?: string): void {
    const schema = this.currentSchema();
    if (!schema) return;
    const field = schema.fields.find(f => f.name === fieldName) ?? schema.fields[0];
    this.conditions.push(this.fb.group({
      field: this.fb.nonNullable.control(field.name),
      op: this.fb.nonNullable.control(field.operators[0]),
      value: this.fb.control<any>(this.defaultValueFor(field)),
    }));
  }

  removeCondition(index: number): void { this.conditions.removeAt(index); }

  /** Narrow the operator and reset the value when the field changes under it. */
  onFieldChange(index: number): void {
    const group = this.conditions.at(index);
    const field = this.fieldFor(group.value.field);
    if (!field) return;
    if (!field.operators.includes(group.value.op)) group.patchValue({ op: field.operators[0] });
    group.patchValue({ value: this.defaultValueFor(field) });
  }

  /** 'in' takes a list, every other operator takes one value. */
  onOperatorChange(index: number): void {
    const group = this.conditions.at(index);
    const field = this.fieldFor(group.value.field);
    if (!field) return;
    group.patchValue({ value: this.defaultValueFor(field, group.value.op) });
  }

  fieldFor(name: string): RuleFieldSchema | undefined {
    return this.currentSchema()?.fields.find(f => f.name === name);
  }

  operatorOptionsFor(name: string): Option[] {
    return (this.fieldFor(name)?.operators ?? []).map(op => ({ label: OPERATOR_LABELS[op] ?? op, value: op }));
  }

  enumOptionsFor(name: string): Option[] {
    return (this.fieldFor(name)?.options ?? []).map(o => ({ label: o, value: o }));
  }

  // --- saving ---

  save(): void {
    const companyId = this.authService.currentCompanyId();
    if (!companyId || this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.saving.set(true);
    this.saveError.set(null);
    this.saveSuccess.set(false);

    this.rulesConfigService.save({
      id: this.editingId() ?? undefined,
      companyId,
      ruleType: this.form.controls.ruleType.value,
      ruleJson: JSON.stringify(this.buildRule()),
      isActive: this.form.controls.isActive.value,
      priority: this.form.controls.priority.value,
    }).subscribe({
      next: () => { this.saving.set(false); this.saveSuccess.set(true); this.loadConfigs(); },
      error: err => { this.saving.set(false); this.saveError.set(err?.error?.message ?? 'Save failed.'); },
    });
  }

  edit(config: RuleConfigDto): void {
    this.editingId.set(config.id);
    this.saveSuccess.set(false);
    this.saveError.set(null);

    this.form.controls.ruleType.setValue(config.ruleType, { emitEvent: false });
    this.ruleTypeSig.set(config.ruleType);

    let rule: RuleDefinitionDto | null = null;
    try { rule = JSON.parse(config.ruleJson); } catch { /* fall back to the starter below */ }
    this.applyRule(rule ?? this.currentSchema()?.starter ?? null);
    this.rebuildTestForm();
  }

  startBlank(): void {
    this.editingId.set(null);
    this.saveSuccess.set(false);
    this.saveError.set(null);
    const schema = this.currentSchema();
    if (!schema) return;
    this.conditions.clear();
    this.form.patchValue({
      actionType: schema.actions[0]?.type ?? '',
      amount: null,
      cap: null,
      isActive: true,
      priority: this.nextPriority(),
    });
  }

  loadStarter(): void {
    this.editingId.set(null);
    this.resetFromStarter();
  }

  seedDefaults(): void {
    this.seeding.set(true);
    this.rulesConfigService.seedDefaults().subscribe({
      next: list => { this.seeding.set(false); this.configs.set(list); },
      error: err => { this.seeding.set(false); this.saveError.set(err?.error?.message ?? 'Could not seed defaults.'); },
    });
  }

  // --- test panel ---

  runTest(): void {
    const companyId = this.authService.currentCompanyId();
    if (!companyId) return;

    this.testing.set(true);
    this.testError.set(null);
    this.testResult.set(null);

    // Only facts the administrator actually filled in are sent, so a blank input reproduces
    // the "field missing from context" path rather than being silently sent as zero.
    const facts: Record<string, any> = {};
    for (const [key, control] of Object.entries(this.testForm.controls)) {
      const value = (control as any).value;
      if (value !== null && value !== undefined && value !== '') facts[key] = value;
    }

    this.rulesConfigService.testEvaluate({ companyId, ruleType: this.form.controls.ruleType.value, facts }).subscribe({
      next: result => { this.testing.set(false); this.testResult.set(result); },
      error: err => { this.testing.set(false); this.testError.set(err?.error?.message ?? 'Test failed.'); },
    });
  }

  // --- internals ---

  private resetFromStarter(): void {
    const schema = this.currentSchema();
    if (!schema) return;
    this.applyRule(schema.starter);
    this.form.patchValue({ isActive: true, priority: this.nextPriority() });
    this.rebuildTestForm();
  }

  private applyRule(rule: RuleDefinitionDto | null): void {
    const schema = this.currentSchema();
    this.conditions.clear();
    if (!schema) return;

    for (const condition of rule?.conditions ?? []) {
      const field = schema.fields.find(f => f.name.toLowerCase() === condition.field?.toLowerCase());
      if (!field) continue; // a field the schema no longer knows is dropped rather than shown broken
      this.conditions.push(this.fb.group({
        field: this.fb.nonNullable.control(field.name),
        op: this.fb.nonNullable.control(field.operators.includes(condition.op) ? condition.op : field.operators[0]),
        value: this.fb.control<any>(condition.value ?? this.defaultValueFor(field, condition.op)),
      }));
    }

    const action = schema.actions.find(a => a.type === rule?.action?.type) ?? schema.actions[0];
    this.form.patchValue({
      actionType: action?.type ?? '',
      amount: rule?.action?.amount ?? null,
      cap: rule?.action?.cap ?? null,
    });
  }

  private buildRule(): RuleDefinitionDto {
    const action = this.actionSchema();
    const rule: RuleDefinitionDto = {
      ruleType: this.form.controls.ruleType.value,
      conditions: this.conditions.controls.map(group => ({
        field: group.value.field,
        op: group.value.op,
        value: group.value.value,
      })),
      action: { type: this.form.controls.actionType.value },
    };

    // Only send the parameters this action declares, so a leftover cap from a previous action
    // does not travel with a rule that has no cap.
    if (action?.parameters.some(p => p.name === 'amount') && this.form.controls.amount.value != null)
      rule.action.amount = this.form.controls.amount.value;
    if (action?.parameters.some(p => p.name === 'cap') && this.form.controls.cap.value != null)
      rule.action.cap = this.form.controls.cap.value;

    return rule;
  }

  private rebuildTestForm(): void {
    const schema = this.currentSchema();
    this.testForm = this.fb.group({});
    if (!schema) return;
    for (const field of schema.fields)
      this.testForm.addControl(field.name, this.fb.control<any>(this.sampleFor(field)));
  }

  /** A plausible starting value so the test panel is useful the moment it opens. */
  private sampleFor(field: RuleFieldSchema): any {
    if (field.kind === 'Enum') return field.options?.[0] ?? null;
    if (field.kind === 'Text') return '';
    return field.name === 'clockInHour' ? 9 : 12;
  }

  private defaultValueFor(field: RuleFieldSchema, op?: string): any {
    if (op === 'in') return [];
    if (field.kind === 'Enum') return field.options?.[0] ?? null;
    if (field.kind === 'Text') return '';
    return 0;
  }

  private nextPriority(): number {
    const stacked = this.stackedRules();
    return stacked.length ? Math.max(...stacked.map(r => r.priority)) + 1 : 0;
  }

  private format(value: number | null | undefined): string {
    return value === null || value === undefined ? '…' : String(value);
  }

  // --- helpers used by the template ---

  /** Display name for a rule type. Typed here because the table rows arrive untyped. */
  labelFor(ruleType: string): string {
    return this.schema()?.[ruleType as RuleType]?.label ?? ruleType;
  }

  summarise(config: RuleConfigDto): string {
    try {
      const rule: RuleDefinitionDto = JSON.parse(config.ruleJson);
      const schema = this.schema()?.[config.ruleType];
      const action = schema?.actions.find(a => a.type === rule.action?.type);
      const conditionText = (rule.conditions ?? [])
        .map(c => `${c.field} ${c.op} ${Array.isArray(c.value) ? c.value.join('/') : c.value}`)
        .join(' and ');
      const outcome = action?.label ?? rule.action?.type ?? 'no action';
      return conditionText ? `${conditionText} → ${outcome}` : outcome;
    } catch {
      return 'Unreadable rule';
    }
  }

  toggleJson(): void { this.showJson.update(v => !v); }
}
