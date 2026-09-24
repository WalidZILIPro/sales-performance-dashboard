# Scaling to large data sets

What this system does today, what breaks first as data grows, and what I would add at each stage.
Only the first item (rate limiting) is implemented. The brief (section 20) asks not to add
distributed architecture to a small system, so the rest is a plan, not code.

## Today

- About 4,800 sales over 12 months, in one PostgreSQL database. Sales are written by the seeder, and
  the API is read-only.
- Every dashboard block is one aggregate SQL query over `sales` / `sale_items`, filtered by date
  range and supported by indexes: a covering `(status, sold_at) INCLUDE (manager_id, total_amount,
  total_cost)` for the KPI sums, `(manager_id, sold_at)` for the ranking and `(sold_at, id)` for the
  newest-first list. No N+1: the recent-sales
  list loads its items in one query.
- One dashboard load = 6 API requests. At this size each query takes milliseconds.

## Stage 1 — protect the API (implemented)

**Rate limiting.** Every endpoint runs an aggregate query, so one misbehaving client (a script, a retry
loop, a user switching periods very fast) can load the database for everyone.

- ASP.NET Core 8's built-in rate limiter: a **token bucket per client IP**, 60 requests burst, 10/s
  sustained (`RateLimiting` in `appsettings.json`). This lets a normal user through (a dashboard load
  is 6 requests) and caps abuse.
- Rejected requests get **429 + `Retry-After`** in the same RFC 7807 format as every other error.
  The frontend backs off for that long instead of retrying straight away.
- Behind the nginx proxy, the client IP comes from `X-Forwarded-For`, trusted only from private
  (container) networks, so a client outside cannot claim another client's bucket.
- `/health` is excluded, so throttling never makes a healthy container look dead.
- Verified: a burst of 300 requests over 10.5 s → 163 × 200 (60 burst + ~10/s), 137 × 429, and
  `/health` stayed 200.

On the frontend side, TanStack Query cancels the previous period's requests when the period changes,
and caches results, so switching back to a period costs nothing.

## Stage 2 — millions of sales (still one database)

What breaks first: aggregating raw rows on every request. Fixes, cheapest first:

1. **Cache responses by (endpoint, period).** Past periods never change, so they can be cached for a
   long time. Only periods that include today need a short TTL.
2. **Pre-aggregated daily rollups** (`manager × day`, `category × day`, `product × day`), updated when
   a sale is written or changes status. Dashboard queries then read about 365 rows per manager
   instead of millions of sales: cost depends on the number of days, not the number of sales.
3. **Partition `sales` by month** and use a **read replica** for the dashboard, so analytics never
   compete with writes.
4. **Keyset pagination** for recent sales (`WHERE sold_at < @last`) instead of `OFFSET`, which gets
   slower the deeper you page.

## Stage 3 — many writers: why a message broker

Once sales come from several systems (web shop, CRM, ERP, POS) at high volume, keeping the rollups
in step inside each write request becomes the bottleneck. A broker decouples **accepting** a sale
from **processing** it:

- **Absorbs spikes.** Quarter-end or Black Friday peaks fill the queue instead of timing out writes.
  Consumers catch up at a steady pace (backpressure).
- **Scales consumers horizontally**, up to the number of partitions.
- **Several independent readers** of the same events: dashboard rollups, BI export, notifications,
  fraud checks, without the writers knowing about any of them.
- **Late changes stay correct.** A `SaleRefunded` event arriving weeks later updates the rollup of
  the sale's original day, which matches this system's rule that refunds are excluded from revenue.

### Kafka or RabbitMQ?

| | **Kafka** | **RabbitMQ** |
|---|---|---|
| Model | Append-only, **replayable log** | **Queue**: a message is gone once acknowledged |
| Throughput | Very high (millions of events/s) | High, but lower |
| Ordering | Per partition (key = `saleId`, so all events of one sale stay in order) | Per queue |
| Replay | Yes: rebuild the rollups from scratch by re-reading the log | No |
| Strength | Event streams, analytics, many consumers | Task queues: routing, per-message retries, dead-letter queues |
| Fit here | **Sales event stream → rollups** | **Background jobs** (e.g. "email the monthly report PDF") |

For this domain I would use **Kafka for sales events**, because being able to replay them lets you
recompute analytics after a bug fix or a rule change (for example, changing how refunds count).
**RabbitMQ** fits one-off jobs better. Both are proven. The choice depends on whether you need
replay or per-task routing and retries.

### What has to come with a broker

- **Transactional outbox:** write the sale and an `outbox` row in the same database transaction, and
  have a relay publish from the outbox. Otherwise a crash between "saved to DB" and "published" loses
  events (the dual-write problem).
- **Idempotent consumers:** delivery is at-least-once, so each event carries an id and consumers
  skip ids they have already processed.
- **Dead-letter handling and monitoring of consumer lag.** Lag is the main health signal: how far
  behind real time the dashboard is.

## Why none of Stage 2–3 is in this repository

Given ~4,800 sales and one writer, a broker, rollups or partitions would add moving parts with no
measurable benefit, which is exactly the over-engineering the brief warns against. The current design
leaves room for them: the dashboard reads through repository interfaces
(`ISalesAnalyticsRepository` and others), so switching from raw aggregates to rollup tables changes
only the Infrastructure layer, not the services, the API contract or the frontend.
