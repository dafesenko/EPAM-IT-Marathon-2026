using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>Submitting the group pre-order from the owner's page in a browser. The steps before it (the booking, the
/// guests, the pre-order) are the shared ones; opening the page is in <see cref="GroupOrderPageStepDefinitions"/>.</summary>
[Binding]
public sealed class GroupOrderSubmitPageStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    [When("{word} submits the pre-order from the page")]
    public Task WhenOwnerSubmitsThePreOrderFromThePage(string owner) =>
        Actor.AttemptsToAsync(new SubmitThePreOrderFromThePage());

    [When("{word} opens the submit dialog and cancels")]
    public Task WhenOwnerOpensTheSubmitDialogAndCancels(string owner) =>
        Actor.AttemptsToAsync(new OpenAndCancelSubmitDialog());

    [Then("the page shows the pre-order as sent to the restaurant")]
    public async Task ThenThePageShowsThePreOrderAsSent()
    {
        var state = await Actor.AsksForAsync(new ThePageState());
        await Assert.That(state.Status).IsEqualTo("Sent to Restaurant").Because($"the page shows '{state.Status}'");
        await Assert.That(state.SubmitButtonEnabled).IsFalse()
            .Because("the pre-order was sent, so there is nothing left to submit");
    }

    [Then("the page still collects pre-orders")]
    public async Task ThenThePageStillCollectsPreOrders()
    {
        var state = await Actor.AsksForAsync(new ThePageState());
        await Assert.That(state.Status).IsEqualTo("Collecting pre-orders").Because($"the page shows '{state.Status}'");
    }

    [Then("the submit button is disabled")]
    public async Task ThenTheSubmitButtonIsDisabled()
    {
        var state = await Actor.AsksForAsync(new ThePageState());
        await Assert.That(state.SubmitButtonEnabled).IsFalse()
            .Because("no guest has confirmed a cart, so the pre-order cannot be submitted yet");
    }
}
