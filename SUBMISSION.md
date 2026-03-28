# Booking Service — Submission Document

---

## 1. Project Overview

### What Was Built

A production-ready **Booking Service** microservice built with **ASP.NET Core 8**, demonstrating modern backend architecture patterns and enterprise-grade engineering practices.

### Core Capabilities

| Feature | Description |
|---------|-------------|
| **Booking Lifecycle** | Full CRUD with state transitions: Pending → Confirmed / Cancelled / Failed |
| **CQRS** | Command/Query separation via MediatR (4 commands, 2 queries) |
| **Transactional Outbox** | Domain events persisted atomically with business data, dispatched asynchronously |
| **Idempotency** | SHA256-based request deduplication with conflict detection for POST requests |
| **Multi-Database** | Pluggable SQL Server and PostgreSQL via EF Core providers |
| **Event-Driven Integration** | Azure Service Bus support with PaymentCompletedEvent consumer |
| **Security** | API Key authentication, comprehensive security headers middleware |
| **Observability** | Structured logging (Serilog), OpenTelemetry distributed tracing |
| **Containerization** | Multi-stage Docker builds, Docker Compose profiles for both databases |

### Architecture

```
┌──────────────────────────────────────────────────────────┐
│  BookingService.Api (Presentation Layer)                  │
│  - Controllers, Middleware, Authentication                │
├──────────────────────────────────────────────────────────┤
│  BookingService.Application (Business Logic)              │
│  - Commands, Queries, Validators, DTOs                    │
├──────────────────────────────────────────────────────────┤
│  BookingService.Domain (Core Domain)                      │
│  - Booking Entity, Domain Events, Value Objects           │
├──────────────────────────────────────────────────────────┤
│  BookingService.Infrastructure (Data & Integration)       │
│  - EF Core, Repositories, Outbox, Service Bus             │
└──────────────────────────────────────────────────────────┘
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/bookings` | List all bookings |
| `GET` | `/api/bookings/{id}` | Get booking by ID |
| `POST` | `/api/bookings` | Create a new booking (requires Idempotency-Key) |
| `PUT` | `/api/bookings/{id}/confirm` | Confirm a pending booking |
| `PUT` | `/api/bookings/{id}/cancel` | Cancel a booking |
| `PUT` | `/api/bookings/{id}/fail` | Mark booking as failed with reason |

### Design Patterns Applied

- **CQRS** (Command Query Responsibility Segregation)
- **Repository Pattern** (abstraction over data access)
- **Transactional Outbox Pattern** (guaranteed event delivery)
- **Idempotency Pattern** (safe retries)
- **Factory Pattern** (`Booking.Create()`)
- **Pipeline Behavior Pattern** (validation via MediatR)
- **Strategy Pattern** (pluggable database providers)
- **Compensation Pattern** (failed bookings auto-cancel)
- **Dependency Injection** (loose coupling)

### Testing

- **6 test classes** covering domain entities, command handlers, and validators
- **xUnit** test framework with **NSubstitute** mocking and **FluentAssertions**
- Tests verify state transitions, event publishing, and input validation

---

## 2. AI Usage Declaration

### 2.1 AI Tools Used

| Tool | Version / Model | Purpose |
|------|----------------|---------|
| **GitHub Copilot** | Latest (IDE integration) | Code autocompletion, inline suggestions during development |
| **ChatGPT** | GPT-4 | Architecture guidance, prompt-based code generation, design pattern advice |
| **Claude** (Anthropic) | Claude Sonnet | Code review, documentation generation, submission summary |

### 2.2 Sections That Were AI-Assisted

| Section | AI Involvement | Details |
|---------|---------------|---------|
| **Project Scaffolding** | AI-assisted | Initial project structure, solution file setup, and NuGet package selection were guided by AI prompts describing the requirements |
| **Code Generation** | AI-assisted | Core implementation files (controllers, command handlers, middleware, EF Core configurations) were generated from detailed prompts describing the booking service requirements |
| **Domain Model Design** | AI-assisted | Booking entity with state machine transitions, domain events, and factory pattern were generated based on CQRS/DDD architecture prompts |
| **Middleware Implementation** | AI-assisted | SecurityHeadersMiddleware, IdempotencyMiddleware, and ApiKeyAuthenticationHandler were generated from security requirement prompts |
| **Infrastructure Layer** | AI-assisted | Repository pattern, Outbox pattern, EF Core setup, and Service Bus integration were prompt-generated |
| **Unit Tests** | AI-assisted | Test classes for command handlers and domain entities were generated from prompts referencing the source code |
| **Docker Configuration** | AI-assisted | Dockerfile (multi-stage build) and docker-compose.yml (dual-profile) were generated from deployment requirement prompts |
| **README Documentation** | AI-assisted | Comprehensive README with setup instructions, API examples, and troubleshooting was AI-generated |

