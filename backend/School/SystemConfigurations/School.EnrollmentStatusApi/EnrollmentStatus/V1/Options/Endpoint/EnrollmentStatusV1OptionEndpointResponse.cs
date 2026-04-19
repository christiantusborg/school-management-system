namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Options.Endpoint;

public sealed class EnrollmentStatusV1OptionEndpointResponse
{
    public required EnrollmentStatusV1OptionInnerEndpointResponse List { get; init; }
    public required EnrollmentStatusV1OptionInnerEndpointResponse Get { get; init; }
    public required EnrollmentStatusV1OptionInnerEndpointResponse Create { get; init; }
    public required EnrollmentStatusV1OptionInnerEndpointResponse Update { get; init; }
    public required EnrollmentStatusV1OptionInnerEndpointResponse SoftDelete { get; init; }
    public required EnrollmentStatusV1OptionInnerEndpointResponse Restore { get; init; }
    public required EnrollmentStatusV1OptionInnerEndpointResponse PermanentDelete { get; init; }
}
