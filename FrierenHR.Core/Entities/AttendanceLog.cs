using FrierenHR.Core.Common;

namespace FrierenHR.Core.Entities;

public class AttendanceLog : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime TimeIn { get; set; }
    public DateTime? TimeOut { get; set; }
    public int BreakMinutes { get; set; } = 0;
    public string Source { get; set; } = "Web"; // Web, Mobile, Biometric
}