# 3. Rules are stored as JSON, and the UI is generated from a schema

Date: 2026-10-10

Status: Accepted

## Context

A rule has a variable number of conditions and an action whose parameters depend on its type. `AccrueDays` takes an amount and an optional cap; `AutoApprove` takes nothing. Modelling that relationally means a `RuleConditions` table, an `RuleActions` table, and a parameter table or a sparse set of nullable columns.

Separately, the first version of the rules screen gave administrators a textarea and asked them to type the JSON. The component also carried its own copy of the starter templates that `RuleConfigService` already held, kept in step by hand.

## Decision

The rule stays a single JSON document in `CompanyRuleConfig.RuleJson`.

The API publishes a schema at `GET /api/rules-config/schema` describing, per rule type, the fields the evaluator will look up (with labels, types, units and the operators each field allows) and the actions it can execute (with their parameters and labels). The schema is built from `RuleSchemaCatalog` in `Core`, beside the evaluator it describes.

The Angular rules screen builds its form from that schema. `SaveAsync` validates incoming rules against the same catalog.

## Consequences

Good:

- No join tables, no sparse columns, and adding an action parameter is not a migration.
- One source of truth. The catalog sits next to `ConditionEvaluator` and `ActionExecutor`, so the form, the server-side validation and the engine cannot disagree about what a valid rule is. The duplicated frontend templates were deleted as a result.
- The form can only offer what the engine implements, so most invalid rules are unreachable rather than merely rejected.

Costs:

- The database cannot query inside a rule. "Which companies accrue more than 1.5 days a month" needs application code or SQL Server's JSON functions, not a WHERE clause on a column.
- No referential integrity on the rule's contents. Nothing at the database level stops a malformed document, so validation has to happen on save and the engine still has to tolerate bad data.
- Adding a field or an action means updating the catalog as well as the evaluator. The catalog makes the duplication visible and testable, but it is still two places.

## Alternatives considered

**Normalised tables for conditions and actions.** Queryable and constraint-checked, at the cost of several joins to read one rule and a migration for every new action parameter. The queries nobody is asking for did not justify it.

**A hand-written form per rule type.** Five forms instead of one generated form. Rejected because it reintroduces the duplication the schema removes: every new field would mean editing the evaluator and a form.
