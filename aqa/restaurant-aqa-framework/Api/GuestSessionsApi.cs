using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Api.Dtos.GuestSessions;

namespace Restaurant_Aqa_Framework.Api;

/// <summary>A guest's own session: joining a group, filling the cart and submitting it. Calls after joining carry the
/// guest's session token, not a Cognito token.</summary>
public sealed class GuestSessionsApi(IAPIRequestContext api) : ApiClientBase(api)
{
    public Task<IAPIResponse> CreateGuestSessionAsync(CreateGuestSessionRequest request) =>
        Api.PostAsync(Url("/guest/sessions"), new APIRequestContextOptions
        {
            DataString = Serialize(request),
            Headers = Headers(),
        });

    public Task<IAPIResponse> AddDishAsync(string guestSessionId, string dishId, int quantity, string sessionToken) =>
        Api.PostAsync(Url($"/guest/sessions/{guestSessionId}/dishes"), new APIRequestContextOptions
        {
            DataString = Serialize(new AddDishToGuestSessionRequest(dishId, quantity)),
            Headers = Headers(sessionToken),
        });

    public Task<IAPIResponse> SubmitCartAsync(string guestSessionId, string sessionToken) =>
        Api.PostAsync(Url($"/guest/sessions/{guestSessionId}/submit"), new APIRequestContextOptions
        {
            Headers = Headers(sessionToken),
        });
}
