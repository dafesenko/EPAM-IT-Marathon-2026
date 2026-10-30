using APIHandler.Constants.Dishes;
using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Models.GuestSessions;
using APIHandler.Models.Orders;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Dishes;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Reservations;
using APIHandler.Repositories.Tables;
using APIHandler.Services.GuestSessions;
using Moq;

namespace APIHandler.Tests.Services;

public class GetGroupSessionsTests
{
    private const string OwnerUserId = "owner-user";
    private const string GroupId = "group-1";
    private const string ReservationId = "reservation-1";

    private readonly Mock<IGuestSessionRepository> _guestSessionRepository = new();
    private readonly Mock<IGroupReservationRepository> _groupReservationRepository = new();
    private readonly Mock<IDishRepository> _dishRepository = new();
    private readonly Mock<IReservationRepository> _reservationRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ITableRepository> _tableRepository = new();
    private readonly GuestSessionService _service;

    public GetGroupSessionsTests()
    {
        _service = new GuestSessionService(
            _groupReservationRepository.Object,
            _guestSessionRepository.Object,
            _dishRepository.Object,
            _reservationRepository.Object,
            _userRepository.Object,
            _tableRepository.Object);
    }

    // Checks that the owner gets every session and the types are Owner / Guest.
    [Fact]
    public async Task GetGroupSessionsAsync_OwnerRequests_ReturnsAllSessionsWithCorrectTypes()
    {
        SetupGroupAndReservation(customerId: OwnerUserId);
        SetupSessions(
            CreateSession("owner-session", "Owner", isOwner: true),
            CreateSession("guest-session-1", "Anna", isOwner: false),
            CreateSession("guest-session-2", "Ivan", isOwner: false));
        SetupDishes();

        GetAllSessionsResponse result = await _service.GetGroupSessionsAsync(OwnerUserId, GroupId);

        var participants = result.Participants.ToList();
        Assert.Equal(3, participants.Count);
        Assert.Equal(ParticipantType.Owner, participants.Single(p => p.Id == "owner-session").Type);
        Assert.Equal(ParticipantType.Guest, participants.Single(p => p.Id == "guest-session-1").Type);
        Assert.Equal(ParticipantType.Guest, participants.Single(p => p.Id == "guest-session-2").Type);
    }

    // Checks that each participant total and the response total are sums of quantity * price.
    [Fact]
    public async Task GetGroupSessionsAsync_ValidDishes_CalculatesParticipantAndResponseTotals()
    {
        SetupGroupAndReservation(customerId: OwnerUserId);
        SetupSessions(
            CreateSession("owner-session", "Owner", isOwner: true, ("dish-a", 2), ("dish-b", 3)),
            CreateSession("guest-session-1", "Anna", isOwner: false, ("dish-a", 1)));
        SetupDishes(CreateDish("dish-a", 10m), CreateDish("dish-b", 5.5m));

        GetAllSessionsResponse result = await _service.GetGroupSessionsAsync(OwnerUserId, GroupId);

        var participants = result.Participants.ToList();
        Assert.Equal(36.5m, participants.Single(p => p.Id == "owner-session").Total); // 2*10 + 3*5.5
        Assert.Equal(10m, participants.Single(p => p.Id == "guest-session-1").Total); // 1*10
        Assert.Equal(46.5m, result.Total);
    }

    // Checks that a dish missing from the repository is skipped without an exception.
    [Fact]
    public async Task GetGroupSessionsAsync_DishMissingFromRepository_SkipsItemAndItsPrice()
    {
        SetupGroupAndReservation(customerId: OwnerUserId);
        SetupSessions(
            CreateSession("owner-session", "Owner", isOwner: true, ("dish-a", 2), ("removed-dish", 1)));
        SetupDishes(CreateDish("dish-a", 10m)); // "removed-dish" is not returned

        GetAllSessionsResponse result = await _service.GetGroupSessionsAsync(OwnerUserId, GroupId);

        Participant owner = result.Participants.Single();
        OrderItemResponse item = Assert.Single(owner.Items);
        Assert.Equal("dish-a", item.Dish.Id);
        Assert.Equal(20m, owner.Total);
        Assert.Equal(20m, result.Total);
    }

    // Checks that a user who is not the reservation owner gets ActionForbiddenException.
    [Fact]
    public async Task GetGroupSessionsAsync_NotReservationOwner_ThrowsActionForbiddenException()
    {
        SetupGroupAndReservation(customerId: "someone-else");

        await Assert.ThrowsAsync<ActionForbiddenException>(() =>
            _service.GetGroupSessionsAsync(OwnerUserId, GroupId));

        // The sessions must not be read before the owner check passes.
        _guestSessionRepository.Verify(r => r.GetAllByGroupReservationIdAsync(It.IsAny<string>()), Times.Never);
    }

