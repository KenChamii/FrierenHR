using FrierenHR.Core.Common;
using FrierenHR.Core.Enums;


namespace FrierenHR.Core.Entities;
public class LeaveBalance : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public LeaveType LeaveType { get; set; }
    public decimal Balance { get; set; }
    public DateTime? LastAccrualDate { get; set; }
}