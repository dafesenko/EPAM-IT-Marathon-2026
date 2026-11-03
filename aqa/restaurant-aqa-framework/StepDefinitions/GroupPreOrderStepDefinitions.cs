using System.Globalization;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>Steps for a group pre-order: an owner books a table, invites guests, each guest orders, and the owner
/// reads the participants list. Each step hands the work to a task or question; the state they share is the actor's
/// memory, keyed by each person's name.</summary>
[Binding]
public sealed class GroupPreOrderStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    [Given("a restaurant location with a table for {int} guests")]
    public Task GivenALocationWithATable(int guests) =>
        Actor.AttemptsToAsync(new SetUpLocation(), new AddTable(seats: guests), new AssignLocationWaiter());

    [Given("a main course priced {int} on its menu")]
    public Task GivenAMainCourseOnTheMenu(int price) =>
        Actor.AttemptsToAsync(new AddDish(DishType.MainCourse, price: price));

    [Given("{word} has signed up")]
    public Task GivenSomeoneHasSignedUp(string name) => Actor.AttemptsToAsync(new BecomeCustomer(name));

    [Given("{word} has booked the table for {int} guests")]
    public async Task GivenSomeoneHasBookedTheTable(string owner, int guests)
    {
        await Actor.AttemptsToAsync(new BecomeCustomer(owner), new BookTheTable(owner, guests));
        await AssertLastRequestSucceeded("The booking");
    }

    [Given("{word} has invited {word}")]
    public async Task GivenOwnerHasInvited(string owner, string guest)
    {
        await Actor.AttemptsToAsync(new InviteGuest(owner, guest));
        await AssertLastRequestSucceeded($"{guest}'s invitation");
    }

    [Given("{word} has added {int} main course(s) to the order")]
    public async Task GivenGuestHasAddedDishes(string guest, int quantity)
    {
        await Actor.AttemptsToAsync(new AddDishToGuestOrder(guest, quantity));
        await AssertLastRequestSucceeded($"Adding dishes for {guest}");
    }

    [Given("{word} has confirmed the order")]
    public async Task GivenGuestHasConfirmedTheOrder(string guest)
    {
        await Actor.AttemptsToAsync(new SubmitGuestOrder(guest));
        await AssertLastRequestSucceeded($"{guest}'s confirmation");
    }

    [When("{word} adds {int} more main course(s)")]
    public Task WhenGuestAddsMoreDishes(string guest, int quantity) =>
        Actor.AttemptsToAsync(new AddDishToGuestOrder(guest, quantity));

    [When("{word} views the group's participants")]
    public Task WhenOwnerViewsTheParticipants(string owner) =>
        Actor.AttemptsToAsync(new ViewTheGroupParticipants(GroupOf(owner), TokenOf(owner)));

    [When("{word} views the participants of {word}'s group")]
    public Task WhenSomeoneViewsTheParticipants(string viewer, string owner) =>
        Actor.AttemptsToAsync(new ViewTheGroupParticipants(GroupOf(owner), TokenOf(viewer)));

    [When("the waiter views the participants of {word}'s group")]
    public async Task WhenTheWaiterViewsTheParticipants(string owner)
    {
        var waiter = await Actor.AsksForAsync(new TheWaiterSession());
        await Actor.AttemptsToAsync(new ViewTheGroupParticipants(GroupOf(owner), waiter.IdToken));
    }

    [When("someone who is not signed in views the participants of {word}'s group")]
    public Task WhenAnonymousSomeoneViewsTheParticipants(string owner) =>
        Actor.AttemptsToAsync(new ViewTheGroupParticipants(GroupOf(owner), bearerToken: null));

    [When("{word} views the participants of a group that does not exist")]
    public Task WhenSomeoneViewsAGroupThatDoesNotExist(string viewer) =>
        Actor.AttemptsToAsync(new ViewTheGroupParticipants(Guid.NewGuid().ToString(), TokenOf(viewer)));

    [Then("the participants are:")]
    public async Task ThenTheParticipantsAre(Table table)
    {
        var group = await Actor.AsksForAsync(new TheGroupParticipants());

        await Assert.That(group.Participants.Count).IsEqualTo(table.Rows.Count)
            .Because($"the group lists {group.Participants.Count} participants: {string.Join(", ", group.Participants.Select(p => p.Name))}");

        foreach (var row in table.Rows)
        {
            var participant = group.Participants.SingleOrDefault(p => p.Name == row["name"]);
            await Assert.That(participant).IsNotNull().Because($"{row["name"]} is not in the participants list");
            await Assert.That(participant!.IsSubmitted).IsEqualTo(row["confirmed"] == "yes")
                .Because($"{row["name"]} is confirmed = {participant.IsSubmitted}");
            await Assert.That(participant.Total).IsEqualTo(decimal.Parse(row["total"], CultureInfo.InvariantCulture))
                .Because($"{row["name"]}'s total is {participant.Total}");
        }
    }

    [Then("the group total is {int}")]
    public async Task ThenTheGroupTotalIs(int total)
    {
        var group = await Actor.AsksForAsync(new TheGroupParticipants());
        await Assert.That(group.Total).IsEqualTo(total).Because($"the group total is {group.Total}");
    }

    [Then("the visitor is not allowed to see the participants")]
    public async Task ThenVisitorIsNotAllowed()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(403).Because($"response body was: {outcome.Body}");
    }

    [Then("the visitor is asked to sign in")]
    public async Task ThenVisitorIsAskedToSignIn()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(401).Because($"response body was: {outcome.Body}");
    }

    [Then("the group could not be found")]
    public async Task ThenTheGroupCouldNotBeFound()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(404).Because($"response body was: {outcome.Body}");
    }

    private string GroupOf(string owner) => Recall<GroupBooking>(owner).GroupReservationId;

    private string TokenOf(string name) => Recall<Visitor>(name).Token;

    private async Task AssertLastRequestSucceeded(string what)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Succeeded).IsTrue().Because($"{what} failed with {outcome.Status}: {outcome.Body}");
    }
}
