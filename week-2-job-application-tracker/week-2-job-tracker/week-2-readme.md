# Week 2 — Job Application Tracker (Blazor vs n8n)

Same problem, built two ways: a tool that watches Gmail for job-application-related
emails, classifies them with Gemini, and keeps a status board current automatically —
no manual data entry once an application email arrives.

## The problem

Applications go out across LinkedIn, company sites, referrals. Rejections are obvious.
Most applications just go quiet, and Gmail doesn't reliably surface that if you're not
actively checking. This tracks every application's real status without relying on
memory or catching every notification.

## Two implementations

### `/blazor` — Blazor Server + PostgreSQL
Code-first, full control. A `BackgroundService` polls Gmail every 5 minutes, sends new
messages to Gemini for classification, matches them against existing applications
(fuzzy match via `EF.Functions.ILike`), and creates or updates rows accordingly.
Dedup handled via a `ProcessedEmails` table keyed on Gmail message ID, so no email is
ever classified twice. Includes a Reports page (weekly/monthly breakdowns) and Excel
export for offers/rejections.

**Stack:** .NET 10, Blazor Server, PostgreSQL, Entity Framework Core, Gmail API,
Gemini API, ClosedXML

### `/n8n` — n8n workflow
Low-code, faster to build. Gmail Trigger → AI Agent (Gemini) for classification →
Google Sheets as the database. Dedup handled by n8n's built-in Remove Duplicates node
instead of a custom table. A separate scheduled workflow sends a weekly HTML summary
email.

**Stack:** n8n, Gmail node, Google Sheets, AI Agent + Google Gemini Chat Model node

## Deliberate difference: no "stale application" nudge

Both guides originally planned a proactive nudge for applications that had gone quiet
too long. After using the tracker with real data, this was dropped — most applications
either get a clear rejection reply or are reasonably assumed dead (including several
likely low-effort "keep the listing open for visibility" postings). A nudge for
something already assumed dead wasn't useful, so it was cut rather than built for the
sake of matching the original spec.

## What was genuinely different building the same idea twice

- **Dedup by necessity, not preference** — Blazor needed a real database table since
  nothing else would stop re-classification; n8n got this for free from a built-in node.
- **Same fuzzy-match logic, different expression** — `EF.Functions.ILike` in C# vs a
  `.includes()` check in a Code node. Same idea, proving it wasn't tool-specific.
- **A real n8n gotcha**: a Code node referenced another node by name
  (`$('Code').first().json`) but the actual node had been renamed — n8n failed silently
  until the reference was corrected. Node names in `$('...')` references have to match
  exactly, which isn't obvious until it breaks.

## Setup

See inline comments in each folder's source for environment variables and credentials
needed (`.env`/`appsettings.Development.json` — neither is committed here, see
`.gitignore`).
