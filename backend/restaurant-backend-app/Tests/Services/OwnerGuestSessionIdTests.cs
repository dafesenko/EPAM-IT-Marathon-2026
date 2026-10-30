using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Dishes;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Reservations;
using APIHandler.Repositories.Tables;
using APIHandler.Services.GuestSessions;
using Moq;

namespace APIHandler.Tests.Services;

public class OwnerGuestSessionIdTests
{
    private const string OwnerUserId = "owner-user";
    private const string GroupId = "group-1";
    private const string ReservationId = "reservation-1";

    private readonly Mock<IGuestSessionRepository> _guestSessionRepository = new();
    private readonly Mock<IGroupReservationRepository> _groupReservationRepository = new();
    private readonly Mock<IReservationRepository> _reservationRepository = new();
    private readonly GuestSessionService _service;

    public OwnerGuestSessionIdTests()
    {
        _service = new GuestSessionService(
            _groupReservationRepository.Object,
            _guestSessionRepository.Object,
            new Mock<IDishRepository>().Object,
            _reservationRepository.Object,
            new Mock<IUserRepository>().Object,
            new Mock<ITableRepository>().Object);
    }

    // Checks that the owner gets the id of the owner session and not the id of a guest session.
    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_OwnerRequests_ReturnsOwnerSessionId()
    {
        SetupGroupAndReservation(customerId: OwnerUserId);
        SetupSessions(
            CreateSession("guest-session-1", isOwner: false, ownerUserId: null),
            CreateSession("owner-session", isOwner: true, ownerUserId: OwnerUserId));

        string result = await _service.GetOwnerGuestSessionIdAsync(OwnerUserId, ReservationId);

        Assert.Equal("owner-session", result);
    }

    // Checks that a user who is not the reservation customer gets ActionForbiddenException.
    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_NotReservationOwner_ThrowsActionForbiddenException()
    {
        SetupGroupAndReservation(customerId: "someone-else");

        await Assert.ThrowsAsync<ActionForbiddenException>(() =>
            _service.GetOwnerGuestSessionIdAsync(OwnerUserId, ReservationId));
    }

    // Checks that a group with no owner session gives GuestSessionNotFoundException.
    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_NoOwnerSessionInGroup_ThrowsGuestSessionNotFoundException()
    {
        SetupGroupAndReservation(customerId: OwnerUserId);
        SetupSessions(CreateSession("guest-session-1", isOwner: false, ownerUserId: null));

        await Assert.ThrowsAsync<GuestSessionNotFoundException>(() =>
            _service.GetOwnerGuestSessionIdAsync(OwnerUserId, ReservationId));
    }

    // Checks that a reservation without a group gives GroupReservationNotFoundException.
    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_GroupReservationNotFound_ThrowsGroupReservationNotFoundException()
    {
        _groupReservationRepository
            .Setup(r => r.GetByReservationIdAsync(ReservationId))
            .ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() =>
            _service.GetOwnerGuestSessionIdAsync(OwnerUserId, ReservationId));
    }

    // Sets up a group for the reservation and a reservation where the given customer is the owner.
    private void SetupGroupAndReservation(string customerId)
    {
        _groupReservationRepository
            .Setup(r => r.GetByReservationIdAsync(ReservationId))
            .ReturnsAsync(new GroupReservation
            {
                GroupReservationId = GroupId,
                ReservationId = ReservationId,
                ReservationToken = "token",
                Status = GroupReservationStatus.Open,
            });

        _reservationRepository
            .Setup(r => r.GetReservationByIdAsync(ReservationId))
            .ReturnsAsync(new Reservation
            {
                Id = ReservationId,
                TableId = "table-1",
                WaiterId = "waiter-1",
                CustomerId = customerId,
                GuestCount = 4,
            });
    }

    private void SetupSessions(params GuestSession[] sessions)
    {
        _guestSessionRepository
            .Setup(r => r.GetAllByGroupReservationIdAsync(GroupId))
            .ReturnsAsync(sessions.ToList());
    }

    private static GuestSession CreateSession(string id, bool isOwner, string? ownerUserId) => new()
    {
        Id = id,
        GuestName = isOwner ? "Owner" : "Anna",
        GroupReservationId = GroupId,
        IsOwnerSession = isOwner,
        OwnerUserId = ownerUserId,
        Dishes = [],
    };
}
