# Notification Sender — Specification

This document is a developer-facing specification describing the implementation details required to build the first version of the Notification Sender service. It expands the brainstorming document into concrete interfaces, schemas, API contracts, persistence model, DI configuration, and testing guidance. Use .NET 8 (C#), EF Core with SQLite, and ASP.NET Core MVC / Razor for the Admin UI.

Table of contents
- Overview
- Project structure
- Persistence (EF Core) models & DbContext
- Domain objects and DTOs
- Messaging layer: interfaces and senders
- Business logic layer (services)
- API: controllers and routes
- AdminView integration points
- Background processing and retries
- Configuration and secrets
- Logging and observability
- Testing plan
- Deployment / local run notes

Overview
- Purpose: implement a simple, extensible notification service that can route events to users via configured channels (Email, Slack). This first version uses a subscription mapping (UsersChannels) to determine which channels each user receives.
- Assumptions: no per-user stored alerts/criteria in first version; events are generic payloads routed to subscribed users.

Project structure (recommended)
- src/
  - Sonrisa.Notifier.Host (ASP.NET Core Web Host - startup project containing Program.cs and appsettings.json)
  - Sonrisa.Notifier.Api (class library containing API controllers, DTOs and viewmodels - no Program.cs; the Host project is the startup)
  - Sonrisa.Notifier.Admin (Razor MVC / Razor Pages project - frontend/Admin UI: views, viewmodels, static assets)
  - Sonrisa.Notifier.Core (domain models, interfaces, shared DTOs)
  - Sonrisa.Notifier.Infrastructure (EF Core DbContext, repositories, concrete senders)
  - Sonrisa.Notifier.Worker (optional background worker for retries or queued sending)
  - Sonrisa.Notifier.Tests (unit & integration tests - MSTest in the current skeleton)

Persistence (EF Core) models & DbContext
- Use Microsoft.EntityFrameworkCore.Sqlite
- Entities
  - User
	- Id Guid (PK)
	- Email string (nullable=false)
	- DisplayName string
	- Timezone string (IANA tz optional)
	- IsActive bool
	- CreatedAt DateTimeOffset
	- UpdatedAt DateTimeOffset?

  - Channel
	- Id Guid (PK)
	- Type string ("email","slack", etc.)
	- ConfigJson string (store JSON config for provider: smtp settings, webhook URL, slack channel id, display name)
	- IsActive bool
	- CreatedAt DateTimeOffset
	- UpdatedAt DateTimeOffset?

  - UsersChannels
	- UserId Guid (FK -> User.Id) (PK composite)
	- ChannelId Guid (FK -> Channel.Id) (PK composite)

	- Note: This first version does not include a DeliveryLog table. Delivery tracking can be added later if needed.

- DbContext
  - SonrisaNotifierDbContext : DbContext
	- DbSets: Users, Channels, UsersChannels
  - Configure composite key on UsersChannels

  - Runtime DB file location & seeding
	- For developer convenience the SQLite file may live inside the Infrastructure project (recommended path: src/Sonrisa.Notifier.Infrastructure/Data/sonrisa_notifier.db).
	- Keep a human-readable seed SQL (e.g., seed.sql or seed.sql.txt) in the same folder as the source of truth. The Host can execute this seed file on first run to create the .db file automatically.
	- Prefer committing the seed SQL. Optionally commit the generated binary .db for quick start, but document how to regenerate it from the seed SQL to avoid drift.
	- Mark Data files as content/none in the Infrastructure project so IDE/build tooling does not attempt to parse SQL during compilation.

Domain objects and DTOs
- Domain objects (Core project)
  - OutgoingMessage
	- Guid MessageId
	- Guid UserId
	- string Title
	- string Body
	- string Priority (low|medium|high)
	- List<string> Channels (target channel types like "email","slack")
	- Dictionary<string, string> Metadata
	- DateTimeOffset CreatedAt

  - DeliveryResult
	- bool Success
	- string Status (Sent, Failed)
	- string ProviderMessageId
	- string ErrorMessage

- DTOs (Api layer)
  - TestEventRequest
	- string Title
	- string Body
	- string Priority
	- Dictionary<string, string> Metadata
	- List<Guid>? UserIds (optional) — if provided, send only to those users; otherwise send to all subscribers

  - UserDto / CreateUserDto / UpdateUserDto
  - ChannelDto / CreateChannelDto / UpdateChannelDto
  - UsersChannelDto (UserId, ChannelId)
	- (No DeliveryLog DTO in first version)

Messaging layer: interfaces and senders
- Core interfaces
  - interface INotificationSender
	- Task<DeliveryResult> SendAsync(OutgoingMessage message, CancellationToken ct = default);

  - interface INotificationDispatcher
	- Task DispatchAsync(OutgoingMessage message, CancellationToken ct = default);
	- Responsibility: orchestrate which channel sender(s) to call for the message and aggregate DeliveryResult(s). For simple version it resolves senders from DI by channel type.

- Implementations (Infrastructure)
  - EmailNotificationSender : INotificationSender
	- Reads channel.ConfigJson for SMTP or provider info (or fallback to global SMTP in appsettings)
	- Supports plain text and HTML bodies
	- Returns DeliveryResult with ProviderMessageId if provider returns one

  - SlackNotificationSender : INotificationSender
	- Uses webhook URL or token from channel.ConfigJson
	- Formats message text and optionally blocks

- Registration
  - Register all INotificationSender implementations in DI (as transient or singleton depending on dependencies). Also register a mapping provider from channel type -> sender.

Business logic layer (services)
- Interfaces
  - interface ISubscriptionService
	- Task<List<Channel>> GetChannelsForUsersAsync(List<Guid> userIds);
	- Task<List<Guid>> GetSubscribedUserIdsAsync(); // or by filter

  - interface IEventHandlerService
	- Task HandleEventAsync(TestEventRequest request, CancellationToken ct = default);
	- Implementation responsibilities:
	  - Resolve recipient user ids (either provided in request.UserIds or all users in UsersChannels)
	  - For each user, resolve subscribed channels via UsersChannels
	  - Build OutgoingMessage objects for each user with appropriate channel list
		- Pass OutgoingMessage(s) to INotificationDispatcher.DispatchAsync

- Throttling / deduplication (optional minimal behavior)
  - Simple in-memory per-user sliding window (Dictionary<UserId, DateTimeOffset lastSent>) configurable by TTL to prevent spamming while testing.

API: controllers and routes
- UsersController
  - GET /api/users
  - GET /api/users/{id}
  - POST /api/users
  - PUT /api/users/{id}
  - DELETE /api/users/{id}

- ChannelsController
  - GET /api/channels
  - GET /api/channels/{id}
  - POST /api/channels
  - PUT /api/channels/{id}
  - DELETE /api/channels/{id}

- UsersChannelsController
  - GET /api/userschannels?userId={userId}
  - POST /api/userschannels  (body: UsersChannelDto)
  - DELETE /api/userschannels?userId={userId}&channelId={channelId}

- TestEventsController (test only)
  - POST /api/test/event
	- Accept TestEventRequest
	- Call IEventHandlerService.HandleEventAsync
	- Return 202 Accepted (or 400 for validation errors)

Admin UI (separate project) integration points
- The Admin UI lives in a separate project (Sonrisa.Notifier.Admin). It is responsible only for rendering views, viewmodels, and static assets.
- The Admin project is a consumer of the API and should interact with Sonrisa.Notifier.Api over HTTP using the documented endpoints. Alternatives:
  - Server-side integration: if you deploy Admin and Api together, Admin can call Api services directly by referencing Core and Infrastructure, but this couples projects and is not recommended for the first version.
  - Recommended (decoupled): Admin calls Api endpoints (HttpClient) and renders results. This makes the UI and backend independently deployable and keeps the Api project focused on backend concerns.

What belongs in each project
- Sonrisa.Notifier.Host
  - ASP.NET Core Web Host (Program.cs) and configuration files (appsettings.json)
  - DI registration and application startup (register DbContext, services, senders, logging, etc.)

- Sonrisa.Notifier.Api
  - API controllers (UsersController, ChannelsController, UsersChannelsController, TestEventsController)
  - DTOs used by the API
  - No Program.cs in this project: controllers and API surface are implemented here and wired up by the Host project

- Sonrisa.Notifier.Admin
  - Razor views/pages, view models specific to UI, client-side assets (css/js)
  - UI controllers that return views and call the backend Api via HttpClient
  - Only reference Sonrisa.Notifier.Core for shared DTOs/viewmodel contracts if necessary

Background processing and retries
- For this first version there is no retry or background queueing logic. Dispatch will be performed synchronously: the IEventHandlerService will create OutgoingMessage objects and call INotificationDispatcher.DispatchAsync which invokes each INotificationSender.
- In the sender implementations, add a short TODO comment explaining where retry/backoff should be added in future iterations (for example, using Polly policies or a background worker processing a persistent queue). Example comment suggestion:
  // TODO: Add retry/backoff here (e.g., use Polly or enqueue to a background worker) to handle transient provider errors and avoid losing messages.

Configuration and secrets
- appsettings.json sections
  - "ConnectionStrings": { "DefaultConnection": "Data Source=notifier.db" }
  - "Smtp": { Host, Port, Username, Password (prefer env var), From }
  - "Slack": { DefaultWebhookUrl (optional) }
  - "Notification": { ThrottleSeconds: 300 }

- Secrets
  - Never store provider secrets in source. Use environment variables or user secrets in development.

Logging and observability
- Use Microsoft.Extensions.Logging throughout services and senders.
- Log structured events for send attempts, successes, and failures.
- Persist DeliveryLog rows for failed attempts with error messages.

Testing plan
- Unit tests (Sonrisa.Notifier.Tests)
  - SubscriptionService: test resolving channels given UsersChannels entries.
  - EventHandlerService: test that given a TestEventRequest and set of UsersChannels the expected OutgoingMessage(s) are created and dispatcher called (use mocks for INotificationDispatcher and repositories).
  - NotificationSender implementations: mock HTTP/SMTP clients or use local test server; assert DeliveryResult mapping.

- Integration tests
  - Use an in-memory SQLite database (EnsureCreated) and run through API endpoints to verify end-to-end flow for TestEventsController -> IEventHandlerService -> INotificationDispatcher (with mocks for senders or test stubs that record calls).

- Manual tests
  - Use the `/api/test/event` endpoint with a sample JSON payload and observe console logs or DeliveryLog entries.

Migration and local run
- Add EF Core migrations:
  - dotnet ef migrations add Init -p src/Sonrisa.Notifier.Infrastructure -s src/Sonrisa.Notifier.Host
  - dotnet ef database update -p src/Sonrisa.Notifier.Infrastructure -s src/Sonrisa.Notifier.Host
- Or use DbContext.Database.EnsureCreated() for simple local setup in development.

Sample implementation notes & code skeleton (files to create)
- src/Sonrisa.Notifier.Core/Models/OutgoingMessage.cs (POCO)
- src/Sonrisa.Notifier.Core/Interfaces/INotificationSender.cs
- src/Sonrisa.Notifier.Core/Interfaces/INotificationDispatcher.cs
- src/Sonrisa.Notifier.Infrastructure/Data/SonrisaNotifierDbContext.cs
- src/Sonrisa.Notifier.Infrastructure/Senders/EmailNotificationSender.cs
- src/Sonrisa.Notifier.Infrastructure/Senders/SlackNotificationSender.cs
- src/Sonrisa.Notifier.Api/Controllers/TestEventsController.cs
- src/Sonrisa.Notifier.Api/Controllers/UsersController.cs
- src/Sonrisa.Notifier.Api/Controllers/ChannelsController.cs
- src/Sonrisa.Notifier.Api/Controllers/UsersChannelsController.cs
- src/Sonrisa.Notifier.Host/Program.cs DI registration:
  - services.AddDbContext<SonrisaNotifierDbContext>(opts => opts.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));
  - services.AddScoped<ISubscriptionService, SubscriptionService>();
  - services.AddScoped<IEventHandlerService, EventHandlerService>();
  - services.AddTransient<EmailNotificationSender>();
  - services.AddTransient<SlackNotificationSender>();
  - services.AddScoped<Func<string, INotificationSender>>(provider => key => provider.GetRequiredService<...>()); // or register mapping

