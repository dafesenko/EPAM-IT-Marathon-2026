# Data Model Notes

Summary of the DynamoDB entities in `dnapp/lambdas/api-handler/Entities/`. The authoritative diagrams are in `docs/assets/` (`ERDiagram`, `ReservationStateDiagram`, `OrderStateDiagram`).

## Main entities

### Reservation (table `Reservations`)
- Key: `id` (hash)
- GSIs: `GSI_ByTableId` (`tableId`), `GSI_UserId` (`customerId`), `GSI_WaiterId` (`waiterId`)
- Fields: `tableId`, `customerId` (nullable for walk-ins), `waiterId`, `createdByWaiter`, `startTime`, `endTime` (DateTime), `guestCount`, `status`
- `ReservationStatus` (stored as int): `Reserved = 0`, `Cancelled = 1`, `InProgress = 2`, `Completed = 3`

### Order (table `Orders`)
- Key: `orderId` (hash)
- GSIs: `GSI_CustomerId` (hash `customerId`, range `customerId` — see note), `GSI_ReservationId` (`reservationId`)
- Fields: `reservationAddress` and `reservationTime` are **denormalized copies** (so listing orders does not join with Reservations), `dishes` (list of `OrderItem`), `status` (`OrderStatus`, stored as int)
- `OrderStatus`: `Created`, `Submitted`, `Cancelled`, `SubmittedByCustomer` (the last one is set by group submit)
- Order state diagram: `docs/assets/OrderStateDiagram.png`

### GroupReservation (table `GroupReservations`)
- Key: `groupReservationId`
- Status (`GroupReservationStatus`): `Open`, `Submitted`, `Closed`
- Created per reservation; has an invite token

### Dish (table `Dishes`)
- Key: `id`
- Fields: `name`, `description`, `dishType` (`DishType`), `price` (decimal), `weight`, `calories`, `proteins`, `fats`, `carbohydrates`, `vitamins`, `imageUrl`, `state` (`DishState`), `locationId`
- Enum labels for display in `Constants/Dishes/*Labels.cs`; sorting options in `DishSortingLabels.cs`

### Location (table `Locations`)
- Key: `id`; fields: `address`, `description`, `totalCapacity`, `averageOccupancy`, `imageUrl`, `rating`

### Table (table `Tables`)
- Key: `id`; GSI on `locationId` (`tables-locationId-global-index`)
- Used by `GetAvailableTablesHandler` and time-slot logic

### WaiterList (table `WaiterList`)
- Maps waiter user → location; GSI `waitersGlobalIndex`

### GuestSession (table `GuestSessions`)
- Key: `guestSessionId`
- GSIs: `groupReservationId` (`GuestSession.GroupReservationId` index), `guestSessionTokenHash` (`GuestSession.SessionTokenHash` index)
- Fields: `groupReservationId`, `guestName`, `dishes` (list of `OrderItem`), `isOwnerSession`, `ownerUserId`, `isSubmitted`, `guestSessionTokenHash` (SHA-256 of the guest token; the raw token is never stored)
- Index names are defined in `Constants/GlobalIndexNames.cs`

### Feedback (table from `FEEDBACK_TABLE_NAME`)
- Per reservation; `type` is `Service` or `CuisineExperience` (`FeedbackType`); has `rating`, `comment`, `createdAt`; sorted via `SortingOrder`

### Notification (table `Notifications`)
- Waiter notifications; GSI `notificationsUserIdIndex`; marked read via `PATCH /notifications/{id}/read`

### ProfileImage (table `ProfileImages`)
- Points to an S3 object in the profile images bucket

### Reports (`Reports`, `ReportsHistory`)
- `Reports` stores generated daily aggregates (`DailyAggregate`)
- `ReportsHistory` stores per-user report history (`ReportHistoryBase`, `ReportHistoryLocation`, `ReportHistoryWaiter`); user lookup via `UserIdIndex`

## Shared value types

- `OrderItem` — dish reference + quantity + state (`OrderItemState`). Used inside `Order.dishes` and `GuestSession.dishes`.
- `ReservationContext` (in `Records/`) — combined reservation data used by services.

## Conventions

- Enums are stored as ints with `EnumAsIntConverter<T>` where marked; keep this when adding enum fields, or existing data breaks.
- Global secondary index names are defined in code (`GlobalIndexNames.cs`) and in `lambda_config.json`; keep both in sync.
- Denormalized fields (e.g. `reservationAddress` on Order) must be updated when the source changes — check the services before changing the source fields.
