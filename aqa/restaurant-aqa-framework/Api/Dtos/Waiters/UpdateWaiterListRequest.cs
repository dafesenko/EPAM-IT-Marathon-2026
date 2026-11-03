namespace Restaurant_Aqa_Framework.Api.Dtos.Waiters;

/// <summary>POST and DELETE /waiters — puts a Waiter account on a location, or takes it off again.</summary>
public sealed record UpdateWaiterListRequest(string Email, string LocationId);
