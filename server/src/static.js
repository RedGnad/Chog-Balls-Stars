// Serves plain files from a few folders, each under its own address prefix: the WebGL build,
// the hero page, the portraits. A Unity Web build ships its large files already compressed
// (".gz" or ".br"); they are sent as they are with the header that tells the browser so.

import { createReadStream } from 'node:fs';
import { stat } from 'node:fs/promises';
import { extname, join, normalize, resolve, sep } from 'node:path';
import { Readable } from 'node:stream';

const TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'application/javascript; charset=utf-8',
  '.mjs': 'application/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.webmanifest': 'application/manifest+json; charset=utf-8',
  '.wasm': 'application/wasm',
  '.data': 'application/octet-stream',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.webp': 'image/webp',
  '.svg': 'image/svg+xml',
  '.ico': 'image/x-icon',
  '.txt': 'text/plain; charset=utf-8',
};
const ENCODINGS = { '.gz': 'gzip', '.br': 'br' };

/** Content type and content encoding of a file name such as "webgl.wasm.gz". */
export function describeFile(name) {
  const encoding = ENCODINGS[extname(name)];
  const plain = encoding ? name.slice(0, -extname(name).length) : name;
  return { type: TYPES[extname(plain).toLowerCase()] ?? 'application/octet-stream', encoding };
}

/**
 * @param {{prefix: string, root: string, cache?: string}[]} mounts "prefix" starts and ends with
 *   a slash; the longest matching prefix wins. "cache" is the Cache-Control of its files.
 * @returns {(request: Request) => Promise<Response|null>} null when no mount covers the address
 */
export function createStatic(mounts) {
  const ordered = mounts
    .map(mount => ({ ...mount, root: resolve(mount.root) }))
    .sort((a, b) => b.prefix.length - a.prefix.length);

  return async function serve(request) {
    const url = new URL(request.url);
    let path;
    try {
      path = decodeURIComponent(url.pathname);
    } catch {
      return new Response('Bad request', { status: 400 });
    }

    // "/hero" is the folder "/hero/": relative addresses in its page need the slash.
    const folder = ordered.find(mount => mount.prefix !== '/' && path + '/' === mount.prefix);
    if (folder) return new Response(null, { status: 308, headers: { Location: folder.prefix + url.search } });

    const mount = ordered.find(candidate => path.startsWith(candidate.prefix));
    if (!mount) return null;
    if (request.method !== 'GET' && request.method !== 'HEAD') return new Response('Method not allowed', { status: 405 });

    // Never leave the folder of the mount, whatever the address contains.
    let file = normalize(join(mount.root, path.slice(mount.prefix.length)));
    if (file !== mount.root && !file.startsWith(mount.root + sep)) return new Response('Not found', { status: 404 });

    let info = await stat(file).catch(() => null);
    if (info?.isDirectory()) {
      file = join(file, 'index.html');
      info = await stat(file).catch(() => null);
    }
    if (!info?.isFile()) return new Response('Not found', { status: 404 });

    const { type, encoding } = describeFile(file);
    const headers = {
      'Content-Type': type,
      'Content-Length': String(info.size),
      'Cache-Control': mount.cache ?? 'no-cache',
      'Last-Modified': info.mtime.toUTCString(),
    };
    if (encoding) headers['Content-Encoding'] = encoding;

    const since = Date.parse(request.headers.get('if-modified-since') ?? '');
    if (since && Math.floor(info.mtimeMs / 1000) * 1000 <= since) {
      delete headers['Content-Length'];
      return new Response(null, { status: 304, headers });
    }
    if (request.method === 'HEAD') return new Response(null, { status: 200, headers });
    return new Response(Readable.toWeb(createReadStream(file)), { status: 200, headers });
  };
}

/**
 * Where a page asked at another address than the public one is sent: the same path at the public
 * address (308), or null when it is already there. The pages may be served elsewhere (on Vercel,
 * tools/release/vercel_site.sh) while these services keep their own address: a page opened at the
 * services' address would show a sign-in message naming another site.
 */
export function movedToPublic(request, publicOrigin) {
  if (!publicOrigin) return null;
  const url = new URL(request.url);
  if (url.host === new URL(publicOrigin).host) return null;
  return new Response(null, { status: 308, headers: { Location: publicOrigin + url.pathname + url.search } });
}
