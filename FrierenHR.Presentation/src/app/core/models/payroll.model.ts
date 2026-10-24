/** Mirrors FrierenHR.Application.Common.DTOs.PayrollDtos. */

export type DeductionKind = 'FixedAmount' | 'PercentOfGross';
export type PayslipLineKind = 'Earning' | 'Deduction';

// Mutable, like HOLIDAY_TYPES and EMPLOYEE_ROLES: PrimeNG's options input does not accept a readonly array.
export const DEDUCTION_KINDS: { label: string; value: DeductionKind }[] = [
  { label: 'Fixed amount', value: 'FixedAmount' },
  { label: 'Percent of gross', value: 'PercentOfGross' },
];

export interface PayrollDeductionDto {
  id: string;
  companyId: string;
  name: string;
  kind: DeductionKind;
  value: number;
  sortOrder: number;
  isActive: boolean;
}

export interface SavePayrollDeductionDto {
  id?: string;
  companyId: string;
  name: string;
  kind: DeductionKind;
  value: number;
  sortOrder: number;
  isActive: boolean;
}

export interface PayslipLineDto {
  label: string;
  kind: PayslipLineKind;
  amount: number;
  basis: string | null;
}

export interface PayslipSummaryDto {
  id: string;
  employeeId: string;
  employeeName: string;
  periodStart: string;
  periodEnd: string;
  grossPay: number;
  netPay: number;
  monthlyRate: number;
  generatedAt: string;
}

export interface PayslipDto {
  id: string;
  employeeId: string;
  employeeName: string;
  departmentName: string | null;
  periodStart: string;
  periodEnd: string;
  monthlyRate: number;
  hourlyRate: number;
  regularHours: number;
  payableOvertimeHours: number;
  grossPay: number;
  totalDeductions: number;
  netPay: number;
  generatedAt: string;
  lines: PayslipLineDto[];
}

export interface PayslipRunDto {
  periodStart: string;
  periodEnd: string;
  generated: number;
  totalNetPay: number;
  payslips: PayslipSummaryDto[];
  /** Employees left out and why. Shown rather than swallowed. */
  skipped: string[];
}
