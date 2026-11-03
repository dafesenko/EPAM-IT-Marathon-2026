using Restaurant_Aqa_Framework.Api;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Submits the group pre-order: the confirmed guests' dishes go into the reservation's pre-order. The outcome
/// is the scenario's last response, the dishes that were passed on when it succeeds.</summary>
public sealed class SubmitTheGroupPreOrder(string groupReservationId, string reservationId, string bearerToken) : ITask
{
    public async Task PerformAsAsync(Actor actor) =>
        await actor.TheLastResponse().CaptureAsync(
            await new GroupReservationsApi(actor.UsingTheApi()).SubmitGroupPreOrderAsync(groupReservationId, reservationId, bearerToken));
}
