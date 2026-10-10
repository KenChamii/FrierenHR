export interface LoginDto { email: string; password: string; }
export interface LoginResultDto { token: string; employeeId: string; fullName: string; role: string; }

/**
 * Company and first administrator, submitted together. The API creates both in one
 * transaction, because a company without an administrator cannot be logged into.
 */
export interface RegisterCompanyDto {
  companyName: string;
  companyCode: string;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}
