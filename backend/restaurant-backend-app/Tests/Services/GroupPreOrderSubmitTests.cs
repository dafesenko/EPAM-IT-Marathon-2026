using APIHandler.Constants;
using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Models.Orders;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using APIHandler.Repositories.Tables;
using APIHandler.Services.Auth;
using APIHandler.Services.GroupReservations;
using APIHandler.Services.Locations;
using Moq;

namespace APIHandler.Tests.Services;

public class GroupPreOrderSubmitTests
{
    private const string OwnerId = "owner-1";
    private const string GroupReservationId = "group-1";
    private const string ReservationId = "reservation-123";

    private readonly Mock<IGroupReservationRepository> _groupReservationRepositoryMock = new();
    private readonly Mock<ITableRepository> _tableRepositoryMock = new();
    private readonly Mock<IReservationRepository> _reservationRepositoryMock = new();
    private readonly Mock<IGuestSessionRepository> _guestSessionRepositoryMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<ILocationService> _locationServiceMock = new();
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();

    private readonly GroupReservationService _service;

    public GroupPreOrderSubmitTests()
    {
        _service = new GroupReservationService(
            _groupReservationRepositoryMock.Object,
            _tableRepositoryMock.Object,
            _reservationRepositoryMock.Object,
            _guestSessionRepositoryMock.Object,
            _userServiceMock.Object,
            _locationServiceMock.Object,
            _orderRepositoryMock.Object);
    }

    // Happy path: merges submitted sessions, updates the order and group, and saves both in one call.
    [Fact]
    public async Task SubmitGroupPreOrderAsync_ValidOwnerAndSubmittedSessions_MergesDishesAndSavesOnce()
    {
        var group = CreateGroup();
        var reservation = CreateReservation(OwnerId);
        var order = CreateOrder(
            new OrderItem { DishId = "dish1", Quantity = 1, State = OrderItemState.Pending });

        SetupGroupReservationAndReservation(group, reservation);
        _orderRepositoryMock
            .Setup(r => r.GetOrderByReservationIdAsync(ReservationId))
            .ReturnsAsync(order);
        _guestSessionRepositoryMock
            .Setup(r => r.GetAllByGroupReservationIdAsync(GroupReservationId))
            .ReturnsAsync(
            [
                CreateSession(isSubmitted: true,
                    new OrderItem { DishId = "dish1", Quantity = 2 },
                    new OrderItem { DishId = "dish2", Quantity = 1 }),
                CreateSession(isSubmitted: true,
                    new OrderItem { DishId = "dish1", Quantity = 1 },
                    new OrderItem { DishId = "dish2", Quantity = 1 }),
                // Not submitted, so its dish must not be included.
                CreateSession(isSubmitted: false,
                    new OrderItem { DishId = "dish3", Quantity = 5 }),
            ]);

        List<DishQuantityResponse> result = await _service.SubmitGroupPreOrderAsync(OwnerId, GroupReservationId, ReservationId);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.DishId == "dish1" && d.Quantity == 3);
        Assert.Contains(result, d => d.DishId == "dish2" && d.Quantity == 2);

        // Existing dish1 quantity (1) plus merged quantity (3) gives 4; dish2 is added as a new item.
        Assert.Equal(4, order.Dishes.Single(d => d.DishId == "dish1").Quantity);
        Assert.Equal(2, order.Dishes.Single(d => d.DishId == "dish2").Quantity);
        Assert.Equal(OrderStatus.SubmittedByCustomer, order.Status);
        Assert.Equal(GroupReservationStatus.Submitted, group.Status);

