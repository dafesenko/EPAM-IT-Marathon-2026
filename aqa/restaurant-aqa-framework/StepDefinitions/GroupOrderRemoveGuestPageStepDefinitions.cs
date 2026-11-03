using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Tasks;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>Removing a guest from the owner's group page in a browser. The participant cards are checked with the
/// shared "the participant cards are:" step in <see cref="GroupOrderPageStepDefinitions"/>.</summary>
[Binding]
public sealed class GroupOrderRemoveGuestPageStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    [When("{word} removes {word} from the page")]
    public Task WhenOwnerRemovesAGuestFromThePage(string owner, string guest) =>
        Actor.AttemptsToAsync(new RemoveGuestOnThePage(guest));

    [When("{word} opens the remove dialog for {word} and cancels")]
    public Task WhenOwnerOpensTheRemoveDialogAndCancels(string owner, string guest) =>
        Actor.AttemptsToAsync(new OpenAndCancelRemoval(guest));
}
