namespace Restaurant_Aqa_Framework.Api.Dtos.GuestSessions;

/// <summary>POST /guest/sessions — a guest joins a group reservation under a name.</summary>
public sealed record CreateGuestSessionRequest(string GroupReservationId, string GuestName);
