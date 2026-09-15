namespace RequestSample.Domain;

public abstract record RequestCommand;
public sealed record SubmitRequest(Guid EmployeeId, DateOnly StartDate, DateOnly EndDate, string Reason) : RequestCommand;
public sealed record AssignReviewer(Guid RequestId, Guid ManagerId) : RequestCommand;
public sealed record ApproveRequest(Guid RequestId) : RequestCommand;
public sealed record RejectRequest(Guid RequestId, string Reason) : RequestCommand;

public abstract record RequestEvent;
public sealed record Created(Guid RequestId, Guid EmployeeId, DateOnly StartDate, DateOnly EndDate, string Reason, DateTimeOffset CreatedAt) : RequestEvent;
public sealed record ReviewerAssigned(Guid RequestId, Guid ManagerId, DateTimeOffset AssignedAt) : RequestEvent;
public sealed record Approved(Guid RequestId, DateTimeOffset ApprovedAt) : RequestEvent;
public sealed record Rejected(Guid RequestId, string Reason, DateTimeOffset RejectedAt) : RequestEvent;

// Railway-oriented programming: every command can take multiple error paths
public abstract record RequestError;
public sealed record StartDateAfterEndDate : RequestError;
public sealed record RequestNotFound : RequestError;
public sealed record NotInAwaitingReviewState : RequestError;
public sealed record RequestAlreadyDecided : RequestError;
public sealed record ManagerNotAssigned : RequestError;

public static class Decider
{
    public static RequestState Evolve(RequestState state, RequestEvent @event) =>
        @event switch
        {
            Created e => state with
            {
                RequestId = e.RequestId,
                EmployeeId = e.EmployeeId,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Reason = e.Reason,
                Status = RequestStatus.Pending,
                CreatedAt = e.CreatedAt
            },
            ReviewerAssigned e => state with
            {
                ManagerId = e.ManagerId,
                Status = RequestStatus.AwaitingReview
            },
            Approved e => state with
            {
                Status = RequestStatus.Approved,
                ApprovedAt = e.ApprovedAt
            },
            Rejected e => state with
            {
                Status = RequestStatus.Rejected,
                RejectionReason = e.Reason,
                RejectedAt = e.RejectedAt
            },
            _ => state
        };

    public static OneOf<RequestEvent[], RequestError> Decide(
        RequestState state,
        RequestCommand command,
        DateTimeOffset now) =>
        command switch
        {
            // Happy path: create request
            // Error path: start ≥ end (date range)
            SubmitRequest c => c.StartDate >= c.EndDate
                ? new StartDateAfterEndDate()
                : new RequestEvent[]
                {
                    new Created(Guid.NewGuid(), c.EmployeeId, c.StartDate, c.EndDate, c.Reason, now)
                },

            // Happy path: assign reviewer
            // Error paths: request doesn't exist, or wrong state
            AssignReviewer c => state.RequestId == Guid.Empty
                ? new RequestNotFound()
                : state.Status != RequestStatus.Pending
                    ? new RequestAlreadyDecided()
                    : new RequestEvent[]
                    {
                        new ReviewerAssigned(c.RequestId, c.ManagerId, now)
                    },

            // Happy path: approve request
            // Error paths: not found, not in review state, no manager assigned
            ApproveRequest c => state.RequestId == Guid.Empty
                ? new RequestNotFound()
                : state.Status != RequestStatus.AwaitingReview
                    ? new NotInAwaitingReviewState()
                    : state.ManagerId == Guid.Empty
                        ? new ManagerNotAssigned()
                        : new RequestEvent[]
                        {
                            new Approved(c.RequestId, now)
                        },

            // Happy path: reject request
            // Error paths: not found, not in review state, no manager assigned
            RejectRequest c => state.RequestId == Guid.Empty
                ? new RequestNotFound()
                : state.Status != RequestStatus.AwaitingReview
                    ? new NotInAwaitingReviewState()
                    : state.ManagerId == Guid.Empty
                        ? new ManagerNotAssigned()
                        : new RequestEvent[]
                        {
                            new Rejected(c.RequestId, c.Reason, now)
                        },

            _ => new RequestNotFound()
        };
}

public class OneOf<T1, T2>
{
    public required object Value { get; init; }
    public bool IsSuccess => Value is T1;
    public bool IsError => Value is T2;
    public T1? AsSuccess => Value is T1 ? (T1)Value : default;
    public T2? AsError => Value is T2 ? (T2)Value : default;

    public static implicit operator OneOf<T1, T2>(T1 value) => new() { Value = value! };
    public static implicit operator OneOf<T1, T2>(T2 error) => new() { Value = error! };
}

public static class ResultExt
{
    public static OneOf<T1, T2> Match<T1, T2>(
        this OneOf<T1, T2> result,
        Action<T1> onSuccess,
        Action<T2> onError)
    {
        if (result.IsSuccess)
            onSuccess(result.AsSuccess!);
        else
            onError(result.AsError!);
        return result;
    }
}
