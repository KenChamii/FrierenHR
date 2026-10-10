# 1. Company policy is configuration, not code

Date: 2026-10-10

Status: Accepted

## Context

HR rules differ per company and change without warning. Leave accrues at different rates, overtime pays at different multipliers, late arrival is forgiven for different numbers of minutes, and approvals escalate on different timetables.

The default approach is to encode each company's rules in services and branch on company where they differ. That works for one client and degrades quickly: every new company is a code change, a deployment, and a regression risk to every other company.

## Decision

Policy is stored per company as a rule definition and evaluated at runtime by `FrierenHR.Core.RulesEngine`.

A rule is a list of conditions and one action. Conditions use a closed operator set (`>=`, `<=`, `==`, `!=`, `in`) against named fields in a `RuleContext`. Actions come from a closed catalog (`AccrueDays`, `AutoApprove`, `RequireApproval`, `OTMultiplier`, `LateGraceMinutes`, `EscalateAfterDays`).

Rules are persisted as JSON in `CompanyRuleConfig.RuleJson`. Several rules of the same type may exist for one company; `EvaluateFirstMatch` tries them in ascending priority order and the first match wins.

## Consequences

Good:

- A new company's policy is data entry, not a release.
- The engine lives in `Core` with no dependencies, so policy logic is testable without a database or a host.
- Priority stacking gives exceptions for nothing extra. A probationary rule above the general rule needs no new concept.

Costs:

- Conditions are AND-only within a rule. Expressing OR means two stacked rules, which is a less obvious mental model than a boolean expression.
- `RuleCondition.Value` is `object?` so any JSON value fits, which pushes type coercion into `ConditionEvaluator` and leaves fact types implicit rather than declared.
- A rule naming a field the context does not hold returns a failure that `EvaluateFirstMatch` records and skips, so a mistyped field silently falls through to the next rule instead of raising. Generating the editing UI from a published schema and validating on save keeps that unreachable through the app, but the engine behaviour is worth revisiting.

## Alternatives considered

**A scripting language** such as Lua, or a C# expression evaluator. Far more expressive, but it makes policy unauditable, hands non-developers a programming language, and opens an obvious injection surface. The closed operator and action sets are the point, not a limitation.

**Columns on the company record**, one per policy knob. Simple until the second company wants a rule that depends on tenure, at which point the schema grows a column per combination.
