# Booking Service

Interview-ready starter for a booking microservice extraction.

## Included
- Controllers-based ASP.NET Core API
- Clean-ish layered structure (`Api` / `Application` / `Domain` / `Infrastructure`)
- EF Core persistence
- SQL Server or PostgreSQL configuration
- Idempotency middleware for `POST` requests via `Idempotency-Key`
- Outbox table + background dispatcher skeleton for Azure Service Bus

---

## Prerequisites

### Local development
- .NET 8 SDK
- Either:
  - PostgreSQL 16+, or
  - SQL Server 2022+

### Docker development
- Docker Desktop
- Docker Compose v2

---

## Project structure

```text
src/
  BookingService.Api/
  BookingService.Application/
  BookingService.Domain/
  BookingService.Infrastructure/
tests/
  BookingService.UnitTests/
```

---

## Configuration

Main app config lives in:
- `src/BookingService.Api/appsettings.json`
- `src/BookingService.Api/appsettings.Development.json`

Relevant settings:

```json
"Database": {
  "Provider": "sqlserver"
},
"ConnectionStrings": {
  "SqlServer": "Server=localhost,1433;Database=BookingService;User Id=sa;Password=Your_password123;TrustServerCertificate=True",
  "Postgres": "Host=localhost;Port=5432;Database=booking_service;Username=postgres;Password=postgres",
  "ServiceBus": ""
},
"ServiceBus": {
  "TopicName": "booking-events"
}
```

### Database provider switch
Use one of these values:
- `sqlserver`
- `postgres`

If `ConnectionStrings:ServiceBus` is empty, the outbox dispatcher stays in no-op mode and only logs skipped publishes.

---

## Run locally

### 1) Restore dependencies
```bash
cd booking-service
dotnet restore
```

### 2) Choose a database

#### Option A: PostgreSQL
Update `src/BookingService.Api/appsettings.json`:

```json
"Database": {
  "Provider": "postgres"
}
```

Make sure PostgreSQL is running and the connection string is valid.

#### Option B: SQL Server
Keep:

```json
"Database": {
  "Provider": "sqlserver"
}
```

Make sure SQL Server is running and the connection string is valid.

> Important: the default config is currently `sqlserver`, so `dotnet run` will fail at startup if SQL Server is not available.

### 3) Start the API
```bash
dotnet run --project src/BookingService.Api
```

### 4) Open Swagger
- <http://localhost:5193/swagger> or
- whatever local URL ASP.NET prints in the console

> Note: the app currently uses `EnsureCreated()` on startup instead of EF Core migrations.

---

## Run with Docker

This repo includes:
- `Dockerfile`
- `docker-compose.yml`
- `.dockerignore`

The API listens on port `8080` in the container.

### Option A: Docker + PostgreSQL
```bash
cd booking-service
docker compose --profile postgres up --build
```

Open:
- <http://localhost:8080/swagger>

This starts:
- `booking-api-postgres`
- `postgres`

The API will use:
- `Database__Provider=postgres`
- `ConnectionStrings__Postgres=Host=postgres;Port=5432;...`

### Option B: Docker + SQL Server
```bash
cd booking-service
docker compose --profile sqlserver up --build
```

Open:
- <http://localhost:8080/swagger>

This starts:
- `booking-api-sqlserver`
- `sqlserver`

The API will use:
- `Database__Provider=sqlserver`
- `ConnectionStrings__SqlServer=Server=sqlserver,1433;...`

### Run detached
```bash
docker compose --profile postgres up -d --build
```

### Stop containers
```bash
docker compose down
```

### Stop containers and remove volumes
```bash
docker compose down -v
```

---

## Override config with environment variables

Examples:

```bash
export DATABASE_PROVIDER=postgres
export SERVICEBUS_CONNECTION_STRING="<your-service-bus-connection-string>"
export SERVICEBUS_TOPIC_NAME="booking-events"
```

Then run:

```bash
docker compose --profile postgres up --build
```

---

## API endpoints
- `GET /api/bookings`
- `GET /api/bookings/{id}`
- `POST /api/bookings`
- `POST /api/bookings/{id}/confirm`
- `POST /api/bookings/{id}/cancel`

---

## Example create request

```http
POST /api/bookings
Idempotency-Key: booking-001
Content-Type: application/json

{
  "customerId": "CUST-001",
  "eventCode": "VN-HCM",
  "tripCode": "SGN-HAN"
}
```

Example with curl:

```bash
curl -X POST http://localhost:8080/api/bookings \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: booking-001" \
  -d '{
    "customerId": "CUST-001",
    "eventCode": "VN-HCM",
    "tripCode": "SGN-HAN"
  }'
```

---

## Notes and limitations
- Current persistence bootstrapping uses `EnsureCreated()` instead of migrations
- Idempotency implementation is basic and stores only successful responses
- Outbox dispatcher is a skeleton for Azure Service Bus publishing
- No production secrets strategy is included yet
- No container healthcheck for the API is defined yet

---

## Validation status

Checked in this workspace:
- `dotnet build BookingService.slnx` ✅
- `dotnet test BookingService.slnx --no-build` ✅
- `docker compose --profile postgres config` ✅
- `docker compose --profile sqlserver config` ✅

Current known limitation:
- Docker daemon was not running during validation, so containers were not actually started in this session.
- Local `dotnet run` failed with the default SQL Server configuration because no SQL Server instance was available.

---

## Troubleshooting

### `dotnet run` fails with SQL Server connection error
Cause:
- default provider is `sqlserver`
- app calls `EnsureCreated()` on startup
- no reachable SQL Server is running

Fix:
- start SQL Server locally, or
- switch provider to `postgres` and start PostgreSQL, or
- use Docker Compose with the matching profile once Docker daemon is running

### `docker compose up` fails before startup
Check:
- Docker Desktop is running
- `docker compose version` works
- daemon socket is available

### Swagger does not open
Check:
- API container or local app is actually running
- port `8080` is free for Docker mode
- local ASP.NET port matches the console output in local mode

---

## Suggested next upgrades
- replace `EnsureCreated()` with EF Core migrations
- add request validation + problem details
- add integration tests
- add retry/backoff + poison message handling for outbox dispatch
- add OpenTelemetry + correlation IDs
- add API health checks
