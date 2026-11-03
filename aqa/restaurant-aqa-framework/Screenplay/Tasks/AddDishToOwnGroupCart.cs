using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>The owner puts the most recent dish into their own session in the group, the way a guest does. The outcome is
/// the scenario's last response.</summary>
public sealed class AddDishToOwnGroupCart(string ownerName, int quantity) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var owner = actor.Recall<Visitor>(ownerName);
        var booking = actor.Recall<GroupBooking>(ownerName);
        var dish = actor.Recall<DishResponse>();
        var groups = new GroupReservationsApi(actor.UsingTheApi());

        var session = await groups.GetOwnerGuestSessionIdAsync(booking.ReservationId, owner.Token);
        if (!session.Ok)
            throw new InvalidOperationException($"Looking up the owner's session failed with {session.Status}: {await session.ReadErrorMessageAsync()}");
        var ownerSession = await session.ReadAsAsync<OwnerGuestSessionIdResponse>();

        await actor.TheLastResponse().CaptureAsync(
            await new GuestSessionsApi(actor.UsingTheApi()).AddDishAsync(ownerSession.GuestSessionId, dish.Id, quantity, owner.Token));
    }
}
