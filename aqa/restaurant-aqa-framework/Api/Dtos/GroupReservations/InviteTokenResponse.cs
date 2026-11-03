namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

/// <summary>GET /reservations/{reservationId}/invite-token — the token the owner shares with guests.</summary>
public sealed record InviteTokenResponse(string Token);
