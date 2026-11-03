using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Opens the group pre-order page of the booking the given owner made, and waits until its heading is shown.
/// The owner must already be signed in through the login form.</summary>
public sealed class OpenTheGroupOrderPage(string ownerName) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var booking = actor.Recall<GroupBooking>(ownerName);
        var page = new GroupOrderPage(actor.LookingAtThePage());

        await page.NavigateAsync(booking.ReservationId);
        await page.Heading.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 15_000 });
    }
}
