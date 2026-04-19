namespace QuVian.SharedLibrary.Permissions;

public interface IPermissionFailed<T>
{
    string? Command { get; set; }

    T? CommandValues { get; set; }

    int? HttpStatusCode { get; set; }

    string? Message { get; set; }

    string? Origin { get; set; }
}
