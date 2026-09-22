# Notification Sending — Chat Export / Summary

Date: 2026-09-22

This document is a compact export of the recent implementation chat for the notification senders and dispatcher (Step 4). It summarizes decisions, changes applied to the workspace, and testing added.

## Goal
- Implement thin notification senders (Email, Slack) and a dispatcher that reads UsersChannels and routes messages to subscribed users.

## Design decisions
- Senders are thin and read channel configuration once in their constructor. They cache configuration for reuse and log send intent (no real SMTP/HTTP clients implemented in this iteration).
- OutgoingMessage is a simple DTO (Title, Body). Senders accept OutgoingMessage and the User entity as arguments to SendAsync.
- Dispatcher builds a mapping of subscriptions and invokes the appropriate sender per channel type.
- Senders and dispatcher were placed in `Core` (behaviour layer); DbContext remains in Infrastructure. The dispatcher depends on Infrastructure DbContext — a conscious tradeoff; project references were adjusted accordingly.

## Files added / modified

- Core
  - Added/modified senders:
	- src/Sonrisa.Notifier.Core/Senders/EmailNotificationSender.cs
	- src/Sonrisa.Notifier.Core/Senders/SlackNotificationSender.cs
  - Dispatcher & factory:
	- src/Sonrisa.Notifier.Core/Dispatchers/NotificationDispatcher.cs
	- src/Sonrisa.Notifier.Core/Dispatchers/NotificationSenderFactory.cs
  - Interfaces / models updated:
	- src/Sonrisa.Notifier.Core/Interfaces/INotificationSender.cs (SendAsync signature: OutgoingMessage, Infrastructure User)
	- src/Sonrisa.Notifier.Core/Interfaces/INotificationSenderFactory.cs (GetSender accepts Channel)
	- src/Sonrisa.Notifier.Core/Models/OutgoingMessage.cs (kept minimal: Title, Body)

- Infrastructure
  - DbContext and entities already existed and were used by dispatcher:
	- src/Sonrisa.Notifier.Infrastructure/SonrisaNotifierDbContext.cs
	- src/Sonrisa.Notifier.Infrastructure/Entities/{User,Channel,UsersChannels}

- Host
  - DI registrations updated to register senders as Scoped and factory/dispatcher as Scoped:
	- src/Sonrisa.Notifier.Host/Program.cs

- Tests
  - Added tests for dispatcher behavior (in-memory Sqlite):
	- tests/Sonrisa.Notifier.Core/NotificationDispatcherTests.cs

## Dispatcher behaviour (final)
1. Query UsersChannels join Channels and Users (filter active channels).
2. Build a dictionary grouping entries by Channel.Type (key = channelType, value = list of (Channel, User) pairs).
3. For each channelType group:
   - Pick a representative Channel and create a sender instance via factory (the Channel is injected into sender constructor so it can read config once).
   - Iterate distinct users in that group and call sender.SendAsync(message, user).

Notes: this groups by channel type. If per-channel (distinct Channel rows same type with different configs) behavior is required, adjust to create a sender per Channel and send to users subscribed to that Channel.

## Sender behaviour (final)
- Constructor receives the Channel and caches channel.ConfigJson (and should parse/cache detailed settings there).
- SendAsync(OutgoingMessage, User) uses the cached configuration and user data to perform delivery. Currently it only logs the intent and returns a successful DeliveryResult.
- Comments in code indicate where to implement SMTP or HTTP clients when required.

## Tests
- NotificationDispatcherTests covers grouping by channel type and verifies which fake sender instances were invoked for which users using an in-memory SQLite database.

## Next steps / Recommendations
- Implement real delivery in senders (SMTP client for email; HttpClient for Slack) using cached channel config.
- Consider whether dispatcher should group by Channel.Type (current) or by Channel.Id (per-channel config correctness).
- Improve factory to let DI manage lifetimes/disposal for disposable senders (use registered factory delegates or service factories) if senders will hold disposable resources.
- Add retries/backoff, metrics, and observability for production readiness.

---
This file was generated from the implementation chat and summarizes the applied changes. For the exact code diffs, view the Git history or inspect the modified files in the workspace.
