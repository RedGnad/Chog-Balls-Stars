// The wallet of the page, wherever it comes from.
//
// A browser on a computer, or the browser inside a wallet app, gives the page a wallet object
// (window.ethereum, EIP-1193) and the game speaks to it directly. The normal browser of a phone
// has none. For that case this file offers a second way: the player picks a wallet app in a
// window drawn over the game, approves there, and the game then speaks to that app through the
// same kind of object. That connection goes through WalletConnect with Reown AppKit, which is
// only loaded when a player asks for it (or comes back to a connection made that way), and only
// when the game services give a Reown project id (GET api/wallet). Without one, nothing here is
// offered and the page behaves as it did before this file.
//
// The game side is ChogWallet.jslib, which asks window.chogWallet for the wallet object.
(function () {
  'use strict';

  // Reown's own browser build, at a fixed version: a published version never changes.
  var BUNDLE = 'https://cdn.jsdelivr.net/npm/@reown/appkit-cdn@1.8.24/dist/appkit.js';
  // Kept in the browser while a connection made here may still be alive, so a returning player
  // is connected again without asking and a visitor never loads the connector for nothing.
  var KEPT = 'chog.wallet.connector';
  // The kind of chain the game uses, as WalletConnect names it (EVM chains).
  var NAMESPACE = 'eip155';

  var projectId = '';
  var configured = null;   // promise: is a connector offered on this deployment
  var starting = null;     // promise: the AppKit object, loaded and ready
  var kit = null;          // the AppKit object once ready
  var address = '';        // the address connected through the connector, '' for none
  var watchers = [];       // told the address each time it changes

  function injected() {
    return window.ethereum && typeof window.ethereum.request === 'function' ? window.ethereum : null;
  }

  function kept() {
    try { return window.localStorage.getItem(KEPT) === '1'; } catch (error) { return false; }
  }

  function keep(yes) {
    try {
      if (yes) window.localStorage.setItem(KEPT, '1');
      else window.localStorage.removeItem(KEPT);
    } catch (error) { /* a browser that refuses storage only loses the automatic return */ }
  }

  function setAddress(next) {
    next = (next || '').toLowerCase();
    if (next === address) return;
    address = next;
    keep(address.length > 0);
    watchers.forEach(function (watcher) { watcher(address); });
  }

  // Asks the game services once whether this deployment has a Reown project id.
  function configure() {
    if (!configured) {
      configured = fetch('api/wallet', { cache: 'no-store' })
        .then(function (response) { return response.ok ? response.json() : {}; })
        .then(function (answer) {
          projectId = answer && typeof answer.reownProjectId === 'string' ? answer.reownProjectId : '';
          return projectId.length > 0;
        })
        .catch(function () { return false; });
    }
    return configured;
  }

  // Loads AppKit and makes its object; resolves once it knows whether a wallet is connected.
  function start() {
    if (!starting) {
      starting = import(BUNDLE).then(function (reown) {
        var networks = [reown.networks.monad];
        var made = reown.createAppKit({
          adapters: [new reown.WagmiAdapter({ projectId: projectId, networks: networks })],
          networks: networks,
          defaultNetwork: reown.networks.monad,
          projectId: projectId,
          metadata: {
            name: 'Chog Genesis',
            description: '3v3 arena. Your Genesis is your hero.',
            // Wallets compare this with the page that asks: it must be the address of the page.
            url: window.location.origin,
            icons: [new URL('TemplateData/icon-192.png', window.location.href).href]
          },
          // Only a wallet the player already has: a Genesis lives in such a wallet, and an
          // account made here by e-mail or a social login would hold none. What the project
          // has switched on in Reown's dashboard wins over these values: e-mail and social
          // logins must be switched off there too.
          features: { email: false, socials: [], analytics: false, swaps: false, onramp: false, send: false },
          // Coinbase's two connectors are left out: each loads a second library at once (one
          // with a telemetry script), for wallets that stay reachable through the list.
          enableCoinbase: false,
          enableBaseAccount: false,
          themeMode: 'dark'
        });
        kit = made;
        made.subscribeAccount(function (account) {
          setAddress(account && account.isConnected ? account.address : '');
        }, NAMESPACE);
        return made.ready().then(function () {
          var account = made.getAccount(NAMESPACE);
          setAddress(account && account.isConnected ? account.address : '');
          return made;
        });
      });
      // A load that failed (no network, a blocked script) may be tried again.
      starting.catch(function () { starting = null; });
    }
    return starting;
  }

  // Opens the window where the player picks a wallet app; resolves with the address once one
  // is connected, rejects with code 4001 when the player closes the window instead.
  function connect() {
    return configure().then(function (offered) {
      if (!offered) throw { code: 0, message: 'no wallet in this browser' };
      return start();
    }).then(function (made) {
      if (address) return address;
      return new Promise(function (resolve, reject) {
        var settled = false;
        var stopAccount = null;
        var stopState = null;
        var settle = function (found) {
          if (settled) return;
          settled = true;
          if (typeof stopAccount === 'function') stopAccount();
          if (typeof stopState === 'function') stopState();
          if (found) resolve(found);
          else reject({ code: 4001, message: 'the wallet window was closed' });
        };
        stopAccount = made.subscribeAccount(function (account) {
          if (account && account.isConnected && account.address) settle(account.address.toLowerCase());
        }, NAMESPACE);
        stopState = made.subscribeState(function (state) {
          if (state && state.open === false) {
            // The window also closes by itself once connected: leave the account a moment to
            // arrive before calling it a refusal. A later connection is still told to the
            // watchers, so nothing is lost if it takes longer.
            setTimeout(function () { settle(address); }, 600);
          }
        });
        made.open({ view: 'Connect', namespace: NAMESPACE });
      });
    });
  }

  window.chogWallet = {
    // The wallet object the game speaks to: the browser's own, else the connected app, else null.
    object: function () {
      var own = injected();
      if (own) return own;
      return kit && address ? (kit.getWalletProvider() || null) : null;
    },

    // True when the wallet in use is a connected app and not the browser's own.
    throughConnector: function () { return !injected() && address.length > 0; },

    connect: connect,

    // Forgets the connected app; the browser's own wallet is not ours to disconnect.
    disconnect: function () {
      if (!kit || !address) return Promise.resolve();
      return kit.disconnect(NAMESPACE).then(function () { setAddress(''); });
    },

    // Calls "available" once if a connector is offered on a page without its own wallet, and
    // "changed" with the address each time the connected app's account changes ('' when it
    // goes away). A player who connected that way on an earlier visit is connected again here.
    watch: function (available, changed) {
      if (injected()) return;
      watchers.push(changed);
      configure().then(function (offered) {
        if (!offered) return;
        available();
        if (kept()) start().catch(function () { keep(false); });
      });
    }
  };
})();
