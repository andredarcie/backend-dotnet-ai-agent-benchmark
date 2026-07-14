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
      "nav.criteria": "Os critérios",
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
      "hero.lede": "Cada modelo de IA recebe exatamente o mesmo pedido — construir uma API REST de cartão de crédito em .NET 10, com PostgreSQL e Kafka, tudo em Docker. Um avaliador automático percorre 8 categorias de engenharia — as que o sistema rodando consegue provar — e devolve uma nota ponderada de 0 a 5.",
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
      "hero.pipeline.scoreSub": "ponderada por 8 pesos",

      "how.eyebrow": "O funil",
      "how.title": "Do prompt à nota, sem opinião humana no caminho crítico",
      "how.lede": "O mesmo prompt vira um projeto inteiro. O avaliador em .NET 10 lê esse projeto de duas formas e transforma o que encontra em números reproduzíveis.",
      "how.static.title": "Modo light (estático)",
      "how.static.body": "Analisa o código-fonte com a árvore sintática do Roslyn e detecta pacotes/arquivos. Rápido, sem Docker, sem rede. A mesma fonte sempre produz a mesma nota.",
      "how.deep.title": "Modo deep (dinâmico)",
      "how.deep.body": "Sobe o projeto de verdade (app + Postgres + Kafka), dirige um oráculo de contrato contra a API viva, roda os testes com cobertura e as ferramentas locais (dotnet format, gitleaks, hadolint, SCA do NuGet) — e só então escreve o relatório. Tudo offline e determinístico.",
      "how.note": "Só as runs deep entram no ranking: elas exercitam o sistema de ponta a ponta. As categorias estáticas são determinísticas; as de runtime variam com a máquina, por isso repetimos as runs (veja Metodologia).",
      "how.passes.title": "Cada modelo constrói em duas passagens",
      "how.pass1.title": "Passagem 1 — Build",
      "how.pass1.body": "O modelo recebe o PROMPT.md e constrói o projeto inteiro — API, PostgreSQL, Kafka, testes e Docker.",
      "how.pass2.title": "Passagem 2 — Revisão",
      "how.pass2.body": "O mesmo modelo recebe o PROMPT-REVIEW.md: revisa criticamente o próprio trabalho contra o brief e verifica do jeito que julgar melhor — ler, buildar, testar, subir o sistema — e aplica o patch final. Ele decide como se convencer; a segunda chance que todo modelo recebe.",
      "how.roslyn.title": "Roslyn AST, não regex",
      "how.roslyn.body": "As checagens estáticas usam o compilador do C# como ferramenta de leitura: direção de dependência entre camadas, catch vazio, interfaces com uma única implementação, god classes. É medição, não busca de texto.",
      "how.oracle.title": "Oráculo de contrato ao vivo",
      "how.oracle.body": "No modo deep o avaliador vira um cliente da API: cria cartão e transação, confere 201/Location/id, força os 400 (FK inexistente, amount ≤ 0, campo obrigatório vazio) e os 404, e observa o evento real chegando no tópico do Kafka. A superfície é leitura + criação — não há PUT nem DELETE.",

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
      "criteria.title": "Os critérios, por peso",
      "criteria.lede": "Oito categorias pontuam (os pesos somam 100%); três são informativas — medidas e reportadas, mas fora da nota, porque a 1–4% nunca separariam duas submissões e cada uma duplicava um sinal que o run já decide. A cor marca quão direta é a medição — todas as notas saem 100% da máquina. Clique para abrir a explicação e o diagrama.",
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
      "criteria.informational": "informativo",
      "criteria.tag.live": "ao vivo",
      "criteria.tag.deep": "deep",

      "auto.FullAuto": "determinístico",
      "auto.SemiOracle": "oráculo",
      "auto.ProxyReview": "proxy",
      "auto.FullAuto.desc": "Pontuado 100% pela máquina a partir de análise estática — o mesmo código-fonte sempre gera a mesma nota.",
      "auto.SemiOracle.desc": "Pontuado 100% pela máquina a cada run contra um oráculo/limiar definido uma vez (suite de aceitação, status esperados).",
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
      "scoring.weights.body": "Domínio crítico (cartão de crédito): o peso está onde o sistema rodando prova alguma coisa — Correção (20%, o oráculo vivo), REST, Segurança, Persistência e Mensageria. Documentação, Portabilidade e Observabilidade não pontuam: a 1–4% não decidiam nada e duplicavam o portão de executabilidade. Pesos são uma calibração deliberada — não há consenso externo sobre eles.",

      "method.eyebrow": "Por que confiar no número",
      "method.title": "Metodologia",
      "method.lede": "Modelos são estocásticos: o mesmo prompt gera um projeto diferente a cada vez. Uma única submissão é uma amostra fraca.",
      "method.multi.title": "Muitas runs, mediana",
      "method.multi.body": "O leaderboard agrupa as runs por modelo, ordena pela mediana das runs deep e mostra a dispersão (±σ, média, faixa, contagem). Modelos com menos de 5 runs ficam marcados como provisórios — trate diferenças dentro da dispersão como empate.",
      "method.det.title": "Determinístico vs. runtime",
      "method.det.body": "Categorias estáticas (Estático, Arquitetura, Qualidade, Performance) são determinísticas dado o Roslyn. Categorias de runtime (build/boot, funcional, evento Kafka) dependem de Docker e da máquina, então variam entre runs.",
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
      "lb.col.effort": "Effort",
      "lb.col.duration": "Tempo",
      "lb.col.cost": "Custo",
      "lb.col.spread": "Dispersão (média ±σ, faixa)",
      "lb.col.build": "Build",
      "lb.col.boot": "Boot",
      "lb.provisional": "provisório (< 5 runs)",
      "lb.singleRun": "run única",
      "lb.generated": "Gerado em",
      "lb.empty": "Leaderboard zerado — de propósito. Todas as runs anteriores foram avaliadas pela rubrica antiga (13 categorias) e por um conjunto de ferramentas que já não existe: os relatórios publicados citavam métricas que NENHUM avaliador deste repo consegue mais emitir. Um número que o código atual não regenera não é resultado, é alegação. As submissões foram apagadas, e o placar recomeça contra a rubrica descrita aqui.",
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
      "nav.criteria": "The criteria",
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
      "hero.lede": "Every AI model gets the exact same brief — build a .NET 10 credit-card REST API with PostgreSQL and Kafka, all in Docker. An automated evaluator walks 8 engineering categories — the ones the running system can actually prove — and returns a weighted 0–5 score.",
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
      "hero.pipeline.scoreSub": "weighted across 8 categories",

      "how.eyebrow": "The pipeline",
      "how.title": "From prompt to score, with no human on the critical path",
      "how.lede": "The same prompt becomes a whole project. The .NET 10 evaluator reads that project two ways and turns what it finds into reproducible numbers.",
      "how.static.title": "Light mode (static)",
      "how.static.body": "Analyses the source with Roslyn's syntax tree and detects packages/files. Fast, no Docker, no network. The same source always yields the same score.",
      "how.deep.title": "Deep mode (dynamic)",
      "how.deep.body": "Boots the project for real (app + Postgres + Kafka), drives a contract oracle against the live API, runs the tests with coverage and the local tools (dotnet format, gitleaks, hadolint, NuGet SCA) — and only then writes the report. All offline and deterministic.",
      "how.note": "Only deep runs enter the ranking: they exercise the system end to end. Static categories are deterministic; runtime ones vary with the host, which is why runs are repeated (see Methodology).",
      "how.passes.title": "Each model builds in two passes",
      "how.pass1.title": "Pass 1 — Build",
      "how.pass1.body": "The model receives PROMPT.md and builds the whole project — API, PostgreSQL, Kafka, tests and Docker.",
      "how.pass2.title": "Pass 2 — Review",
      "how.pass2.body": "The same model receives PROMPT-REVIEW.md: it critically reviews its own work against the brief and verifies it however it judges best — read, build, test, run the system — then applies the final patch. It decides how to convince itself; the second chance every model gets.",
      "how.roslyn.title": "Roslyn AST, not regex",
      "how.roslyn.body": "The static checks use the C# compiler as a reading tool: layer dependency direction, empty catch, single-implementation interfaces, god classes. It's measurement, not text search.",
      "how.oracle.title": "Live contract oracle",
      "how.oracle.body": "In deep mode the evaluator becomes an API client: it creates a card and a transaction, checks 201/Location/id, forces the 400s (missing FK, amount ≤ 0, empty required field) and the 404s, and watches the real event land on the Kafka topic. The surface is read + create — there is no PUT and no DELETE.",

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
      "criteria.title": "The criteria, by weight",
      "criteria.lede": "Eight categories are scored (the weights sum to 100%); three are informational — measured and reported, but out of the score, because at 1–4% they could never separate two submissions and each duplicated a signal the run already decides. The colour marks how directly it is measured — every score is produced 100% by the machine. Click to open the explanation and diagram.",
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
      "criteria.informational": "informational",
      "criteria.tag.live": "live",
      "criteria.tag.deep": "deep",

      "auto.FullAuto": "deterministic",
      "auto.SemiOracle": "oracle",
      "auto.ProxyReview": "proxy",
      "auto.FullAuto.desc": "Scored 100% by the machine from static analysis — the same source always yields the same score.",
      "auto.SemiOracle.desc": "Scored 100% by the machine every run against a fixed oracle/threshold defined once (acceptance suite, expected status codes).",
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
      "scoring.weights.body": "Critical domain (a credit card): the weight sits where the running system proves something — Correctness (20%, the live oracle), REST, Security, Persistence and Messaging. Documentation, Portability and Observability carry none: at 1–4% they decided nothing and duplicated the executability gate. Weights are a deliberate calibration — there's no external consensus on them.",

      "method.eyebrow": "Why trust the number",
      "method.title": "Methodology",
      "method.lede": "Models are stochastic: the same prompt yields a different project each time. A single submission is a weak sample.",
      "method.multi.title": "Many runs, median",
      "method.multi.body": "The leaderboard groups runs per model, ranks by the median of the deep runs and shows the spread (±σ, mean, range, count). Models with fewer than 5 runs are flagged provisional — treat gaps within the spread as ties.",
      "method.det.title": "Deterministic vs. runtime",
      "method.det.body": "Static categories (Static, Architecture, Quality, Performance) are deterministic given Roslyn. Runtime categories (build/boot, functional, the Kafka event) depend on Docker and the host, so they vary run to run.",
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
      "lb.col.effort": "Effort",
      "lb.col.duration": "Time",
      "lb.col.cost": "Cost",
      "lb.col.spread": "Spread (mean ±σ, range)",
      "lb.col.build": "Build",
      "lb.col.boot": "Boot",
      "lb.provisional": "provisional (< 5 runs)",
      "lb.singleRun": "single run",
      "lb.generated": "Generated",
      "lb.empty": "The leaderboard was reset — on purpose. Every earlier run was graded under the old 13-category rubric and a tool set that no longer exists: the published reports cited metrics NO evaluator in this repo can emit any more. A number the current code cannot regenerate is not a result, it is a claim. The submissions were deleted, and the board restarts against the rubric described here.",
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

  /* ---- the criteria: 8 scored (weights sum to 100) + 3 informational (weightPct 0) ----
     Language-neutral facts + {pt,en} prose. `informational: true` => reported, never ranked. */
  criteria: [
    {
      number: 1, key: "functional", weightPct: 20, iso: "Functional suitability",
      automation: "SemiOracle", diagram: "requestFlow",
      title: { pt: "Correção funcional e testes", en: "Functional Correctness & Tests" },
      tagline: {
        pt: "O sistema faz o que promete, do começo ao fim?",
        en: "Does the system do what it promises, end to end?"
      },
      body: {
        pt: "O critério mais básico e, de longe, o de maior peso. Quem decide é o oráculo de contrato: o avaliador dirige a API viva por HTTP real, contra o Postgres e o Kafka de verdade, e cobra o contrato documentado. É o único sinal que o modelo não consegue escrever a seu favor — por isso a suíte de testes do próprio projeto entrou aqui ao lado dele, com peso baixo, em vez de ser uma categoria à parte: uma suíte que o modelo escreve para se auto-avaliar não é um sinal independente.",
        en: "The most basic criterion and, by far, the heaviest. What decides it is the contract oracle: the evaluator drives the live API over real HTTP, against the real Postgres and Kafka, and holds it to the documented contract. It is the one signal a model cannot write in its own favour — which is why the project's own test suite now sits here beside it, at low weight, instead of standing as its own category: a suite the model writes to grade itself is not an independent signal."
      },
      look: {
        pt: ["Todos os endpoints do spec implementados e corretos", "Regras de domínio aplicadas (FK existe, amount > 0, campos obrigatórios)", "Casos de borda tratados, não só o caminho feliz", "Testes unitários de verdade — e só unitários (Testcontainers reprova)", "Cobertura ≥ 60% no que importa (crédito parcial a partir de 35%)"],
        en: ["Every endpoint in the spec implemented and correct", "Domain rules applied (FK exists, amount > 0, required fields)", "Edge cases handled, not just the happy path", "Real unit tests — and unit only (Testcontainers is a Fail)", "Coverage ≥ 60% on the code that matters (half credit from 35%)"]
      },
      how: {
        pt: "Um oráculo de caixa-preta — o próprio avaliador dirigindo a API viva por HTTP — confere cada status esperado (201, 400, 404) da superfície de leitura + criação e carrega a maior parte do peso. A suíte do projeto roda uma única vez (dotnet test + Coverlet) e entrega, dessa mesma execução, a taxa de aprovação e a cobertura.",
        en: "A black-box oracle — the evaluator itself driving the live API over HTTP — checks every expected status (201, 400, 404) of the read + create surface, and carries most of the weight. The project's suite runs exactly once (dotnet test + Coverlet), and that single run yields both its pass rate and its coverage."
      }
    },
    {
      number: 2, key: "architecture", weightPct: 12, iso: "Maintainability",
      automation: "ProxyReview", diagram: "archLayers",
      title: { pt: "Arquitetura e design", en: "Architecture & Design" },
      tagline: {
        pt: "As dependências apontam para dentro, e a complexidade é proporcional ao problema?",
        en: "Do dependencies point inward, and is complexity proportional to the problem?"
      },
      body: {
        pt: "Camadas claras — apresentação, aplicação, domínio, infraestrutura — com o domínio sem conhecer a infra. Trocar o banco ou o broker não deveria reescrever regra de negócio. E simplicidade conta de verdade: aqui mora a métrica que torna o YAGNI exigível. Entregar o que o brief mandou NÃO construir — um PUT, um outbox, um consumer, OpenTelemetry, versionamento de API — é defeito, não ambição. Não é engenharia; é não ter lido o enunciado.",
        en: "Clear layers — presentation, application, domain, infrastructure — with the domain unaware of infra. Swapping the database or broker shouldn't rewrite business rules. And simplicity genuinely counts: this is where the metric that makes YAGNI enforceable lives. Shipping what the brief said NOT to build — a PUT, an outbox, a consumer, OpenTelemetry, API versioning — is a defect, not ambition. It isn't engineering; it's not having read the brief."
      },
      look: {
        pt: ["Separação de camadas com dependências apontando para dentro", "Controllers finos, sem regra de negócio", "Sem god classes", "Zero gold-plating: nada de PUT/DELETE, outbox, consumer, OTel ou versionamento"],
        en: ["Layer separation with dependencies pointing inward", "Thin controllers, no business logic", "No god classes", "Zero gold-plating: no PUT/DELETE, outbox, consumer, OTel or versioning"]
      },
      how: {
        pt: "Roslyn lê os usings para checar a direção das dependências, mede o tamanho das classes e detecta a maquinaria que o brief proibiu. O antigo proxy de overengineering (interfaces com uma só implementação) foi aposentado: essas interfaces são justamente o encaixe de inversão de dependência que esta mesma categoria premia — na prática ele nunca reprovava ninguém.",
        en: "Roslyn reads the usings to check dependency direction, measures class size, and detects the machinery the brief ruled out. The old overengineering proxy (single-implementation interfaces) is retired: those interfaces are precisely the dependency-inversion seam this same category rewards — in practice it could never fail anyone."
      }
    },
    {
      number: 3, key: "quality", weightPct: 10, iso: "Maintainability",
      automation: "FullAuto", diagram: null,
      title: { pt: "Qualidade de código", en: "Code Quality" },
      tagline: {
        pt: "Legível, idiomático e sem sujeira?",
        en: "Readable, idiomatic and free of cruft?"
      },
      body: {
        pt: "O micro-nível: nomes expressivos, métodos curtos, sem catch vazio engolindo exceção, sem código morto nem TODO pendente. Analisadores ligados via .editorconfig e o projeto limpo no dotnet format. O I/O assíncrono passou a morar aqui: um .Result ou .Wait() no caminho da requisição é bug de starvation do thread-pool no ASP.NET Core — logo, é defeito de código, e reprova.",
        en: "The micro level: expressive names, short methods, no empty catch swallowing exceptions, no dead code or lingering TODOs. Analyzers on via .editorconfig and the project clean under dotnet format. Async I/O now lives here: a .Result or .Wait() on the request path is thread-pool starvation in ASP.NET Core — so it is a code defect, and it fails."
      },
      look: {
        pt: ["Sem catch vazio (exceção engolida)", "Sem TODO/FIXME/HACK pendente", "Analisadores/.editorconfig habilitados", "I/O assíncrono, sem sync-over-async (.Result/.Wait())", "dotnet format limpo, 0 warnings de build"],
        en: ["No empty catch (swallowed exception)", "No lingering TODO/FIXME/HACK", "Analyzers/.editorconfig enabled", "Async I/O, no sync-over-async (.Result/.Wait())", "dotnet format clean, 0 build warnings"]
      },
      how: {
        pt: "Totalmente automático: Roslyn conta catches vazios, TODOs e chamadas bloqueantes; o modo deep roda dotnet format e lê os warnings do build de Release (o mesmo build que gateia a executabilidade — não há segundo build).",
        en: "Full-auto: Roslyn counts empty catches, TODOs and blocking calls; deep mode runs dotnet format and reads the warnings from the Release build (the same build that gates executability — no second build is run)."
      }
    },
    {
      number: 4, key: "rest", weightPct: 14, iso: "Compatibility / Interoperability",
      automation: "SemiOracle", diagram: "restStatus",
      title: { pt: "Design da API REST", en: "REST API Design" },
      tagline: {
        pt: "O contrato HTTP é previsível — e o que a API responde de verdade bate com o que ela promete?",
        en: "Is the HTTP contract predictable — and does what the API really answers match what it promises?"
      },
      body: {
        pt: "Verbos e status corretos (nível 2 de Richardson), erros padronizados em RFC 9457 (application/problem+json), DTOs na entrada e na saída, JSON em camelCase, coleções paginadas e um OpenAPI que realmente descreve os endpoints. Quase tudo aqui é cobrado no sistema vivo: um spec servido e vazio ('paths': {}) é defeito real, e a detecção por presença deixava passar batido.",
        en: "Correct verbs and status codes (Richardson level 2), standardised errors in RFC 9457 (application/problem+json), DTOs in and out, camelCase JSON, paginated collections and an OpenAPI that actually describes the endpoints. Almost all of it is asserted on the running system: a served-but-empty spec ('paths': {}) is a real defect, and presence-detection silently passed it."
      },
      look: {
        pt: ["201 com header Location na criação", "Erros em application/problem+json (RFC 9457)", "JSON camelCase e paginação que respeita o page size pedido", "OpenAPI servido e populado (spec vazio = reprova; spec ausente = reprova)", "DTOs — nunca a entidade do EF exposta"],
        en: ["201 with a Location header on create", "Errors in application/problem+json (RFC 9457)", "camelCase JSON and pagination that honours the requested page size", "OpenAPI served and populated (empty spec = Fail; no spec = Fail)", "DTOs — never the EF entity exposed"]
      },
      how: {
        pt: "O oráculo observa a forma real da resposta (Location, media type, camelCase, page size) e o probe busca o documento OpenAPI servido e conta as operações declaradas. Versionamento de API saiu da rubrica: há uma versão de uma API — versioná-la é cerimônia, e agora conta como gold-plating.",
        en: "The oracle observes the real response shape (Location, media type, camelCase, page size) and the probe fetches the served OpenAPI document and counts the operations it declares. API versioning is out of the rubric: there is one version of one API — versioning it is ceremony, and it now counts as gold-plating."
      }
    },
    {
      number: 5, key: "persistence", weightPct: 13, iso: "Reliability / Performance",
      automation: "ProxyReview", diagram: "domain1n",
      title: { pt: "Persistência e banco", en: "Persistence & Database" },
      tagline: {
        pt: "O banco garante integridade, e as queries são previsíveis?",
        en: "Does the database guarantee integrity, and are the queries predictable?"
      },
      body: {
        pt: "Integridade referencial por PK/FK no próprio banco, índices nas colunas de FK e de filtro, migrações versionadas (não EnsureCreated) e AsNoTracking nas leituras. Controle de concorrência otimista (rowversion) saiu da rubrica — e do enunciado: a superfície é leitura + criação, não existe UPDATE em lugar nenhum, então um token de concorrência protege contra um conflito de escrita que não pode acontecer. Exigi-lo era a rubrica contrariando o próprio YAGNI.",
        en: "Referential integrity via PK/FK in the database itself, indexes on FK and filter columns, versioned migrations (not EnsureCreated) and AsNoTracking on reads. Optimistic concurrency (rowversion) is out of the rubric — and out of the brief: the surface is read + create, there is no UPDATE anywhere, so a concurrency token guards against a write conflict that cannot happen. Demanding it was the rubric contradicting its own YAGNI rule."
      },
      look: {
        pt: ["Migrações versionadas, não EnsureCreated", "FK/relacionamentos e índices definidos", "AsNoTracking nas leituras", "Sem N+1 no caminho quente"],
        en: ["Versioned migrations, not EnsureCreated", "FK/relationships and indexes defined", "AsNoTracking on reads", "No N+1 on the hot path"]
      },
      how: {
        pt: "Roslyn detecta migrações, FKs, índices e AsNoTracking; depois o schema é exercitado de verdade — o oráculo cria cartões e transações contra o Postgres que a submissão subiu, então migração que não aplica ou mapeamento quebrado aparece como check de contrato falhando (um 500 no lugar do 201), não como opinião estática.",
        en: "Roslyn detects migrations, FKs, indexes and AsNoTracking; then the schema is exercised for real — the oracle creates cards and transactions against the Postgres the submission booted, so a migration that doesn't apply or a broken mapping surfaces as a failed contract check (a 500 instead of a 201), not as a static opinion."
      }
    },
    {
      number: 6, key: "messaging", weightPct: 13, iso: "Reliability / Compatibility",
      automation: "FullAuto", diagram: "produce",
      title: { pt: "Mensageria (Kafka)", en: "Messaging (Kafka)" },
      tagline: {
        pt: "O evento chega mesmo no tópico — e uma queda do broker não derruba a requisição?",
        en: "Does the event really land on the topic — and does a broker hiccup leave the request alone?"
      },
      body: {
        pt: "Escopo é só o lado publicador — a essência. Um producer durável (acks=all / idempotência) publica o evento no tópico transactions, chaveado pelo id, depois que a linha foi persistida. E a publicação é desacoplada do sucesso da requisição: broker fora do ar vira catch-and-log, não um 500 depois que o dado já foi salvo. Consumer, outbox e DLQ estão fora de escopo — construí-los conta como gold-plating.",
        en: "Scope is the publishing side only — the essence. A durable producer (acks=all / idempotence) publishes the event to the transactions topic, keyed by id, after the row is persisted. And publishing is decoupled from the request's success: a broker outage is caught-and-logged, not turned into a 500 once the data is saved. Consumer, outbox and DLQ are out of scope — building them counts as gold-plating."
      },
      look: {
        pt: ["Cliente Kafka presente e chamada de publicação no create bem-sucedido", "Producer durável (Acks.All / EnableIdempotence)", "Evento real observado no tópico, com key == id", "Falha do broker não vira 500"],
        en: ["A Kafka client present and a publish call on the successful create", "Durable producer (Acks.All / EnableIdempotence)", "A real event observed on the topic, keyed by id", "A broker failure does not become a 500"]
      },
      how: {
        pt: "Roslyn detecta o cliente, a chamada de publicação e a config de durabilidade. No modo deep, o harness pluga o PRÓPRIO consumidor (kcat) no tópico transactions e confirma que um evento real foi publicado para uma transação recém-criada — essa observação ao vivo é a prova.",
        en: "Roslyn detects the client, the publish call and the durability config. In deep mode the harness attaches its OWN consumer (kcat) to the transactions topic and confirms a real event was published for a just-created transaction — that live observation is the proof."
      }
    },
    {
      number: 7, key: "security", weightPct: 14, iso: "Security",
      automation: "ProxyReview", diagram: "panMask",
      title: { pt: "Segurança (PCI)", en: "Security (PCI)" },
      tagline: {
        pt: "O PAN está protegido, e nada sensível vaza para o código, o log ou o repositório?",
        en: "Is the PAN protected, and does nothing sensitive leak into the code, the logs or the repo?"
      },
      body: {
        pt: "Domínio crítico: aqui vale o PCI DSS Requisito 3. O PAN tem de estar protegido (cifrado, tokenizado ou truncado) e nunca logado; dado de autenticação sensível (CVV/CVC, trilha, PIN) nunca pode ser armazenado. Fora isso: nada de segredo hardcoded, validação de toda entrada, rate limiting e dependências sem vulnerabilidade conhecida. Autenticação é opcional e não pontua — não há modelo de usuário no escopo.",
        en: "A critical domain: PCI DSS Requirement 3 applies. The PAN must be protected (encrypted, tokenised or truncated) and never logged; sensitive authentication data (CVV/CVC, track, PIN) must never be stored. Beyond that: no hardcoded secrets, validation of every input, rate limiting, and dependencies free of known vulnerabilities. Auth is optional and unscored — there is no user model in scope."
      },
      look: {
        pt: ["Zero PAN válido por Luhn no código/config de produção (fixtures de teste não contam)", "Zero campo de CVV/CVC/trilha/PIN", "Zero segredo no repositório (gitleaks)", "Zero dependência vulnerável High/Critical", "Validação de entrada e rate limiting"],
        en: ["Zero Luhn-valid PAN in production code/config (test fixtures excluded)", "Zero CVV/CVC/track/PIN field", "Zero secret in the repository (gitleaks)", "Zero High/Critical vulnerable dependency", "Input validation and rate limiting"]
      },
      how: {
        pt: "As checagens PCI rodam sobre o AST do Roslyn, não sobre regex do texto: todo literal de string do código de produção e todo valor de appsettings passa por Luhn, e os identificadores são varridos atrás de CVV/trilha/PIN. Ao lado disso, gitleaks varre a árvore e dotnet list package --vulnerable lê o grafo NuGet.",
        en: "The PCI checks run over the Roslyn AST, not regex over text: every production string literal and every appsettings value is Luhn-checked, and identifiers are searched for CVV/track/PIN. Alongside, gitleaks scans the tree and dotnet list package --vulnerable reads the NuGet graph."
      }
    },
    {
      number: 8, key: "resilience", weightPct: 4, iso: "Reliability",
      automation: "FullAuto", diagram: "resilience",
      title: { pt: "Resiliência e erros", en: "Resilience & Error Handling" },
      tagline: {
        pt: "Falha acontece — o serviço degrada com elegância ou vaza stack trace?",
        en: "Failure happens — does the service degrade gracefully or leak a stack trace?"
      },
      body: {
        pt: "Um handler global de exceção (nada de stack trace vazando para o cliente), retries/timeouts/circuit breakers no I/O externo (banco e broker) e shutdown gracioso. O peso caiu para 4% de propósito: o que esta categoria alegava já é provado onde ele é DEMONSTRADO, não declarado — o /health é o portão de executabilidade (quem não sobe é capado em 1.5), e o oráculo já dispara uma requisição malformada e cobra um 4xx limpo. O que sobra aqui é sinal estático, e pesa como tal.",
        en: "A global exception handler (no stack trace leaking to clients), retries/timeouts/circuit breakers on external I/O (database and broker), and graceful shutdown. The weight dropped to 4% on purpose: what this category used to claim is now proved where it is DEMONSTRATED rather than declared — /health is the executability gate (a service that never comes up is capped at 1.5), and the oracle already fires a malformed request and demands a clean 4xx. What is left here is a static signal, and it is weighted like one."
      },
      look: {
        pt: ["Handler global único de exceção", "Requisição malformada responde 4xx, sem vazar internals", "Políticas de retry/timeout/circuit breaker (Polly) no banco e no broker", "Shutdown gracioso"],
        en: ["A single global exception handler", "A malformed request answers 4xx without leaking internals", "Retry/timeout/circuit-breaker policies (Polly) on the database and the broker", "Graceful shutdown"]
      },
      how: {
        pt: "Roslyn detecta as políticas, o handler global e o shutdown. Ao vivo, o oráculo manda uma requisição deliberadamente malformada e exige 4xx limpo — sem stack trace, sem internals do Npgsql/EF, sem marcador .cs:linha no corpo.",
        en: "Roslyn detects the policies, the global handler and the shutdown. Live, the oracle sends a deliberately malformed request and demands a clean 4xx — no stack trace, no Npgsql/EF internals, no .cs:line marker in the body."
      }
    },
    {
      number: 9, key: "observability", weightPct: 0, informational: true, iso: "— (enabler)",
      automation: "FullAuto", diagram: "pillars",
      title: { pt: "Observabilidade", en: "Observability" },
      tagline: {
        pt: "Dá para diagnosticar um incidente sem adicionar código?",
        en: "Can you diagnose an incident without adding code?"
      },
      body: {
        pt: "Medida e reportada, mas SEM peso na nota. O sinal decisivo dela é o /health — e esse é o portão de executabilidade: quem nunca responde 2xx é capado em 1.5/5, aconteça o que acontecer aqui. O que sobra (log JSON estruturado, correlation id) é prática real e vale mostrar, mas não é número para ranquear. OpenTelemetry e /metrics saíram do enunciado — adicioná-los agora conta como gold-plating.",
        en: "Measured and reported, but with NO weight in the score. Its decisive signal is /health — and that is the executability gate: a service that never answers 2xx is capped at 1.5/5 no matter what happens here. What remains (structured JSON logs, a correlation id) is real practice worth showing, but not a number to rank on. OpenTelemetry and /metrics are out of the brief — adding them now counts as gold-plating."
      },
      look: {
        pt: ["Log estruturado em JSON (Serilog / AddJsonConsole)", "Correlation/trace id propagado ponta a ponta", "/health respondendo 2xx no sistema vivo", "PAN jamais logado (isso pontua em Segurança)"],
        en: ["Structured JSON logging (Serilog / AddJsonConsole)", "A correlation/trace id propagated end to end", "/health answering 2xx on the live system", "The PAN never logged (that one scores under Security)"]
      },
      how: {
        pt: "Roslyn confirma o log estruturado e o correlation id no código; um probe HTTP confirma o /health no sistema vivo. Tudo isso entra no relatório — e em nenhum momento na nota.",
        en: "Roslyn confirms the structured logging and the correlation id in the source; an HTTP probe confirms /health on the live system. All of it lands in the report — and none of it in the score."
      }
    },
    {
      number: 10, key: "portability", weightPct: 0, informational: true, iso: "Portability",
      automation: "FullAuto", diagram: null,
      title: { pt: "Portabilidade e deploy", en: "Portability & Deploy" },
      tagline: {
        pt: "clone → up → funciona, em qualquer máquina?",
        en: "clone → up → it works, on any machine?"
      },
      body: {
        pt: "Medida e reportada, mas SEM peso — porque a parte que importa aqui não é um checklist: é o portão de executabilidade. O harness sobe o docker-compose DA PRÓPRIA submissão, e quem não fica saudável é capado em 1.0–1.5/5. Pontuar 'existe um Dockerfile' com 2% em cima de um portão que já rodou a coisa era contar o mesmo fato duas vezes. O workflow de CI saiu da rubrica: nada aqui o executa — a métrica pontuava a existência de um YAML.",
        en: "Measured and reported, but with NO weight — because the part that matters here isn't a checklist: it's the executability gate. The harness boots the submission's OWN docker-compose, and one that never turns healthy is capped at 1.0–1.5/5. Scoring 'a Dockerfile exists' at 2% on top of a gate that already ran the thing was counting the same fact twice. The CI workflow is out of the rubric: nothing here runs it — the metric scored the existence of a YAML file."
      },
      look: {
        pt: ["Config só por variável de ambiente (12-Factor III/IV)", "Dependências pinadas (lock file / global.json / CPM)", "Container roda como não-root", "Dockerfile sem violações (hadolint)"],
        en: ["Config from environment variables only (12-Factor III/IV)", "Pinned dependencies (lock file / global.json / CPM)", "The container runs as non-root", "Dockerfile free of violations (hadolint)"]
      },
      how: {
        pt: "Checagens de arquivo para Dockerfile, compose, config por env, pinagem e USER não-root; hadolint linta o Dockerfile. Se o projeto sobe de verdade, quem responde é o portão — não esta lista.",
        en: "File checks for the Dockerfile, the compose, env-based config, pinning and a non-root USER; hadolint lints the Dockerfile. Whether the project actually comes up is answered by the gate — not by this list."
      }
    },
    {
      number: 11, key: "documentation", weightPct: 0, informational: true, iso: "Maintainability / Usability",
      automation: "ProxyReview", diagram: null,
      title: { pt: "Documentação", en: "Documentation" },
      tagline: {
        pt: "Um dev novo sobe o projeto só com o README?",
        en: "Can a new dev bring the project up with only the README?"
      },
      body: {
        pt: "Medida e reportada, mas SEM peso. No peso antigo (1%), a diferença entre um README impecável e NENHUM README mexia 0,05 na nota final — menos que o ruído entre dois runs do mesmo modelo no mesmo prompt. Era um número com cara de medição, incapaz de agir como uma. O que de fato importa no contrato — o OpenAPI descrever mesmo os endpoints — é cobrado AO VIVO no critério 4, onde vale ponto.",
        en: "Measured and reported, but with NO weight. At its old 1%, the gap between a flawless README and NO README at all moved the final score by 0.05 — less than the run-to-run noise of the same model on the same prompt. It was a number that looked like a measurement and could not act like one. What genuinely matters about the contract — that the OpenAPI really describes the endpoints — is asserted LIVE in criterion 4, where it counts."
      },
      look: {
        pt: ["README com propósito, setup/pré-requisitos e como rodar", "Stack e variáveis de ambiente documentadas"],
        en: ["A README with purpose, setup/prerequisites and how to run", "The stack and the environment variables documented"]
      },
      how: {
        pt: "Parsing das seções do README. Doc-comments (densidade de ///) saiu da rubrica: mede digitação, não engenharia, e é trivialmente burlável por um modelo que comenta toda propriedade.",
        en: "Parsing of the README's sections. Doc comments (/// density) are out of the rubric: they measure typing, not engineering, and are trivially gamed by a model that comments every property."
      }
    }
  ],

  /* ---- per-metric breakdown: exactly what the evaluator emits, per category ----
     `t: "live"` = asserted against the running system; `t: "deep"` = needs the deep harness.
     Categories 9–11 are informational: their metrics are reported, never scored. */
  criteriaChecks: {
    1: [
      { n: "create-card-201", w: 1, t: "live", how: {
        pt: "POST /credit-cards com payload VISA válido tem de responder 201 Created (requisição HTTP real).",
        en: "POST /credit-cards with a valid VISA payload must answer 201 Created (real HTTP request)." } },
      { n: "create-card-id", w: 1, t: "live", how: {
        pt: "A resposta da criação traz o id novo no corpo JSON (aceita envelope data/value).",
        en: "The create response returns the new id in the JSON body (data/value envelope tolerated)." } },
      { n: "card-required-400", w: 1, t: "live", how: {
        pt: "POST com cardholderName/cardNumber vazios tem de responder 400.",
        en: "POST with empty cardholderName/cardNumber must answer 400." } },
      { n: "list-cards-200", w: 0.5, t: "live", how: {
        pt: "GET /credit-cards (a coleção) tem de responder 200.",
        en: "GET /credit-cards (the collection) must answer 200." } },
      { n: "get-card-200", w: 1, t: "live", how: {
        pt: "GET /credit-cards/{id criado} tem de responder 200.",
        en: "GET /credit-cards/{created id} must answer 200." } },
      { n: "get-card-404", w: 1, t: "live", how: {
        pt: "GET /credit-cards/{id inexistente = 999000111} tem de responder 404.",
        en: "GET /credit-cards/{missing id = 999000111} must answer 404." } },
      { n: "create-tx-201", w: 1, t: "live", how: {
        pt: "POST /transactions com um creditCardId válido tem de responder 201 Created.",
        en: "POST /transactions with a valid creditCardId must answer 201 Created." } },
      { n: "create-tx-id", w: 1, t: "live", how: {
        pt: "A resposta da criação da transação traz o id novo no corpo JSON.",
        en: "The transaction create response returns the new id in the JSON body." } },
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
      { n: "list-tx-200", w: 0.5, t: "live", how: {
        pt: "GET /transactions (a coleção) tem de responder 200.",
        en: "GET /transactions (the collection) must answer 200." } },
      { n: "get-tx-200", w: 1, t: "live", how: {
        pt: "GET /transactions/{id criado} tem de responder 200.",
        en: "GET /transactions/{created id} must answer 200." } },
      { n: "get-tx-404", w: 1, t: "live", how: {
        pt: "GET /transactions/{id inexistente} tem de responder 404.",
        en: "GET /transactions/{missing id} must answer 404." } },
      { n: "card-transactions-200", w: 1, t: "live", how: {
        pt: "GET /credit-cards/{id}/transactions tem de responder 200 (a relação 1:N exposta).",
        en: "GET /credit-cards/{id}/transactions must answer 200 (the 1:N relation exposed)." } },
      { n: "card-transactions-404", w: 0.5, t: "live", how: {
        pt: "GET /credit-cards/{id inexistente}/transactions tem de responder 404, não uma lista vazia.",
        en: "GET /credit-cards/{missing id}/transactions must answer 404, not an empty list." } },
      { n: "unit-tests", w: 1, how: {
        pt: "Projeto de teste que declara casos de verdade — o AST enxerga [Fact]/[Theory]/[Test], não só um csproj referenciando o framework. Substitui os antigos test-project/test-framework/coverage-tool: eram três checagens de presença para o mesmo fato, e 'o pacote está referenciado' não é sinal de engenharia.",
        en: "A test project that actually declares test cases — the AST sees [Fact]/[Theory]/[Test], not just a csproj referencing the framework. It replaces the old test-project/test-framework/coverage-tool: three presence checks for one fact, and 'a package is referenced' is not an engineering signal." } },
      { n: "unit-only", w: 1, how: {
        pt: "Pacote Testcontainers → Fail (proibido: exige daemon Docker e sobe Postgres/Kafka a cada run). WebApplicationFactory no AST → Partial (roda em processo, mas é teste de aceitação que a tarefa não pediu — esse papel é do oráculo vivo). Nenhum dos dois → Pass.",
        en: "A Testcontainers package → Fail (forbidden: needs a Docker daemon and boots a Postgres/Kafka per run). WebApplicationFactory in the AST → Partial (in-process, but an acceptance test the task never asked for — that job belongs to the live oracle). Neither → Pass." } },
      { n: "test-pass-rate", w: 1, t: "deep", how: {
        pt: "Roda `dotnet test` uma única vez; regex extrai Passed/Failed; nota = passed / total. Peso baixo de propósito: é a suíte que o próprio modelo escreveu — sinal auto-avaliado, ao lado (e muito abaixo) do oráculo independente.",
        en: "Runs `dotnet test` exactly once; a regex extracts Passed/Failed; score = passed / total. Deliberately low weight: it is the suite the model wrote itself — a self-graded signal, sitting beside (and far below) the independent oracle." } },
      { n: "coverage", w: 2, t: "deep", how: {
        pt: "Da MESMA execução do `dotnet test` (XPlat Code Coverage), faz merge de todos os coverage.cobertura.xml (união das linhas cobertas); LineRate ≥60% → Pass, ≥35% → Partial (régua relaxada de propósito, para não incentivar teste de enchimento).",
        en: "From the SAME `dotnet test` run (XPlat Code Coverage), merges every coverage.cobertura.xml (union of covered lines); LineRate ≥60% → Pass, ≥35% → Partial (a deliberately relaxed bar, so there is no incentive to pad tests)." } }
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
      { n: "no-gold-plating", w: 1, how: {
        pt: "Conta a maquinaria que o brief PROIBIU: endpoints PUT/PATCH/DELETE, consumer Kafka, outbox transacional, SDK do OpenTelemetry, versionamento de API, Testcontainers. 0 → Pass; 1–2 → Partial; ≥3 → Fail. Substitui o antigo overengineering-proxy, que contava interfaces com uma só implementação — justamente as portas de inversão de dependência que esta categoria premia, e por isso nunca reprovava ninguém.",
        en: "Counts the machinery the brief RULED OUT: PUT/PATCH/DELETE endpoints, a Kafka consumer, a transactional outbox, the OpenTelemetry SDK, API versioning, Testcontainers. 0 → Pass; 1–2 → Partial; ≥3 → Fail. It replaces the old overengineering-proxy, which counted single-implementation interfaces — precisely the dependency-inversion ports this category rewards, which is why it could never fail anyone." } },
      { n: "no-god-class", w: 0.5, how: {
        pt: "O maior tipo (linhas medidas pelo AST) tem ≤600 linhas → Pass; senão Partial.",
        en: "The largest type (lines measured by the AST) is ≤600 lines → Pass; otherwise Partial." } }
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
      { n: "async-io", w: 1, how: {
        pt: "AST: contagem de métodos marcados async > 0. Veio da antiga categoria Performance.",
        en: "AST: count of methods marked async > 0. Moved here from the old Performance category." } },
      { n: "no-sync-over-async", w: 1, how: {
        pt: "AST: qualquer .Result/.Wait()/.GetAwaiter().GetResult() → Fail. Antes era meia-nota (Partial): num serviço ASP.NET Core, bloquear o caminho da requisição é starvation do thread-pool — bug, não estilo.",
        en: "AST: any .Result/.Wait()/.GetAwaiter().GetResult() → Fail. It used to be half credit (Partial): in an ASP.NET Core service, blocking the request path is thread-pool starvation — a bug, not a style choice." } },
      { n: "format", w: 1, t: "deep", how: {
        pt: "`dotnet format --verify-no-changes`; sem mudanças pendentes → Pass.",
        en: "`dotnet format --verify-no-changes`; no pending changes → Pass." } },
      { n: "build-warnings", w: 1, t: "deep", how: {
        pt: "Contagem de warnings do build Release único do harness; 0 → Pass, ≤10 → Partial.",
        en: "Warning count from the harness's single Release build; 0 → Pass, ≤10 → Partial." } }
    ],
    4: [
      { n: "http-verbs", w: 1, how: {
        pt: "Atributos [HttpGet]/[HttpPost] ou invocações MapGet/MapPost (Richardson L2). PUT/DELETE não somam aqui — são penalizados como gold-plating no critério 2.",
        en: "[HttpGet]/[HttpPost] attributes or MapGet/MapPost invocations (Richardson L2). PUT/DELETE earn nothing here — they are penalised as gold-plating under criterion 2." } },
      { n: "problem-details", w: 1, how: {
        pt: "new ProblemDetails, ou AddProblemDetails/Problem(), ou IExceptionHandler (RFC 9457).",
        en: "new ProblemDetails, or AddProblemDetails/Problem(), or IExceptionHandler (RFC 9457)." } },
      { n: "dtos", w: 0.5, how: {
        pt: "Pasta Dtos/DTOs, ou tipos cujo nome contém Request/Response/Dto.",
        en: "A Dtos/DTOs folder, or types whose names contain Request/Response/Dto." } },
      { n: "create-card-location", w: 0.5, t: "live", how: {
        pt: "A resposta 201 da criação do cartão traz o header Location.",
        en: "The card create 201 response carries a Location header." } },
      { n: "json-camelcase", w: 0.5, t: "live", how: {
        pt: "Toda chave do JSON de resposta é camelCase (nenhuma começa maiúscula nem contém _).",
        en: "Every response JSON key is camelCase (none starts uppercase or contains _)." } },
      { n: "problem-details-live", w: 0.5, t: "live", how: {
        pt: "O corpo de erro tem Content-Type application/problem+json (RFC 9457).",
        en: "The error body has Content-Type application/problem+json (RFC 9457)." } },
      { n: "create-tx-location", w: 0.5, t: "live", how: {
        pt: "A resposta 201 da criação da transação traz o header Location.",
        en: "The transaction create 201 response carries a Location header." } },
      { n: "pagination", w: 0.5, t: "live", how: {
        pt: "Tenta pageSize=1/limit=1/perPage=1/… e exige exatamente 1 item ou metadados de paginação (a coleção é semeada com 2 cartões). O antigo check estático de paginação (procurar Skip/Take no código) saiu: provava que a palavra existe, não que a API pagina.",
        en: "Tries pageSize=1/limit=1/perPage=1/… and requires exactly 1 item or paging metadata (the collection is seeded with 2 cards). The old static pagination check (looking for Skip/Take in the source) is gone: it proved the word exists, not that the API paginates." } },
      { n: "openapi-populated", w: 1, t: "live", how: {
        pt: "Baixa o OpenAPI servido e conta as operações. ops > 0 → Pass; 0 (paths vazio) → Fail (contrato vazio e inútil); NENHUM doc servido → Fail também — a tarefa exige OpenAPI, então a ausência é defeito, não medição faltante. Substitui de vez o antigo check estático 'openapi' (que só provava o middleware ligado, e ainda era contado de novo em Documentação como api-docs).",
        en: "Fetches the served OpenAPI and counts operations. ops > 0 → Pass; 0 (empty paths) → Fail (an empty, useless contract); NO doc served → Fail as well — the task requires OpenAPI, so its absence is a defect, not a missing measurement. It fully replaces the old static 'openapi' check (which only proved the middleware was wired, and was counted a second time under Documentation as api-docs)." } }
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
      { n: "read-perf", w: 0.5, how: {
        pt: "AsNoTracking / AsNoTrackingWithIdentityResolution nas leituras.",
        en: "AsNoTracking / AsNoTrackingWithIdentityResolution on reads." } }
    ],
    6: [
      { n: "broker-client", w: 1, how: {
        pt: "Pacote Confluent.Kafka/MassTransit, ou os genéricos IProducer/ProducerBuilder.",
        en: "Confluent.Kafka/MassTransit package, or the IProducer/ProducerBuilder generics." } },
      { n: "publishes", w: 1, how: {
        pt: "Uma chamada de publish no AST: Produce ou ProduceAsync.",
        en: "A publish call in the AST: Produce or ProduceAsync." } },
      { n: "durable-producer", w: 1, how: {
        pt: "Acesso a membro Acks.All, ou o identificador EnableIdempotence.",
        en: "Member access Acks.All, or the EnableIdempotence identifier." } },
      { n: "kafka-event-live", w: 1, t: "live", how: {
        pt: "O sidecar kafka-check (kcat) do harness consome o tópico transactions durante a run: evento com key = id → Pass; evento com outra key → Parcial; nenhum evento → Fail; broker inalcançável → Indeterminado.",
        en: "The harness kafka-check (kcat) sidecar consumes the transactions topic during the run: an event keyed by id → Pass; an event with another key → Partial; no event → Fail; broker unreachable → Indeterminate." } }
    ],
    7: [
      { n: "pci-pan", w: 1, how: {
        pt: "Regex de 13–19 dígitos sobre literais string de produção + valores de appsettings.json, filtrado por Luhn; qualquer sequência válida → Fail (testes excluídos).",
        en: "A 13–19 digit regex over production string literals + appsettings.json values, filtered by Luhn; any valid sequence → Fail (tests excluded)." } },
      { n: "pci-sad", w: 1, how: {
        pt: "Identificador contendo cvv/cvc/cardverification/track2/pinblock → Fail (dado sensível de autenticação).",
        en: "An identifier containing cvv/cvc/cardverification/track2/pinblock → Fail (sensitive auth data)." } },
      { n: "validation", w: 0.5, how: {
        pt: "FluentValidation, ou [Required]/[Range]/[StringLength], ou ModelState.",
        en: "FluentValidation, or [Required]/[Range]/[StringLength], or ModelState." } },
      { n: "rate-limit", w: 0.5, how: {
        pt: "AddRateLimiter / RequireRateLimiting (OWASP API #4).",
        en: "AddRateLimiter / RequireRateLimiting (OWASP API #4)." } },
      { n: "secrets", w: 1, how: {
        pt: "`gitleaks detect --source <root> --no-git --no-banner`; exit 0 → Pass.",
        en: "`gitleaks detect --source <root> --no-git --no-banner`; exit 0 → Pass." } },
      { n: "sca", w: 1, t: "deep", how: {
        pt: "`dotnet list package --vulnerable --include-transitive`; procura 'vulnerable' ou > High/Critical no output. Sem fonte NuGet alcançável → Indeterminado, nunca um Pass silencioso.",
        en: "`dotnet list package --vulnerable --include-transitive`; looks for 'vulnerable' or > High/Critical in the output. With no reachable NuGet source → Indeterminate, never a silent Pass." } }
    ],
    8: [
      { n: "resilience-policies", w: 1, how: {
        pt: "Pacote Polly/Microsoft.Extensions.Http.Resilience, ou AddResilienceHandler/WaitAndRetry/AddPolicyHandler/AddStandardResilienceHandler.",
        en: "Polly/Microsoft.Extensions.Http.Resilience package, or AddResilienceHandler/WaitAndRetry/AddPolicyHandler/AddStandardResilienceHandler." } },
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
      { n: "structured-logs", w: 1, how: {
        pt: "Pacote Serilog, ou AddJsonConsole/UseSerilog/AddSerilog.",
        en: "Serilog package, or AddJsonConsole/UseSerilog/AddSerilog." } },
      { n: "correlation", w: 1, how: {
        pt: "Identificador CorrelationId/TraceId/traceparent, ou acesso a Activity.Current.",
        en: "A CorrelationId/TraceId/traceparent identifier, or Activity.Current access." } },
      { n: "live-health", w: 1, t: "live", how: {
        pt: "GET {base}/health tem de responder 2xx/3xx no sistema vivo. Os antigos checks estáticos health-endpoint e metrics-endpoint saíram: o primeiro era a TERCEIRA cópia do mesmo sinal de health (o portão de executabilidade já capa em 1.5 quem não responde), e o segundo pertencia a um requisito (/metrics) que a tarefa deixou de pedir.",
        en: "GET {base}/health must answer 2xx/3xx on the live system. The old static health-endpoint and metrics-endpoint checks are gone: the first was the THIRD copy of the same health signal (the executability gate already caps a service that never answers at 1.5), and the second belonged to a requirement (/metrics) the task has dropped." } }
    ],
    10: [
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
      { n: "non-root", w: 0.5, how: {
        pt: "Regex ^\\s*USER\\s+ (multiline) encontra uma diretiva USER no Dockerfile. O antigo check 'ci' saiu: nada aqui executa o workflow — ele pontuava a existência de um YAML.",
        en: "Regex ^\\s*USER\\s+ (multiline) finds a USER directive in the Dockerfile. The old 'ci' check is gone: nothing here runs the workflow — it scored the existence of a YAML file." } },
      { n: "hadolint", w: 0.5, t: "deep", how: {
        pt: "`hadolint <Dockerfile>`; limpo → Pass, senão Partial.",
        en: "`hadolint <Dockerfile>`; clean → Pass, otherwise Partial." } }
    ],
    11: [
      { n: "readme", w: 1, how: {
        pt: "Existe README.md.",
        en: "A README.md exists." } },
      { n: "readme-sections", w: 1, how: {
        pt: "Três regex sobre o README (purpose/overview; setup/install/prereq; run/usage/docker compose); nota = seções encontradas / 3. Os antigos api-docs (duplicata do check de OpenAPI do critério 4) e doc-comments (densidade de ///) saíram.",
        en: "Three regexes over the README (purpose/overview; setup/install/prereq; run/usage/docker compose); score = sections found / 3. The old api-docs (a duplicate of criterion 4's OpenAPI check) and doc-comments (/// density) are gone." } }
    ]
  }
};
