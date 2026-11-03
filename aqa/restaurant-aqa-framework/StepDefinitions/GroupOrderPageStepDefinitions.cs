using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>The owner's group pre-order page in a browser: the owner signs in through the login form and opens the
/// page, then reads the participant cards. The set-up steps before this are the shared ones in
/// <see cref="GroupPreOrderStepDefinitions"/>.</summary>
[Binding]
public sealed class GroupOrderPageStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    [When("{word} opens the group order page")]
    public async Task WhenOwnerOpensTheGroupOrderPage(string owner)
    {
        var account = Recall<CustomerAccount>(owner);
        await Actor.AttemptsToAsync(new SignInThroughTheLoginForm(account.Email, account.Password), new OpenTheGroupOrderPage(owner));
    }

    [Then("the participant cards are:")]
    public async Task ThenTheParticipantCardsAre(Table table)
    {
        var cards = await Actor.AsksForAsync(new TheParticipantCards());

        await Assert.That(cards.Count).IsEqualTo(table.Rows.Count)
            .Because($"the page shows {cards.Count} cards: {string.Join(", ", cards.Select(c => c.Title))}");

        // The page does not promise an order for its cards, so each row is matched to its card by title.
        foreach (var row in table.Rows)
        {
            var card = cards.SingleOrDefault(c => c.Title == CardTitle(row["participant"]));
            await Assert.That(card).IsNotNull().Because($"no card is titled '{CardTitle(row["participant"])}'");
            await Assert.That(card!.IsSubmitted).IsEqualTo(row["confirmed"] == "yes")
                .Because($"{row["participant"]}'s card says submitted = {card.IsSubmitted}");
            await Assert.That(card.Subtotal).IsEqualTo(decimal.Parse(row["subtotal"], System.Globalization.CultureInfo.InvariantCulture))
                .Because($"{row["participant"]}'s subtotal is {card.Subtotal}");
        }
    }

    // The page titles the owner's own card "My Pre-Order" and each guest's card "Guest: <name>".
    private static string CardTitle(string participant) =>
        participant == "Owner" ? "My Pre-Order" : $"Guest: {participant}";
}
