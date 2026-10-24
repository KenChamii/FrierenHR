export interface LoginDto { email: string; password: string; }
export interface LoginResultDto {
  token: string;
  employeeId: string;
  fullName: string;
  role: string;
  /** True while the account is still on a password somebody else chose. */
  mustChangePassword?: boolean;
}

/** Company and first administrator, submitted together. */
export interface RegisterCompanyDto {
  companyName: string;
  companyCode: string;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}
