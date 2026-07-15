export interface LoginDto { email: string; password: string; }
export interface LoginResultDto { token: string; employeeId: string; fullName: string; role: string; }