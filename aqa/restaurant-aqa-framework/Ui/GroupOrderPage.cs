using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui.Components;

namespace Restaurant_Aqa_Framework.Ui;

/// <summary>The group pre-order page: the reservation's participants, one card each, as the owner sees them.</summary>
public sealed class GroupOrderPage(IPage page) : BasePage(page)
{
    public ILocator Heading => Page.GetByRole(AriaRole.Heading, new() { Name = "Group Pre-Order", Exact = true });

    public ILocator ParticipantCards => Page.Locator("app-participant-card");

    public Task NavigateAsync(string reservationId) => NavigateToAsync($"/reservations/{reservationId}/group-order");

    public GroupParticipantCardComponent Card(int index) => new(ParticipantCards.Nth(index));

    /// <summary>The page's "Submit Pre-Order" button. It is shown while the group is open, and disabled until a guest
    /// has confirmed a cart.</summary>
    public ILocator SubmitPreOrderButton => Page.GetByRole(AriaRole.Button, new() { Name = "Submit Pre-Order", Exact = true });

    // The confirmation dialog's buttons. Only the dialog has a button named "Submit" or "Cancel" while it is open.
    public ILocator DialogSubmitButton => Page.GetByRole(AriaRole.Button, new() { Name = "Submit", Exact = true });

    public ILocator DialogCancelButton => Page.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true });

    /// <summary>The status badge: "Collecting pre-orders" while the group is open, "Sent to Restaurant" once submitted.</summary>
    public ILocator StatusBadge => Page.Locator(".badge");
}
