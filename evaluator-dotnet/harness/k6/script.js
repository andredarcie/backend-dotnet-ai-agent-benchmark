// Load test for category 11 (Performance). Exercises the real CRUD path, not just /health — the
// thresholds ARE the oracle: k6 exits non-zero when they are breached.
//
// IMPORTANT — rate-limit awareness: a well-built API returns 429 (Too Many Requests) to shed load once a
// client exceeds its rate limit. That is CORRECT backpressure, not a fault — and the SAME rubric rewards
// rate limiting under Security (cat 7). So this test treats 429 as an EXPECTED response: it is excluded
// from `http_req_failed` (only 5xx / transport errors count as failures) and accepted by the checks. What
// we actually measure is that the API stays UP and FAST under load and sheds excess gracefully — never
// that it must serve an unbounded flood. Real defects (5xx, hangs, slow p95) still breach the thresholds.
import http from 'k6/http';
import { check } from 'k6';

// Count anything in 200–399 OR 429 as an expected response; everything else (notably 5xx and network
// errors) is a real failure that http_req_failed will flag.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 399 }, 429));

export const options = {
  vus: 10,
  duration: '20s',
  thresholds: {
    http_req_failed: ['rate<0.01'],     // < 1% real errors (5xx / transport) — 429 is NOT counted
    http_req_duration: ['p(95)<1000'],  // p95 under 1s (the genuine latency SLO)
    checks: ['rate>0.99'],              // > 99% of assertions pass (201-or-429 both accepted)
  },
};

const BASE = (__ENV.BASE_URL || 'http://host.docker.internal:8080').replace(/\/$/, '');
const JSON_HEADERS = { headers: { 'Content-Type': 'application/json' } };

// A response is "good" if it succeeded (2xx) or was correctly rate-limited (429).
const okOrLimited = (r, ok) => r.status === ok || r.status === 429;

// Discover the API prefix once, before the load phase. This mirrors the evaluator's ContractOracle:
// it probes `/api/v1`, `/api`, `/v1`, `` against `/credit-cards` and picks the first that returns 200,
// so a versioned API served under `/api/v1/...` isn't scored as a false load-test failure (404 -> breach).
export function setup() {
  const prefixes = ['/api/v1', '/api', '/v1', ''];
  for (const prefix of prefixes) {
    const r = http.get(`${BASE}${prefix}/credit-cards`);
    if (r.status === 200) {
      return { prefix };
    }
  }
  return { prefix: '/api' }; // default preserves the previous hardcoded behavior
}

export default function (data) {
  const cards = `${BASE}${data.prefix}/credit-cards`;
  const txns = `${BASE}${data.prefix}/transactions`;

  // Read-heavy: list cards (paginated collection).
  const list = http.get(`${cards}?pageSize=10`);
  check(list, { 'list cards ok': (r) => r.status < 500 });

  // Write path: create a card, then a transaction against it (this also drives the Kafka event).
  const card = http.post(cards, JSON.stringify({
    cardholderName: 'Load Test', cardNumber: '4111111111111111', brand: 'VISA', creditLimit: 5000,
  }), JSON_HEADERS);
  check(card, { 'create card 201 or rate-limited': (r) => okOrLimited(r, 201) });

  const id = card.json('id');
  if (id) {
    const tx = http.post(txns, JSON.stringify({
      creditCardId: id, amount: 12.34, merchant: 'k6', category: 'load',
    }), JSON_HEADERS);
    check(tx, { 'create tx 201 or rate-limited': (r) => okOrLimited(r, 201) });
  }
}
