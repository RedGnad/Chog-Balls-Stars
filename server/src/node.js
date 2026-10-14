// What the services need from Node itself: an HTTP server around handlers written against the
// web-standard Request and Response, and careers and the session secret kept in a folder.

import { execFile } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { mkdir, readdir, readFile, rename, writeFile } from 'node:fs/promises';
import { createServer } from 'node:http';
import { dirname, join } from 'node:path';
import { Readable } from 'node:stream';
import { pipeline } from 'node:stream/promises';

const MAX_REQUEST_BYTES = 100_000;
// A slice of the input history Photon's server sends is larger than anything a page sends.
const MAX_WEBHOOK_BYTES = 4_000_000;
export const WEBHOOK_PREFIX = '/api/photon/hook/';

/**
 * @param {(request: Request) => Promise<Response>} handle
 * @param {object} [options]
 * @param {boolean} [options.trustForwarded] the server sits behind a proxy that sets
 *   X-Forwarded-Proto and X-Forwarded-Host; they are otherwise removed, so a client cannot
 *   choose the address written in the message a wallet signs
 * @param {(line: string) => void} [options.log]
 */
export function createHttpServer(handle, options) {
  return createServer(createListener(handle, options));
}

/** The request listener behind createHttpServer, for a server made another way (HTTPS in a test). */
export function createListener(handle, { trustForwarded = false, log = console.log } = {}) {
  return async (incoming, outgoing) => {
    try {
      const chunks = [];
      let size = 0;
      const limit = (incoming.url ?? '').startsWith(WEBHOOK_PREFIX) ? MAX_WEBHOOK_BYTES : MAX_REQUEST_BYTES;
      for await (const chunk of incoming) {
        size += chunk.length;
        if (size > limit) {
          outgoing.writeHead(413).end();
          return;
        }
        chunks.push(chunk);
      }

      const headers = new Headers();
      for (const [name, value] of Object.entries(incoming.headers)) {
        if (!trustForwarded && name.startsWith('x-forwarded-')) continue;
        headers.set(name, Array.isArray(value) ? value.join(', ') : value);
      }
      const hasBody = incoming.method !== 'GET' && incoming.method !== 'HEAD';
      const request = new Request(`http://${incoming.headers.host ?? 'localhost'}${incoming.url}`, {
        method: incoming.method, headers, body: hasBody ? Buffer.concat(chunks) : undefined,
      });

      const response = await handle(request);
      outgoing.writeHead(response.status, Object.fromEntries(response.headers));
      if (response.body) await pipeline(Readable.fromWeb(response.body), outgoing);
      else outgoing.end();
      if (incoming.url.startsWith('/api/')) log(`${new Date().toISOString()} ${incoming.method} ${incoming.url.split('?')[0]} ${response.status}`);
    } catch (error) {
      console.error(`${incoming.method} ${incoming.url} failed:`, error);
      if (!outgoing.headersSent) outgoing.writeHead(500);
      outgoing.end();
    }
  };
}

/** The "load" and "save" of the careers, as one JSON file in that folder. */
export function careerFile(dataFolder) {
  const path = join(dataFolder, 'careers.json');
  return {
    load: async () => {
      try {
        return JSON.parse(await readFile(path, 'utf8'));
      } catch (error) {
        if (error.code === 'ENOENT') return null;
        throw error;
      }
    },
    // Written beside the file, then moved over it, so a crash never leaves half a file.
    save: async state => {
      await writeFile(path + '.new', JSON.stringify(state));
      await rename(path + '.new', path);
    },
  };
}

/** The secret that authenticates sessions: the given one, else one made once and kept in the folder. */
export function sessionSecret(dataFolder, given) {
  return keptSecret(dataFolder, 'session-secret', given);
}

/**
 * The secret Photon's dashboard sends with its requests: the given one, else one made once and
 * kept in the folder, to be copied from that file into the dashboard.
 */
export function photonSecret(dataFolder, given) {
  return keptSecret(dataFolder, 'photon-secret', given);
}

