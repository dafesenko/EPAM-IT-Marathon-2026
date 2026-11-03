namespace Restaurant_Aqa_Framework.Screenplay;

/// <summary>A customer's login details, so a UI scenario can sign them in through the login form.</summary>
public sealed record CustomerAccount(string Email, string Password);

/// <summary>A signed-in person and the bearer token their calls carry: a customer's Cognito id token, or a guest's
/// session token. Remembered by the person's name.</summary>
public sealed record Visitor(string Name, string Token);

/// <summary>A guest's session id in the group, remembered by the guest's name.</summary>
public sealed record GuestSession(string SessionId);

/// <summary>The reservation an owner booked for a group, with what the owner needs to invite guests and read the
/// participants. Remembered by the owner's name.</summary>
public sealed record GroupBooking(string ReservationId, string GroupReservationId, string InviteToken);
