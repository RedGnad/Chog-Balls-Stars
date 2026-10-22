// Durable rank events on Upstash Redis. Match replay files remain local to the judge;
// only a verified rank event must survive a Render restart.
import { randomUUID } from 'node:crypto';

const NAMESPACE = 'chog:rank:v1:';

export function createRemoteRankStore({ url, token, fetchImpl = fetch }) {
  if (!/^https:\/\/[^/]+\/?$/.test(url ?? '') || !token) {
    throw new Error('rank storage needs an HTTPS REST URL and token');
  }
  const endpoint = url.replace(/\/$/, '');
  const keyOf = key => NAMESPACE + key;

  async function command(parts) {
    const response = await fetchImpl(endpoint, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
      body: JSON.stringify(parts),
      signal: AbortSignal.timeout(10000),
    });
    if (!response.ok) throw new Error(`rank storage HTTP ${response.status}`);
    const answer = await response.json();
    if (answer.error) throw new Error(`rank storage command failed: ${answer.error}`);
    if (!Object.hasOwn(answer, 'result')) throw new Error('rank storage response has no result');
    return answer.result;
  }

  return {
    ping: async () => (await command(['PING'])) === 'PONG',
    // A short-lived write proves the configured token can credit events, not just read them.
    probeWrite: async () => (await command(['SET', keyOf(`probe/${randomUUID()}`), 'ready', 'NX', 'EX', 60])) === 'OK',
    get: key => command(['GET', keyOf(key)]),
    // Overwrites: the mirror of the match records (node.js, mirroredMatchStore) keeps the latest text of a key.
    set: async (key, value) => (await command(['SET', keyOf(key), value])) === 'OK',
    // A game ID is credited once, even when two replay callbacks race.
    setIfAbsent: async (key, value) => (await command(['SET', keyOf(key), value, 'NX'])) === 'OK',
    list: async prefix => {
      const found = new Set();
      let cursor = '0';
      do {
        const page = await command(['SCAN', cursor, 'MATCH', keyOf(prefix) + '*', 'COUNT', 100]);
        if (!Array.isArray(page) || page.length !== 2 || !Array.isArray(page[1])) {
          throw new Error('rank storage returned an invalid key page');
        }
        cursor = String(page[0]);
        for (const key of page[1]) {
          if (typeof key === 'string' && key.startsWith(NAMESPACE)) found.add(key.slice(NAMESPACE.length));
        }
      } while (cursor !== '0');
      return [...found].sort();
    },
  };
}
