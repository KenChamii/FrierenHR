using FrierenHR.Core.Common;

namespace FrierenHR.Core.Entities;

public class AppConfig : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}