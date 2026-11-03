using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>The owner removes a guest from the group page: presses the guest's "Remove Guest" button and confirms. Waits
/// until the guest's button is gone from the page.</summary>
public sealed class RemoveGuestOnThePage(string guestName) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());

        await page.RemoveGuestButton(guestName).ClickAsync();
        await page.DialogRemoveGuestButton.ClickAsync();
        await page.RemoveGuestButton(guestName).WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 15_000 });
    }
}
