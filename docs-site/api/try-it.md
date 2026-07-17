---
title: Try It Live
---

# Try It Live

A full interactive [Scalar](https://scalar.com/) API client, embedded right here - browse every
endpoint and actually call your own running `VaultGuard.API` without leaving the docs. This is the same
reference UI the API serves itself at `/scalar` (see [Accessing Scalar](/api/reference#accessing-scalar)),
just pointed at whichever instance you tell it about below.

Nothing is proxied through this site or any third party: your browser talks to your API directly.

<ApiTryIt />

## What a live call looks like

Below: `Test Request → Send` on `POST /api/ApiKeys/issue` from this exact page, hitting a real running
`VaultGuard.API` on `localhost` - the response panel shows a genuine `401 Unauthorized` (no `X-API-Key`
header was set) in 15ms, request/response headers and all. Point the field above at your own instance,
add the right headers/body for whichever endpoint you're testing, and you'll get real data back the same
way.

<img src="/screenshots/api/tryit-live-response.png" alt="Try It Live - real response" style="border-radius: 8px; border: 1px solid var(--vp-c-divider)" />
