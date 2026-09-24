# AI_NOTES

## Context about the author

I am still learning Russian. The task brief is in Russian and I worked from it with AI help
(reading, translating terms, checking my understanding). It was not a barrier: the domain terms
(Revenue, Gross Profit, Margin, Average Check) are standard, and the code, comments, README and
API contracts in this repository are written in English so they are easy to review.

The seed data uses Russian names and company names on purpose, so the dashboard looks like a real
product for the Russian market (DJI-Market.ru).

## Tools

- Claude Code (Sonnet 5) for design, scaffolding and implementation of the backend.
- Claude Code (Opus 5.5) for containerisation, the frontend, the dark theme, CI and the README.

_The remaining sections (delegated work, AI mistakes, rejected suggestions, how the result was
verified) are filled in as the work progresses._

## Where the AI got it wrong (and how it was caught)

- **Unrealistic seed margins.** The first seed gave big-ticket drones 7–12% catalogue margin, then
  stacked manager, segment and bulk discounts on top. Most Enterprise and Professional lines ended up
  priced at cost. The dashboard showed a 2.3% overall margin and managers with negative gross profit.
  The code compiled and the tests passed. It was caught before building the UI, by checking the live
  API numbers against what a drone retailer's P&L should look like (the agent flagged it in that
  check). Fix: realistic list margins (20–26% on drones) and smaller segment discounts. Now 15.7%
  overall, 5–20% across managers.
- **A test helper bug**, hidden while the tests could not run locally: `name[..2]` threw on a
  one-letter name. It surfaced the first time the suite ran in Docker.
