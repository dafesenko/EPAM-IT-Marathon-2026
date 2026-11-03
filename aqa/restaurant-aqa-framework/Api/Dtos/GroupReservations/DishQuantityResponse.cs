namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

/// <summary>One dish with the total quantity the confirmed guests ordered of it.</summary>
public sealed record DishQuantityResponse(string DishId, int Quantity);
