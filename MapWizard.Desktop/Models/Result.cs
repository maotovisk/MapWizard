namespace MapWizard.Desktop.Models;

public class Result<T> where T : class?
{
    public T? Value { get; set; }
    public ResultStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}