Security and production notes
- The `/api/test/event` endpoint is only for local testing. Protect or remove it in production.
- For production, migrate from SQLite to real RDBMS (Postgres/SQL Server) and secure secrets in a vault.

Open questions / future enhancements
- Add per-user alert criteria storage and event-matching rules.
- Add SSO / authentication for Admin UI and secure API with JWT or other mechanisms.
- Add provider-specific delivery tracking and callbacks (e.g., SendGrid webhooks for bounces).

This spec should give sufficient detail to implement the service.

Implementation plan — step-by-step
The implementation will be split into focused steps. Each step includes unit tests and a short validation checklist.

Step 1 — Project skeleton (backend only)
- Goal: create solution and projects: Sonrisa.Notifier.Host, Sonrisa.Notifier.Api, Sonrisa.Notifier.Core, Sonrisa.Notifier.Infrastructure, Sonrisa.Notifier.Tests. Configure shared project references and CI-friendly layout.
- Deliverables:
  - Solution file, project folders, basic Program.cs/Startup wiring in the Host project, Core project with DTOs and interfaces, Infrastructure project with DbContext placeholder.
  - Test project scaffold using MSTest and a sample unit test (Arrange-Act-Assert) for a simple domain type (e.g., OutgoingMessage).
- Validation: solution builds, tests run and pass (sample test), project references are correct.

