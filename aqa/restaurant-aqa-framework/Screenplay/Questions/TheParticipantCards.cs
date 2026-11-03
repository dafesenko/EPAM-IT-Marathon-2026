using System.Globalization;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>One participant card as the page shows it: its heading, whether the cart is submitted, and its subtotal.</summary>
public sealed record ParticipantCard(string Title, bool IsSubmitted, decimal Subtotal);

/// <summary>The participant cards on the group pre-order page, in the order the page shows them. Waits for the page to
/// render the list (the owner's card is always there), so an answer is read after loading has finished.</summary>
public sealed class TheParticipantCards : IQuestion<IReadOnlyList<ParticipantCard>>
{
    public async Task<IReadOnlyList<ParticipantCard>> AnsweredByAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());
        await page.ParticipantCards.First.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 15_000 });

        var cards = new List<ParticipantCard>();
        var count = await page.ParticipantCards.CountAsync();
        for (var index = 0; index < count; index++)
        {
            var card = page.Card(index);
            cards.Add(new ParticipantCard(
                Title: (await card.Title.TextContentAsync() ?? "").Trim(),
                IsSubmitted: (await card.StatusChip.TextContentAsync())?.Trim() == "Submitted",
                Subtotal: ParseAmount(await card.Subtotal.TextContentAsync() ?? "")));
        }

        return cards;
    }

    // The subtotal reads like "Subtotal$360.00": keep the digits and the decimal point, whatever the currency sign.
    private static decimal ParseAmount(string text) =>
        decimal.Parse(new string(text.Where(c => char.IsDigit(c) || c == '.').ToArray()), CultureInfo.InvariantCulture);
}
