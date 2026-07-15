import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { DropdownModule } from 'primeng/dropdown';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { TextareaModule } from 'primeng/textarea';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { PanelModule } from 'primeng/panel';
import { RulesConfigService } from '../../../core/services/rules-config.service';
import { RULE_TYPES } from '../../../core/models/enums.model';
import { RuleEvaluationResultDto } from '../../../core/models/rules-config.model';

const SAMPLE_RULE_JSON = JSON.stringify(
  { ruleType: 'LeaveAccrual', conditions: [{ field: 'tenureMonths', op: '>=', value: 12 }], action: { type: 'AccrueDays', amount: 1.25, cap: 15 } },
  null, 2,
);

@Component({
  selector: 'app-rule-config',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, DropdownModule, InputNumberModule, ToggleSwitchModule, TextareaModule, ButtonModule, MessageModule, PanelModule],
  templateUrl: './rule-config.component.html',
  styleUrl: './rule-config.component.scss',
})
export class RuleConfigComponent {
  private readonly fb = inject(FormBuilder);
  private readonly rulesConfigService = inject(RulesConfigService);

  readonly ruleTypes = RULE_TYPES;
  readonly saving = signal(false);
  readonly testing = signal(false);
  readonly saveError = signal<string | null>(null);
  readonly saveSuccess = signal(false);
  readonly testResult = signal<RuleEvaluationResultDto | null>(null);
  readonly testError = signal<string | null>(null);

  form = this.fb.group({
    companyId: ['', Validators.required],
    ruleType: ['LeaveAccrual', Validators.required],
    ruleJson: [SAMPLE_RULE_JSON, Validators.required],
    isActive: [true],
    priority: [0],
  });

  testFactsJson = signal('{ "tenureMonths": 14, "currentBalance": 3 }');

  save(): void {
    if (this.form.invalid) return;
    // Validate JSON client-side before hitting the API — same discipline as RuleConfigService.SaveAsync server-side.
    try { JSON.parse(this.form.value.ruleJson!); }
    catch { this.saveError.set('Rule JSON is not valid JSON.'); return; }

    this.saving.set(true);
    this.saveError.set(null);
    this.saveSuccess.set(false);
    this.rulesConfigService.save(this.form.getRawValue() as any).subscribe({
      next: () => { this.saving.set(false); this.saveSuccess.set(true); },
      error: (err) => { this.saving.set(false); this.saveError.set(err?.error?.message ?? 'Save failed.'); },
    });
  }

  runTest(): void {
    let facts: Record<string, any>;
    try { facts = JSON.parse(this.testFactsJson()); }
    catch { this.testError.set('Facts JSON is not valid JSON.'); return; }

    const companyId = this.form.value.companyId;
    const ruleType = this.form.value.ruleType;
    if (!companyId || !ruleType) { this.testError.set('Set Company and Rule Type first.'); return; }

    this.testing.set(true);
    this.testError.set(null);
    this.testResult.set(null);
    this.rulesConfigService.testEvaluate({ companyId, ruleType: ruleType as any, facts }).subscribe({
      next: (result) => { this.testing.set(false); this.testResult.set(result); },
      error: (err) => { this.testing.set(false); this.testError.set(err?.error?.message ?? 'Test failed.'); },
    });
  }
}