using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Locations;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Configuration;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Puts the location Waiter account on the most recently set-up location, so the location can take bookings.
/// Taking the account off that location is scheduled for the end of the scenario. A set-up task: it throws if the
/// assignment fails, and it leaves the scenario's last response alone.</summary>
public sealed class AssignLocationWaiter : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var (email, _) = Settings.Current.RequireLocationWaiter();
        var location = actor.Recall<LocationResponse>();
        var api = new WaitersApi(actor.UsingTheApi());

        var added = await api.AddWaiterAsync(email, location.Id);
        if (!added.Ok)
            throw new InvalidOperationException(
                $"Putting the location waiter on the location failed with {added.Status}: {await added.ReadErrorMessageAsync()}");

        actor.Recall<CleanupScope>().TrackDelete(
            $"location-waiter:{location.Id}",
            () => api.RemoveWaiterAsync(email, location.Id));
    }
}