    // Checks that an unknown group reservation gives GroupReservationNotFoundException.
    [Fact]
    public async Task GetGroupSessionsAsync_GroupReservationNotFound_ThrowsGroupReservationNotFoundException()
    {
        _groupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(GroupId))
            .ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() =>
            _service.GetGroupSessionsAsync(OwnerUserId, GroupId));
    }

    // Checks that a group whose parent reservation is missing gives ReservationNotFoundException.
    [Fact]
    public async Task GetGroupSessionsAsync_ReservationNotFound_ThrowsReservationNotFoundException()
    {
        _groupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(GroupId))
            .ReturnsAsync(CreateGroupReservation());

        _reservationRepository
            .Setup(r => r.GetReservationByIdAsync(ReservationId))
            .ReturnsAsync((Reservation?)null);

        await Assert.ThrowsAsync<ReservationNotFoundException>(() =>
            _service.GetGroupSessionsAsync(OwnerUserId, GroupId));
    }

    // Checks that deleting a guest session removes it, returns the owner as Owner and skips a dish missing from the menu.
    [Fact]
    public async Task DeleteGuestSessionAsync_AfterDelete_ReturnsSnapshotWithOwnerTypeAndSkipsMissingDish()
    {
        SetupGroupAndReservation(customerId: OwnerUserId);

        GuestSession removedSession = CreateSession("guest-session-1", "Anna", isOwner: false);
        _guestSessionRepository
            .Setup(r => r.GetByGuestSessionId("guest-session-1"))
            .ReturnsAsync(removedSession);

        SetupSessions(
            CreateSession("owner-session", "Owner", isOwner: true, ("dish-a", 2)),
            CreateSession("guest-session-2", "Ivan", isOwner: false, ("removed-dish", 1)));
        SetupDishes(CreateDish("dish-a", 10m)); // "removed-dish" is not returned

        GetAllSessionsResponse result = await _service.DeleteGuestSessionAsync(OwnerUserId, GroupId, "guest-session-1");

        _guestSessionRepository.Verify(r => r.DeleteAsync(removedSession), Times.Once);

        var participants = result.Participants.ToList();
        Assert.Equal(ParticipantType.Owner, participants.Single(p => p.Id == "owner-session").Type);

        Participant guest = participants.Single(p => p.Id == "guest-session-2");
        Assert.Equal(ParticipantType.Guest, guest.Type);
        Assert.Empty(guest.Items);
        Assert.Equal(0m, guest.Total); // the removed dish price is not counted
        Assert.Equal(20m, result.Total);
    }

    // Checks that the owner session cannot be deleted and nothing is removed from the group.
    [Fact]
    public async Task DeleteGuestSessionAsync_OwnerSession_ThrowsValidationExceptionAndDeletesNothing()
    {
        SetupGroupAndReservation(customerId: OwnerUserId);

        _guestSessionRepository
            .Setup(r => r.GetByGuestSessionId("owner-session"))
            .ReturnsAsync(CreateSession("owner-session", "Owner", isOwner: true));

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.DeleteGuestSessionAsync(OwnerUserId, GroupId, "owner-session"));

        _guestSessionRepository.Verify(r => r.DeleteAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    // Checks that a deleted session still returned by the index read after delete is left out of the snapshot.
    [Fact]
    public async Task DeleteGuestSessionAsync_StaleIndexStillListsDeletedSession_SnapshotExcludesIt()
    {
        SetupGroupAndReservation(customerId: OwnerUserId);

        GuestSession deletedSession = CreateSession("guest-session-1", "Anna", isOwner: false, ("dish-a", 1));
        _guestSessionRepository
            .Setup(r => r.GetByGuestSessionId("guest-session-1"))
            .ReturnsAsync(deletedSession);

        // The index still returns the deleted session (eventual consistency).
        SetupSessions(
            CreateSession("owner-session", "Owner", isOwner: true, ("dish-a", 2)),
            deletedSession);
        SetupDishes(CreateDish("dish-a", 10m));

        GetAllSessionsResponse result = await _service.DeleteGuestSessionAsync(OwnerUserId, GroupId, "guest-session-1");

        Participant owner = Assert.Single(result.Participants);
        Assert.Equal("owner-session", owner.Id);
        Assert.Equal(20m, result.Total); // only the owner's 2 * 10
    }

    // Checks that a user who is not the reservation owner cannot delete a guest session.
    [Fact]
    public async Task DeleteGuestSessionAsync_NotReservationOwner_ThrowsActionForbiddenException()
    {
        SetupGroupAndReservation(customerId: "someone-else");

        await Assert.ThrowsAsync<ActionForbiddenException>(() =>
            _service.DeleteGuestSessionAsync(OwnerUserId, GroupId, "guest-session-1"));

        _guestSessionRepository.Verify(r => r.DeleteAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    private static GroupReservation CreateGroupReservation() => new()
    {
        GroupReservationId = GroupId,
        ReservationId = ReservationId,
        ReservationToken = "token",
        Status = GroupReservationStatus.Open,
    };

    private static Reservation CreateReservation(string customerId) => new()
    {
        Id = ReservationId,
        TableId = "table-1",
        WaiterId = "waiter-1",
        CustomerId = customerId,
        GuestCount = 4,
    };

    private static Dish CreateDish(string id, decimal price) => new()
    {
        Id = id,
        Name = $"Dish {id}",
        Description = string.Empty,
        Price = price,
        State = DishState.Available,
        LocationId = "location-1",
    };

    private static GuestSession CreateSession(string id, string guestName, bool isOwner, params (string DishId, int Quantity)[] dishes) => new()
    {
        Id = id,
        GuestName = guestName,
        GroupReservationId = GroupId,
        IsOwnerSession = isOwner,
        Dishes = dishes
            .Select(d => new OrderItem { DishId = d.DishId, Quantity = d.Quantity, State = OrderItemState.Pending })
            .ToList(),
    };

    // Sets up a valid group and reservation where the given customer is the reservation owner.
    private void SetupGroupAndReservation(string customerId)
    {
        _groupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(GroupId))
            .ReturnsAsync(CreateGroupReservation());

        _reservationRepository
            .Setup(r => r.GetReservationByIdAsync(ReservationId))
            .ReturnsAsync(CreateReservation(customerId));
    }

    private void SetupSessions(params GuestSession[] sessions)
    {
        _guestSessionRepository
            .Setup(r => r.GetAllByGroupReservationIdAsync(GroupId))
            .ReturnsAsync(sessions.ToList());
    }

    private void SetupDishes(params Dish[] dishes)
    {
        _dishRepository
            .Setup(r => r.GetDishesByIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(dishes);
    }
}
