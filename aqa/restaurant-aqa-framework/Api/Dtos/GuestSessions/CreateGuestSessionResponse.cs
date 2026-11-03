namespace Restaurant_Aqa_Framework.Api.Dtos.GuestSessions;

/// <summary>The guest's session id, and the token that authorises that guest's own calls.</summary>
public sealed record CreateGuestSessionResponse(string SessionId, string SessionToken);