        _orderRepositoryMock.Verify(r => r.SaveOrderAndGroupReservationAsync(order, group), Times.Once);
    }

    // Security: a user who does not own the reservation gets 403 and nothing is saved.
    [Fact]
    public async Task SubmitGroupPreOrderAsync_UserIsNotOwner_ThrowsActionForbiddenAndSavesNothing()
    {
        var group = CreateGroup();
        var reservation = CreateReservation("someone-else");

        SetupGroupReservationAndReservation(group, reservation);

        await Assert.ThrowsAsync<ActionForbiddenException>(() =>
            _service.SubmitGroupPreOrderAsync(OwnerId, GroupReservationId, ReservationId));

        _orderRepositoryMock.Verify(
            r => r.SaveOrderAndGroupReservationAsync(It.IsAny<Order>(), It.IsAny<GroupReservation>()),
            Times.Never);
    }

    // Unknown group reservation id gives GroupReservationNotFoundException (404 in the handler).
    [Fact]
    public async Task SubmitGroupPreOrderAsync_GroupNotFound_ThrowsGroupReservationNotFoundException()
    {
        _groupReservationRepositoryMock
            .Setup(r => r.GetGroupReservationByIdAsync(GroupReservationId))
            .ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() =>
            _service.SubmitGroupPreOrderAsync(OwnerId, GroupReservationId, ReservationId));
    }

    // No guest session has been submitted yet, so there is nothing to put into the order.
    [Fact]
    public async Task SubmitGroupPreOrderAsync_NoSubmittedSessions_ThrowsValidationException()
    {
        var group = CreateGroup();
        SetupGroupReservationAndReservation(group, CreateReservation(OwnerId));
        _guestSessionRepositoryMock
            .Setup(r => r.GetAllByGroupReservationIdAsync(GroupReservationId))
            .ReturnsAsync(
            [
                CreateSession(isSubmitted: false,
                    new OrderItem { DishId = "dish1", Quantity = 1 }),
            ]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.SubmitGroupPreOrderAsync(OwnerId, GroupReservationId, ReservationId));

        _orderRepositoryMock.Verify(
            r => r.SaveOrderAndGroupReservationAsync(It.IsAny<Order>(), It.IsAny<GroupReservation>()),
            Times.Never);
    }

    // Submitted sessions exist but contain no dishes, so the merged list is empty.
    [Fact]
    public async Task SubmitGroupPreOrderAsync_MergedListIsEmpty_ThrowsValidationException()
    {
        var group = CreateGroup();
        SetupGroupReservationAndReservation(group, CreateReservation(OwnerId));
        _guestSessionRepositoryMock
            .Setup(r => r.GetAllByGroupReservationIdAsync(GroupReservationId))
            .ReturnsAsync(
            [
                CreateSession(isSubmitted: true),
            ]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.SubmitGroupPreOrderAsync(OwnerId, GroupReservationId, ReservationId));

        _orderRepositoryMock.Verify(
            r => r.SaveOrderAndGroupReservationAsync(It.IsAny<Order>(), It.IsAny<GroupReservation>()),
            Times.Never);
    }

    // The reservation has no active order yet, so the submit is rejected with a validation error (400).
    [Fact]
    public async Task SubmitGroupPreOrderAsync_NoActiveOrder_ThrowsValidationException()
    {
        var group = CreateGroup();
        SetupGroupReservationAndReservation(group, CreateReservation(OwnerId));
        _guestSessionRepositoryMock
            .Setup(r => r.GetAllByGroupReservationIdAsync(GroupReservationId))
            .ReturnsAsync(
            [
                CreateSession(isSubmitted: true,
                    new OrderItem { DishId = "dish1", Quantity = 1 }),
            ]);
        _orderRepositoryMock
            .Setup(r => r.GetOrderByReservationIdAsync(ReservationId))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.SubmitGroupPreOrderAsync(OwnerId, GroupReservationId, ReservationId));

        _orderRepositoryMock.Verify(
            r => r.SaveOrderAndGroupReservationAsync(It.IsAny<Order>(), It.IsAny<GroupReservation>()),
            Times.Never);
    }

    private static GroupReservation CreateGroup() => new()
    {
        GroupReservationId = GroupReservationId,
        ReservationId = ReservationId,
        ReservationToken = "token-1",
        Status = GroupReservationStatus.Open,
    };

    private static Reservation CreateReservation(string customerId) => new()
    {
        Id = ReservationId,
        TableId = "table-1",
        WaiterId = "waiter-1",
        CustomerId = customerId,
        Status = ReservationStatus.Reserved,
    };

    private static Order CreateOrder(params OrderItem[] items) => new()
    {
        OrderId = "order-1",
        CustomerId = OwnerId,
        ReservationId = ReservationId,
        ReservationAddress = "Some street 1, Table 1",
        ReservationTime = "Oct 30, 2026, 7:00 p.m.",
        Dishes = items.ToList(),
        Status = OrderStatus.Created,
    };

    private static GuestSession CreateSession(bool isSubmitted, params OrderItem[] dishes) => new()
    {
        Id = Guid.NewGuid().ToString(),
        GuestName = "Guest",
        GroupReservationId = GroupReservationId,
        Dishes = dishes.ToList(),
        IsSubmitted = isSubmitted,
    };

    private void SetupGroupReservationAndReservation(GroupReservation group, Reservation reservation)
    {
        _groupReservationRepositoryMock
            .Setup(r => r.GetGroupReservationByIdAsync(GroupReservationId))
            .ReturnsAsync(group);

        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(ReservationId))
            .ReturnsAsync(reservation);
    }
}
