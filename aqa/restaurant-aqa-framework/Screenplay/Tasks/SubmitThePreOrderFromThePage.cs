using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>The owner presses "Submit Pre-Order" and confirms in the dialog. Waits until the page shows the pre-order as
/// sent to the restaurant, so the next step reads the new status.</summary>
public sealed class SubmitThePreOrderFromThePage : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());

        await page.SubmitPreOrderButton.ClickAsync();
        await page.DialogSubmitButton.ClickAsync();
        await page.StatusBadge.Filter(new() { HasText = "Sent to Restaurant" })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
    }
}
