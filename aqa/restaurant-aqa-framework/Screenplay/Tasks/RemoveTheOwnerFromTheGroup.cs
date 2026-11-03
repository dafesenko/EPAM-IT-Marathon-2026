using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>The owner tries to remove their own session from their group. The backend refuses it; the outcome is the
/// scenario's last response.</summary>
public sealed class RemoveTheOwnerFromTheGroup(string ownerName) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var owner = actor.Recall<Visitor>(ownerName);
        var booking = actor.Recall<GroupBooking>(ownerName);
        var groups = new GroupReservationsApi(actor.UsingTheApi());

        var session = await groups.GetOwnerGuestSessionIdAsync(booking.ReservationId, owner.Token);
        if (!session.Ok)
            throw new InvalidOperationException($"Looking up the owner's session failed with {session.Status}: {await session.ReadErrorMessageAsync()}");
        var ownerSession = await session.ReadAsAsync<OwnerGuestSessionIdResponse>();

        await actor.TheLastResponse().CaptureAsync(
            await groups.RemoveGuestSessionAsync(booking.GroupReservationId, ownerSession.GuestSessionId, owner.Token));
    }
}
