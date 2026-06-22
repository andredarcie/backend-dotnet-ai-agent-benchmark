# Submissions

One folder per model. Each folder is a self-contained project that satisfies
[`../PROMPT.md`](../PROMPT.md).

## How to add a submission

1. Create a folder named after the model, e.g. `submissions/claude-opus-4-8/`.
2. Drop the **entire project** the model generated into it.
3. The folder root **must** contain a `docker-compose.yml` that starts API + Postgres + Kafka.
4. After `docker compose up --build` from that folder:
   - the API is reachable at `http://localhost:8080`,
   - Kafka is reachable at `localhost:29092`,
   - creating a transaction publishes to the `transactions` topic.

```
submissions/
├── gpt-5-5-xhigh/        # ← one folder per model (you add these)
├── claude-opus-4-8/
└── gemini-3-pro/
```

## Naming

Use a stable, filesystem-safe name (kebab-case). It becomes the report filename
(`evaluator/results/<name>.json`) and the row label in the leaderboard.

## Port usage

Submissions are evaluated **one at a time** (the runner brings each compose down before
starting the next), so every submission can safely use the same host ports:
`8080` (API), `29092` (Kafka). Postgres stays internal to each compose network.
