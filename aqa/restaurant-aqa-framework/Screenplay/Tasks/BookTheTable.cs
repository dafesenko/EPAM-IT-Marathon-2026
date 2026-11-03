using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;
using Restaurant_Aqa_Framework.Api.Dtos.Locations;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Dtos.Tables;
using Restaurant_Aqa_Framework.Api.Seeding;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>The owner books the most recently set-up table for a group of guests. Booking a table for two or more
/// guests opens a group reservation the owner can invite guests into. The booking is remembered under the owner's
/// name and cancelled again at the end of the scenario.</summary>
public sealed class BookTheTable(string ownerName, int guests) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var owner = actor.Recall<Visitor>(ownerName);
        var table = actor.Recall<TableResponse>();
        var reservations = new ReservationsApi(actor.UsingTheApi());
        var groups = new GroupReservationsApi(actor.UsingTheApi());
        var response = actor.TheLastResponse();

        // Starts well over the 15-minute minimum from now, so the booking is accepted whenever the scenario runs.
        var start = DateTime.UtcNow.AddMinutes(60);
        await response.CaptureAsync(await reservations.CreateReservationAsync(
            new CreateReservationRequest(table.Id, start, start.AddMinutes(90), guests),
            owner.Token));
        if (!response.Succeeded)
            throw new InvalidOperationException($"Booking the table failed with {response.Status}: {response.Body}");

        var reservation = response.Json<ReservationResponse>();
        var invite = await ReadAsync<InviteTokenResponse>(await groups.GetInviteTokenAsync(reservation.Id, owner.Token));
        var group = await ReadAsync<GroupInviteResponse>(await groups.GetInviteAsync(invite.Token));

        actor.Remember(ownerName, new GroupBooking(reservation.Id, group.GroupReservationId, invite.Token));

        actor.Recall<CleanupScope>().TrackDelete(
            $"reservation:{reservation.Id}",
            () => reservations.CancelReservationAsync(reservation.Id, owner.Token));
    }

    private static async Task<T> ReadAsync<T>(Microsoft.Playwright.IAPIResponse response)
    {
        if (!response.Ok)
            throw new InvalidOperationException(
                $"Expected a successful response to read as {typeof(T).Name}, but got {response.Status}: {await response.ReadErrorMessageAsync()}");

        return await response.ReadAsAsync<T>();
    }
}
