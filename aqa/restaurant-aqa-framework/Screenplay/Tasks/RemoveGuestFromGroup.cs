using Restaurant_Aqa_Framework.Api;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Removes a guest session from a group, as the given caller. The outcome is the scenario's last response: the
/// participants list after the removal when it succeeds, the refusal otherwise.</summary>
public sealed class RemoveGuestFromGroup(string groupReservationId, string guestSessionId, string? bearerToken) : ITask
{
    public async Task PerformAsAsync(Actor actor) =>
        await actor.TheLastResponse().CaptureAsync(
            await new GroupReservationsApi(actor.UsingTheApi()).RemoveGuestSessionAsync(groupReservationId, guestSessionId, bearerToken));
}
