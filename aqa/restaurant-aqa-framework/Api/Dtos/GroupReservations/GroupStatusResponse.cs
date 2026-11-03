namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

/// <summary>GET /guest/groups/{groupReservationId}/status — how many seats are taken, and whether the group is still open.</summary>
public sealed record GroupStatusResponse(int CurrentParticipants, int MaxCapacity, string Status);
