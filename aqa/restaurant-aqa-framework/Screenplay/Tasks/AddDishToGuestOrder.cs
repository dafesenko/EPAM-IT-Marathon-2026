using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>A guest puts the most recently added dish into their own cart, the given number of times. The outcome is
/// the scenario's last response; a Given that needs the dish to be in the cart checks that outcome itself.</summary>
public sealed class AddDishToGuestOrder(string guestName, int quantity) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var guest = actor.Recall<GuestSession>(guestName);
        var visitor = actor.Recall<Visitor>(guestName);
        var dish = actor.Recall<DishResponse>();

        await actor.TheLastResponse().CaptureAsync(
            await new GuestSessionsApi(actor.UsingTheApi()).AddDishAsync(guest.SessionId, dish.Id, quantity, visitor.Token));
    }
}
