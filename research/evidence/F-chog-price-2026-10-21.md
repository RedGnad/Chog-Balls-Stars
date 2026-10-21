# The price of $CHOG in dollars: which public source answers (21 October 2026)

Stream F, lot 1. Checked with `curl` from this Mac, each request with a browser-like User-Agent and no API
key, for the token `0x350035555E10d9AfAF1566AaebfCeD5BA6C27777` on Monad (chain 143).

| Source | Request | Answer that morning | Limits (from each site's documentation, read that day) |
| --- | --- | --- | --- |
| CoinGecko | `GET https://api.coingecko.com/api/v3/simple/token_price/monad?contract_addresses=0x3500...7777&vs_currencies=usd` | `{"0x350035555e10d9afaf1566aaebfced5ba6c27777":{"usd":0.00150916}}`; the coin is listed as `chog` (market cap rank 2592 in `/search?query=chog`) | public plan, a few calls a minute, attribution asked |
| GeckoTerminal | `GET https://api.geckoterminal.com/api/v2/networks/monad/tokens/0x3500...7777` | `price_usd: "0.001507730107"`, `coingecko_coin_id: "chog"`, three pools listed | 30 calls a minute |
| DexScreener | `GET https://api.dexscreener.com/latest/dex/tokens/0x3500...7777` | first pair nad.fun `0x116e...8f1` CHOG/MON, `priceUsd: "0.001522"`, liquidity $96,259, 24 h volume $9,720 | 300 calls a minute |

So all three answer. The services (`server/src/price.js`) try CoinGecko, then GeckoTerminal, then DexScreener, keep the
answer five minutes, answer the last price read while every source is down (marked `stale` past thirty minutes), and
never fail a page: `GET /api/price` answers `usd: null` until a source has answered, and the game then shows amounts
in $CHOG alone (`ChogPrice.cs`).

At that price, 1,000 $CHOG is $1.51; the shop's second form (500 $CHOG) is $0.75; a 10 $CHOG a day rental is $0.02 a
day.

MON that morning: $0.02287 (CoinGecko `simple/price?ids=monad`); gas price 102 gwei (`eth_gasPrice` on rpc.monad.xyz).
