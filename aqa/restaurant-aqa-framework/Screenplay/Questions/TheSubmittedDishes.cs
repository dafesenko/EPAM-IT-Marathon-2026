using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>The dishes the last group submit passed on to the pre-order, merged by dish.</summary>
public sealed class TheSubmittedDishes : IQuestion<IReadOnlyList<DishQuantityResponse>>
{
    public Task<IReadOnlyList<DishQuantityResponse>> AnsweredByAsync(Actor actor) =>
        Task.FromResult<IReadOnlyList<DishQuantityResponse>>(actor.TheLastResponse().Json<List<DishQuantityResponse>>());
}
