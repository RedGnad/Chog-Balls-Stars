// Seasons of the player ladder. A season is an id and an end time given in the services'
// configuration (SEASON_ID and SEASON_ENDS_AT for host.js, --season and --season-ends for
// local.js). When the end has passed, the season is closed once: every wallet above the floor
// gets its reset event (ranks.js), and a marker says the season was closed. Closing again
// changes nothing. The rules are in research/evidence/ladder-references-2026-10-21.md.

import { SEASON_ID } from './ranks.js';

/**
 * The season the configuration names, or null when it names none. Throws when it is half
 * named or its times cannot be read, so that a host does not run with a season it misread.
 */
export function seasonFrom({ id = '', endsAt = '', startsAt = '' } = {}) {
  id = String(id ?? '').trim();
  endsAt = String(endsAt ?? '').trim();
  startsAt = String(startsAt ?? '').trim();
  if (!id && !endsAt) return null;
  if (!SEASON_ID.test(id)) throw new RangeError('a season id is 1 to 40 letters, digits or -');
  if (!Number.isFinite(Date.parse(endsAt))) throw new RangeError('a season ends at an ISO time (SEASON_ENDS_AT)');
  if (startsAt && !Number.isFinite(Date.parse(startsAt))) throw new RangeError('a season starts at an ISO time (SEASON_STARTS_AT)');
  const ends = new Date(endsAt).toISOString();
  const starts = startsAt ? new Date(startsAt).toISOString() : null;
  if (starts && starts >= ends) throw new RangeError('a season starts before it ends');
  return { id, startsAt: starts, endsAt: ends };
}

/**
 * @param {object} options
 * @param {ReturnType<import('./ranks.js').createRanks>} options.ranks
 * @param {{get, set, setIfAbsent?, list}} options.store the rank store: the markers live beside the events
 * @param {{id: string, startsAt: string|null, endsAt: string} | null} options.season
 * @param {() => number} [options.now]
 * @param {(closing: {seasonId: string, endsAt: string, resets: object[]}) => Promise<void> | void} [options.onClosed]
 *   called once, after the season was closed here
 */
export function createSeasons({ ranks, store, season = null, now = Date.now, onClosed }) {
  const closedKeyOf = seasonId => `seasons/${seasonId}_closed`;

  async function closedMarker(seasonId) {
    const text = await store.get(closedKeyOf(seasonId));
    return text === null ? null : JSON.parse(text);
  }

  /** The configured season and whether it has ended and been closed; null without one. */
  async function current() {
    if (!season) return null;
    const closed = await closedMarker(season.id);
    return { id: season.id, startsAt: season.startsAt, endsAt: season.endsAt, ended: new Date(now()).toISOString() >= season.endsAt, closed: closed !== null };
  }

  /**
   * Closes the configured season when its end has passed: the resets are written, then the
   * marker. Answers null when there is nothing to do, else what was closed. A marker written
   * by another instance in between means the season was closed there: nothing is called twice.
   */
  async function closeIfEnded() {
    if (!season || new Date(now()).toISOString() < season.endsAt) return null;
    if (await closedMarker(season.id)) return null;
    const resets = await ranks.resetSeason({ seasonId: season.id, at: season.endsAt });
    const marker = { seasonId: season.id, endsAt: season.endsAt, closedAt: new Date(now()).toISOString(), resets: resets.length };
    const written = store.setIfAbsent
      ? await store.setIfAbsent(closedKeyOf(season.id), JSON.stringify(marker))
      : (await store.set(closedKeyOf(season.id), JSON.stringify(marker)), true);
    if (!written) return null;
    const closing = { seasonId: season.id, endsAt: season.endsAt, resets };
    if (onClosed) await onClosed(closing);
    return closing;
  }

  /** Looks for the season's end every "everyMs", and once now. The timer never keeps a process alive. */
  function watch({ everyMs = 60000, setIntervalImpl = setInterval } = {}) {
    const look = () => closeIfEnded().catch(error => console.warn(`Season not closed: ${error.message}`));
    look();
    const timer = setIntervalImpl(look, everyMs);
    timer?.unref?.();
    return timer;
  }

  return { current, closeIfEnded, watch };
}
