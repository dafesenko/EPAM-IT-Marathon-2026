# it marathon: backend (restaurant)

## Що це
Serverless бекенд ресторану: .NET 10 Lambda, API Gateway, DynamoDB, Cognito, S3.
Застосунок: backend/restaurant-backend-app.

## Карта коду
- Точка входу: backend/restaurant-backend-app/dnapp/lambdas/api-handler/Function.cs
- Маршрути: .../api-handler/Routing/RouteRegistry.cs (перший збіг перемагає, специфічні маршрути вище за загальні)
- Шари: Handlers → Services → Repositories → Entities
- Таблиці і змінні середовища: .../api-handler/lambda_config.json
- Документація API: backend/restaurant-backend-app/docs/ApiRoutes.md

## Поточне завдання
Див. homework-backend.md (корінь репозиторію), рівні 1–3:
1. GET /guest/groups/{groupReservationId}/sessions
2. GET /reservations/{reservationId}/guest-session-id і POST /group-reservations/{groupReservationId}/submit
3. DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId} (вже реалізовано, є дві проблеми, див. docs/backend-analysis.md)

## Правила
- Один endpoint за раз. Спершу код, потім перевірка reviewer-агентом, потім збірка.
- Не змінювати deployment_resources.json вручну (файл генерується Syndicate).
- Не комітити ключі AWS, токени і секрети.
- Ігнорувати при пошуку і читанні: syndicate_venv/, .terraform/, .idea/.
- Коди помилок: 404 для «не знайдено», 400 для невалідних даних і станів, 403 для чужих дій, 401 для відсутнього або невалідного токена.
- Після змін запускати збірку: dotnet build backend/restaurant-backend-app/restaurant-backend-app.sln

## Відомі проблеми
Повний список з описом: docs/backend-analysis.md (корінь репозиторію).
