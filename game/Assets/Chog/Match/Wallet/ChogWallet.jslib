// Bridge to the player's wallet (EIP-1193). The wallet object is the one the browser itself
// provides (window.ethereum: the extension on a computer, the wallet's own browser on a phone)
// or, on a page that has none, a wallet app connected through the page's connector
// (window.chogWallet, TemplateData/wallet-connector.js of the game page). No wallet library is
// part of the game: the connector loads one only when a player asks for it.
// Every answer comes back through SendMessage(object, method, json).
mergeInto(LibraryManager.library, {
  // 1 when the browser itself has a wallet, 0 otherwise.
  ChogWalletHasProvider: function () {
    return typeof window !== 'undefined' && window.ethereum && typeof window.ethereum.request === 'function' ? 1 : 0;
  },

  // For a page without its own wallet: calls availableMethod once (with "") if the page can
  // connect a wallet app, and answerMethod with {"address":"0x...","changed":true} each time
  // the connected app's account changes, an empty address when it goes away. A player who
  // connected that way on an earlier visit is connected again without being asked.
  ChogWalletWatchConnector: function (objectPtr, availableMethodPtr, answerMethodPtr) {
    var object = UTF8ToString(objectPtr);
    var availableMethod = UTF8ToString(availableMethodPtr);
    var answerMethod = UTF8ToString(answerMethodPtr);
    if (typeof window === 'undefined' || !window.chogWallet) return;

    window.chogWallet.watch(function () {
      SendMessage(object, availableMethod, '');
    }, function (address) {
      SendMessage(object, answerMethod, JSON.stringify({ address: String(address || ''), changed: true }));
    });
  },

  // Asks the wallet for the player's address: {"address":"0x..."} or {"error":"...","code":4001}.
  // An empty address means the wallet shared none. With prompt = 0 the wallet is only asked for
  // an address it already shared with this page, without opening it. The same message is sent
  // again, with "changed":true, whenever the player switches account in the wallet.
  // On a page without its own wallet, prompt = 1 opens the connector's window, where the
  // player picks a wallet app.
  ChogWalletRequestAddress: function (objectPtr, methodPtr, prompt) {
    var object = UTF8ToString(objectPtr);
    var method = UTF8ToString(methodPtr);
    var answer = function (payload) { SendMessage(object, method, JSON.stringify(payload)); };
    var first = function (accounts) { return accounts && accounts.length ? String(accounts[0]) : ''; };
    var refusal = function (error) {
      answer({ error: String((error && error.message) || error || 'refused'), code: Number((error && error.code) || 0) });
    };

    var own = typeof window !== 'undefined' && window.ethereum && typeof window.ethereum.request === 'function' ? window.ethereum : null;
    if (!own) {
      if (typeof window === 'undefined' || !window.chogWallet || !prompt) {
        answer({ error: 'no wallet in this browser', code: 0 });
        return;
      }
      window.chogWallet.connect().then(function (address) { answer({ address: String(address || '') }); }).catch(refusal);
      return;
    }

    if (!window.chogWalletWatched && typeof own.on === 'function') {
      window.chogWalletWatched = true;
      own.on('accountsChanged', function (accounts) {
        answer({ address: first(accounts), changed: true });
      });
    }

    own.request({ method: prompt ? 'eth_requestAccounts' : 'eth_accounts' }).then(function (accounts) {
      answer({ address: first(accounts) });
    }).catch(refusal);
  },

  // Asks the wallet to sign a text with the key of that address (personal_sign): no transaction,
  // no cost. Answers {"signature":"0x..."} or {"error":"...","code":4001}.
  ChogWalletSign: function (objectPtr, methodPtr, messagePtr, addressPtr) {
    var object = UTF8ToString(objectPtr);
    var method = UTF8ToString(methodPtr);
    var message = UTF8ToString(messagePtr);
    var address = UTF8ToString(addressPtr);
    var answer = function (payload) { SendMessage(object, method, JSON.stringify(payload)); };

    // The browser's own wallet, else the wallet app connected through the page's connector.
    var wallet = typeof window === 'undefined' ? null
      : window.chogWallet ? window.chogWallet.object()
      : window.ethereum && typeof window.ethereum.request === 'function' ? window.ethereum : null;
    if (!wallet) {
      answer({ error: 'no wallet in this browser', code: 0 });
      return;
    }

    // personal_sign takes the text as the hex of its UTF-8 bytes.
    var bytes = new TextEncoder().encode(message);
    var hex = '0x';
    for (var i = 0; i < bytes.length; i++) hex += ('0' + bytes[i].toString(16)).slice(-2);

    wallet.request({ method: 'personal_sign', params: [hex, address] }).then(function (signature) {
      answer({ signature: String(signature) });
    }).catch(function (error) {
      answer({ error: String((error && error.message) || error || 'refused'), code: Number((error && error.code) || 0) });
    });
  },

  // Leaves this page for another address: used to reopen the game inside a wallet's own
  // browser. The page itself navigates, which a phone browser allows without a new window.
  ChogWalletLeaveFor: function (urlPtr) {
    window.location.href = UTF8ToString(urlPtr);
  },

  // Sends one request to the wallet as it is (EIP-1193: a method and its parameters as JSON) and
  // answers {"id":n,"result":"<what the wallet returned, as JSON text>"} or
  // {"id":n,"error":"...","code":4001}. Used for what a purchase needs: switching to the chain,
  // reading an allowance, sending a transaction, asking for its receipt.
  ChogWalletRequest: function (objectPtr, methodPtr, id, rpcMethodPtr, paramsPtr) {
    var object = UTF8ToString(objectPtr);
    var method = UTF8ToString(methodPtr);
    var rpcMethod = UTF8ToString(rpcMethodPtr);
    var answer = function (payload) { payload.id = id; SendMessage(object, method, JSON.stringify(payload)); };

    var params;
    try { params = JSON.parse(UTF8ToString(paramsPtr)); } catch (error) { answer({ error: 'malformed parameters', code: 0 }); return; }
    // The browser's own wallet, else the wallet app connected through the page's connector.
    var wallet = typeof window === 'undefined' ? null
      : window.chogWallet ? window.chogWallet.object()
      : window.ethereum && typeof window.ethereum.request === 'function' ? window.ethereum : null;
    if (!wallet) {
      answer({ error: 'no wallet in this browser', code: 0 });
      return;
    }

    wallet.request({ method: rpcMethod, params: params }).then(function (result) {
      answer({ result: JSON.stringify(result === undefined ? null : result) });
    }).catch(function (error) {
      answer({ error: String((error && error.message) || error || 'refused'), code: Number((error && error.code) || 0) });
    });
  },

  // Hands a link to the share sheet of the phone when the browser has one, else copies it.
  // Answers {"outcome":"shared"}, {"outcome":"copied"} or {"outcome":"failed"}. The link is
  // also left in window.chogLastLink, where a scripted test can read it.
  ChogWalletShare: function (objectPtr, methodPtr, textPtr, urlPtr) {
    var object = UTF8ToString(objectPtr);
    var method = UTF8ToString(methodPtr);
    var text = UTF8ToString(textPtr);
    var url = UTF8ToString(urlPtr);
    var answer = function (outcome) { SendMessage(object, method, JSON.stringify({ outcome: outcome })); };
    window.chogLastLink = url;

    // Works on a page that is not served over HTTPS, where the clipboard interface is absent.
    var copyTheOldWay = function () {
      var field = document.createElement('textarea');
      field.value = url;
      field.setAttribute('readonly', '');
      field.style.position = 'fixed';
      field.style.opacity = '0';
      document.body.appendChild(field);
      field.select();
      var copied = false;
      try { copied = document.execCommand('copy'); } catch (error) { copied = false; }
      document.body.removeChild(field);
      answer(copied ? 'copied' : 'failed');
    };
    var copy = function () {
      if (navigator.clipboard && typeof navigator.clipboard.writeText === 'function') {
        navigator.clipboard.writeText(url).then(function () { answer('copied'); }).catch(copyTheOldWay);
      } else {
        copyTheOldWay();
      }
    };

    if (typeof navigator.share === 'function') {
      navigator.share({ title: 'Chog Genesis', text: text, url: url }).then(function () {
        answer('shared');
      }).catch(function (error) {
        // The player closed the sheet: nothing to report. Anything else falls back to a copy.
        if (error && error.name === 'AbortError') answer('shared');
        else copy();
      });
    } else {
      copy();
    }
  }
});
