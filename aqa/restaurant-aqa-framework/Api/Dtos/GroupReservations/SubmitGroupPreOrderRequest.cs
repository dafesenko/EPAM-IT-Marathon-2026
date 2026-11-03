namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

/// <summary>POST /group-reservations/{groupReservationId}/submit — the reservation whose pre-order receives the guests' dishes.</summary>
public sealed record SubmitGroupPreOrderRequest(string ReservationId);
