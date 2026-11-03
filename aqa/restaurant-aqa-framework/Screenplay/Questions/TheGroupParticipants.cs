using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>The participants list the last request returned, with each participant's dishes and totals.</summary>
public sealed class TheGroupParticipants : IQuestion<GroupSessionsResponse>
{
    public Task<GroupSessionsResponse> AnsweredByAsync(Actor actor) =>
        Task.FromResult(actor.TheLastResponse().Json<GroupSessionsResponse>());
}
