using Restaurant_Aqa_Framework.Api;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Asks for the pre-order of a reservation as the given caller. The outcome is the scenario's last response.</summary>
public sealed class ViewTheOrder(string reservationId, string bearerToken) : ITask
{
    public async Task PerformAsAsync(Actor actor) =>
        await actor.TheLastResponse().CaptureAsync(
            await new OrdersApi(actor.UsingTheApi()).GetOrderAsync(reservationId, bearerToken));
}