/**
 * The Reown project id the game page needs to reach a wallet app from a browser that has no
 * wallet: the given one, else the text of the file "reown-project-id" in the folder, else
 * none. It is not a secret (every page that uses it shows it), but it belongs to one Reown
 * account, so it is a setting of the deployment and not a line of the repository. Anything
 * that is not 32 hexadecimal digits is ignored, with a word in the log.
 */
export async function reownProjectId(dataFolder, given) {
  let text = (given ?? '').trim();
  if (!text) {
    try {
      text = (await readFile(join(dataFolder, 'reown-project-id'), 'utf8')).trim();
    } catch {
      return '';
    }
  }
  if (/^[0-9a-f]{32}$/i.test(text)) return text.toLowerCase();
  if (text) console.warn('The Reown project id is not 32 hexadecimal digits: ignored, no wallet connector is offered.');
  return '';
}

/**
 * The build of the game a site folder serves: the stamp at the start of "build" in its
 * RELEASE.json (tools/release/assemble_site.py), which is also the version that build gives
 * Photon. None for a folder that was not assembled: nothing is then compared with it.
 */
export async function servedBuild(siteFolder) {
  try {
    const { build } = JSON.parse(await readFile(join(siteFolder, 'RELEASE.json'), 'utf8'));
    const stamp = typeof build === 'string' ? build.split('-')[0] : '';
    return /^\d{8}T\d{9}Z$/.test(stamp) ? stamp : '';
  } catch {
    return '';
  }
}

async function keptSecret(dataFolder, name, given) {
  if (given) return given;
  await mkdir(dataFolder, { recursive: true });
  const path = join(dataFolder, name);
  try {
    return (await readFile(path, 'utf8')).trim();
  } catch {
    const made = randomBytes(32).toString('hex');
    await writeFile(path, made + '\n', { mode: 0o600 });
    return made;
  }
}

/**
 * The store of the matches (matches.js) as plain files: the key "folder/name" is the file
 * folder/name.json under "root".
 */
export function matchFiles(root) {
  const fileOf = key => {
    if (!/^[A-Za-z0-9_-]+\/[A-Za-z0-9_-]+$/.test(key)) throw new Error(`Not a key of the match store: ${key}`);
    return join(root, key + '.json');
  };
  const missing = fallback => error => {
    if (error.code === 'ENOENT') return fallback;
    throw error;
  };
  return {
    root,
    get: key => readFile(fileOf(key), 'utf8').catch(missing(null)),
    // Written beside the file, then moved over it, so a crash never leaves half a file.
    set: async (key, text) => {
      const path = fileOf(key);
      await mkdir(dirname(path), { recursive: true });
      await writeFile(path + '.new', text);
      await rename(path + '.new', path);
    },
    list: async prefix => {
      const [folder, start = ''] = prefix.split('/');
      const names = await readdir(join(root, folder)).catch(missing([]));
      return names.filter(name => name.endsWith('.json') && name.startsWith(start)).map(name => `${folder}/${name.slice(0, -5)}`).sort();
    },
  };
}

/**
 * The program that plays a match again (tools/quantum/Verifier.cs), as the judge calls it: it
 * is run on the folder of the match and its report is read back.
 * @param {object} options
 * @param {string} options.dotnet the dotnet executable
 * @param {string} options.verifier Chog.Quantum.Verifier.dll
 * @param {string} options.database the asset database exported with the build (quantum-db.json)
 * @param {string} options.lut the engine's LUT folder
 * @param {string} options.matchesRoot where matchFiles keeps the matches
 */
export function dotnetReplay({ dotnet, verifier, database, lut, matchesRoot }) {
  return folder => new Promise((resolve, reject) => {
    const record = join(matchesRoot, folder);
    const report = join(record, 'replay-report.json');
    execFile(dotnet, [verifier, record, database, lut, report], { timeout: 330_000, maxBuffer: 4_000_000 }, (error, stdout, stderr) => {
      if (error) {
        reject(new Error(`The replay of ${folder} failed: ${String(stderr || error.message).slice(-600)}`));
        return;
      }
      readFile(report, 'utf8').then(text => resolve(JSON.parse(text)), reject);
    });
  });
}
