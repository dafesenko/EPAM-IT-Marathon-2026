using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>The owner opens the submit dialog and backs out of it. Waits until the dialog has closed.</summary>
public sealed class OpenAndCancelSubmitDialog : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());

        await page.SubmitPreOrderButton.ClickAsync();
        await page.DialogCancelButton.ClickAsync();
        await page.DialogSubmitButton.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 15_000 });
    }
}
