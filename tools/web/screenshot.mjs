// Screenshot of a web page in headless Chrome, muted, with a real phone viewport by default
// (the window of headless Chrome cannot be made narrower than about 500 px, so the size is set
// through device emulation). Node 22+.
//
//   node tools/web/screenshot.mjs <url> <output.png> [width=390] [height=844] [full page: 1 or 0, default 1]
import {spawn} from 'node:child_process';
import {mkdtemp, readFile, writeFile, rm} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join} from 'node:path';

const [url, output, width = '390', height = '844', fullPage = '1'] = process.argv.slice(2);
if (!url || !output) {
  console.error('usage: node tools/web/screenshot.mjs <url> <output.png> [width] [height] [full page 1|0]');
  process.exit(2);
}

const profile = await mkdtemp(join(tmpdir(), 'chog-page-'));
const chrome = spawn('/Applications/Google Chrome.app/Contents/MacOS/Google Chrome', [
  '--headless=new', '--mute-audio', `--user-data-dir=${profile}`, '--remote-debugging-port=0',
  '--no-first-run', '--no-default-browser-check', '--hide-scrollbars', 'about:blank',
], {stdio: 'ignore'});
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

try {
  let port;
  for (let attempt = 0; attempt < 100 && !port; attempt++) {
    try { port = Number((await readFile(join(profile, 'DevToolsActivePort'), 'utf8')).split('\n')[0]); } catch { await sleep(200); }
  }
  if (!port) throw new Error('Chrome did not start');
  const targets = await (await fetch(`http://127.0.0.1:${port}/json/list`)).json();
  const socket = new WebSocket(targets.find(target => target.type === 'page').webSocketDebuggerUrl);
  await new Promise((resolve, reject) => { socket.onopen = resolve; socket.onerror = reject; });
  const pending = new Map();
  const problems = [];
  let serial = 0;
  socket.onmessage = ({data}) => {
    const message = JSON.parse(data);
    if (message.id && pending.has(message.id)) {
      pending.get(message.id)(message);
      pending.delete(message.id);
    } else if (message.method === 'Runtime.exceptionThrown') {
      problems.push('exception: ' + (message.params.exceptionDetails.exception?.description ?? message.params.exceptionDetails.text));
    } else if (message.method === 'Runtime.consoleAPICalled' && ['error', 'warning'].includes(message.params.type)) {
      problems.push(message.params.type + ': ' + message.params.args.map(argument => argument.value ?? argument.description).join(' '));
    } else if (message.method === 'Network.loadingFailed') {
      problems.push('request failed: ' + message.params.errorText);
    }
  };
  const command = (method, params = {}) => new Promise((resolve, reject) => {
    const id = ++serial;
    pending.set(id, message => (message.error ? reject(new Error(JSON.stringify(message.error))) : resolve(message.result)));
    socket.send(JSON.stringify({id, method, params}));
  });

  for (const domain of ['Page', 'Runtime', 'Network']) await command(domain + '.enable');
  await command('Emulation.setDeviceMetricsOverride', {width: Number(width), height: Number(height), deviceScaleFactor: 2, mobile: true});
  // CHOG_PAGE_AS says which phone the page should take the browser for (android or iphone):
  // some pages decide from that name what to show. CHOG_PAGE_WAIT_MS replaces the wait before the picture.
  const as = {
    android: 'Mozilla/5.0 (Linux; Android 11; ChogPageTest) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Mobile Safari/537.36',
    iphone: 'Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1',
  }[process.env.CHOG_PAGE_AS ?? ''];
  if (as) await command('Emulation.setUserAgentOverride', {userAgent: as});
  // CHOG_PAGE_SLOW_KBPS slows the downloads to that speed, to see what a page shows while it loads.
  if (process.env.CHOG_PAGE_SLOW_KBPS) {
    const bytesPerSecond = Number(process.env.CHOG_PAGE_SLOW_KBPS) * 125;
    await command('Network.emulateNetworkConditions', {offline: false, latency: 100, downloadThroughput: bytesPerSecond, uploadThroughput: bytesPerSecond});
  }
  await command('Page.navigate', {url});
  // Let the page load its data, its portraits and the answers of the services.
  await sleep(Number(process.env.CHOG_PAGE_WAIT_MS ?? 5000));
  const overflow = await command('Runtime.evaluate', {
    expression: 'document.documentElement.scrollWidth - document.documentElement.clientWidth', returnByValue: true,
  });
  const shot = await command('Page.captureScreenshot', {format: 'png', captureBeyondViewport: fullPage === '1'});
  await writeFile(output, Buffer.from(shot.data, 'base64'));
  console.log(`${output}  sideways overflow ${overflow.result.value}px  problems ${problems.length}`);
  for (const problem of problems.slice(0, 8)) console.log('  ' + problem);
} finally {
  chrome.kill();
  await sleep(300);
  await rm(profile, {recursive: true, force: true}).catch(() => {});
}
