namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

/// <summary>GET /guest/invites/{token} — the group a guest is invited to.</summary>
public sealed record GroupInviteResponse(
    string GroupReservationId,
    string RestaurantName,
    string LocationAddress,
    DateTime StartTime);
