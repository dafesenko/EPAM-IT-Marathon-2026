# Homework Notes

Source brief: `marathon2026/homework-backend.md` (Ukrainian). Analysis of the code against it: `marathon2026/docs/backend-analysis.md`.
Project-level rules: `marathon2026/CLAUDE.md`.

## Current status

Brief received and read. **No code written yet** — implementation has not started.

## Levels

### Level 1 — list group participants (not implemented)
- `GET /guest/groups/{groupReservationId}/sessions`, owner only.
- Returns `participants` (id, type `Guest`, name, items with dish id/name/price and quantity, `total`, `isSubmitted`) and the group `total`.
- Analysis: the snapshot logic already exists in `GuestSessionService.DeleteGuestSessionAsync`; extract it into a shared method and reuse it.

### Level 2 — owner session id and consolidated submit (not implemented)
- `GET /reservations/{reservationId}/guest-session-id` → `{ "guestSessionId": ... }` for the owner's session.
  - Analysis: `GuestSessionRepository.GetByGroupReservationId` returns the first record, not the owner's. Filter by `IsOwnerSession && OwnerUserId == userId`.
- `POST /group-reservations/{groupReservationId}/submit`, body `{ "reservationId": ... }`, returns `[{ "dishId", "quantity" }]` merged across submitted sessions.
  - Only submitted guest sessions are used; dishes merged by `DishId`, quantities summed.
  - Order status → `SubmittedByCustomer`; group status → `Submitted`.
  - Plan from analysis: check owner → check group status (`Submitted`/`Closed` rejected) → take submitted sessions (none → 400) → merge dishes (none → 400) → find active order (none → 400) → replace order items, set status, save → set group `Submitted`, save → return list.
  - Risk: two separate writes can leave data inconsistent; DynamoDB `TransactWriteItems` is the proposed fix.

### Level 3 — delete guest session from group (implemented, with known issues)
- `DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId}`, owner only.
- Returns the refreshed participants snapshot and new total.
- Codes: 400 if session does not belong to this group, 404 if not found, 403/401 per the project rules.
- Known issues from analysis (see below): owner session can be deleted; snapshot can be stale due to GSI eventual consistency.

## Project rules that apply to this homework (from `marathon2026/CLAUDE.md`)

- One endpoint at a time: code first, then reviewer agent check, then build.
- Do not edit `deployment_resources.json` by hand (Syndicate generates it).
- Never commit AWS keys, tokens or secrets.
- Ignore `syndicate_venv/`, `.terraform/`, `.idea/` when searching.
- Error codes: 404 not found, 400 invalid data or state, 403 forbidden for others' actions, 401 missing or invalid token.
- Build after changes: `dotnet build backend/restaurant-backend-app/restaurant-backend-app.sln` (from the course root).

## Known problems to keep in mind (from `docs/backend-analysis.md`)

1. Owner is counted twice in the guest limit (`CountByGroupReservationId` includes the owner, then +1 is added). Affects `GuestSessionService.CreateGuestSessionAsync` and `GetCurrentParticipantCountAsync`.
2. Owner session can be deleted — `DeleteGuestSessionAsync` does not check `IsOwnerSession`.
3. Stale snapshot after delete (GSI eventual consistency).
4. Malformed token causes 500 instead of 401 — `IsAuthorizedAsync` calls `Convert.FromBase64String` without validation.
5. Race condition on the guest limit (check-then-save is not atomic).
6. Submit writes order and group separately.
7. Minor: owner name placeholder `"Owner"` in snapshot; `dishesById[dishId]` throws if a dish was removed from the menu; `GetGroupReservationStatusHandler` leaks exception text in 500; `OrderRepository.GetOrderByReservationIdAsync` reads only the first page.

## Open items

- [ ] Level 1: `GET /guest/groups/{id}/sessions`
- [ ] Level 2a: `GET /reservations/{id}/guest-session-id`
- [ ] Level 2b: `POST /group-reservations/{id}/submit`
- [ ] Level 3: review existing `DELETE` and fix issues 2 and 3 (owner delete, stale snapshot)
- [ ] Decide whether to fix analysis items 1, 4–7 (ask the user first; they are not in the brief)
- [ ] Update `docs/ApiRoutes.md` for each new endpoint
- [ ] Add tests in `Tests/Services/` for changed service logic
- [ ] Run `dotnet build` after each endpoint