### 2.3 What Was Manually Validated

| Validation Area | What Was Checked | Status |
|----------------|-----------------|--------|
| **PR Code Review** | Reviewed all AI-generated code in pull request diffs for correctness, consistency, and adherence to .NET best practices | ✅ Verified |
| **Project Architecture** | Validated clean architecture layer separation (API → Application → Domain → Infrastructure), ensuring no circular dependencies | ✅ Verified |
| **Docker Execution** | Ran `docker compose --profile postgres up --build` and `docker compose --profile sqlserver up --build` to verify containerized deployment | ✅ Verified |
| **Unit Tests** | Executed `dotnet test BookingService.slnx` to confirm all tests pass (domain entity tests, command handler tests, validator tests) | ✅ Verified |
| **Build Verification** | Ran `dotnet build BookingService.slnx` to ensure clean compilation with no warnings or errors | ✅ Verified |
| **API Testing** | Manually tested all 6 API endpoints using cURL commands with API key authentication | ✅ Verified |
| **Prompt Refinement** | Iteratively adjusted AI prompts to improve output quality — corrected naming conventions, fixed configuration patterns, and ensured consistent coding style | ✅ Verified |
| **Security Review** | Verified API key authentication, security headers, and idempotency implementation for correctness | ✅ Verified |

### 2.4 AI Usage Breakdown (Estimated)

```
AI-Generated Code .............. ~70%
Manual Review & Validation ..... ~15%
Manual Adjustments & Fixes ..... ~10%
Manual Configuration ........... ~5%
```

---

## 3. How Preventing Blind AI Usage in the Backend Team

### The Problem

Blind AI usage occurs when developers accept AI-generated code without understanding it, leading to:
- **Security vulnerabilities** (e.g., SQL injection, missing auth checks)
- **Performance issues** (e.g., N+1 queries, unnecessary allocations)
- **Architectural drift** (inconsistent patterns across the codebase)
- **Technical debt** (code that nobody understands or can maintain)
- **Incorrect business logic** (AI hallucinations in domain rules)

### Recommended Strategies

#### 1. Establish a Code Review Gate for AI-Generated Code

- **Require PR reviews** for all code — human reviewers must verify AI output
- **Add an "AI-assisted" label** on PRs that contain AI-generated code
- Use a **checklist in PR templates** specifically for AI-generated code:
  - [ ] I understand every line of code in this PR
  - [ ] I have verified the business logic is correct
  - [ ] I have checked for security vulnerabilities
  - [ ] I have verified no sensitive data is exposed
  - [ ] I have tested edge cases not covered by AI

#### 2. Mandate Understanding Before Merging

- **"Explain Code" Rule**: Any developer must be able to explain what their code does line-by-line in a review session
- **Pair Programming Sessions**: When using AI tools, pair with another developer to discuss and validate output
- **Design Document Requirement**: For complex features, require a brief design doc *before* prompting AI — this ensures the developer understands the problem first

#### 3. Implement Automated Quality Gates

| Gate | Tool | Purpose |
|------|------|---------|
| **Static Analysis** | SonarQube, Roslyn Analyzers | Detect code smells, complexity, security issues |
| **Security Scanning** | CodeQL, Snyk, Dependabot | Find vulnerabilities in AI-generated code |
| **Test Coverage Threshold** | Coverlet (≥80%) | Ensure AI code is properly tested |
| **Architecture Tests** | ArchUnitNET | Enforce layer boundaries and dependency rules |
| **Linting** | .editorconfig, dotnet format | Maintain consistent code style |


#### 4. Monitor and Measure

- **Track AI Usage Metrics**: How often AI tools are used, rejection rates in reviews
- **Incident Postmortems**: If a bug reaches production, check if it was AI-generated
- **Quality Trend Analysis**: Compare defect density between AI-assisted and manually written code
- **Periodic Code Audits**: Randomly audit merged PRs for blind AI usage indicators

---

## 4. Project Statistics

| Metric | Value |
|--------|-------|
| Language | C# / .NET 8 |
| Projects | 4 source + 1 test |
| Source Files | ~35 |
| Source Lines | ~1,529 |
| Test Files | 6 |
| Test Lines | ~434 |
| API Endpoints | 6 |
| Design Patterns | 9+ |
| Docker Profiles | 2 (PostgreSQL, SQL Server) |
| NuGet Packages | ~20 |

---

