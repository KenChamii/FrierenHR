import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { DropdownModule } from 'primeng/dropdown';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { TextareaModule } from 'primeng/textarea';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { PanelModule } from 'primeng/panel';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { RulesConfigService } from '../../../core/services/rules-config.service';
import { AuthService } from '../../../core/services/auth.service';
import { RULE_TYPES, RuleType } from '../../../core/models/enums.model';
import { RuleConfigDto, RuleEvaluationResultDto } from '../../../core/models/rules-config.model';

// Starter templates for each rule type — mirrors RuleConfigService.DefaultTemplates on the
// backend (kept in sync manually; these are just example payloads, not business logic, so
// duplicating them here is low-risk). "Load Default" fills the editor without saving, so
// someone can start from a working example and tweak it before committing. "Seed Default
// Rules" (below) instead writes ALL of these straight into the database as active configs,
// for a company that just wants something usable to test against immediately.
const DEFAULT_TEMPLATES: Record<RuleType, string> = {
  LeaveAccrual: JSON.stringify(
    { ruleType: 'LeaveAccrual', conditions: [{ field: 'tenureMonths', op: '>=', value: 0 }], action: { type: 'AccrueDays', amount: 1.25, cap: 15 } },
    null, 2,
  ),
  LeaveApproval: JSON.stringify(
    { ruleType: 'LeaveApproval', conditions: [{ field: 'days', op: '<=', value: 2 }], action: { type: 'AutoApprove' } },
    null, 2,
  ),
  OTMultiplier: JSON.stringify(
    { ruleType: 'OTMultiplier', conditions: [{ field: 'otHours', op: '>=', value: 0 }], action: { type: 'OTMultiplier', amount: 1.5 } },
    null, 2,
  ),
  LateGracePeriod: JSON.stringify(
    { ruleType: 'LateGracePeriod', conditions: [{ field: 'clockInHour', op: '>=', value: 9 }], action: { type: 'LateGraceMinutes', amount: 15 } },
    null, 2,
  ),
  ApprovalEscalation: JSON.stringify(
    { ruleType: 'ApprovalEscalation', conditions: [{ field: 'daysWaiting', op: '>=', value: 3 }], action: { type: 'EscalateAfterDays', amount: 3 } },
    null, 2,
  ),
};

@Component({
  selector: 'app-rule-config',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule, DropdownModule, InputNumberModule,
    ToggleSwitchModule, TextareaModule, ButtonModule, MessageModule, PanelModule, TableModule, TagModule,
  ],
  templateUrl: './rule-config.component.html',
  styleUrl: './rule-config.component.scss',
})
export class RuleConfigComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly rulesConfigService = inject(RulesConfigService);
  private readonly authService = inject(AuthService);

  readonly ruleTypes = RULE_TYPES;
  readonly configs = signal<RuleConfigDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly seeding = signal(false);
  readonly testing = signal(false);
  readonly saveError = signal<string | null>(null);
  readonly saveSuccess = signal(false);
  readonly testResult = signal<RuleEvaluationResultDto | null>(null);
  readonly testError = signal<string | null>(null);
  readonly editingId = signal<string | null>(null);

  form = this.fb.group({
    ruleType: ['LeaveAccrual' as RuleType, Validators.required],
    ruleJson: [DEFAULT_TEMPLATES['LeaveAccrual'], Validators.required],
    isActive: [true],
    priority: [0],
  });

  testFactsJson = signal('{ "tenureMonths": 14, "currentBalance": 3 }');

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.rulesConfigService.getMine().subscribe({
      next: (list) => { this.configs.set(list); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  // Option A: seed working defaults for every rule type at once, so there's immediately
  // something real configured to test and refine — instead of starting from nothing.
  seedDefaults(): void {
    this.seeding.set(true);
    this.rulesConfigService.seedDefaults().subscribe({
      next: (list) => { this.seeding.set(false); this.configs.set(list); },
      error: (err) => { this.seeding.set(false); this.saveError.set(err?.error?.message ?? 'Could not seed defaults.'); },
    });
  }

  // Option B: load just the selected type's starter template into the editor, unsaved —
  // for building a custom config that starts from a known-good example.
  loadDefaultTemplate(): void {
    const ruleType = this.form.value.ruleType as RuleType;
    this.form.patchValue({ ruleJson: DEFAULT_TEMPLATES[ruleType] });
  }

  // Option C: blank slate — write a rule with no starting point at all.
  startBlank(): void {
    this.editingId.set(null);
    this.form.reset({ ruleType: this.form.value.ruleType ?? 'LeaveAccrual', ruleJson: '{\n  \n}', isActive: true, priority: 0 });
  }

  edit(config: RuleConfigDto): void {
    this.editingId.set(config.id);
    this.form.setValue({
      ruleType: config.ruleType, ruleJson: JSON.stringify(JSON.parse(config.ruleJson), null, 2),
      isActive: config.isActive, priority: config.priority,
    });
    this.saveSuccess.set(false);
    this.saveError.set(null);
  }

  save(): void {
    if (this.form.invalid) return;
    // Validate JSON client-side before hitting the API — same discipline as RuleConfigService.SaveAsync server-side.
    try { JSON.parse(this.form.value.ruleJson!); }
    catch { this.saveError.set('Rule JSON is not valid JSON.'); return; }

    const companyId = this.authService.currentCompanyId();
    if (!companyId) return;

    this.saving.set(true);
    this.saveError.set(null);
    this.saveSuccess.set(false);
    const raw = this.form.getRawValue();
    this.rulesConfigService.save({
      id: this.editingId() ?? undefined, companyId,
      ruleType: raw.ruleType as RuleType, ruleJson: raw.ruleJson!, isActive: raw.isActive!, priority: raw.priority!,
    }).subscribe({
      next: () => { this.saving.set(false); this.saveSuccess.set(true); this.load(); },
      error: (err) => { this.saving.set(false); this.saveError.set(err?.error?.message ?? 'Save failed.'); },
    });
  }

  runTest(): void {
    let facts: Record<string, any>;
    try { facts = JSON.parse(this.testFactsJson()); }
    catch { this.testError.set('Facts JSON is not valid JSON.'); return; }

    const companyId = this.authService.currentCompanyId();
    const ruleType = this.form.value.ruleType;
    if (!companyId || !ruleType) { this.testError.set('Pick a Rule Type first.'); return; }

    this.testing.set(true);
    this.testError.set(null);
    this.testResult.set(null);
    this.rulesConfigService.testEvaluate({ companyId, ruleType: ruleType as RuleType, facts }).subscribe({
      next: (result) => { this.testing.set(false); this.testResult.set(result); },
      error: (err) => { this.testing.set(false); this.testError.set(err?.error?.message ?? 'Test failed.'); },
    });
  }
}
