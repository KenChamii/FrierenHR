export interface CompanyDto { id: string; name: string; code: string; isActive: boolean; employeeCount: number; }
export interface CreateCompanyDto { name: string; code: string; }
export interface DepartmentDto { id: string; companyId: string; name: string; employeeCount: number; }
export interface CreateDepartmentDto { name: string; }