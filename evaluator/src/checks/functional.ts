import { WEIGHTS } from '../config';
import { http } from '../http';
import type { CheckResult } from '../types';

interface Assertion {
  id: string;
  description: string;
  passed: boolean;
  detail: string;
}

export async function runFunctionalChecks(): Promise<CheckResult[]> {
  const a: Assertion[] = [];
  const MISSING = 99_999_999; // an id that should never exist (99,999,999)

  const record = (id: string, description: string, passed: boolean, detail = ''): void => {
    a.push({ id, description, passed, detail });
  };

  const safe = async (id: string, fn: () => Promise<void>) => {
    try {
      await fn();
    } catch (err) {
      record(`${id}.error`, 'unexpected error during functional run', false, String(err));
    }
  };

  // --- health ---
  await safe('health', async () => {
    const r = await http('GET', '/health');
    record('func.health', 'GET /health → 200', r.status === 200, `status=${r.status}`);
  });

  // --- credit card lifecycle ---
  let cardId: number | undefined;
  await safe('card.create', async () => {
    const r = await http('POST', '/api/credit-cards', {
      cardholderName: 'Ada Lovelace',
      cardNumber: '4111111111111111',
      brand: 'VISA',
      creditLimit: 5000,
    });
    const b = r.body;
    cardId = typeof b?.id === 'number' ? b.id : undefined;
    record('func.card.create', 'POST /api/credit-cards → 201 + numeric id',
      r.status === 201 && cardId !== undefined, `status=${r.status}, id=${b?.id}`);
    record('func.card.create.location', 'POST /api/credit-cards sets Location header',
      typeof r.headers.location === 'string' && r.headers.location.length > 0, `location=${r.headers.location ?? '(none)'}`);
    record('func.card.schema', 'Created credit card echoes all fields (camelCase)',
      b?.cardholderName === 'Ada Lovelace' && b?.cardNumber === '4111111111111111' &&
        b?.brand === 'VISA' && Number(b?.creditLimit) === 5000 && typeof b?.createdAt === 'string' && b.createdAt.length > 0,
      `fields: ${b ? Object.keys(b).join(',') : '(none)'}`);
  });

  await safe('card.get', async () => {
    if (cardId === undefined) return record('func.card.get', 'GET /api/credit-cards/{id} → 200', false, 'no card id');
    const r = await http('GET', `/api/credit-cards/${cardId}`);
    record('func.card.get', 'GET /api/credit-cards/{id} → 200 + matches',
      r.status === 200 && r.body?.cardholderName === 'Ada Lovelace', `status=${r.status}`);
  });

  await safe('card.list', async () => {
    const r = await http('GET', '/api/credit-cards');
    record('func.card.list', 'GET /api/credit-cards → 200 array', r.status === 200 && Array.isArray(r.body),
      `status=${r.status}, isArray=${Array.isArray(r.body)}`);
  });

  await safe('card.validation', async () => {
    const r = await http('POST', '/api/credit-cards', { cardholderName: '', cardNumber: '', creditLimit: 100 });
    record('func.card.validation', 'POST /api/credit-cards empty name → 400', r.status === 400, `status=${r.status}`);
  });

  await safe('card.missing', async () => {
    const r = await http('GET', `/api/credit-cards/${MISSING}`);
    record('func.card.missing', 'GET missing credit card → 404', r.status === 404, `status=${r.status}`);
  });

  // --- transaction lifecycle ---
  let txnId: number | undefined;
  await safe('txn.create', async () => {
    if (cardId === undefined) return record('func.txn.create', 'POST /api/transactions → 201 + id', false, 'no card id');
    const r = await http('POST', '/api/transactions', {
      creditCardId: cardId,
      amount: 199.9,
      merchant: 'Amazon',
      category: 'shopping',
    });
    const b = r.body;
    txnId = typeof b?.id === 'number' ? b.id : undefined;
    record('func.txn.create', 'POST /api/transactions → 201 + numeric id',
      r.status === 201 && txnId !== undefined, `status=${r.status}, id=${b?.id}`);
    record('func.txn.create.location', 'POST /api/transactions sets Location header',
      typeof r.headers.location === 'string' && r.headers.location.length > 0, `location=${r.headers.location ?? '(none)'}`);
    record('func.txn.schema', 'Created transaction echoes all fields (camelCase)',
      b?.creditCardId === cardId && Number(b?.amount) === 199.9 && b?.merchant === 'Amazon' &&
        b?.category === 'shopping' && typeof b?.createdAt === 'string' && b.createdAt.length > 0,
      `fields: ${b ? Object.keys(b).join(',') : '(none)'}`);
  });

  await safe('txn.get', async () => {
    if (txnId === undefined) return record('func.txn.get', 'GET /api/transactions/{id} → 200', false, 'no txn id');
    const r = await http('GET', `/api/transactions/${txnId}`);
    record('func.txn.get', 'GET /api/transactions/{id} → 200', r.status === 200, `status=${r.status}`);
  });

  await safe('txn.list', async () => {
    const r = await http('GET', '/api/transactions');
    record('func.txn.list', 'GET /api/transactions → 200 array', r.status === 200 && Array.isArray(r.body),
      `status=${r.status}`);
  });

  await safe('txn.amount', async () => {
    if (cardId === undefined) return record('func.txn.amount', 'POST txn amount<=0 → 400', false, 'no card id');
    const r = await http('POST', '/api/transactions', { creditCardId: cardId, amount: 0, merchant: 'X' });
    record('func.txn.amount', 'POST /api/transactions amount<=0 → 400', r.status === 400, `status=${r.status}`);
  });

  await safe('txn.badcard', async () => {
    const r = await http('POST', '/api/transactions', { creditCardId: MISSING, amount: 10, merchant: 'X' });
    record('func.txn.badcard', 'POST /api/transactions bad creditCardId → 400', r.status === 400, `status=${r.status}`);
  });

  // --- relationship endpoint ---
  await safe('rel.list', async () => {
    if (cardId === undefined) return record('func.rel.list', 'GET card transactions → 200', false, 'no card id');
    const r = await http('GET', `/api/credit-cards/${cardId}/transactions`);
    const includesTxn = Array.isArray(r.body) && (txnId === undefined || r.body.some((t: any) => t.id === txnId));
    record('func.rel.list', 'GET /api/credit-cards/{id}/transactions → 200 incl. txn',
      r.status === 200 && includesTxn, `status=${r.status}`);
  });

  await safe('rel.missing', async () => {
    const r = await http('GET', `/api/credit-cards/${MISSING}/transactions`);
    record('func.rel.missing', 'GET transactions for missing card → 404', r.status === 404, `status=${r.status}`);
  });

  // --- update + delete ---
  await safe('txn.update', async () => {
    if (txnId === undefined || cardId === undefined) return record('func.txn.update', 'PUT txn → 200/204', false, 'no ids');
    const r = await http('PUT', `/api/transactions/${txnId}`, {
      creditCardId: cardId, amount: 250.5, merchant: 'Amazon EU', category: 'shopping',
    });
    record('func.txn.update', 'PUT /api/transactions/{id} → 200/204', r.status === 200 || r.status === 204,
      `status=${r.status}`);
  });

  await safe('txn.delete', async () => {
    if (txnId === undefined) return record('func.txn.delete', 'DELETE txn → 204', false, 'no txn id');
    const r = await http('DELETE', `/api/transactions/${txnId}`);
    record('func.txn.delete', 'DELETE /api/transactions/{id} → 204', r.status === 204, `status=${r.status}`);
  });

  await safe('txn.gone', async () => {
    if (txnId === undefined) return record('func.txn.gone', 'GET deleted txn → 404', false, 'no txn id');
    const r = await http('GET', `/api/transactions/${txnId}`);
    record('func.txn.gone', 'GET deleted transaction → 404', r.status === 404, `status=${r.status}`);
  });

  await safe('card.update', async () => {
    if (cardId === undefined) return record('func.card.update', 'PUT card → 200/204', false, 'no card id');
    const r = await http('PUT', `/api/credit-cards/${cardId}`, {
      cardholderName: 'Ada Lovelace (updated)', cardNumber: '4111111111111111', brand: 'MASTERCARD', creditLimit: 7500,
    });
    let persisted = r.status === 200 || r.status === 204;
    if (persisted) {
      const after = await http('GET', `/api/credit-cards/${cardId}`);
      persisted = after.body?.cardholderName === 'Ada Lovelace (updated)' && Number(after.body?.creditLimit) === 7500;
    }
    record('func.card.update', 'PUT /api/credit-cards/{id} → 200/204 + persisted', persisted, `status=${r.status}`);
  });

  await safe('card.update.missing', async () => {
    const r = await http('PUT', `/api/credit-cards/${MISSING}`, {
      cardholderName: 'Nobody', cardNumber: '4111111111111111', brand: 'VISA', creditLimit: 100,
    });
    record('func.card.update.missing', 'PUT missing credit card → 404', r.status === 404, `status=${r.status}`);
  });

  await safe('txn.update.missing', async () => {
    if (cardId === undefined) return record('func.txn.update.missing', 'PUT missing txn → 404', false, 'no card id');
    const r = await http('PUT', `/api/transactions/${MISSING}`, { creditCardId: cardId, amount: 5, merchant: 'X' });
    record('func.txn.update.missing', 'PUT missing transaction → 404', r.status === 404, `status=${r.status}`);
  });

  await safe('card.delete', async () => {
    if (cardId === undefined) return record('func.card.delete', 'DELETE card → 204', false, 'no card id');
    const r = await http('DELETE', `/api/credit-cards/${cardId}`);
    record('func.card.delete', 'DELETE /api/credit-cards/{id} → 204', r.status === 204, `status=${r.status}`);
  });

  await safe('card.gone', async () => {
    if (cardId === undefined) return record('func.card.gone', 'GET deleted card → 404', false, 'no card id');
    const r = await http('GET', `/api/credit-cards/${cardId}`);
    record('func.card.gone', 'GET deleted credit card → 404', r.status === 404, `status=${r.status}`);
  });

  // Distribute the functional weight evenly across the assertions actually run.
  // The 25 points are split evenly over however many assertions ran, so an unexpected
  // exception adds one more assertion (the `*.error` record) and slightly dilutes the
  // per-assertion weight. Kept intentionally simple.
  const per = a.length ? WEIGHTS.functional / a.length : 0;
  return a.map((x) => ({
    id: x.id,
    category: 'functional' as const,
    description: x.description,
    weight: per,
    passed: x.passed,
    earned: x.passed ? per : 0,
    detail: x.detail,
  }));
}
