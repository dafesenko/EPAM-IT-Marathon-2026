using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>The group status the last request returned.</summary>
public sealed class TheGroupStatus : IQuestion<GroupStatusResponse>
{
    public Task<GroupStatusResponse> AnsweredByAsync(Actor actor) =>
        Task.FromResult(actor.TheLastResponse().Json<GroupStatusResponse>());
}
