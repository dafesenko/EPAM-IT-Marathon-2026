using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>The owner starts their own pre-order on the booking, with the most recent dish in the given quantity. A
/// pre-order needs at least one dish. The guests' confirmed dishes are added to it when the group is submitted.</summary>
public sealed class StartPreOrder(string ownerName, int quantity) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var owner = actor.Recall<Visitor>(ownerName);
        var booking = actor.Recall<GroupBooking>(ownerName);
        var dish = actor.Recall<DishResponse>();

        await actor.TheLastResponse().CaptureAsync(await new OrdersApi(actor.UsingTheApi()).CreateOrderAsync(
            new CreateOrderRequest(booking.ReservationId, [new OrderItemRequest(dish.Id, quantity)]),
            owner.Token));
    }
}
