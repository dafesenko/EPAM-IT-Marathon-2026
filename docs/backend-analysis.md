# Аналіз бекенду: ресторан (serverless, .NET 10)

Цей файл — довідка для розробки і для агентів. Шлях до коду: backend/restaurant-backend-app/dnapp/lambdas/api-handler/ (далі — `api-handler/`).

## 1. Архітектура

- **API Gateway** приймає HTTP і передає запит у Lambda `api-handler`.
- **Lambda `api-handler`**: один обробник для всіх REST-endpoint-ів.
  - `Function.cs` → `FunctionHandler`: єдина точка входу. Нормалізує метод і шлях, відповідає на OPTIONS, шукає маршрут, ловить винятки (необроблене → 500).
  - `Routing/RouteRegistry.cs`: таблиця всіх маршрутів і DI-реєстрація. Шаблони `{param}` перетворюються на regex.
- **Шари**: `Handlers/` (HTTP) → `Services/` (бізнес-правила) → `Repositories/` (DynamoDB) → `Entities/` (форма запису в базі).
- **DynamoDB**: окремі таблиці (групові бронювання, гості, замовлення, бронювання тощо). Назви таблиць — у `lambda_config.json`.
- **Cognito**: користувачі і JWT. API Gateway передає `sub` і `cognito:groups` у `RequestContext.Authorizer.Claims`. Handlers читають їх звідти.
- **Гостьова авторизація**: гість отримує випадковий токен. У базі зберігається лише SHA-256 від нього (`SessionTokenHash`). `GuestAuthorizationService` перевіряє заголовок `Authorization: Bearer ...`.
- **Інші Lambda** (`reports-handler`, `report-sender`): асинхронний пайплайн звітів, пов'язаний через SQS. Детально не аналізувався.
- **Деплой**: AWS Syndicate (`syndicate build`, `syndicate deploy`). Ресурси (Cognito, S3, SQS, IAM) — у `infra/terraform/`.

## 2. Модель даних

- `Reservation` — бронювання столика клієнтом (кількість гостей, час).
- `GroupReservation` — створюється для кожного бронювання. Має `ReservationToken` для запрошення і статус `Open` → `Submitted` або `Closed`.
- `GuestSession` — учасник групи. Owner має `IsOwnerSession = true` і `OwnerUserId`. Гості мають `GroupReservationId` (GSI) і `IsSubmitted`.
- `Order` — попереднє замовлення бронювання. Статуси: `Created`, `Submitted`, `SubmittedByCustomer`, `Cancelled`. Позиції — `OrderItem` (dishId, quantity, state).

## 3. Стан домашки (homework-backend.md)

| Рівень | Endpoint | Стан |
|---|---|---|
| 1 | GET /guest/groups/{id}/sessions | Не реалізовано. Логіка побудови снапшоту вже є в `GuestSessionService.DeleteGuestSessionAsync`. Потрібно винести її в спільний метод. |
| 2a | GET /reservations/{id}/guest-session-id | Не реалізовано. `GuestSessionRepository.GetByGroupReservationId` повертає перший запис, а не сесію власника. Потрібен фільтр `IsOwnerSession && OwnerUserId == userId`. |
| 2b | POST /group-reservations/{id}/submit | Не реалізовано. Статус `SubmittedByCustomer` уже використовується в `OrderService.SubmitPreOrderAsync`. Злиття страв по `DishId` — новий код. |
| 3 | DELETE /guest/groups/{gid}/sessions/{sid} | Реалізовано. Маршрут, перевірка власника, коди 404/400, відповідь зі снапшотом. Є проблеми 2 і 3 нижче. |

### План для рівня 2b (submit)
1. Перевірити власника групи (`ValidateReservationOwnerAsync`).
2. Перевірити статус групи: `Submitted` і `Closed` — помилка.
3. Взяти сесії з `IsSubmitted = true`. Якщо їх немає — 400.
4. Зібрати страви з усіх сесій, злити по `DishId`, сумувати `Quantity`. Якщо список порожній — 400.
5. Знайти активне замовлення бронювання. Якщо немає — 400.
6. Замінити позиції замовлення, виставити `SubmittedByCustomer`, зберегти.
7. Виставити групі `Submitted`, зберегти.
8. Повернути список `[{dishId, quantity}]`.

## 4. Знайдені проблеми

1. **Owner рахується двічі в ліміті гостей.** `CountByGroupReservationId` рахує всі сесії групи, включно з owner-сесією. Далі додається `+1` за owner. Місця: `GuestSessionService.CreateGuestSessionAsync`, `GetCurrentParticipantCountAsync`. Наслідок: гості блокуються на одне місце раніше.
2. **Owner-сесію можна видалити.** `DeleteGuestSessionAsync` не перевіряє `IsOwnerSession`. Треба відхиляти видалення з 403 або 400.
3. **Застарілий снапшот після видалення.** Снапшот читається через GSI одразу після запису. GSI працюють з eventual consistency, тому видалений гість може ще з'явитися у відповіді.
4. **Некоректний токен дає 500, а не 401.** `IsAuthorizedAsync` викликає `Convert.FromBase64String` без перевірки, і на сміття кидає виняток.
5. **Гонка на ліміті гостей.** Перевірка «порахувати, потім зберегти» не атомарна. Два гості, що приєднуються одночасно, можуть обидва пройти перевірку.
6. **Submit — два окремих записи.** Замовлення і група зберігаються послідовно. Збій між ними залишить дані неузгодженими. Варіант: `TransactWriteItems` у DynamoDB.
7. **Дрібниці:**
   - ім'я owner у снапшоті — `"Owner"` (справжнє ім'я з Cognito береться лише в `GetGuestSessionInfoAsync`);
   - `dishesById[d.DishId]` кидає виняток, якщо страву видалили з меню;
   - `GetGroupReservationStatusHandler` повертає текст винятку в 500-й відповіді;
   - `OrderRepository.GetOrderByReservationIdAsync` читає лише першу сторінку результатів.

## 5. Як запустити і перевірити

- Локальне налаштування: `backend/restaurant-backend-app/local-setup-guide.md`.
- Збірка: `dotnet build backend/restaurant-backend-app/restaurant-backend-app.sln`.
- Опис API: `backend/restaurant-backend-app/docs/ApiRoutes.md`, Swagger у `swagger_src/`.
- Тести: `Tests/` (поки що мінімальні).
