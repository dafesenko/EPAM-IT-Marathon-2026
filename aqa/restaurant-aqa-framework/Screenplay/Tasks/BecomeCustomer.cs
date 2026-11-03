using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Seeding;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Registers a new customer under a unique email and signs in, then remembers the customer as a
/// <see cref="Visitor"/> under the given name. Self-service sign-up always yields a customer.</summary>
public sealed class BecomeCustomer(string name) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var api = new AuthApi(actor.UsingTheApi());
        var email = SampleDataFactory.UniqueEmail();
        var password = SampleDataFactory.ValidPassword();

        var registration = await api.RegisterAsync(new RegisterRequest(name, "Tester", email, password, password));
        if (!registration.Ok)
            throw new InvalidOperationException(
                $"Registering {name} failed with {registration.Status}: {await registration.ReadErrorMessageAsync()}");

        var signIn = await api.SignInAsync(new SignInRequest(email, password));
        if (!signIn.Ok)
            throw new InvalidOperationException(
                $"Signing in {name} failed with {signIn.Status}: {await signIn.ReadErrorMessageAsync()}");

        actor.Remember(name, new Visitor(name, (await signIn.ReadAsAsync<SignInResponse>()).IdToken));
    }
}
