using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>The owner opens the removal dialog for a guest and backs out. Waits until the dialog has closed.</summary>
public sealed class OpenAndCancelRemoval(string guestName) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());

        await page.RemoveGuestButton(guestName).ClickAsync();
        await page.DialogCancelButton.ClickAsync();
        await page.DialogRemoveGuestButton.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 15_000 });
    }
}
