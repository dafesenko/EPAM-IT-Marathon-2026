using Microsoft.Playwright;

namespace Restaurant_Aqa_Framework.Ui.Components;

/// <summary>`app-participant-card` on the group pre-order page: a heading ("My Pre-Order", "Guest: …"), a chip that says
/// whether the cart is submitted, and the subtotal. The page uses CSS classes here: the card has no role or label
/// that names its parts.</summary>
public sealed class GroupParticipantCardComponent(ILocator root)
{
    public ILocator Title => root.Locator("h2.title");
    public ILocator StatusChip => root.Locator(".chip");
    public ILocator Subtotal => root.Locator(".subtotal");
}