Step 2 — DAL (EF Core) implementation
- Goal: implement entities (User, Channel, UsersChannels) and SonrisaNotifierDbContext with migrations or EnsureCreated support using SQLite.
- Deliverables:
-  - Entity classes and DbContext in Infrastructure
-  - Repository interfaces (IUserRepository, IChannelRepository, IUsersChannelsRepository) and simple implementations to encapsulate common data operations; repositories should be registered in DI.
-  - Seed SQL that can be executed to create a baseline developer DB and optional runtime behavior in Host to create the DB file from the seed on first run
-  - Unit tests using in-memory or SQLite in-memory provider validating CRUD operations via DbContext and repository behavior
- Validation: migrations can be created or EnsureCreated succeeds; unit tests cover create/read/update/delete for Users and Channels and UsersChannels mapping.

Step 3 — API endpoints and data maintenance logic
- Goal: implement API controllers and services to manage Users, Channels, and UsersChannels (subscriptions).
- Deliverables:
  - UsersController, ChannelsController, UsersChannelsController with DTOs and model validation
  - Services that encapsulate business logic called by controllers
  - Unit tests for controllers and services (use mocked DbContext or in-memory DB)
- Validation: API endpoints can create/read/update/delete resources; tests assert correct behavior and validation errors.

Step 4 — Notification senders
- Goal: implement INotificationSender and concrete senders for Email and Slack (sender logic may be mocked for now). Wire INotificationDispatcher to route messages to senders.
- Deliverables:
  - INotificationSender and INotificationDispatcher interfaces in Core
  - EmailNotificationSender and SlackNotificationSender in Infrastructure with TODO comments for provider integration
  - Unit tests that mock external clients and verify DeliveryResult behavior
