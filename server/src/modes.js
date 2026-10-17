// A page names itself to Photon with the stamp of its simulation ("sim-" and sixteen hex digits;
// before 17 October 2026, its build), followed by the mode its player chose on the menu ("-siege" or
// "-coins", Chog.Screens.MatchModeChoice): Photon only lets pages that give the same name meet, so a
// player meets the players of the same simulation who chose the same mode. That name is what the
// services compare with the one they serve and judge (servedPhotonVersion in node.js); the mode
// itself is in the match's config, which the judge replays with.

export const MODES = ['siege', 'coins'];

/** The simulation (or build) in the name a page gives Photon, without the mode chosen after it. */
export function buildOf(appVersion) {
  if (typeof appVersion !== 'string') return appVersion;
  const cut = appVersion.lastIndexOf('-');
  return cut > 0 && MODES.includes(appVersion.slice(cut + 1)) ? appVersion.slice(0, cut) : appVersion;
}
