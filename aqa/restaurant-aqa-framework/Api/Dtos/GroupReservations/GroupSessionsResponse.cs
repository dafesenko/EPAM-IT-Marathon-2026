namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

/// <summary>GET /guest/groups/{groupReservationId}/sessions — every participant of a group, the owner included.</summary>
public sealed record GroupSessionsResponse(IReadOnlyList<GroupParticipant> Participants, decimal Total);

public sealed record GroupParticipant(
    string Id,
    string Type,
    string Name,
    IReadOnlyList<ParticipantItem> Items,
    decimal Total,
    bool IsSubmitted);

public sealed record ParticipantItem(ParticipantDish Dish, int Quantity);

public sealed record ParticipantDish(string Id, string Name, decimal Price);
