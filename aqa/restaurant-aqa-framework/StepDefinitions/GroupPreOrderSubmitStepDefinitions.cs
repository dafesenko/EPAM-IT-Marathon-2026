using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>Steps for submitting a group pre-order: the owner starts their pre-order, the confirmed guests' dishes go
/// into it, and the restaurant and the waiter see the result. Refusals are checked with the outcome steps.</summary>
[Binding]
public sealed class GroupPreOrderSubmitStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    [Given("{word} has started a pre-order with {int} main course(s)")]
    public async Task GivenOwnerHasStartedAPreOrder(string owner, int quantity)
    {
        await Actor.AttemptsToAsync(new StartPreOrder(owner, quantity));
        await AssertLastRequestSucceeded($"{owner}'s pre-order");
    }

    [When("{word} submits the group pre-order")]
    public Task WhenOwnerSubmitsTheGroupPreOrder(string owner)
    {
        var booking = Recall<GroupBooking>(owner);
        return Actor.AttemptsToAsync(new SubmitTheGroupPreOrder(booking.GroupReservationId, booking.ReservationId, Recall<Visitor>(owner).Token));
    }

    [When("{word} tries to submit the group pre-order of {word}'s group")]
    public Task WhenSomeoneTriesToSubmitTheGroupPreOrder(string viewer, string owner)
    {
        var booking = Recall<GroupBooking>(owner);
        return Actor.AttemptsToAsync(new SubmitTheGroupPreOrder(booking.GroupReservationId, booking.ReservationId, Recall<Visitor>(viewer).Token));
    }

    [When("{word} submits a group pre-order that does not exist")]
    public Task WhenSomeoneSubmitsAGroupThatDoesNotExist(string owner) =>
        Actor.AttemptsToAsync(new SubmitTheGroupPreOrder(Guid.NewGuid().ToString(), Recall<GroupBooking>(owner).ReservationId, Recall<Visitor>(owner).Token));

    [When("{word} checks the group status")]
    public Task WhenOwnerChecksTheGroupStatus(string owner)
    {
        var booking = Recall<GroupBooking>(owner);
        return Actor.AttemptsToAsync(new CheckTheGroupStatus(booking.GroupReservationId, Recall<Visitor>(owner).Token));
    }

    [When("the waiter views the order of {word}")]
    public async Task WhenTheWaiterViewsTheOrder(string owner)
    {
        var waiter = await Actor.AsksForAsync(new TheWaiterSession());
        await Actor.AttemptsToAsync(new ViewTheOrder(Recall<GroupBooking>(owner).ReservationId, waiter.IdToken));
    }

    [When("{word} adds {int} main course(s) to their own group cart")]
    public Task WhenOwnerAddsDishesToOwnCart(string owner, int quantity) =>
        Actor.AttemptsToAsync(new AddDishToOwnGroupCart(owner, quantity));

    [Then("the restaurant receives {int} main course(s)")]
    public async Task ThenTheRestaurantReceivesDishes(int quantity)
    {
        await AssertLastRequestSucceeded("Submitting the group pre-order");

        var dish = Recall<DishResponse>();
        var dishes = await Actor.AsksForAsync(new TheSubmittedDishes());
        var received = dishes.Where(d => d.DishId == dish.Id).Sum(d => d.Quantity);

        await Assert.That(received).IsEqualTo(quantity).Because($"the restaurant received {received} of the dish");
    }

    [Then("the group is submitted")]
    public async Task ThenTheGroupIsSubmitted()
    {
        var group = await Actor.AsksForAsync(new TheGroupStatus());
        await Assert.That(group.Status).IsEqualTo("Submitted").Because($"the group status is '{group.Status}'");
    }

    [Then("the waiter sees {int} main course(s) on the order")]
    public async Task ThenTheWaiterSeesDishesOnTheOrder(int quantity)
    {
        var order = await Actor.AsksForAsync(new TheOrder());
        var dish = Recall<DishResponse>();
        var seen = order.OrderItems.Where(i => i.Dish.Id == dish.Id).Sum(i => i.Quantity);

        await Assert.That(seen).IsEqualTo(quantity).Because($"the order holds {seen} of the dish");
    }

    [Then("the order is marked as submitted by the customer")]
    public async Task ThenTheOrderIsMarkedAsSubmittedByTheCustomer()
    {
        var order = await Actor.AsksForAsync(new TheOrder());
        await Assert.That(order.Status).IsEqualTo(OrderStatus.SubmittedByCustomer)
            .Because($"the order status is {order.Status}");
    }

    [Then("the visitor is told the pre-order is not found")]
    public async Task ThenTheVisitorIsToldThePreOrderIsNotFound()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Succeeded).IsFalse().Because($"the request unexpectedly succeeded: {outcome.Body}");
        await Assert.That(outcome.ErrorMessage).StartsWith("Active pre-order for reservation")
            .Because($"the message was: {outcome.ErrorMessage}");
    }

    [Then("the dish is added to the owner's cart")]
    public Task ThenTheDishIsAddedToTheOwnersCart() =>
        AssertLastRequestSucceeded("Adding a dish to the owner's own cart");

    private async Task AssertLastRequestSucceeded(string what)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Succeeded).IsTrue().Because($"{what} failed with {outcome.Status}: {outcome.Body}");
    }
}
