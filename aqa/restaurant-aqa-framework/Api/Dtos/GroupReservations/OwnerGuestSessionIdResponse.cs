namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

/// <summary>GET /reservations/{reservationId}/guest-session-id — the owner's own session in the group.</summary>
public sealed record OwnerGuestSessionIdResponse(string GuestSessionId);
