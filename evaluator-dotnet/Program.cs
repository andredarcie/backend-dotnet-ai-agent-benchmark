using BackendEvaluator.Cli;

// Backend Evaluator (.NET 10) — walks the 13 categories of EVALUATION-CRITERIA.md,
// runs each category's local checks/tools, and emits a weighted score + report (console/MD/JSON).
return await Runner.Run(args);
