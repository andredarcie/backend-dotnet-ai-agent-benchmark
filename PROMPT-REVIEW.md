# Second pass: review, verify it builds, and final patch

You already built this project (in the current folder) to satisfy the original brief (`PROMPT.md`,
reproduced above). Now do a **second, final pass** — treat it as reviewing a teammate's pull request the
moment before it ships. This is a **fast, static review**: you do **not** boot the system or run it in
Docker — you read the code, confirm it compiles, and fix what's wrong. Same stack, **no new scope**.

Work in this order.

## 1. Re-read and self-review
- Re-read the original brief and **every file you produced**. List, honestly, where the implementation
  diverges from the brief or from production-grade quality. Look hardest at the parts that are easy to
  get wrong by inspection: an endpoint that returns a shape but doesn't persist; the wrong status code
  (missing 201/`Location`, a 500 where the brief wants 400, no 404); an OpenAPI doc that isn't actually
  wired; a Kafka producer created but never published to; migrations that don't run on startup.

## 2. Trace the contract by reading the code (no running)
Walk each rule and confirm the code actually implements it:
- **Create** returns **201** with a **`Location`** header and the new **`id`**; JSON is **camelCase**.
- **Validation → 400** for empty `cardholderName`/`cardNumber`, `amount <= 0`, empty `merchant`, and a
  `creditCardId` that doesn't exist (the FK is checked in the app, not left to a 500).
- **404** on GET/PUT/DELETE of a missing id; **204** on successful DELETE.
- Errors use **`application/problem+json`** (RFC 9457) and never leak a stack trace.
- **Pagination** actually limits the rows on both collections.
- **Kafka**: a successful transaction create publishes to the **`transactions`** topic **after** it
  persists, **keyed by the transaction id**.
- **Security**: the PAN is never logged or stored in plain text; **CVV/PIN/track data are never stored**;
  no secrets hardcoded (env vars only).

## 3. Confirm it compiles and is clean
- `dotnet build` must succeed and be **warning-clean**; `dotnet format` clean. Fix every build error and
  warning. (You may run `dotnet build`/`dotnet format`/`dotnet test` — those don't need Docker — but do
  **not** try to `docker compose up`; the grader boots it later.)
- If a **critical rule** (FK exists, `amount > 0`, required fields, 201/`Location`, the 404s) has no unit
  test, add a focused one.

## 4. Final patch
- Fix **every** gap you found above. This is your last change: prioritise **correctness and
  production-readiness** over polish, and **do not gold-plate** — no new layers, patterns, or features
  the brief didn't ask for. Removing accidental complexity is a valid fix.
- Leave the project so that a fresh `docker compose up --build` (run by the grader, not you) would bring
  the whole system up and every rule above would hold.

When you're done, briefly note what you changed and why. The bar is unchanged: this should read like a
service you'd actually ship.
