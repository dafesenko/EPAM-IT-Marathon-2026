using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

namespace Restaurant_Aqa_Framework.Api;

/// <summary>Group reservations as the owner and the invited guests see them: invites, the owner's session, and the
/// participants list. Guest-session calls live in <see cref="GuestSessionsApi"/>.</summary>
public sealed class GroupReservationsApi(IAPIRequestContext api) : ApiClientBase(api)
{
    public Task<IAPIResponse> GetInviteTokenAsync(string reservationId, string bearerToken) =>
        GetAsync(Url($"/reservations/{reservationId}/invite-token"), new APIRequestContextOptions
        {
            Headers = Headers(bearerToken),
        });

    public Task<IAPIResponse> GetOwnerGuestSessionIdAsync(string reservationId, string bearerToken) =>
        GetAsync(Url($"/reservations/{reservationId}/guest-session-id"), new APIRequestContextOptions
        {
            Headers = Headers(bearerToken),
        });

    public Task<IAPIResponse> GetInviteAsync(string token) =>
        GetAsync(Url($"/guest/invites/{token}"));

    public Task<IAPIResponse> GetGroupSessionsAsync(string groupReservationId, string? bearerToken = null) =>
        GetAsync(Url($"/guest/groups/{groupReservationId}/sessions"), new APIRequestContextOptions
        {
            Headers = Headers(bearerToken),
        });

    public Task<IAPIResponse> GetGroupStatusAsync(string groupReservationId, string? bearerToken = null) =>
        GetAsync(Url($"/guest/groups/{groupReservationId}/status"), new APIRequestContextOptions
        {
            Headers = Headers(bearerToken),
        });

    /// <summary>The owner removes a guest session from the group. Answers with the participants list after the removal.</summary>
    public Task<IAPIResponse> RemoveGuestSessionAsync(string groupReservationId, string guestSessionId, string? bearerToken) =>
        DeleteAsync(Url($"/guest/groups/{groupReservationId}/sessions/{guestSessionId}"), new APIRequestContextOptions
        {
            Headers = Headers(bearerToken),
        });

    public Task<IAPIResponse> SubmitGroupPreOrderAsync(string groupReservationId, string reservationId, string bearerToken) =>
        Api.PostAsync(Url($"/group-reservations/{groupReservationId}/submit"), new APIRequestContextOptions
        {
            DataString = Serialize(new SubmitGroupPreOrderRequest(reservationId)),
            Headers = Headers(bearerToken),
        });
}
