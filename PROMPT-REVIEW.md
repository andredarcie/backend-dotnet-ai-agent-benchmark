# Second pass: review, run, validate, and final patch

You already built this project (in the current folder) to satisfy the original brief
(`PROMPT.md`, reproduced above). Now do a **second, final pass** — treat it as reviewing a
teammate's pull request the moment before it ships. Same stack, same one-command boot; **no new
scope**. The goal is a project that genuinely **builds, boots, and satisfies the brief end to end** —
not more features.

Work in this order.

## 1. Re-read and self-review
- Re-read the original brief and **every file you produced**. List, honestly, where the implementation
  diverges from the brief or from production-grade quality. Look hardest at the parts that are easy to
  fake: an endpoint that returns a shape but doesn't persist; an OpenAPI doc that's served but empty; a
  Kafka producer that's wired but never actually publishes; migrations that don't run on startup.

## 2. Actually run it
- `docker compose up --build` from the project root. Wait for health. If it does **not** come up cleanly
  with no manual steps, that is the first thing to fix.
- Confirm: API on `http://localhost:8080`, `GET /health` → 200, Kafka reachable on `localhost:29092`,
  the schema created via **EF Core migrations** on startup.

## 3. Validate the real contract (exercise the running API)
Drive the endpoints and confirm the observable behaviour, not just the code:
- **Create**: `POST /api/credit-cards` and `POST /api/transactions` → **201** with a **`Location`** header
  and the new **`id`** in the body; JSON is **camelCase**.
- **Validation → 400**: empty `cardholderName`/`cardNumber`; `amount <= 0`; empty `merchant`;
  `creditCardId` that doesn't exist (FK enforced by the API, not a 500).
- **Not found → 404** on GET/PUT/DELETE of a missing id; **204** on successful DELETE and (200/204) on PUT.
- **Errors** use **`application/problem+json`** (RFC 9457) and a **malformed request never leaks a stack
  trace / internals**.
- **Pagination** works on both collections (a page size actually limits the rows).
- **Kafka**: creating a transaction publishes it to the **`transactions`** topic **after** it persists,
  **keyed by the transaction id**. Verify a message actually lands.
- **Security**: the PAN is never logged and never stored in plain text; **CVV/PIN/track data are never
  stored**; no secrets hardcoded (env vars only).

## 4. Prove it with tests
- Run the test suite. `dotnet build` must be **warning-clean** and `dotnet format` clean.
- If a **critical rule** (FK exists, `amount > 0`, required fields, 201/Location, 404s) has no test,
  add a focused one. Aim for the ≥80% line coverage the brief asks for on the paths that matter — don't
  pad coverage with trivial tests.

## 5. Final patch
- Fix **every** gap you found above. This is your last change: prioritise **correctness and
  production-readiness** over polish, and **do not gold-plate** — no new layers, patterns, or features
  the brief didn't ask for. Removing accidental complexity is a valid fix.
- Leave the working tree in a state where a fresh `docker compose up --build` brings the whole system up
  and every rule above holds.

When you're done, briefly note what you changed and why. The bar is unchanged: this should read like a
service you'd actually ship.
