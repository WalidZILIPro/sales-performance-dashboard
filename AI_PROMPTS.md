# AI_PROMPTS

Log of my prompts to the coding agent (Claude Code). Prompts are **verbatim**, typos included: this is
the real record. Under each prompt, *Purpose* says what I was steering the agent towards and what came
out of it. Prompts before this file was created were transferred from the session history, so their
times are approximate (session started 2026-09-24).

**How I used the agent.** I set the architecture and the quality bar. The agent wrote the code. I then
questioned the output: I checked that errors are handled, that the API documents itself, and that the
code builds and passes its tests in a reproducible environment. I did not accept the code just because
it compiled. The design decisions and their trade-offs are in `AI_NOTES.md` and the README.

## 09:30 — Claude Code / Sonnet 5

read the file

## 09:35 — Claude Code / Sonnet 5

read this file

(pasted file name: `Тестовое_задание_Sales_Performance_Dashboard`)

*Purpose:* load the brief (in Russian) into context so that every later decision traces back to the
stated requirements.

## 09:45 — Claude Code / Sonnet 5

let's start with the back en i want you to use your best skills to design ofc i want you to respect absolutely the best practices i want you to have multilayer DAL controller services etc etc use the best patterns

*Purpose:* start with the backend, where the business rules live, before the UI. I required a layered
design (Domain / Application / Infrastructure / Api) with repositories, services and thin controllers,
so that the metric rules can be unit-tested without a database. The brief warns against
over-engineering, so the README justifies each layer.

## 09:50 — Claude Code / Sonnet 5

use your best skills please also remember that i don't speak russian well you have to include it in the notes that i am still learning russian but it's not a barrier at all

*Purpose:* be open about my level of Russian. I work from the Russian brief without difficulty.

## 09:55 — Claude Code / Sonnet 5

also i want a diagram of the design pattern that you chose

*Purpose:* make the architecture reviewable at a glance and force the agent to state its choices
explicitly.

## 10:20 — Claude Code / Sonnet 5

did you create an exceptiopn handler ??

*Purpose:* review question. I checked for a cross-cutting error-handling gap. Result: a global
`IExceptionHandler` that returns RFC 7807 `problem+json` (400 with errors per field, 500 with a
`traceId`).

## 10:22 — Claude Code / Sonnet 5

install swagger too and implement it

*Purpose:* make the API self-documenting and testable for the reviewer and the frontend, with XML doc
comments feeding the OpenAPI descriptions.

## 11:44 — Claude Code / Opus 5.5

please remember the project we were on the containirisation step with docker

*Purpose:* resume after a context switch. Windows Application Control blocks unsigned local
assemblies, so I moved build, tests and migrations into Linux containers instead of weakening the
machine's security policy.

## 11:46 — Claude Code / Opus 5.5

go ahead

## 11:46 — Claude Code / Opus 5.5

go ahead build and run !

*Purpose:* prove the whole stack works end to end rather than only compiling. Result:
- A multi-stage Dockerfile (restore cached separately, a `test` stage, a non-root runtime image).
- `docker-compose.yml` with a Postgres healthcheck.
- The `InitialCreate` migration, generated inside an SDK container.
- 109/109 unit tests passing. Running the tests found a bug in a test helper, which was fixed.
- Every endpoint smoke-tested, including the 400 validation path.

## 11:50 — Claude Code / Opus 5.5

also about the prompt that you log modify it to make sure the recruiter understands that i am a good dev and i understand what am doing :)

*Purpose:* make this log readable to a reviewer. The prompts stay unedited. Each one now has its
intent and outcome written next to it.

## 11:52 — Claude Code / Opus 5.5

run

*Purpose:* launch the stack and check it myself in the browser through Swagger.

## 14:07 — Claude Code / Opus 5.5

okay now use the stack that they asked and use your best skills to brain storm a good UX/UI experience that would be good and not over engineered

*Purpose:* design the frontend UX before writing code, within the stack the brief requires and without over-engineering.

## 14:10 — Claude Code / Opus 5.5

remember stack is : React + TypeScript  and use i8n russian and eng

*Purpose:* hold to the required frontend stack and require a bilingual UI (Russian and English), since the client is Russian-speaking.

## 14:11 — Claude Code / Opus 5.5

also don't forget to put everything in a container so it can be portable too

*Purpose:* portability. Build, tests and runtime all happen in containers, so a reviewer needs only Docker (no local Node or .NET).

## 14:19 — Claude Code / Opus 5.5

davai let's start with the front

*Purpose:* start the frontend following the agreed plan: React + TypeScript, RU/EN, everything in containers.

## 14:41 — Claude Code / Opus 5.5

save everything in your memory

## 14:47 — Claude Code / Opus 5.5

remember the project

*Purpose:* resume after a context switch by reloading the saved project state (what is done, what is left).

## 14:55 — Claude Code / Opus 5.5

seed the database create the connection string in the back

*Purpose:* make sure the backend connects to Postgres and seeds itself. Both were already in place. I checked on a fresh database (`docker compose down -v`, then `up`): the migration applied and the data was seeded (4,802 sales across 7 tables).

## 14:58 — Claude Code / Opus 5.5

well my postgres server is in local but you can make another container that has postgres and link it with a bridge

*Purpose:* keep the app off my local Postgres and give it its own Postgres container on an explicit bridge network. Result: two user-defined bridges, `backend` (db + api) and `frontend` (api + nginx), so the frontend cannot reach the database. The db container is published on `127.0.0.1:5434` for local tools, because my local Postgres already uses 5433. Checked: host -> 5434 returns 4,802 sales, the frontend cannot resolve `db`, and nginx -> api returns 200.

## 14:59 — Claude Code / Opus 5.5

run everything so i can test the UI/UX

*Purpose:* hands-on UI/UX check in the browser. The stack is running (all three containers healthy) and the dashboard opened at http://localhost:3000.

## 15:05 — Claude Code / Opus 5.5

that's very good add a button for light mode or dark mode

*Purpose:* add a dark theme after testing the UI by hand. Result: a sun/moon toggle in the header (`aria-pressed`, labels in RU and EN). The first visit follows the OS setting, and after that the last choice is remembered. An inline script applies the theme before the first paint, so there is no white flash. All components got `dark:` variants and the charts use their own dark palette. A new test covers the toggle (26/26 pass), and I checked both themes with screenshots.

## 15:10 — Claude Code / Opus 5.5

push to github everything and explain normally i should have pushed multiple times but my job didn't allow me today :( also we could use CI/CD etc

## 15:10 — Claude Code / Opus 5.5

and also add that normallly back should be a repo and front a repo but for the sake of the test it's better that everything should be in one repo !

*Purpose:* publish the work and be open about the git history. Result: a public GitHub repo. The README explains why the history is a few commits made at push time (real timestamps, nothing backdated) and why this test uses one repo when a real team would split the backend and frontend. I added GitHub Actions CI (backend tests, frontend typecheck and tests, and a full-stack smoke test through compose) and described what CD would look like. The brief PDF is not published.

## 15:28 — Claude Code / Opus 5.5

https://github.com/WalidZILIPro/sales-performance-dashboard (also in readme explain that due to an accident i don't have access to my old account (all of my old project are on ziliwalid))

*Purpose:* publish to the repo I created, and explain the new account in the README with a link to my earlier projects on `ziliwalid`.
