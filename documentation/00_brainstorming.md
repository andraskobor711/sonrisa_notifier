# Notification Sender — Brainstorming

Summary
- Purpose: Allow users to set alerts so they receive notifications for important events (breaking news, market moves, natural disasters, etc.). Support Email and Slack initially; design for easy addition of channels later. Include an Admin view for CRUD and delivery visibility.
- Tech choices (approved): .NET 8 (C#), SQLite (file-based), Admin UI: ASP.NET MVC / Razor.

Architecture — Layers and responsibilities
- DAL (Data Access Layer)
  - Persist users, alerts/subscriptions, channel configurations, and delivery logs.
  - Recommend EF Core with SQLite backend or a thin repository abstraction to isolate persistence.
- BLL (Business Logic Layer)
  - Domain rules: create/update/delete alerts, evaluate events against subscriptions, handle throttling, deduplication, and prioritization.
  - Provide services for Admin operations (validation, aggregates for UI).
- Messaging Layer
  - Two responsibilities:
	1. Receive inbound events (webhooks, scheduled checks, manual ingestion).
	2. Notification sending: interface-driven senders per channel (Email, Slack). Handle templating, formatting, retries, and error handling.
  - Pattern: `INotificationSender.SendAsync(OutgoingMessage)` with `EmailNotificationSender` and `SlackNotificationSender` implementations; register via DI.
 - API Layer
  - Expose RESTful CRUD endpoints for users, channels, and user-channel subscriptions (UsersChannels) consumed by AdminView.
 - AdminView (ASP.NET MVC / Razor)
  - Pages to list and edit Users, Channels, and user subscriptions (UsersChannels). Server-rendered views calling the API for data.

High-level flow
1. Event arrives (webhook, poller, or manual).
2. BLL queries DAL (UsersChannels) to determine recipient channels for users.
3. The system produces `OutgoingMessage` objects targeting one or more channels for each recipient.
4. Messaging layer picks registered `INotificationSender` instances and calls `SendAsync`.

DAL — suggested tables (high-level)
- Users
  - Id (GUID), Email, DisplayName, Timezone, IsActive, CreatedAt, UpdatedAt
- Channels
  - Id (GUID), Type (Email, Slack, ...), ConfigJson (webhook URL, channel id, metadata), IsActive, CreatedAt, UpdatedAt
- UsersChannels
  - UserId (GUID), ChannelId (GUID) — maps which channels a user is subscribed to

Messaging layer — design notes
 - OutgoingMessage (transport object) contains messageId, userId, title, body, priority, channels, metadata, createdAt.
- `INotificationSender`
  - Signature: `Task<DeliveryResult> SendAsync(OutgoingMessage message, CancellationToken ct)`
  - `DeliveryResult` includes Status, ProviderMessageId, Error.
- Email sender
  - Use SMTP or provider SDK; support templating and plain/html bodies. For demo, local SMTP or mock acceptable.
- Slack sender
  - Use Incoming Webhook or Slack API; support text and blocks payload.
- Retries and backoff
  - Implement configurable retry policy, log attempts, and surface errors in `DeliveryLog`.

Business logic — rules and features
- Alert matching: support simple rule types (keyword/tag match, numeric thresholds). Store rules as JSON for flexibility.
- Throttling: per-alert or per-user windows to reduce spam (e.g., once per X minutes).
- Prioritization: allow higher-priority alerts to bypass some throttles or use alternate channels.
- Admin functions: enable/disable alerts, adjust throttles, export logs.

API — high-level endpoints
- Users: GET/POST/PUT/DELETE `/api/users`
- Channels: GET/POST/PUT/DELETE `/api/channels`
- UsersChannels (subscriptions): GET/POST/DELETE `/api/userschannels`
Authentication note: for interview/demo, a simple API key or local auth is acceptable; document secure defaults for production.

Test endpoint (for local/manual testing)
- Route: POST `/api/test/event` (or `/api/events/test`) — a lightweight test-only endpoint that accepts a JSON payload describing an event and invokes the same event handler used by real inbound sources.
- Behavior: the controller validates the payload, invokes the messaging/BLL event handler (synchronously or queued async) which resolves recipients via `UsersChannels`, creates `OutgoingMessage` instances and calls registered `INotificationSender` implementations. Returns 202 Accepted on success.
- Example request payload:
{
  "title": "Test: Market Move",
  "body": "Test event: S&P 500 moved -1%",
  "priority": "low",
  "metadata": { "source": "manual-test", "tags": ["market"] },
  "userIds": ["{user-guid}"] // optional: target specific users; omit to broadcast to all subscribed users
}

Note: This endpoint is intended only for local testing and demos; disable or protect it for production.

AdminView — suggested pages and interactions
- Users list / edit
- Channels list / edit (Slack webhook or email config)
- Subscriptions (UsersChannels) list / edit (manage which channels each user receives)
- UI should use server-rendered Razor pages calling API endpoints for operations.

Data models — concise examples

Stored user (JSON-like)
{
  "id": "guid",
  "email": "user@example.com",
  "displayName": "Jane Doe",
  "timezone": "America/Los_Angeles",
  "isActive": true,
  "createdAt": "2026-09-22T12:00:00Z"
}

OutgoingMessage
{
  "messageId": "guid",
  "userId": "guid",
  "priority": "high",
  "title": "Significant market move",
  "body": "S&P 500 moved -3.2% in 10 minutes.",
  "channels": ["email","slack"],
  "metadata": { "source":"market-watcher","tags":["market","urgent"] },
  "createdAt": "2026-09-22T12:34:00Z"
}

Email payload (example)
{
  "to": "user@example.com",
  "subject": "Alert: Significant market move",
  "htmlBody": "<p>S&P 500 moved -3.2% ...</p>",
  "plainBody": "S&P 500 moved -3.2% ..."
}

Slack payload (example)
{
  "channel": "#alerts",
  "text": "*Alert:* Significant market move\nS&P 500 moved -3.2% ...",
  "blocks": [ /* optional structured message */ ]
}

Extensions, testing, and ops
- Adding channels: implement `INotificationSender`, register with DI, and map channel types in sending service.
- Tests: unit tests for BLL (rule matching, throttling), mocked sender tests; integration tests for end-to-end delivery and `DeliveryLog`.
- Local dev: use a local SQLite DB file and simple EF Core migrations or an init script.
- Secrets: store Slack tokens and SMTP credentials securely (env vars or secrets store); avoid leaking in logs.

Ops & security reminders
- Mask and protect channel secrets; do not log sensitive tokens.
- Limit log verbosity for production; use structured logging for troubleshooting.
- For production, use provider SDKs (SendGrid, Mailgun) and secure webhook handling.

End of document.
