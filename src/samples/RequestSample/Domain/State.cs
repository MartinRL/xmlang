namespace RequestSample.Domain;

public enum RequestStatus
{
    Pending,
    AwaitingReview,
    Approved,
    Rejected
}

public record RequestState(
    Guid RequestId,
    Guid EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    RequestStatus Status,
    Guid? ManagerId = null,
    string? RejectionReason = null,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? ApprovedAt = null,
    DateTimeOffset? RejectedAt = null)
{
    public static RequestState Initial => new(
        Guid.Empty, Guid.Empty, default, default, string.Empty, RequestStatus.Pending);
}
