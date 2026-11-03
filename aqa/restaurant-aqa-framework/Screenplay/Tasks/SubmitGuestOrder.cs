using Restaurant_Aqa_Framework.Api;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>A guest confirms their cart. The outcome is the scenario's last response.</summary>
public sealed class SubmitGuestOrder(string guestName) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var guest = actor.Recall<GuestSession>(guestName);
        var visitor = actor.Recall<Visitor>(guestName);

        await actor.TheLastResponse().CaptureAsync(
            await new GuestSessionsApi(actor.UsingTheApi()).SubmitCartAsync(guest.SessionId, visitor.Token));
    }
}
