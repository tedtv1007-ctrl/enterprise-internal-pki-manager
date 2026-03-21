namespace EnterprisePKI.Shared.Models
{
    public record ApiError(string Error, string Message, object? Details = null);
}
