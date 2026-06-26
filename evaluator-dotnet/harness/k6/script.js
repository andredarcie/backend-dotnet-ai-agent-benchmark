// Load test for category 11 (Performance). Exercises the real CRUD path, not just /health — the
// thresholds ARE the oracle: k6 exits non-zero when they are breached.
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  vus: 10,
  duration: '20s',
  thresholds: {
    http_req_failed: ['rate<0.01'],     // < 1% errors
    http_req_duration: ['p(95)<1000'],  // p95 under 1s
    checks: ['rate>0.99'],              // > 99% of assertions pass
  },
};

const BASE = (__ENV.BASE_URL || 'http://host.docker.internal:8080').replace(/\/$/, '');
const JSON_HEADERS = { headers: { 'Content-Type': 'application/json' } };

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
  check(list, { 'list cards < 500': (r) => r.status < 500 });

  // Write path: create a card, then a transaction against it (this also drives the Kafka event).
  const card = http.post(cards, JSON.stringify({
    cardholderName: 'Load Test', cardNumber: '4111111111111111', brand: 'VISA', creditLimit: 5000,
  }), JSON_HEADERS);
  check(card, { 'create card 201': (r) => r.status === 201 });

  const id = card.json('id');
  if (id) {
    const tx = http.post(txns, JSON.stringify({
      creditCardId: id, amount: 12.34, merchant: 'k6', category: 'load',
    }), JSON_HEADERS);
    check(tx, { 'create tx 201': (r) => r.status === 201 });
  }
}