- Validation: dispatching to mapped senders is exercised in unit tests; senders return expected DeliveryResult values for success/failure cases.

Step 5 — Test event endpoint and end-to-end wiring
- Goal: implement TestEventsController and IEventHandlerService to accept TestEventRequest, resolve recipients via UsersChannels, build OutgoingMessage objects and dispatch via INotificationDispatcher.
- Deliverables:
  - TestEventsController with validation and 202 response
  - EventHandlerService implementing routing logic
  - Integration-style tests using in-memory SQLite and mocked senders to verify end-to-end flow from endpoint to dispatcher invocation
- Validation: POST /api/test/event returns 202 and mocked senders are invoked for expected users/channels; tests confirm correct OutgoingMessage construction.

Notes on testing and incremental work
- Each step must include unit tests and simple integration checks. Prefer small commits per completed step.
- Keep code simple and well-typed; add TODO comments where future improvements (retry, delivery logging, advanced throttling) will go.

If you approve this split, I can scaffold the Step 1 project skeleton (uncommitted) and create the initial tests. Indicate which test framework you prefer (xUnit, NUnit) if you have a preference.

Step 6 — Admin UI (frontend) skeleton
- Goal: create a separate Sonrisa.Notifier.Admin project and wire basic pages that call the API for data. This step focuses on the minimal UI scaffolding so developers can work on frontend and backend independently.
- Deliverables:
  - New project Sonrisa.Notifier.Admin (Razor MVC or Razor Pages) added to the solution
  - Basic layout, navigation, and DI for an HttpClient configured to call the Api (or use appsettings for API base URL)
  - A sample page that lists Users by calling GET /api/users and renders results
  - Unit tests (Razor page unit tests or controller tests) and a small e2e manual checklist
- Validation: Admin project builds, can call Api endpoints in development (use localhost URLs), and the Users list page shows data from the Api.

Step 7 — Admin UI features and test trigger
- Goal: implement minimal Admin pages to manage Users, Channels, and Subscriptions and to trigger the Test Event endpoint from the UI.
- Deliverables:
  - Users list/create/edit pages that use the Api endpoints
  - Channels list/create/edit pages
  - Subscriptions management page that lets an admin add/remove UsersChannels mappings
  - A Test Event form that posts to POST /api/test/event and shows a simple response status (202 accepted or validation errors)
  - Unit tests for UI controllers/pages and a small integration test that verifies the Test Event form posts to the Api (can be mocked)
- Validation: From the Admin UI you can create a user, create a channel, subscribe the user to the channel, and trigger a Test Event that the Api accepts (202).
