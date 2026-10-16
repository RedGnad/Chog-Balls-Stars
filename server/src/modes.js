// A page names itself to Photon with its build, followed by the mode its player chose on the menu
// ("-siege" or "-coins", Chog.Screens.MatchModeChoice): Photon only lets pages that give the same
// name meet, so a player meets the players of the same build who chose the same mode. The build is
// what the services compare with the one they serve and judge; the mode itself is in the match's
// config, which the judge replays with.

export const MODES = ['siege', 'coins'];

/** The build in the name a page gives Photon, without the mode chosen after it. */
export function buildOf(appVersion) {
  if (typeof appVersion !== 'string') return appVersion;
  const cut = appVersion.lastIndexOf('-');
  return cut > 0 && MODES.includes(appVersion.slice(cut + 1)) ? appVersion.slice(0, cut) : appVersion;
}
