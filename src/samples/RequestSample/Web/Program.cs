using RequestSample.Domain;
using Xmlang;

var builder = WebApplication.CreateBuilder(args);

// Load and lint specs at startup
var specsPath = Path.Combine(AppContext.BaseDirectory, "Specs");
var emSpec = File.ReadAllText(Path.Combine(specsPath, "time-off.em.yaml"));
var xmSpec = File.ReadAllText(Path.Combine(specsPath, "time-off.xm.yaml"));

// Parse xm spec (linting happens in the parser)
var xmModel = XmParser.Parse(xmSpec);
Console.WriteLine($"✓ Loaded experience model with {xmModel.Surfaces.Count} surfaces");

// In-memory request store
var requests = new Dictionary<Guid, List<RequestEvent>>();

var app = builder.Build();

app.MapPost("/requests", (SubmitRequest cmd) =>
{
    var result = Decider.Decide(RequestState.Initial, cmd, DateTimeOffset.UtcNow);
    if (!result.IsSuccess)
        return Results.BadRequest(result.AsError);

    var events = result.AsSuccess!;
    var requestId = (events[0] as Created)?.RequestId ?? Guid.NewGuid();
    requests[requestId] = [..events];

    return Results.Created($"/requests/{requestId}", new { requestId });
});

app.MapGet("/requests/{id}", (Guid id) =>
{
    if (!requests.TryGetValue(id, out var events))
        return Results.NotFound();

    var state = RequestState.Initial with { RequestId = id };
    foreach (var evt in events)
        state = Decider.Evolve(state, (RequestEvent)evt);

    return Results.Ok(state);
});

app.MapPost("/requests/{id}/approve", (Guid id, Guid managerId) =>
{
    if (!requests.TryGetValue(id, out var events))
        return Results.NotFound();

    var state = RequestState.Initial;
    foreach (var evt in events)
        state = Decider.Evolve(state, (RequestEvent)evt);

    var result = Decider.Decide(state, new ApproveRequest(id), DateTimeOffset.UtcNow);
    if (!result.IsSuccess)
        return Results.BadRequest(result.AsError);

    events.AddRange(result.AsSuccess!);
    return Results.Ok("approved");
});

app.Run();
