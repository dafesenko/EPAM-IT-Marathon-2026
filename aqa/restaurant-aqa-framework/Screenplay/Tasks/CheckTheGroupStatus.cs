using Restaurant_Aqa_Framework.Api;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Asks for the group's status. The outcome is the scenario's last response.</summary>
public sealed class CheckTheGroupStatus(string groupReservationId, string bearerToken) : ITask
{
    public async Task PerformAsAsync(Actor actor) =>
        await actor.TheLastResponse().CaptureAsync(
            await new GroupReservationsApi(actor.UsingTheApi()).GetGroupStatusAsync(groupReservationId, bearerToken));
}
