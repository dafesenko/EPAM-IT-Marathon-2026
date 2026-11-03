namespace Restaurant_Aqa_Framework.Api.Dtos.GuestSessions;

/// <summary>POST /guest/sessions/{guestSessionId}/dishes — puts a dish into a guest's cart.</summary>
public sealed record AddDishToGuestSessionRequest(string DishId, int Quantity);
