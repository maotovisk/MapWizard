using MapWizard.Desktop.Enums;

namespace MapWizard.Desktop.Models;

public class Result<T>
{
    public T? Value { get; set; }
    public ResultStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}