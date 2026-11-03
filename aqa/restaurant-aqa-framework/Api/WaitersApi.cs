using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Api.Dtos.Waiters;

namespace Restaurant_Aqa_Framework.Api;

/// <summary>Which Waiter account serves which location. A waiter can be on one location's list at a time.</summary>
public sealed class WaitersApi(IAPIRequestContext api) : ApiClientBase(api)
{
    public Task<IAPIResponse> AddWaiterAsync(string email, string locationId) =>
        Api.PostAsync(Url("/waiters"), new APIRequestContextOptions
        {
            DataString = Serialize(new UpdateWaiterListRequest(email, locationId)),
            Headers = Headers(),
        });

    public Task<IAPIResponse> RemoveWaiterAsync(string email, string locationId) =>
        DeleteAsync(Url("/waiters"), new APIRequestContextOptions
        {
            DataString = Serialize(new UpdateWaiterListRequest(email, locationId)),
            Headers = Headers(),
        });
}
