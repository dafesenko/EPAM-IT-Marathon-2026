using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>Steps for removing a guest from a group pre-order. Only the owner may remove a guest; the owner's own
/// session cannot be removed. The participants and the totals are read with the shared steps in
/// <see cref="GroupPreOrderStepDefinitions"/>.</summary>
[Binding]
public sealed class GroupPreOrderRemoveStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    [Given("another table for {int} guests")]
    public Task GivenAnotherTable(int guests) => Actor.AttemptsToAsync(new AddTable(seats: guests));

    [When("{word} removes {word} from the group")]
    public Task WhenOwnerRemovesAGuest(string owner, string guest)
    {
        var booking = Recall<GroupBooking>(owner);
        return Actor.AttemptsToAsync(new RemoveGuestFromGroup(
            booking.GroupReservationId, Recall<GuestSession>(guest).SessionId, Recall<Visitor>(owner).Token));
    }

    [When("{word} tries to remove {word} from the group")]
    public Task WhenOwnerTriesToRemoveAGuestFromTheirGroup(string owner, string guest)
    {
        var booking = Recall<GroupBooking>(owner);
        return Actor.AttemptsToAsync(new RemoveGuestFromGroup(
            booking.GroupReservationId, Recall<GuestSession>(guest).SessionId, Recall<Visitor>(owner).Token));
    }

    [When("{word} tries to remove {word} from {word}'s group")]
    public Task WhenSomeoneTriesToRemoveAGuestFromAGroup(string viewer, string guest, string owner)
    {
        var booking = Recall<GroupBooking>(owner);
        return Actor.AttemptsToAsync(new RemoveGuestFromGroup(
            booking.GroupReservationId, Recall<GuestSession>(guest).SessionId, Recall<Visitor>(viewer).Token));
    }

    [When("{word} tries to remove a guest who does not exist")]
    public Task WhenOwnerTriesToRemoveAGuestThatDoesNotExist(string owner) =>
        Actor.AttemptsToAsync(new RemoveGuestFromGroup(
            Recall<GroupBooking>(owner).GroupReservationId, Guid.NewGuid().ToString(), Recall<Visitor>(owner).Token));

    [When("{word} tries to remove the owner from the group")]
    public Task WhenOwnerTriesToRemoveThemselves(string owner) => Actor.AttemptsToAsync(new RemoveTheOwnerFromTheGroup(owner));

    [Then("the visitor is told the guest could not be found")]
    public async Task ThenTheVisitorIsToldTheGuestCouldNotBeFound()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(404).Because($"response body was: {outcome.Body}");
        await Assert.That(outcome.ErrorMessage).Contains("not found").Because($"the message was: {outcome.ErrorMessage}");
    }
}
