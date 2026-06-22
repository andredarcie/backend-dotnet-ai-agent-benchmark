import { Kafka, logLevel } from 'kafkajs';
import { config, WEIGHTS } from '../config';
import { http } from '../http';
import type { CheckResult } from '../types';
import { check, sleep } from '../util';

const W = WEIGHTS.kafka;

/**
 * Connects a consumer to the broker on the host, then creates a transaction over HTTP and
 * waits for the matching event on the `transactions` topic.
 */
export async function runKafkaChecks(): Promise<CheckResult[]> {
  const results: CheckResult[] = [];

  const kafka = new Kafka({
    clientId: 'benchmark-evaluator',
    brokers: config.kafka.brokers,
    logLevel: logLevel.NOTHING,
    retry: { retries: 4, initialRetryTime: 500 },
  });

  // Unique group per run so fromBeginning replays everything in the (freshly created) topic.
  const groupId = `bench-${Date.now()}-${Math.floor(Math.random() * 1e6)}`;
  const consumer = kafka.consumer({ groupId });
  const messages: { key: string | null; value: any }[] = [];

  try {
    await consumer.connect();
    await consumer.subscribe({ topic: config.kafka.topic, fromBeginning: true });
    await consumer.run({
      eachMessage: async ({ message }) => {
        const raw = message.value?.toString() ?? '';
        const key = message.key?.toString() ?? null;
        try {
          messages.push({ key, value: JSON.parse(raw) });
        } catch {
          messages.push({ key, value: { __raw: raw } });
        }
      },
    });
  } catch (err) {
    results.push(check('kafka.broker', 'kafka', `Broker reachable on host (${config.kafka.brokers.join(',')})`,
      W.brokerReachable, false, `connect failed: ${String(err)}`));
    results.push(check('kafka.event', 'kafka', 'Transaction create publishes to topic', W.eventPublished, false,
      'skipped (broker unreachable)'));
    results.push(check('kafka.eventKey', 'kafka', 'Event message key = transaction id', W.eventKey, false,
      'skipped (broker unreachable)'));
    try {
      await consumer.disconnect();
    } catch {
      /* ignore */
    }
    return results;
  }

  results.push(check('kafka.broker', 'kafka', `Broker reachable on host (${config.kafka.brokers.join(',')})`,
    W.brokerReachable, true, `subscribed to "${config.kafka.topic}"`));

  // Generate a uniquely identifiable transaction.
  const merchant = `KafkaProbe-${Date.now()}`;
  let txnId: number | undefined;
  try {
    const card = await http('POST', '/api/credit-cards', {
      cardholderName: 'Kafka Probe', cardNumber: '4000000000000000', brand: 'VISA', creditLimit: 1000,
    });
    const cardId = card.body?.id;
    const txn = await http('POST', '/api/transactions', {
      creditCardId: cardId, amount: 42.42, merchant, category: 'probe',
    });
    txnId = typeof txn.body?.id === 'number' ? txn.body.id : undefined;
  } catch {
    /* the wait loop below will simply time out */
  }

  const matches = (m: { key: string | null; value: any }) =>
    (txnId !== undefined && m.value?.id === txnId) || m.value?.merchant === merchant;

  const deadline = Date.now() + config.kafka.waitMs;
  let hit: { key: string | null; value: any } | undefined;
  while (Date.now() < deadline) {
    hit = messages.find(matches);
    if (hit) break;
    await sleep(500);
  }

  // Prompt also requires the message key to be the transaction id (as a string).
  const keyOk = hit !== undefined && txnId !== undefined && hit.key === String(txnId);
  const detail = hit
    ? `received event for txn ${txnId ?? merchant}; key="${hit.key}" (expected "${txnId}") → ${keyOk ? 'match' : 'MISMATCH'}`
    : `no matching message within ${config.kafka.waitMs}ms (saw ${messages.length})`;
  results.push(check('kafka.event', 'kafka', 'Transaction create publishes to topic (value + key)', W.eventPublished, hit !== undefined, detail));

  // The produced message key must equal the transaction id (as a string) — its own scored check.
  const keyDetail = hit
    ? `key="${hit.key ?? '(none)'}" (expected "${txnId}")`
    : 'no event received';
  results.push(check('kafka.eventKey', 'kafka', 'Event message key = transaction id', W.eventKey, keyOk, keyDetail));

  try {
    await consumer.disconnect();
  } catch {
    /* ignore */
  }
  return results;
}
