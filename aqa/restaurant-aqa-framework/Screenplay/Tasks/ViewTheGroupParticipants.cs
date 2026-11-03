using Restaurant_Aqa_Framework.Api;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Asks for the participants of a group, as whoever holds the given bearer token (or as nobody, when it is
/// null). The outcome is the scenario's last response: the list when the caller may see it, the refusal otherwise.</summary>
public sealed class ViewTheGroupParticipants(string groupReservationId, string? bearerToken) : ITask
{
    public async Task PerformAsAsync(Actor actor) =>
        await actor.TheLastResponse().CaptureAsync(
            await new GroupReservationsApi(actor.UsingTheApi()).GetGroupSessionsAsync(groupReservationId, bearerToken));
}
