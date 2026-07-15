import { EmployeeRole } from './enums.model';

export interface EmployeeDto {
  id: string; companyId: string; departmentId?: string; departmentName?: string;
  managerId?: string; managerName?: string; firstName: string; lastName: string; email: string;
  hireDate: string; tenureMonths: number; role: EmployeeRole; isActive: boolean;
}
export interface CreateEmployeeDto {
  companyId: string; departmentId?: string; managerId?: string;
  firstName: string; lastName: string; email: string; password: string; hireDate: string; role: EmployeeRole;
}
export interface UpdateEmployeeDto {
  departmentId?: string; managerId?: string; firstName: string; lastName: string; role: EmployeeRole; isActive: boolean;
}