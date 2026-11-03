using Restaurant_Aqa_Framework.Api.Dtos.Orders;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>The pre-order the last request returned.</summary>
public sealed class TheOrder : IQuestion<GetOrderResponse>
{
    public Task<GetOrderResponse> AnsweredByAsync(Actor actor) =>
        Task.FromResult(actor.TheLastResponse().Json<GetOrderResponse>());
}
