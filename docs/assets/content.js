/* ------------------------------------------------------------------ *
 *  content.js — all authored, bilingual copy for the site.
 *  Dynamic run/leaderboard data lives in data/data.js (generated).
 *  UI strings: window.CONTENT.ui[lang][key]
 *  Rubric:     window.CONTENT.criteria[] (each has {pt,en} fields)
 * ------------------------------------------------------------------ */
window.CONTENT = {

  ui: {
    pt: {
      "lang.name": "PT",
      "lang.switch": "English",
      "theme.toggle": "Alternar tema",

      "nav.overview": "Visão geral",
      "nav.how": "Como funciona",
      "nav.task": "A tarefa",
      "nav.criteria": "Os 13 critérios",
      "nav.scoring": "Pontuação",
      "nav.methodology": "Metodologia",
      "nav.leaderboard": "Leaderboard",
      "nav.details": "Detalhes",
      "nav.github": "GitHub",

      "hero.eyebrow": "Benchmark de código de backend feito por IA",
      "hero.kicker": "Um prompt · todo modelo · uma nota",
      "hero.byline": "Um benchmark opinativo de André N. Darcie — a stack, a rubrica e os pesos refletem o backend que uso no dia a dia no trabalho.",
      "hero.title.a": "Um prompt.",
      "hero.title.b": "Todo modelo.",
      "hero.title.c": "Uma nota.",
      "hero.lede": "Cada modelo de IA recebe exatamente o mesmo pedido — construir uma API REST de cartão de crédito em .NET 10, com PostgreSQL e Kafka, tudo em Docker. Um avaliador automático percorre 13 categorias de qualidade e devolve uma nota ponderada de 0 a 5.",
      "hero.cta.leaderboard": "Ver o leaderboard",
      "hero.cta.how": "Como funciona",
      "hero.details": "Ver os detalhes →",
      "details.eyebrow": "Por dentro do benchmark",
      "details.title": "Os detalhes",
      "details.lede": "A tarefa que os modelos constroem, como uma métrica vira uma nota de 0 a 5, e por que as runs são repetidas.",
      "details.back": "← Leaderboard",
      "hero.stat.categories": "categorias avaliadas",
      "hero.stat.weighted": "nota ponderada",
      "hero.stat.local": "execução 100% local",
      "hero.gauge.label": "Líder atual",
      "hero.gauge.sub": "mediana das runs deep",
      "hero.pipeline.prompt": "PROMPT.md",
      "hero.pipeline.promptSub": "o mesmo para todos",
      "hero.pipeline.model": "O modelo constrói",
      "hero.pipeline.modelSub": "2 passagens: build → revisão",
      "hero.pipeline.eval": "Avaliador",
      "hero.pipeline.evalSub": "Roslyn AST + oráculo ao vivo",
      "hero.pipeline.score": "Nota 0–5",
      "hero.pipeline.scoreSub": "ponderada por 13 pesos",

      "how.eyebrow": "O funil",
      "how.title": "Do prompt à nota, sem opinião humana no caminho crítico",
      "how.lede": "O mesmo prompt vira um projeto inteiro. O avaliador em .NET 10 lê esse projeto de duas formas e transforma o que encontra em números reproduzíveis.",
      "how.static.title": "Modo light (estático)",
      "how.static.body": "Analisa o código-fonte com a árvore sintática do Roslyn e detecta pacotes/arquivos. Rápido, sem Docker, sem rede. A mesma fonte sempre produz a mesma nota.",
      "how.deep.title": "Modo deep (dinâmico)",
      "how.deep.body": "Sobe o projeto de verdade (app + Postgres + Kafka), dirige um oráculo de contrato contra a API viva, roda os testes, cobertura e as ferramentas locais (SAST/DAST/lint) — e só então escreve o relatório.",
      "how.note": "Só as runs deep entram no ranking: elas exercitam o sistema de ponta a ponta. As categorias estáticas são determinísticas; as de runtime variam com a máquina, por isso repetimos as runs (veja Metodologia).",
      "how.passes.title": "Cada modelo constrói em duas passagens",
      "how.pass1.title": "Passagem 1 — Build",
      "how.pass1.body": "O modelo recebe o PROMPT.md e constrói o projeto inteiro — API, PostgreSQL, Kafka, testes e Docker.",
      "how.pass2.title": "Passagem 2 — Revisão",
      "how.pass2.body": "O mesmo modelo recebe o PROMPT-REVIEW.md: revê o próprio código, sobe o sistema, valida o contrato ao vivo e aplica o patch final. É a segunda chance que todo modelo recebe.",
      "how.roslyn.title": "Roslyn AST, não regex",
      "how.roslyn.body": "As checagens estáticas usam o compilador do C# como ferramenta de leitura: direção de dependência entre camadas, catch vazio, interfaces com uma única implementação, god classes. É medição, não busca de texto.",
      "how.oracle.title": "Oráculo de contrato ao vivo",
      "how.oracle.body": "No modo deep o avaliador vira um cliente da API: cria cartão e transação, confere 201/Location/id, força os 400 (FK inexistente, amount ≤ 0, campo obrigatório vazio), os 404 e 204, e observa o evento real chegando no tópico do Kafka.",

      "task.eyebrow": "O que os modelos constroem",
      "task.title": "Uma API de cartão de crédito pronta para produção",
      "task.lede": "O spec funcional é só a linha de base. A régua é: isto deveria parecer um serviço que você colocaria no ar — o como importa tanto quanto o se os endpoints funcionam.",
      "task.domain.title": "Domínio — 1:N",
      "task.domain.body": "Um CreditCard tem muitas Transaction. Toda transação aponta para um cartão existente por chave estrangeira obrigatória.",
      "task.event.title": "Evento no Kafka",
      "task.event.body": "A cada transação criada com sucesso (POST → 201), a transação é publicada no tópico transactions, com a key igual ao id — depois de persistir, nunca antes.",
      "task.rules.title": "Regras que o oráculo cobra",
      "task.rule.fk": "creditCardId precisa referenciar um cartão existente, senão 400.",
      "task.rule.amount": "amount tem de ser > 0, senão 400.",
      "task.rule.required": "cardholderName, cardNumber e merchant não podem ser vazios.",
      "task.rule.pan": "O número do cartão (PAN) é sensível: nunca em log, nunca em texto puro; CVV/PIN nunca são armazenados.",
      "task.stack.title": "Stack fixa",
      "task.card": "CreditCard",
      "task.tx": "Transaction",
      "task.oneToMany": "1 : N",

      "criteria.eyebrow": "A rubrica",
      "criteria.title": "Os 13 critérios, por peso",
      "criteria.lede": "Cada categoria vale de 0 a 5 e tem um peso — a barra mostra quanto ela pesa na nota final. A cor marca quão direta é a medição — todas as notas saem 100% da máquina. Clique para abrir a explicação e o diagrama.",
      "criteria.expand": "Abrir explicação",
      "criteria.collapse": "Fechar",
      "criteria.weight": "Peso",
      "criteria.iso": "ISO/IEC 25010",
      "criteria.look": "O que a gente procura",
      "criteria.how": "Como é medido",
      "criteria.liveScore": "Nesta run",
      "criteria.noScore": "sem run ainda",
      "criteria.sortWeight": "Por peso",
      "criteria.sortOrder": "Por número",
      "criteria.checks": "Como o avaliador checa (técnico)",
      "criteria.checksNote": "Cada linha é uma métrica real do evaluator-dotnet — o nome no relatório, o mecanismo exato e o peso.",
      "criteria.tag.live": "ao vivo",
      "criteria.tag.deep": "deep",

      "auto.FullAuto": "determinístico",
      "auto.SemiOracle": "oráculo",
      "auto.ProxyReview": "proxy",
      "auto.FullAuto.desc": "Pontuado 100% pela máquina a partir de análise estática — o mesmo código-fonte sempre gera a mesma nota.",
      "auto.SemiOracle.desc": "Pontuado 100% pela máquina a cada run contra um oráculo/limiar definido uma vez (suite de aceitação, status esperados, SLO).",
      "auto.ProxyReview.desc": "Pontuado 100% pela máquina a partir de um proxy objetivo (métricas de acoplamento, contagem de violações de regra, checagens de presença) — sem humano no processo.",
      "auto.legend": "Como cada categoria é medida (tudo 100% automático)",

      "scoring.eyebrow": "Da métrica à nota",
      "scoring.title": "Como a pontuação é calculada",
      "scoring.lede": "Nada de nota chutada. Cada métrica é Pass, Partial ou Fail; o que não deu para medir vira Indeterminate e é excluído (não penaliza).",
      "scoring.step1.title": "1 · Métrica",
      "scoring.step1.body": "Pass = 1,0 · Partial = 0,5 · Fail = 0,0. Indeterminate sai da conta.",
      "scoring.step2.title": "2 · Categoria",
      "scoring.step2.body": "Média das métricas medidas, ponderada, × 5 → nota de 0 a 5 na categoria.",
      "scoring.step3.title": "3 · Final",
      "scoring.step3.body": "Média das categorias ponderada pelos pesos da rubrica, renormalizada sobre o que foi medido (a cobertura aparece no relatório).",
      "scoring.scale.title": "A escala 0–5",
      "scoring.scale.0": "Ausente ou não funcional",
      "scoring.scale.1": "Presente, mas com falhas sérias",
      "scoring.scale.2": "Funciona no caminho feliz, frágil",
      "scoring.scale.3": "Adequado, segue o básico esperado",
      "scoring.scale.4": "Sólido, com boas práticas aplicadas",
      "scoring.scale.5": "Exemplar, pronto para produção",
      "scoring.weights.title": "Onde o peso está",
      "scoring.weights.body": "Domínio crítico (cartão de crédito): mais peso em Correção, Segurança, Mensageria e Persistência; menos em Documentação e Deploy. Pesos são uma calibração deliberada — não há consenso externo sobre eles.",

      "method.eyebrow": "Por que confiar no número",
      "method.title": "Metodologia",
      "method.lede": "Modelos são estocásticos: o mesmo prompt gera um projeto diferente a cada vez. Uma única submissão é uma amostra fraca.",
      "method.multi.title": "Muitas runs, mediana",
      "method.multi.body": "O leaderboard agrupa as runs por modelo, ordena pela mediana das runs deep e mostra a dispersão (±σ, média, faixa, contagem). Modelos com menos de 5 runs ficam marcados como provisórios — trate diferenças dentro da dispersão como empate.",
      "method.det.title": "Determinístico vs. runtime",
      "method.det.body": "Categorias estáticas (Estático, Arquitetura, Qualidade) são determinísticas dado o Roslyn. Categorias de runtime (build/boot, funcional, Kafka, estresse) dependem de Docker e da máquina, então variam entre runs.",
      "method.patch.title": "Avaliado como entregue",
      "method.patch.body": "Nenhum humano edita a submissão. Ela é pontuada exatamente como o modelo a gerou — sem patches. Um bloqueio de build/boot não é \"consertado\": é tratado pela trava de executabilidade, que limita a nota de quem não compila (≤0,5), não entrega um sistema executável (≤1,0) ou nunca sobe saudável (≤1,5).",
      "method.patch.note": "A nota vem 100% da ferramenta evaluator-dotnet — sem LLM, sem humano no caminho crítico.",

      "lb.eyebrow": "Sempre evoluindo",
      "lb.title": "Leaderboard",
      "lb.lede": "Ordenado pela mediana por modelo da nota ponderada (0–5). Só runs deep contam. Rode docs/generate-data.ps1 depois de avaliar novas runs e esta tabela se atualiza sozinha.",
      "lb.col.rank": "#",
      "lb.col.model": "Modelo",
      "lb.col.runs": "Runs",
      "lb.col.median": "Mediana /5",
      "lb.col.spread": "Dispersão (média ±σ, faixa)",
      "lb.col.build": "Build",
      "lb.col.boot": "Boot",
      "lb.provisional": "provisório (< 5 runs)",
      "lb.singleRun": "run única",
      "lb.generated": "Gerado em",
      "lb.empty": "Nenhuma run deep avaliada ainda. Rode o avaliador e depois docs/generate-data.ps1.",
      "lb.detail.title": "Perfil por categoria",
      "lb.detail.run": "Relatório da run",
      "lb.detail.patch": "Nota limitada",
      "lb.detail.builds": "Compila",
      "lb.detail.boots": "Sobe (/health)",
      "lb.detail.coverage": "Cobertura da rubrica",
      "lb.detail.close": "Fechar",
      "run.meta.title": "Procedência",
      "run.meta.harness": "Agente / CLI",
      "run.meta.effort": "Effort",
      "run.meta.duration": "Duração",
      "run.meta.passes": "Passagens",
      "run.meta.passes.hint": "build + revisão",
      "run.meta.attempts": "Tentativas",
      "run.meta.tokens": "Tokens (in/out)",
      "run.meta.cost": "Custo",
      "run.meta.prompt": "Prompt",
      "run.meta.produced": "Produzido em",
      "lb.detail.metrics": "métricas",
      "lb.detail.measured": "medidas",
      "lb.detail.indeterminate": "indeterminadas",
      "lb.status.Pass": "Passou",
      "lb.status.Partial": "Parcial",
      "lb.status.Fail": "Falhou",
      "lb.status.Indeterminate": "Indeterminado",
      "lb.yes": "sim",
      "lb.no": "não",
      "lb.viewProfile": "Ver perfil",

      "footer.tagline": "Um prompt, muitos modelos, uma nota automática.",
      "footer.author": "Feito por André N. Darcie · benchmark opinativo, baseado na stack que uso no trabalho.",
      "footer.add.title": "Adicione seu modelo",
      "footer.add.body": "Rode o model-runner com o nome do modelo: ele faz as duas passagens (build + revisão), grava a run e a procedência. Depois avalie e regenere os dados.",
      "footer.links": "Documentos",
      "footer.link.prompt": "PROMPT.md — o prompt exato",
      "footer.link.criteria": "EVALUATION-CRITERIA.md — a rubrica completa",
      "footer.link.methodology": "METHODOLOGY.md — como ler o leaderboard",
      "footer.link.evaluator": "evaluator-dotnet — o avaliador",
      "footer.regen": "Regenerar os dados do site",
      "footer.built": "Feito com Roslyn AST, um oráculo de contrato ao vivo e Docker.",

      "misc.weightUnit": "%",
      "misc.of5": "/5"
    },

    en: {
      "lang.name": "EN",
      "lang.switch": "Português",
      "theme.toggle": "Toggle theme",

      "nav.overview": "Overview",
      "nav.how": "How it works",
      "nav.task": "The task",
      "nav.criteria": "The 13 criteria",
      "nav.scoring": "Scoring",
      "nav.methodology": "Methodology",
      "nav.leaderboard": "Leaderboard",
      "nav.details": "Details",
      "nav.github": "GitHub",

      "hero.eyebrow": "A benchmark of AI-written backend code",
      "hero.kicker": "One prompt · every model · one score",
      "hero.byline": "An opinionated benchmark by André N. Darcie — its stack, rubric and weights reflect the backend I work with day to day.",
      "hero.title.a": "One prompt.",
      "hero.title.b": "Every model.",
      "hero.title.c": "One score.",
      "hero.lede": "Every AI model gets the exact same brief — build a .NET 10 credit-card REST API with PostgreSQL and Kafka, all in Docker. An automated evaluator walks 13 quality categories and returns a weighted 0–5 score.",
      "hero.cta.leaderboard": "See the leaderboard",
      "hero.cta.how": "How it works",
      "hero.details": "See the details →",
      "details.eyebrow": "Inside the benchmark",
      "details.title": "The details",
      "details.lede": "The task the models build, how a metric becomes a 0–5 score, and why runs are repeated.",
      "details.back": "← Leaderboard",
      "hero.stat.categories": "categories scored",
      "hero.stat.weighted": "weighted score",
      "hero.stat.local": "runs 100% locally",
      "hero.gauge.label": "Current leader",
      "hero.gauge.sub": "median of deep runs",
      "hero.pipeline.prompt": "PROMPT.md",
      "hero.pipeline.promptSub": "same for everyone",
      "hero.pipeline.model": "The model builds",
      "hero.pipeline.modelSub": "2 passes: build → review",
      "hero.pipeline.eval": "Evaluator",
      "hero.pipeline.evalSub": "Roslyn AST + live oracle",
      "hero.pipeline.score": "Score 0–5",
      "hero.pipeline.scoreSub": "weighted across 13 categories",

      "how.eyebrow": "The pipeline",
      "how.title": "From prompt to score, with no human on the critical path",
      "how.lede": "The same prompt becomes a whole project. The .NET 10 evaluator reads that project two ways and turns what it finds into reproducible numbers.",
      "how.static.title": "Light mode (static)",
      "how.static.body": "Analyses the source with Roslyn's syntax tree and detects packages/files. Fast, no Docker, no network. The same source always yields the same score.",
      "how.deep.title": "Deep mode (dynamic)",
      "how.deep.body": "Boots the project for real (app + Postgres + Kafka), drives a contract oracle against the live API, runs the tests, coverage and the local tools (SAST/DAST/lint) — and only then writes the report.",
      "how.note": "Only deep runs enter the ranking: they exercise the system end to end. Static categories are deterministic; runtime ones vary with the host, which is why runs are repeated (see Methodology).",
      "how.passes.title": "Each model builds in two passes",
      "how.pass1.title": "Pass 1 — Build",
      "how.pass1.body": "The model receives PROMPT.md and builds the whole project — API, PostgreSQL, Kafka, tests and Docker.",
      "how.pass2.title": "Pass 2 — Review",
      "how.pass2.body": "The same model receives PROMPT-REVIEW.md: it reviews its own code, boots the system, validates the live contract and applies the final patch. It's the second chance every model gets.",
      "how.roslyn.title": "Roslyn AST, not regex",
      "how.roslyn.body": "The static checks use the C# compiler as a reading tool: layer dependency direction, empty catch, single-implementation interfaces, god classes. It's measurement, not text search.",
      "how.oracle.title": "Live contract oracle",
      "how.oracle.body": "In deep mode the evaluator becomes an API client: it creates a card and a transaction, checks 201/Location/id, forces the 400s (missing FK, amount ≤ 0, empty required field), the 404s and 204s, and watches the real event land on the Kafka topic.",

      "task.eyebrow": "What the models build",
      "task.title": "A production-grade credit-card API",
      "task.lede": "The functional spec is only the baseline. The bar: this should read like a service you'd actually ship — how it's built matters as much as whether the endpoints work.",
      "task.domain.title": "Domain — 1:N",
      "task.domain.body": "One CreditCard has many Transaction. Every transaction points at an existing card through a required foreign key.",
      "task.event.title": "Kafka event",
      "task.event.body": "On every successful transaction (POST → 201), the transaction is published to the transactions topic, keyed by its id — after it persists, never before.",
      "task.rules.title": "Rules the oracle enforces",
      "task.rule.fk": "creditCardId must reference an existing card, otherwise 400.",
      "task.rule.amount": "amount must be > 0, otherwise 400.",
      "task.rule.required": "cardholderName, cardNumber and merchant can't be empty.",
      "task.rule.pan": "The card number (PAN) is sensitive: never logged, never in plain text; CVV/PIN are never stored.",
      "task.stack.title": "Fixed stack",
      "task.card": "CreditCard",
      "task.tx": "Transaction",
      "task.oneToMany": "1 : N",

      "criteria.eyebrow": "The rubric",
      "criteria.title": "The 13 criteria, by weight",
      "criteria.lede": "Each category scores 0–5 and carries a weight — the bar shows how much it counts toward the final score. The colour marks how directly it is measured — every score is produced 100% by the machine. Click to open the explanation and diagram.",
      "criteria.expand": "Open explanation",
      "criteria.collapse": "Close",
      "criteria.weight": "Weight",
      "criteria.iso": "ISO/IEC 25010",
      "criteria.look": "What we look for",
      "criteria.how": "How it's measured",
      "criteria.liveScore": "This run",
      "criteria.noScore": "no run yet",
      "criteria.sortWeight": "By weight",
      "criteria.sortOrder": "By number",
      "criteria.checks": "How the evaluator checks it (technical)",
      "criteria.checksNote": "Each row is a real evaluator-dotnet metric — its name in the report, the exact mechanism and its weight.",
      "criteria.tag.live": "live",
      "criteria.tag.deep": "deep",

      "auto.FullAuto": "deterministic",
      "auto.SemiOracle": "oracle",
      "auto.ProxyReview": "proxy",
      "auto.FullAuto.desc": "Scored 100% by the machine from static analysis — the same source always yields the same score.",
      "auto.SemiOracle.desc": "Scored 100% by the machine every run against a fixed oracle/threshold defined once (acceptance suite, expected status codes, SLO).",
      "auto.ProxyReview.desc": "Scored 100% by the machine from an objective proxy (coupling metrics, rule-violation counts, presence checks) — no human in the loop.",
      "auto.legend": "How each category is measured (all 100% automated)",

      "scoring.eyebrow": "From metric to score",
      "scoring.title": "How the score is computed",
      "scoring.lede": "No guessed scores. Each metric is Pass, Partial or Fail; anything that couldn't be measured becomes Indeterminate and is excluded (it doesn't penalise).",
      "scoring.step1.title": "1 · Metric",
      "scoring.step1.body": "Pass = 1.0 · Partial = 0.5 · Fail = 0.0. Indeterminate drops out.",
      "scoring.step2.title": "2 · Category",
      "scoring.step2.body": "Weighted mean of the measured metrics × 5 → a 0–5 score for the category.",
      "scoring.step3.title": "3 · Final",
      "scoring.step3.body": "Mean of the categories weighted by the rubric weights, renormalised over what was measured (coverage shows in the report).",
      "scoring.scale.title": "The 0–5 scale",
      "scoring.scale.0": "Absent or non-functional",
      "scoring.scale.1": "Present, but with serious flaws",
      "scoring.scale.2": "Works on the happy path, fragile",
      "scoring.scale.3": "Adequate, follows the expected basics",
      "scoring.scale.4": "Solid, with good practices applied",
      "scoring.scale.5": "Exemplary, production-ready",
      "scoring.weights.title": "Where the weight sits",
      "scoring.weights.body": "Critical domain (a credit card): more weight on Correctness, Security, Messaging and Persistence; less on Documentation and Deploy. Weights are a deliberate calibration — there's no external consensus on them.",

      "method.eyebrow": "Why trust the number",
      "method.title": "Methodology",
      "method.lede": "Models are stochastic: the same prompt yields a different project each time. A single submission is a weak sample.",
      "method.multi.title": "Many runs, median",
      "method.multi.body": "The leaderboard groups runs per model, ranks by the median of the deep runs and shows the spread (±σ, mean, range, count). Models with fewer than 5 runs are flagged provisional — treat gaps within the spread as ties.",
      "method.det.title": "Deterministic vs. runtime",
      "method.det.body": "Static categories (Static, Architecture, Quality) are deterministic given Roslyn. Runtime categories (build/boot, functional, Kafka, stress) depend on Docker and the host, so they vary run to run.",
      "method.patch.title": "Graded as submitted",
      "method.patch.body": "No human ever edits a submission. It is scored exactly as the model produced it — no patches. A build/boot blocker is never \"fixed\": it is handled by the executability gate, which caps the score of anything that doesn't compile (≤0.5), ships no runnable system (≤1.0), or never boots healthy (≤1.5).",
      "method.patch.note": "The score comes 100% from the evaluator-dotnet tool — no LLM, no human on the critical path.",

      "lb.eyebrow": "Always evolving",
      "lb.title": "Leaderboard",
      "lb.lede": "Ranked by per-model median of the weighted score (0–5). Only deep runs count. Run docs/generate-data.ps1 after grading new runs and this table updates itself.",
      "lb.col.rank": "#",
      "lb.col.model": "Model",
      "lb.col.runs": "Runs",
      "lb.col.median": "Median /5",
      "lb.col.spread": "Spread (mean ±σ, range)",
      "lb.col.build": "Build",
      "lb.col.boot": "Boot",
      "lb.provisional": "provisional (< 5 runs)",
      "lb.singleRun": "single run",
      "lb.generated": "Generated",
      "lb.empty": "No deep runs graded yet. Run the evaluator, then docs/generate-data.ps1.",
      "lb.detail.title": "Per-category profile",
      "lb.detail.run": "Run report",
      "lb.detail.patch": "Score capped",
      "lb.detail.builds": "Builds",
      "lb.detail.boots": "Boots (/health)",
      "lb.detail.coverage": "Rubric coverage",
      "lb.detail.close": "Close",
      "run.meta.title": "Provenance",
      "run.meta.harness": "Agent / CLI",
      "run.meta.effort": "Effort",
      "run.meta.duration": "Duration",
      "run.meta.passes": "Passes",
      "run.meta.passes.hint": "build + review",
      "run.meta.attempts": "Attempts",
      "run.meta.tokens": "Tokens (in/out)",
      "run.meta.cost": "Cost",
      "run.meta.prompt": "Prompt",
      "run.meta.produced": "Produced",
      "lb.detail.metrics": "metrics",
      "lb.detail.measured": "measured",
      "lb.detail.indeterminate": "indeterminate",
      "lb.status.Pass": "Pass",
      "lb.status.Partial": "Partial",
      "lb.status.Fail": "Fail",
      "lb.status.Indeterminate": "Indeterminate",
      "lb.yes": "yes",
      "lb.no": "no",
      "lb.viewProfile": "View profile",

      "footer.tagline": "One prompt, many models, one automated score.",
      "footer.author": "Made by André N. Darcie · an opinionated benchmark, based on the stack I use at work.",
      "footer.add.title": "Add your model",
      "footer.add.body": "Run model-runner with the model name: it does both passes (build + review), records the run and its provenance. Then grade it and regenerate the data.",
      "footer.links": "Documents",
      "footer.link.prompt": "PROMPT.md — the exact prompt",
      "footer.link.criteria": "EVALUATION-CRITERIA.md — the full rubric",
      "footer.link.methodology": "METHODOLOGY.md — how to read the leaderboard",
      "footer.link.evaluator": "evaluator-dotnet — the evaluator",
      "footer.regen": "Regenerate the site data",
      "footer.built": "Built with Roslyn AST, a live contract oracle and Docker.",

      "misc.weightUnit": "%",
      "misc.of5": "/5"
    }
  },

  /* ---- the 13 criteria; language-neutral facts + {pt,en} prose ---- */
  criteria: [
    {
      number: 1, key: "functional", weightPct: 12, iso: "Functional suitability",
      automation: "SemiOracle", diagram: "requestFlow",
      title: { pt: "Correção funcional", en: "Functional Correctness" },
      tagline: {
        pt: "O sistema faz o que promete, do começo ao fim?",
        en: "Does the system do what it promises, end to end?"
      },
      body: {
        pt: "É o critério mais básico e o de maior peso: os endpoints existem, respondem e entregam o efeito certo. Nada de fachada — um endpoint que existe mas não persiste, não conta. O fluxo completo precisa fechar: HTTP → regra de negócio → persistência → evento.",
        en: "The most basic criterion and the heaviest: the endpoints exist, respond and deliver the right effect. No façade — an endpoint that exists but doesn't persist doesn't count. The full flow must close: HTTP → business rule → persistence → event."
      },
      look: {
        pt: ["Todos os endpoints do spec implementados e corretos", "Regras de domínio aplicadas (FK existe, amount > 0, campos obrigatórios)", "Casos de borda tratados, não só o caminho feliz", "100% dos testes de aceitação passando"],
        en: ["Every endpoint in the spec implemented and correct", "Domain rules applied (FK exists, amount > 0, required fields)", "Edge cases handled, not just the happy path", "100% of acceptance tests passing"]
      },
      how: {
        pt: "Um oráculo de caixa-preta dirige a API viva (Testcontainers) e confere cada status esperado; dotnet test roda a suíte do próprio projeto.",
        en: "A black-box oracle drives the live API (Testcontainers) and checks every expected status; dotnet test runs the project's own suite."
      }
    },
    {
      number: 2, key: "architecture", weightPct: 10, iso: "Maintainability",
      automation: "ProxyReview", diagram: "archLayers",
      title: { pt: "Arquitetura e design", en: "Architecture & Design" },
      tagline: {
        pt: "As dependências apontam para dentro, e a complexidade é proporcional ao problema?",
        en: "Do dependencies point inward, and is complexity proportional to the problem?"
      },
      body: {
        pt: "Camadas claras — apresentação, aplicação, domínio, infraestrutura — com o domínio sem conhecer a infra. Trocar o banco ou o broker não deveria reescrever regra de negócio. E simplicidade conta: abstração só onde há ponto de variação real. Overengineering é defeito, não bônus.",
        en: "Clear layers — presentation, application, domain, infrastructure — with the domain unaware of infra. Swapping the database or broker shouldn't rewrite business rules. And simplicity counts: abstraction only where there's a real variation point. Overengineering is a defect, not a bonus."
      },
      look: {
        pt: ["Separação de camadas com dependências apontando para dentro", "Controllers finos, sem regra de negócio", "Sem god classes", "Poucas interfaces com uma só implementação (proxy de overengineering)"],
        en: ["Layer separation with dependencies pointing inward", "Thin controllers, no business logic", "No god classes", "Few single-implementation interfaces (overengineering proxy)"]
      },
      how: {
        pt: "Roslyn lê os usings para checar a direção das dependências e mede tamanho de classe e interfaces especulativas. Camadas e overengineering são pontuados automaticamente a partir dessas métricas.",
        en: "Roslyn reads the usings to check dependency direction and measures class size and speculative interfaces. Layering and overengineering are scored automatically from those metrics."
      }
    },
    {
      number: 3, key: "quality", weightPct: 8, iso: "Maintainability",
      automation: "FullAuto", diagram: null,
      title: { pt: "Qualidade de código", en: "Code Quality" },
      tagline: {
        pt: "Legível, idiomático e sem sujeira?",
        en: "Readable, idiomatic and free of cruft?"
      },
      body: {
        pt: "O micro-nível: nomes expressivos, métodos curtos, sem duplicação, sem catch vazio engolindo exceção, sem código morto nem TODO pendente. Analisadores ligados via .editorconfig e o projeto limpo no dotnet format. Sem micro-otimização sem benchmark que a justifique.",
        en: "The micro level: expressive names, short methods, no duplication, no empty catch swallowing exceptions, no dead code or lingering TODOs. Analyzers on via .editorconfig and the project clean under dotnet format. No micro-optimisation without a benchmark to justify it."
      },
      look: {
        pt: ["Sem catch vazio (exceção engolida)", "Sem TODO/FIXME/HACK pendente", "Analisadores/.editorconfig habilitados", "dotnet format limpo, 0 warnings de build"],
        en: ["No empty catch (swallowed exception)", "No lingering TODO/FIXME/HACK", "Analyzers/.editorconfig enabled", "dotnet format clean, 0 build warnings"]
      },
      how: {
        pt: "Totalmente automático: Roslyn conta catches vazios e TODOs, e o modo deep roda dotnet format e checa os warnings de build.",
        en: "Full-auto: Roslyn counts empty catches and TODOs, and deep mode runs dotnet format and checks build warnings."
      }
    },
    {
      number: 4, key: "rest", weightPct: 11, iso: "Compatibility / Interoperability",
      automation: "SemiOracle", diagram: "restStatus",
      title: { pt: "Design da API REST", en: "REST API Design" },
      tagline: {
        pt: "O contrato HTTP é previsível, versionado e com erros padronizados?",
        en: "Is the HTTP contract predictable, versioned and with standardised errors?"
      },
      body: {
        pt: "Verbos e status codes corretos (nível 2 do Richardson), payloads consistentes em camelCase, paginação nas coleções, DTOs separados das entidades, versionamento e OpenAPI. Erros como Problem Details (RFC 9457, application/problem+json) — estruturados e acionáveis.",
        en: "Correct verbs and status codes (Richardson level 2), consistent camelCase payloads, pagination on collections, DTOs separate from entities, versioning and OpenAPI. Errors as Problem Details (RFC 9457, application/problem+json) — structured and actionable."
      },
      look: {
        pt: ["Verbos e status codes coerentes (201, 400, 404, 204…)", "Header Location no 201; JSON em camelCase", "Paginação, versionamento e OpenAPI expostos", "Erros em RFC 9457 (application/problem+json)"],
        en: ["Coherent verbs and status codes (201, 400, 404, 204…)", "Location header on 201; camelCase JSON", "Pagination, versioning and OpenAPI exposed", "Errors in RFC 9457 (application/problem+json)"]
      },
      how: {
        pt: "O oráculo ao vivo confere Location, camelCase e paginação na API real; Roslyn detecta verbos, ProblemDetails, DTOs e versionamento.",
        en: "The live oracle checks Location, camelCase and pagination on the real API; Roslyn detects verbs, ProblemDetails, DTOs and versioning."
      }
    },
    {
      number: 5, key: "persistence", weightPct: 10, iso: "Reliability / Performance",
      automation: "ProxyReview", diagram: "domain1n",
      title: { pt: "Persistência e banco", en: "Persistence & Database" },
      tagline: {
        pt: "O banco garante integridade, e as queries são previsíveis?",
        en: "Does the database guarantee integrity, and are the queries predictable?"
      },
      body: {
        pt: "Schema modelado (3NF como alvo), integridade referencial por PK/FK no banco, índices nas colunas de FK e de filtro, migrações versionadas (não EnsureCreated), controle de concorrência e AsNoTracking nas leituras. Sem N+1 nem varredura sequencial no caminho quente.",
        en: "Schema modelled (3NF as the target), referential integrity via PK/FK at the database level, indexes on FK and filter columns, versioned migrations (not EnsureCreated), concurrency control and AsNoTracking on reads. No N+1 and no sequential scans on the hot path."
      },
      look: {
        pt: ["Migrações versionadas, não EnsureCreated", "FK/relacionamentos e índices definidos", "Controle de concorrência (otimista, rowversion)", "AsNoTracking nas leituras"],
        en: ["Versioned migrations, not EnsureCreated", "FK/relationships and indexes defined", "Concurrency control (optimistic, rowversion)", "AsNoTracking on reads"]
      },
      how: {
        pt: "Roslyn detecta migrações, FKs, índices, concorrência e AsNoTracking; o formato do schema (heurísticas de 3NF) é pontuado automaticamente, e sinais de N+1/seq-scan usam o banco vivo no modo deep.",
        en: "Roslyn detects migrations, FKs, indexes, concurrency and AsNoTracking; schema shape (3NF heuristics) is scored automatically, and N+1/seq-scan signals use the live database in deep mode."
      }
    },
    {
      number: 6, key: "messaging", weightPct: 11, iso: "Reliability / Compatibility",
      automation: "FullAuto", diagram: "outbox",
      title: { pt: "Mensageria", en: "Messaging" },
      tagline: {
        pt: "O banco e o broker ficam consistentes, mesmo com falha?",
        en: "Do the database and the broker stay consistent, even on failure?"
      },
      body: {
        pt: "O padrão é at-least-once → consumidor idempotente: reprocessar não duplica efeito. Um Transactional Outbox resolve o dual-write (grava a mensagem na mesma transação do banco), há caminho de dead-letter, e o offset só é commitado depois do processamento — nunca antes.",
        en: "The default is at-least-once → idempotent consumer: reprocessing doesn't duplicate the effect. A Transactional Outbox solves the dual-write (it writes the message in the same DB transaction), there's a dead-letter path, and the offset is committed after processing — never before."
      },
      look: {
        pt: ["Produtor durável (Acks.All / idempotência)", "Consumidor idempotente (dedupe por id)", "Transactional Outbox (consistência banco↔broker)", "Dead-letter e commit de offset após processar"],
        en: ["Durable producer (Acks.All / idempotence)", "Idempotent consumer (dedupe by id)", "Transactional Outbox (DB↔broker consistency)", "Dead-letter and offset commit after processing"]
      },
      how: {
        pt: "Roslyn detecta produtor durável, outbox, DLQ e commit manual; no deep, o harness observa o evento real chegando no tópico transactions com a key = id.",
        en: "Roslyn detects the durable producer, outbox, DLQ and manual commit; in deep, the harness watches the real event land on the transactions topic keyed by id."
      }
    },
    {
      number: 7, key: "security", weightPct: 12, iso: "Security",
      automation: "ProxyReview", diagram: "panMask",
      title: { pt: "Segurança", en: "Security" },
      tagline: {
        pt: "Os dados do cartão estão protegidos e nada sensível vaza?",
        en: "Is the card data protected and does nothing sensitive leak?"
      },
      body: {
        pt: "Domínio crítico, peso máximo. Nada de segredo hardcoded (só env vars); todo input validado; rate limiting contra consumo abusivo. E a régua do PCI DSS: o PAN é protegido (cifrado/tokenizado/truncado) e nunca logado; dados sensíveis de autenticação (CVV, PIN, track) nunca são armazenados.",
        en: "Critical domain, top weight. No hardcoded secrets (env vars only); every input validated; rate limiting against abusive consumption. And the PCI DSS bar: the PAN is protected (encrypted/tokenised/truncated) and never logged; sensitive authentication data (CVV, PIN, track) is never stored."
      },
      look: {
        pt: ["Sem PAN válido (Luhn) embutido no código/config", "CVV/PIN/track nunca armazenados", "Validação de input e rate limiting", "Sem dependências com CVE High/Critical; TLS/HSTS em produção"],
        en: ["No Luhn-valid PAN embedded in code/config", "CVV/PIN/track never stored", "Input validation and rate limiting", "No High/Critical CVE dependencies; TLS/HSTS in production"]
      },
      how: {
        pt: "Roslyn e regex de PAN varrem código/config; SCA lista dependências vulneráveis. SAST/DAST e o teste de BOLA são pontuados automaticamente a partir da saída das ferramentas e do oráculo ao vivo.",
        en: "Roslyn and a PAN regex scan code/config; SCA lists vulnerable dependencies. SAST/DAST and the BOLA test are scored automatically from the tools' output and the live oracle."
      }
    },
    {
      number: 8, key: "resilience", weightPct: 8, iso: "Reliability",
      automation: "FullAuto", diagram: "resilience",
      title: { pt: "Resiliência e erros", en: "Resilience & Error Handling" },
      tagline: {
        pt: "Falhas transitórias são absorvidas sem intervenção manual?",
        en: "Are transient failures absorbed without manual intervention?"
      },
      body: {
        pt: "Um handler global de exceções (sem vazar stack trace ao cliente), health checks de liveness e readiness, e políticas de retry/timeout/circuit breaker (Polly) nas chamadas ao banco e ao Kafka. Graceful shutdown para drenar antes de encerrar.",
        en: "A single global exception handler (no stack traces leaking to the client), liveness and readiness health checks, and retry/timeout/circuit-breaker policies (Polly) on calls to the database and Kafka. Graceful shutdown to drain before terminating."
      },
      look: {
        pt: ["Handler global de exceções, sem vazar stack trace", "Health checks (liveness/readiness)", "Retry/timeout/circuit breaker (Polly)", "Graceful shutdown e cancelamento propagado"],
        en: ["Global exception handler, no stack-trace leak", "Health checks (liveness/readiness)", "Retry/timeout/circuit breaker (Polly)", "Graceful shutdown and propagated cancellation"]
      },
      how: {
        pt: "Roslyn detecta Polly, health checks, handler global e shutdown; o oráculo confere que um erro devolve corpo limpo, sem stack trace.",
        en: "Roslyn detects Polly, health checks, the global handler and shutdown; the oracle checks that an error returns a clean body, no stack trace."
      }
    },
    {
      number: 9, key: "testing", weightPct: 8, iso: "Enabler",
      automation: "FullAuto", diagram: "pyramid",
      title: { pt: "Testes", en: "Testing" },
      tagline: {
        pt: "Os testes provam que as regras valem — e pegam bugs?",
        en: "Do the tests prove the rules hold — and catch bugs?"
      },
      body: {
        pt: "Uma pirâmide saudável: base larga de testes unitários das regras de negócio, mais testes de integração (Testcontainers) da API, banco e mensageria. Cobertura coletada (Coverlet). Cobrir o caminho crítico, não só a porcentagem crua. Mutation testing (Stryker) é opcional.",
        en: "A healthy pyramid: a wide base of unit tests for business rules, plus integration tests (Testcontainers) of the API, database and messaging. Coverage collected (Coverlet). Cover the critical path, not just the raw percentage. Mutation testing (Stryker) is optional."
      },
      look: {
        pt: ["Projeto de teste com framework (xUnit/NUnit)", "Pirâmide: unitário + integração", "Cobertura ≥ 80% no caminho crítico (Coverlet)", "Testes de aceitação de caixa-preta"],
        en: ["Test project with a framework (xUnit/NUnit)", "Pyramid: unit + integration", "Coverage ≥ 80% on the critical path (Coverlet)", "Black-box acceptance tests"]
      },
      how: {
        pt: "Roslyn classifica a pirâmide e detecta a ferramenta de cobertura; o deep roda os testes e mede a cobertura real.",
        en: "Roslyn classifies the pyramid and detects the coverage tool; deep runs the tests and measures real coverage."
      }
    },
    {
      number: 10, key: "observability", weightPct: 4, iso: "Enabler",
      automation: "FullAuto", diagram: "pillars",
      title: { pt: "Observabilidade", en: "Observability" },
      tagline: {
        pt: "Dá para diagnosticar um incidente sem adicionar código?",
        en: "Can you diagnose an incident without adding code?"
      },
      body: {
        pt: "Os três pilares — logs, métricas e traces (modelo OpenTelemetry) — com um correlation id que atravessa a operação inteira (HTTP → evento → consumo). Logs estruturados em JSON, métricas dos quatro sinais de ouro do SRE, e endpoints /health e /metrics.",
        en: "The three pillars — logs, metrics and traces (the OpenTelemetry model) — with a correlation id that threads through the whole operation (HTTP → event → consumption). Structured JSON logs, metrics for the SRE four golden signals, and /health and /metrics endpoints."
      },
      look: {
        pt: ["OpenTelemetry (traces/métricas/logs)", "Logs estruturados em JSON", "Correlation id ponta a ponta", "/health e /metrics respondendo ao vivo"],
        en: ["OpenTelemetry (traces/metrics/logs)", "Structured JSON logs", "End-to-end correlation id", "/health and /metrics responding live"]
      },
      how: {
        pt: "Roslyn detecta o SDK do OpenTelemetry, logs estruturados e correlação; o oráculo confere /health e /metrics vivos.",
        en: "Roslyn detects the OpenTelemetry SDK, structured logs and correlation; the oracle checks /health and /metrics live."
      }
    },
    {
      number: 11, key: "performance", weightPct: 3, iso: "Performance efficiency",
      automation: "SemiOracle", diagram: null,
      title: { pt: "Performance e escala", en: "Performance & Scalability" },
      tagline: {
        pt: "Aguenta dezenas de requisições concorrentes sem 5xx nem travar?",
        en: "Does it survive dozens of concurrent requests with no 5xx and no hangs?"
      },
      body: {
        pt: "I/O assíncrono e não-bloqueante em toda parte (sem sync-over-async), API stateless para escalar horizontalmente, e paginação nas coleções. Otimização guiada por medição, não por palpite — micro-otimização sem benchmark é penalizada (veja Arquitetura e Qualidade).",
        en: "Async, non-blocking I/O throughout (no sync-over-async), a stateless API to scale horizontally, and pagination on collections. Measurement-guided optimisation, not guesswork — micro-optimisation without a benchmark is penalised (see Architecture and Quality)."
      },
      look: {
        pt: ["I/O assíncrono, sem .Wait()/.Result", "API stateless (escala horizontal)", "Paginação nas coleções", "Sobrevive à carga concorrente: 0 5xx, sem travar"],
        en: ["Async I/O, no .Wait()/.Result", "Stateless API (horizontal scaling)", "Pagination on collections", "Survives concurrent load: 0 5xx, no hangs"]
      },
      how: {
        pt: "Roslyn conta métodos async e detecta sync-over-async e estado em memória; o oráculo dispara requisições concorrentes e mede 5xx e latência.",
        en: "Roslyn counts async methods and detects sync-over-async and in-memory state; the oracle fires concurrent requests and measures 5xx and latency."
      }
    },
    {
      number: 12, key: "portability", weightPct: 2, iso: "Portability",
      automation: "FullAuto", diagram: null,
      title: { pt: "Portabilidade e deploy", en: "Portability & Deploy" },
      tagline: {
        pt: "clone → up → run funciona em qualquer máquina?",
        en: "Does clone → up → run work on any machine?"
      },
      body: {
        pt: "Configuração externalizada em env vars (12-Factor III/IV), serviços de apoio plugáveis, dependências pinadas, um Dockerfile rodando como usuário não-root e o docker-compose que sobe tudo com um comando. Uma pipeline de CI que compila, testa e faz lint.",
        en: "Config externalised in env vars (12-Factor III/IV), pluggable backing services, pinned dependencies, a Dockerfile running as a non-root user and the docker-compose that brings everything up with one command. A CI pipeline that builds, tests and lints."
      },
      look: {
        pt: ["Dockerfile (não-root) e docker-compose", "Config só via env vars (0 segredo no código)", "Dependências pinadas (lock file / global.json / CPM)", "Pipeline de CI presente"],
        en: ["Dockerfile (non-root) and docker-compose", "Config only via env vars (0 secrets in code)", "Pinned dependencies (lock file / global.json / CPM)", "CI pipeline present"]
      },
      how: {
        pt: "Totalmente automático: detecção de Dockerfile, compose, env config, pinning, CI e usuário não-root; hadolint no deep.",
        en: "Full-auto: detection of Dockerfile, compose, env config, pinning, CI and non-root user; hadolint in deep."
      }
    },
    {
      number: 13, key: "documentation", weightPct: 1, iso: "Maintainability / Usability",
      automation: "ProxyReview", diagram: null,
      title: { pt: "Documentação", en: "Documentation" },
      tagline: {
        pt: "Um dev novo sobe o projeto seguindo só o README?",
        en: "Can a new dev bring the project up following only the README?"
      },
      body: {
        pt: "Um README com propósito, stack, pré-requisitos e como rodar; a API documentada em OpenAPI/Swagger; doc comments nos contratos públicos. Presença de seções, completude do OpenAPI e cobertura de doc comments são pontuadas automaticamente.",
        en: "A README with purpose, stack, prerequisites and how to run; the API documented in OpenAPI/Swagger; doc comments on public contracts. Section presence, OpenAPI completeness and doc-comment coverage are all scored automatically."
      },
      look: {
        pt: ["README com propósito + setup + como rodar", "API documentada (OpenAPI/Swagger)", "Doc comments nos contratos públicos", "Sem links quebrados"],
        en: ["README with purpose + setup + how to run", "API documented (OpenAPI/Swagger)", "Doc comments on public contracts", "No broken links"]
      },
      how: {
        pt: "A presença de seções, a completude do OpenAPI e a cobertura de doc comments são pontuadas automaticamente.",
        en: "Section presence, OpenAPI completeness and doc-comment coverage are all scored automatically."
      }
    }
  ],

  /* ---- per-metric technical breakdown of what the evaluator actually checks ---- *
   * Keyed by criterion number. Each row is a REAL evaluator-dotnet metric:
   *   n = metric name (as it appears in the report)   w = weight
   *   t = "live" (contract oracle / HTTP probe, needs a running system, --base-url)
   *       "deep" (only runs in --deep: dotnet test/build, SAST/SCA, lint)
   *       omitted = static (Roslyn AST / packages / files — runs in every mode)
   *   how = the exact mechanism, {pt,en}                                            */
  criteriaChecks: {
    1: [
      { n: "test-project", w: 1, how: {
        pt: "Existe um csproj com 'Test' no nome, ou um pacote xunit/nunit/MSTest.",
        en: "A csproj with 'Test' in the name, or an xunit/nunit/MSTest package, exists." } },
      { n: "acceptance-blackbox", w: 1, how: {
        pt: "O AST detecta WebApplicationFactory, ou o projeto referencia o pacote Testcontainers.",
        en: "The AST detects WebApplicationFactory, or the project references the Testcontainers package." } },
      { n: "mutation-config", w: 0.5, how: {
        pt: "stryker-config.json/yaml ou pacote Stryker → bônus (Pass 0.5); ausente = Indeterminado (opcional, não penaliza).",
        en: "stryker-config.json/yaml or a Stryker package → bonus (Pass 0.5); absent = Indeterminate (optional, no penalty)." } },
      { n: "create-card-201", w: 1, t: "live", how: {
        pt: "POST /credit-cards com payload VISA válido tem de responder 201 Created (requisição HTTP real).",
        en: "POST /credit-cards with a valid VISA payload must answer 201 Created (real HTTP request)." } },
      { n: "create-card-id", w: 1, t: "live", how: {
        pt: "A resposta da criação traz o id novo no corpo JSON (aceita envelope data/value).",
        en: "The create response returns the new id in the JSON body (data/value envelope tolerated)." } },
      { n: "json-camelcase", w: 0.5, t: "live", how: {
        pt: "Toda chave do JSON de resposta é camelCase (nenhuma começa maiúscula nem contém _).",
        en: "Every response JSON key is camelCase (none starts uppercase or contains _)." } },
      { n: "card-required-400", w: 1, t: "live", how: {
        pt: "POST com cardholderName/cardNumber vazios tem de responder 400.",
        en: "POST with empty cardholderName/cardNumber must answer 400." } },
      { n: "get-card-404", w: 1, t: "live", how: {
        pt: "GET /credit-cards/{id inexistente = 999000111} tem de responder 404.",
        en: "GET /credit-cards/{missing id = 999000111} must answer 404." } },
      { n: "create-tx-201 · create-tx-id", w: 1, t: "live", how: {
        pt: "POST /transactions responde 201 e devolve o id novo no corpo.",
        en: "POST /transactions answers 201 and returns the new id in the body." } },
      { n: "create-tx-echo", w: 0.5, t: "live", how: {
        pt: "A resposta ecoa os campos persistidos: amount = 199.90 e merchant = 'Amazon'.",
        en: "The response echoes the persisted fields: amount = 199.90 and merchant = 'Amazon'." } },
      { n: "tx-amount-positive-400", w: 1.5, t: "live", how: {
        pt: "amount menor ou igual a 0 tem de responder 400 (regra de negócio, peso reforçado).",
        en: "amount less than or equal to 0 must answer 400 (business rule, weighted up)." } },
      { n: "tx-merchant-required-400", w: 1.5, t: "live", how: {
        pt: "merchant vazio tem de responder 400.",
        en: "empty merchant must answer 400." } },
      { n: "tx-fk-exists-400", w: 1.5, t: "live", how: {
        pt: "creditCardId inexistente tem de responder 400 — integridade de FK cobrada pela API.",
        en: "a non-existent creditCardId must answer 400 — FK integrity enforced at the API." } },
      { n: "list/get/put/delete · status", w: 1, t: "live", how: {
        pt: "CRUD completo dirigido: GET coleção e por id → 200; PUT existente → 200/204; DELETE existente → 204; qualquer id inexistente em GET/PUT/DELETE → 404 (várias métricas, peso 0.5–1 cada).",
        en: "Full CRUD driven: GET collection and by id → 200; PUT existing → 200/204; DELETE existing → 204; any missing id on GET/PUT/DELETE → 404 (several metrics, weight 0.5–1 each)." } },
      { n: "schemathesis", w: 1, t: "deep", how: {
        pt: "Descobre o OpenAPI servido e roda `schemathesis run <spec> --checks all -n 20`; violações → Fail; schema não-carregado/suite vazia → Indeterminado.",
        en: "Discovers the served OpenAPI and runs `schemathesis run <spec> --checks all -n 20`; violations → Fail; unloadable schema/empty suite → Indeterminate." } },
      { n: "test-pass-rate", w: 2, t: "deep", how: {
        pt: "Roda `dotnet test` uma vez (compartilhado com Testes); regex extrai Passed/Failed; nota = passed / total.",
        en: "Runs `dotnet test` once (shared with Tests); a regex extracts Passed/Failed; score = passed / total." } }
    ],
    2: [
      { n: "layering", w: 1, how: {
        pt: "Existem as três camadas por pasta: (Domain|Entities|Models) E (Infrastructure|Repositories|Data) E (Controllers|Api|Endpoints).",
        en: "The three layers exist as folders: (Domain|Entities|Models) AND (Infrastructure|Repositories|Data) AND (Controllers|Api|Endpoints)." } },
      { n: "application-layer", w: 1, how: {
        pt: "Existe pasta UseCases, Application, Services ou Handlers.",
        en: "A UseCases, Application, Services or Handlers folder exists." } },
      { n: "dependency-direction", w: 1, how: {
        pt: "Roslyn lê os `using` dos arquivos sob Domain/Entities e conta os que referenciam EntityFrameworkCore|Npgsql|Confluent.Kafka; 0 vazamentos → Pass.",
        en: "Roslyn reads the `using`s of files under Domain/Entities and counts those referencing EntityFrameworkCore|Npgsql|Confluent.Kafka; 0 leaks → Pass." } },
      { n: "overengineering-proxy", w: 0.5, how: {
        pt: "Razão (interfaces com ≤1 implementação / total de interfaces), com implementadores contados no AST; ≤0.5 → Pass; >0.5 → Indeterminado (portas DIP são normais, informativo).",
        en: "Ratio (interfaces with ≤1 implementation / total interfaces), implementers counted in the AST; ≤0.5 → Pass; >0.5 → Indeterminate (DIP ports are normal, informational)." } },
      { n: "no-god-class", w: 0.5, how: {
        pt: "O maior tipo (linhas medidas pelo AST) tem ≤600 linhas → Pass; senão Partial.",
        en: "The largest type (lines measured by the AST) is ≤600 lines → Pass; otherwise Partial." } },
      { n: "resharper", w: 0.5, t: "deep", how: {
        pt: "Se `jb` estiver instalado: `jb inspectcode <root>` e conta os `<Issue>` no XML de saída; 0 → Pass.",
        en: "If `jb` is installed: `jb inspectcode <root>`, counting `<Issue>` entries in the output XML; 0 → Pass." } }
    ],
    3: [
      { n: "no-empty-catch", w: 1, how: {
        pt: "AST: conta blocos `catch` com 0 statements (exceção engolida); contagem 0 → Pass.",
        en: "AST: counts `catch` blocks with 0 statements (swallowed exception); count 0 → Pass." } },
      { n: "no-todos", w: 1, how: {
        pt: "Regex \\b(TODO|FIXME|HACK)\\b sobre a trivia de comentário do AST; 0 → Pass, ≤3 → Partial.",
        en: "Regex \\b(TODO|FIXME|HACK)\\b over the AST comment trivia; 0 → Pass, ≤3 → Partial." } },
      { n: "analyzers-enabled", w: 1, how: {
        pt: "TreatWarningsAsErrors=true, ou EnableNETAnalyzers=true, ou existe .editorconfig.",
        en: "TreatWarningsAsErrors=true, or EnableNETAnalyzers=true, or an .editorconfig exists." } },
      { n: "format", w: 1, t: "deep", how: {
        pt: "`dotnet format --verify-no-changes`; sem mudanças pendentes → Pass.",
        en: "`dotnet format --verify-no-changes`; no pending changes → Pass." } },
      { n: "build-warnings", w: 1, t: "deep", how: {
        pt: "Contagem de warnings do build Release único do harness; 0 → Pass, ≤10 → Partial.",
        en: "Warning count from the harness's single Release build; 0 → Pass, ≤10 → Partial." } }
    ],
    4: [
      { n: "http-verbs", w: 1, how: {
        pt: "Atributos [HttpGet/Post/Put/Patch/Delete] ou invocações MapGet/MapPost/… (Richardson L2).",
        en: "[HttpGet/Post/Put/Patch/Delete] attributes or MapGet/MapPost/… invocations (Richardson L2)." } },
      { n: "status-codes", w: 1, how: {
        pt: "Uso de StatusCodes ou dos helpers Created/Ok/NoContent/BadRequest/NotFound/Conflict/….",
        en: "Use of StatusCodes or the Created/Ok/NoContent/BadRequest/NotFound/Conflict/… helpers." } },
      { n: "problem-details", w: 1, how: {
        pt: "new ProblemDetails, ou AddProblemDetails/Problem(), ou IExceptionHandler (RFC 9457).",
        en: "new ProblemDetails, or AddProblemDetails/Problem(), or IExceptionHandler (RFC 9457)." } },
      { n: "openapi", w: 1, how: {
        pt: "Pacote Swashbuckle/NSwag/Microsoft.AspNetCore.OpenApi, ou AddOpenApi/AddSwaggerGen/MapOpenApi/UseSwagger.",
        en: "Swashbuckle/NSwag/Microsoft.AspNetCore.OpenApi package, or AddOpenApi/AddSwaggerGen/MapOpenApi/UseSwagger." } },
      { n: "versioning", w: 0.5, how: {
        pt: "using Asp.Versioning ou o identificador ApiVersion.",
        en: "using Asp.Versioning or the ApiVersion identifier." } },
      { n: "dtos", w: 0.5, how: {
        pt: "Pasta Dtos/DTOs, ou tipos cujo nome contém Request/Response/Dto.",
        en: "A Dtos/DTOs folder, or types whose names contain Request/Response/Dto." } },
      { n: "openapi-populated", w: 1, t: "live", how: {
        pt: "Baixa o OpenAPI servido e conta as operações; ops > 0 → Pass; 0 (paths vazio) → Fail (contrato vazio).",
        en: "Fetches the served OpenAPI and counts operations; ops > 0 → Pass; 0 (empty paths) → Fail (empty contract)." } },
      { n: "create-card-location · create-tx-location", w: 0.5, t: "live", how: {
        pt: "As respostas 201 (cartão e transação) trazem o header Location.",
        en: "The 201 responses (card and transaction) carry a Location header." } },
      { n: "problem-details-live", w: 0.5, t: "live", how: {
        pt: "O corpo de erro tem Content-Type application/problem+json (RFC 9457).",
        en: "The error body has Content-Type application/problem+json (RFC 9457)." } },
      { n: "pagination", w: 0.5, t: "live", how: {
        pt: "Tenta pageSize=1/limit=1/perPage=1/… e exige exatamente 1 item ou metadados de paginação (a coleção é semeada com 2 cartões).",
        en: "Tries pageSize=1/limit=1/perPage=1/… and requires exactly 1 item or paging metadata (the collection is seeded with 2 cards)." } },
      { n: "spectral", w: 1, t: "deep", how: {
        pt: "Se houver arquivo openapi/swagger: `spectral lint <spec> -f json` e conta as ocorrências de severity 0 (erros).",
        en: "If an openapi/swagger file exists: `spectral lint <spec> -f json`, counting severity-0 occurrences (errors)." } },
      { n: "swagger-validate", w: 0.5, t: "deep", how: {
        pt: "`swagger-cli validate <spec>`; válido → Pass.",
        en: "`swagger-cli validate <spec>`; valid → Pass." } }
    ],
    5: [
      { n: "migrations", w: 1, how: {
        pt: "Pasta Migrations / MigrationBuilder / arquivo .sql presente E ausência de EnsureCreated; com EnsureCreated → Partial.",
        en: "Migrations folder / MigrationBuilder / a .sql file present AND no EnsureCreated; with EnsureCreated → Partial." } },
      { n: "referential-integrity", w: 1, how: {
        pt: "Roslyn deduz a relação: nav property para outra entidade DbSet, FK <Outra>Id (int/long/Guid), [ForeignKey], ou HasForeignKey/HasOne/WithMany.",
        en: "Roslyn infers the relationship: a nav property to another DbSet entity, an <Other>Id FK (int/long/Guid), [ForeignKey], or HasForeignKey/HasOne/WithMany." } },
      { n: "indexes", w: 0.5, how: {
        pt: "Invocações HasIndex ou CreateIndex.",
        en: "HasIndex or CreateIndex invocations." } },
      { n: "concurrency", w: 0.5, how: {
        pt: "[Timestamp]/[ConcurrencyCheck], ou IsRowVersion/RowVersion, ou IsConcurrencyToken().",
        en: "[Timestamp]/[ConcurrencyCheck], or IsRowVersion/RowVersion, or IsConcurrencyToken()." } },
      { n: "read-perf", w: 0.5, how: {
        pt: "AsNoTracking / AsNoTrackingWithIdentityResolution nas leituras.",
        en: "AsNoTracking / AsNoTrackingWithIdentityResolution on reads." } },
      { n: "sqlfluff", w: 0.5, t: "deep", how: {
        pt: "Se houver .sql: `sqlfluff lint --dialect postgres <dir>`; limpo → Pass, senão Partial.",
        en: "If a .sql exists: `sqlfluff lint --dialect postgres <dir>`; clean → Pass, otherwise Partial." } }
    ],
    6: [
      { n: "broker-client", w: 1, how: {
        pt: "Pacote Confluent.Kafka/MassTransit, ou genéricos IProducer/IConsumer/ProducerBuilder/ConsumerBuilder.",
        en: "Confluent.Kafka/MassTransit package, or IProducer/IConsumer/ProducerBuilder/ConsumerBuilder generics." } },
      { n: "durable-producer", w: 1, how: {
        pt: "Acesso a membro Acks.All, ou o identificador EnableIdempotence.",
        en: "Member access Acks.All, or the EnableIdempotence identifier." } },
      { n: "idempotent-consumer", w: 1, how: {
        pt: "Detecção por nomes no AST: tipo/DbSet 'Outbox', ou tipo com Inbox/ProcessedMessage/Idempot/Deduplicat no nome, ou identificador AlreadyProcessed/Idempotenc. É presença estática — não um teste de reprocessamento em runtime.",
        en: "Name detection in the AST: an 'Outbox' type/DbSet, or a type named Inbox/ProcessedMessage/Idempot/Deduplicat, or an AlreadyProcessed/Idempotenc identifier. Static presence — not a runtime reprocessing test." } },
      { n: "outbox", w: 1, how: {
        pt: "Um tipo ou DbSet<> cujo nome contém 'Outbox'.",
        en: "A type or DbSet<> whose name contains 'Outbox'." } },
      { n: "dlq", w: 0.5, how: {
        pt: "Tipo ou identificador contendo DeadLetter/Dlq.",
        en: "A type or identifier containing DeadLetter/Dlq." } },
      { n: "offset-after-process", w: 0.5, how: {
        pt: "EnableAutoCommit presente E uma invocação Commit/CommitAsync (auto-commit desligado, commit manual).",
        en: "EnableAutoCommit present AND a Commit/CommitAsync invocation (auto-commit off, manual commit)." } },
      { n: "messaging-tests", w: 0.5, how: {
        pt: "Pacote Testcontainers.Kafka, ou Testcontainers com um cliente de broker presente.",
        en: "Testcontainers.Kafka package, or Testcontainers with a broker client present." } }
    ],
    7: [
      { n: "pci-pan", w: 1, how: {
        pt: "Regex de 13–19 dígitos sobre literais string de produção + valores de appsettings.json, filtrado por Luhn; qualquer sequência válida → Fail (testes excluídos).",
        en: "A 13–19 digit regex over production string literals + appsettings.json values, filtered by Luhn; any valid sequence → Fail (tests excluded)." } },
      { n: "pci-sad", w: 1, how: {
        pt: "Identificador contendo cvv/cvc/cardverification/track2/pinblock → Fail (dado sensível de autenticação).",
        en: "An identifier containing cvv/cvc/cardverification/track2/pinblock → Fail (sensitive auth data)." } },
      { n: "authz", w: 0, how: {
        pt: "[Authorize]/AddAuthentication/AddAuthorization — reportado com peso 0 (fora de escopo: não há modelo de usuário; ausência não penaliza).",
        en: "[Authorize]/AddAuthentication/AddAuthorization — reported at weight 0 (out of scope: no user model; absence is not penalised)." } },
      { n: "validation", w: 0.5, how: {
        pt: "FluentValidation, ou [Required]/[Range]/[StringLength], ou ModelState.",
        en: "FluentValidation, or [Required]/[Range]/[StringLength], or ModelState." } },
      { n: "rate-limit", w: 0.5, how: {
        pt: "AddRateLimiter / RequireRateLimiting (OWASP API #4).",
        en: "AddRateLimiter / RequireRateLimiting (OWASP API #4)." } },
      { n: "tls", w: 0.5, how: {
        pt: "UseHsts → Pass; só UseHttpsRedirection → Partial; nada → Fail (a tarefa proíbe forçar redirect na porta HTTP do container).",
        en: "UseHsts → Pass; UseHttpsRedirection only → Partial; neither → Fail (the task forbids forcing a redirect on the container's HTTP port)." } },
      { n: "secrets", w: 1, how: {
        pt: "`gitleaks detect --source <root> --no-git --no-banner`; exit 0 → Pass.",
        en: "`gitleaks detect --source <root> --no-git --no-banner`; exit 0 → Pass." } },
      { n: "sca", w: 1, t: "deep", how: {
        pt: "`dotnet list package --vulnerable --include-transitive`; procura 'vulnerable' ou > High/Critical no output.",
        en: "`dotnet list package --vulnerable --include-transitive`; looks for 'vulnerable' or > High/Critical in the output." } },
      { n: "sca-trivy", w: 0.5, t: "deep", how: {
        pt: "`trivy fs --scanners vuln --severity HIGH,CRITICAL --exit-code 1 <root>`; FATAL (DB/rede) → Indeterminado.",
        en: "`trivy fs --scanners vuln --severity HIGH,CRITICAL --exit-code 1 <root>`; FATAL (DB/network) → Indeterminate." } },
      { n: "sast", w: 1, t: "deep", how: {
        pt: "`semgrep --error --config auto --exclude .github <root>`; exit 0 → Pass, senão Partial.",
        en: "`semgrep --error --config auto --exclude .github <root>`; exit 0 → Pass, otherwise Partial." } }
    ],
    8: [
      { n: "resilience-policies", w: 1, how: {
        pt: "Pacote Polly/Microsoft.Extensions.Http.Resilience, ou AddResilienceHandler/WaitAndRetry/AddPolicyHandler/AddStandardResilienceHandler.",
        en: "Polly/Microsoft.Extensions.Http.Resilience package, or AddResilienceHandler/WaitAndRetry/AddPolicyHandler/AddStandardResilienceHandler." } },
      { n: "health-checks", w: 1, how: {
        pt: "AddHealthChecks / MapHealthChecks / UseHealthChecks.",
        en: "AddHealthChecks / MapHealthChecks / UseHealthChecks." } },
      { n: "global-error-handling", w: 1, how: {
        pt: "IExceptionHandler, ou UseExceptionHandler/UseProblemDetails/AddProblemDetails.",
        en: "IExceptionHandler, or UseExceptionHandler/UseProblemDetails/AddProblemDetails." } },
      { n: "graceful-shutdown", w: 0.5, how: {
        pt: "IHostApplicationLifetime/BackgroundService, ApplicationStopping, ou StopAsync.",
        en: "IHostApplicationLifetime/BackgroundService, ApplicationStopping, or StopAsync." } },
      { n: "timeouts", w: 0.5, how: {
        pt: "AddRequestTimeouts, ou CommandTimeout/CancellationToken.",
        en: "AddRequestTimeouts, or CommandTimeout/CancellationToken." } },
      { n: "no-stacktrace-leak", w: 1, t: "live", how: {
        pt: "Envia um JSON malformado; falha se o status for ≥500 ou se o corpo contiver marcadores de exceção (StackTrace, '   at ', .cs:line, EntityFrameworkCore, Npgsql., DbUpdateException).",
        en: "Sends a malformed JSON; fails if the status is ≥500 or the body contains exception markers (StackTrace, '   at ', .cs:line, EntityFrameworkCore, Npgsql., DbUpdateException)." } }
    ],
    9: [
      { n: "test-framework", w: 0.5, how: {
        pt: "csproj com 'Test' no nome, ou pacote xunit/nunit/MSTest (peso baixo — evita auto-avaliação).",
        en: "csproj with 'Test' in the name, or an xunit/nunit/MSTest package (low weight — reduces self-grading)." } },
      { n: "pyramid", w: 1, how: {
        pt: "Pastas UnitTests/IntegrationTests, ou Testcontainers, ou WebApplicationFactory (unit + integração).",
        en: "UnitTests/IntegrationTests folders, or Testcontainers, or WebApplicationFactory (unit + integration)." } },
      { n: "coverage-tool", w: 0.5, how: {
        pt: "Pacote coverlet.",
        en: "coverlet package." } },
      { n: "mutation-tool", w: 0.5, how: {
        pt: "Config/pacote Stryker → Pass (bônus); ausente = Indeterminado (opcional).",
        en: "Stryker config/package → Pass (bonus); absent = Indeterminate (optional)." } },
      { n: "coverage", w: 2, t: "deep", how: {
        pt: "Coleta XPlat Code Coverage no `dotnet test`, faz merge de todos os coverage.cobertura.xml (união das linhas cobertas); LineRate ≥80% → Pass, ≥50% → Partial.",
        en: "Collects XPlat Code Coverage on `dotnet test`, merges every coverage.cobertura.xml (union of covered lines); LineRate ≥80% → Pass, ≥50% → Partial." } }
    ],
    10: [
      { n: "otel", w: 1, how: {
        pt: "Pacote OpenTelemetry, ou using OpenTelemetry.",
        en: "OpenTelemetry package, or using OpenTelemetry." } },
      { n: "structured-logs", w: 1, how: {
        pt: "Pacote Serilog, ou AddJsonConsole/UseSerilog/AddSerilog.",
        en: "Serilog package, or AddJsonConsole/UseSerilog/AddSerilog." } },
      { n: "metrics-endpoint", w: 0.5, how: {
        pt: "AddPrometheusExporter/MapPrometheusScrapingEndpoint, ou o tipo Meter.",
        en: "AddPrometheusExporter/MapPrometheusScrapingEndpoint, or the Meter type." } },
      { n: "correlation", w: 0.5, how: {
        pt: "Identificador CorrelationId/TraceId/traceparent, ou acesso a Activity.Current.",
        en: "A CorrelationId/TraceId/traceparent identifier, or Activity.Current access." } },
      { n: "health-endpoint", w: 0.5, how: {
        pt: "MapHealthChecks / AddHealthChecks.",
        en: "MapHealthChecks / AddHealthChecks." } },
      { n: "live-health", w: 1, t: "live", how: {
        pt: "GET {base}/health tem de responder 2xx/3xx no sistema vivo.",
        en: "GET {base}/health must answer 2xx/3xx on the live system." } },
      { n: "live-metrics", w: 0.5, t: "live", how: {
        pt: "GET {base}/metrics tem de responder 2xx/3xx no sistema vivo.",
        en: "GET {base}/metrics must answer 2xx/3xx on the live system." } }
    ],
    11: [
      { n: "async-io", w: 1, how: {
        pt: "AST: contagem de métodos marcados async > 0.",
        en: "AST: count of methods marked async > 0." } },
      { n: "no-sync-over-async", w: 1, how: {
        pt: "AST: presença de invocações .Wait() ou .GetResult() → Partial (bloqueio sync-over-async).",
        en: "AST: presence of .Wait() or .GetResult() invocations → Partial (sync-over-async blocking)." } },
      { n: "stateless", w: 1, how: {
        pt: "AddSession, ou > 3 campos static mutáveis (não const/readonly) → Partial (estado em memória).",
        en: "AddSession, or > 3 mutable static fields (not const/readonly) → Partial (in-memory state)." } },
      { n: "pagination", w: 0.5, how: {
        pt: "Identificador pageSize/pageNumber, ou invocações Skip/Take.",
        en: "A pageSize/pageNumber identifier, or Skip/Take invocations." } },
      { n: "concurrency", w: 1, t: "live", how: {
        pt: "Dispara 60 GETs com 20 concorrentes num endpoint real; 0 respostas 5xx e 0 falhas de transporte → Pass.",
        en: "Fires 60 GETs at 20 concurrent against a real endpoint; 0 5xx responses and 0 transport failures → Pass." } },
      { n: "load", w: 2, t: "deep", how: {
        pt: "Se BENCH_K6_SCRIPT estiver definido: `k6 run -e BASE_URL=… <script>`; thresholds de SLO atingidos → Pass.",
        en: "If BENCH_K6_SCRIPT is set: `k6 run -e BASE_URL=… <script>`; SLO thresholds met → Pass." } }
    ],
    12: [
      { n: "dockerfile", w: 1, how: {
        pt: "Existe um arquivo Dockerfile.",
        en: "A Dockerfile exists." } },
      { n: "compose", w: 1, how: {
        pt: "docker-compose*.yml, ou compose.yaml/compose.yml.",
        en: "docker-compose*.yml, or compose.yaml/compose.yml." } },
      { n: "env-config", w: 1, how: {
        pt: "GetEnvironmentVariable, ou IConfiguration, ou builder.Configuration (12-Factor III/IV).",
        en: "GetEnvironmentVariable, or IConfiguration, or builder.Configuration (12-Factor III/IV)." } },
      { n: "pinning", w: 0.5, how: {
        pt: "packages.lock.json, ou global.json, ou Directory.Packages.props com ManagePackageVersionsCentrally=true (lido via XML).",
        en: "packages.lock.json, or global.json, or Directory.Packages.props with ManagePackageVersionsCentrally=true (read via XML)." } },
      { n: "ci", w: 0.5, how: {
        pt: ".github/workflows/, ou .gitlab-ci.yml/azure-pipelines.yml.",
        en: ".github/workflows/, or .gitlab-ci.yml/azure-pipelines.yml." } },
      { n: "non-root", w: 0.5, how: {
        pt: "Regex ^\\s*USER\\s+ (multiline) encontra uma diretiva USER no Dockerfile.",
        en: "Regex ^\\s*USER\\s+ (multiline) finds a USER directive in the Dockerfile." } },
      { n: "hadolint", w: 0.5, t: "deep", how: {
        pt: "`hadolint <Dockerfile>`; limpo → Pass, senão Partial.",
        en: "`hadolint <Dockerfile>`; clean → Pass, otherwise Partial." } },
      { n: "outdated", w: 0.5, t: "deep", how: {
        pt: "`dotnet-outdated <root>`.",
        en: "`dotnet-outdated <root>`." } }
    ],
    13: [
      { n: "readme", w: 1, how: {
        pt: "Existe README.md.",
        en: "A README.md exists." } },
      { n: "readme-sections", w: 1, how: {
        pt: "Três regex sobre o README (purpose/overview; setup/install/prereq; run/usage/docker compose); nota = seções encontradas / 3.",
        en: "Three regexes over the README (purpose/overview; setup/install/prereq; run/usage/docker compose); score = sections found / 3." } },
      { n: "api-docs", w: 0.5, how: {
        pt: "Pacote Swashbuckle/NSwag/OpenApi, ou AddOpenApi/AddSwaggerGen.",
        en: "Swashbuckle/NSwag/OpenApi package, or AddOpenApi/AddSwaggerGen." } },
      { n: "doc-comments", w: 0.5, how: {
        pt: "GenerateDocumentationFile=true, ou > 5 comentários de documentação no AST.",
        en: "GenerateDocumentationFile=true, or > 5 documentation comments in the AST." } },
      { n: "markdownlint", w: 0.5, t: "deep", how: {
        pt: "`markdownlint -c <ruleset do benchmark> <README>` (MD013/MD034 desligados); crash de Node → Indeterminado.",
        en: "`markdownlint -c <benchmark ruleset> <README>` (MD013/MD034 off); Node crash → Indeterminate." } },
      { n: "links", w: 0.5, t: "deep", how: {
        pt: "`lychee --exclude-loopback --exclude localhost <README>`; sem links quebrados → Pass.",
        en: "`lychee --exclude-loopback --exclude localhost <README>`; no broken links → Pass." } }
    ]
  }
};
