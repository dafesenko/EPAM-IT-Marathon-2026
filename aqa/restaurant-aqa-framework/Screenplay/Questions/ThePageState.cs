using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>What the group pre-order page shows about the submission: the status label, and whether the submit button
/// is there and can be pressed.</summary>
public sealed record PageState(string Status, bool SubmitButtonEnabled);

/// <summary>The group page's status and submit button. Waits for the status badge, so the answer is read after loading.</summary>
public sealed class ThePageState : IQuestion<PageState>
{
    public async Task<PageState> AnsweredByAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());
        await page.StatusBadge.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

        var status = (await page.StatusBadge.TextContentAsync() ?? "").Trim();
        var enabled = await page.SubmitPreOrderButton.IsVisibleAsync() && await page.SubmitPreOrderButton.IsEnabledAsync();
        return new PageState(status, enabled);
    }
}
