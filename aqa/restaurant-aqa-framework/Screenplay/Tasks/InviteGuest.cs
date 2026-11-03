using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;
using Restaurant_Aqa_Framework.Api.Dtos.GuestSessions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>A guest accepts the owner's invitation and joins the group under their own name. The guest is remembered
/// under that name as both a <see cref="GuestSession"/> and a <see cref="Visitor"/> (with the guest's session token).</summary>
public sealed class InviteGuest(string ownerName, string guestName) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var booking = actor.Recall<GroupBooking>(ownerName);
        var groups = new GroupReservationsApi(actor.UsingTheApi());
        var guests = new GuestSessionsApi(actor.UsingTheApi());
        var response = actor.TheLastResponse();

        var invitation = await groups.GetInviteAsync(booking.InviteToken);
        if (!invitation.Ok)
            throw new InvalidOperationException($"Opening the invitation failed with {invitation.Status}: {await invitation.ReadErrorMessageAsync()}");
        var group = await invitation.ReadAsAsync<GroupInviteResponse>();

        await response.CaptureAsync(await guests.CreateGuestSessionAsync(new CreateGuestSessionRequest(group.GroupReservationId, guestName)));
        if (!response.Succeeded)
            throw new InvalidOperationException($"{guestName} could not join the group: {response.Status}: {response.Body}");

        var joined = response.Json<CreateGuestSessionResponse>();
        actor.Remember(guestName, new GuestSession(joined.SessionId));
        actor.Remember(guestName, new Visitor(guestName, joined.SessionToken));
    }
}
