# Counts the ChogWager v3 events since its deployment, by event signature (rpc2 serves 10,000 blocks a call).
import json, sys, urllib.request, urllib.error, collections
RPC = "https://rpc2.monad.xyz"
ADDR = "0xa9d1faa2e09753f5419d768cae05b1e2596b3e4f"
import time
def call(method, params, tries=6):
    req = urllib.request.Request(RPC, json.dumps({"jsonrpc": "2.0", "id": 1, "method": method, "params": params}).encode(),
                                 {"Content-Type": "application/json", "User-Agent": "Mozilla/5.0"})
    for i in range(tries):
        try:
            return json.load(urllib.request.urlopen(req, timeout=30))["result"]
        except urllib.error.HTTPError as e:
            if e.code != 429 or i == tries - 1: raise
            time.sleep(2 + 2 * i)
head = int(call("eth_blockNumber", []), 16)
start = 115733839
topics = collections.Counter()
txs = set()
b = start
while b <= head:
    end = min(b + 9999, head)
    for log in call("eth_getLogs", [{"address": ADDR, "fromBlock": hex(b), "toBlock": hex(end)}]):
        topics[log["topics"][0]] += 1
        txs.add(log["transactionHash"])
    b = end + 1
    time.sleep(0.4)
print("blocks", start, head, "logs", sum(topics.values()), "txs", len(txs))
for t, n in topics.most_common():
    print(t, n)
