"""Gas of ChogCareers calls under Monad mainnet's own rules, without deploying anything.

    python3 -I tools/contracts/estimate_careers_gas.py

Asks `eth_estimateGas` of https://rpc.monad.xyz with the contract's runtime code and storage given as a
state override at a made-up address (the referee written in its slot), so the node prices the calls
with Monad's opcode costs (MIP-8 storage pages included), which forge's local EVM does not. Nothing
is signed or sent. Needs foundry (`~/.foundry/bin`) and the contracts built.
"""
import json
import os
import subprocess
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
FOUNDRY = os.path.expanduser('~/.foundry/bin')
RPC = os.environ.get('MONAD_RPC', 'https://rpc.monad.xyz')
CONTRACT = '0x00000000000000000000000000000000c4a7ee75'
REFEREE = '0x000000000000000000000000000000000000beef'
# Storage slots of ChogCareers (forge inspect ChogCareers storageLayout): the careers from 0 to 1969,
# then the referee and the owner.
REFEREE_SLOT = 1970
OWNER_SLOT = 1971


def word(number):
    return '0x' + format(number, '064x')


def run(*command, cwd=None):
    return subprocess.check_output(command, text=True, cwd=cwd).strip()


def calldata(signature, *args):
    return run(os.path.join(FOUNDRY, 'cast'), 'calldata', signature, *args)


def estimate(code, data, played_tokens=()):
    state = {word(REFEREE_SLOT): word(int(REFEREE, 16)), word(OWNER_SLOT): word(int(REFEREE, 16))}
    for token in played_tokens:
        # A career that already played: 5 matches, 2 wins, last played long ago.
        state[word(token)] = word(5 | (2 << 32) | (1_700_000_000 << 192))
    body = {'jsonrpc': '2.0', 'id': 1, 'method': 'eth_estimateGas',
            'params': [{'from': REFEREE, 'to': CONTRACT, 'data': data}, 'latest',
                       {CONTRACT: {'code': code, 'stateDiff': state}}]}
    request = urllib.request.Request(RPC, json.dumps(body).encode(),
                                     {'content-type': 'application/json', 'user-agent': 'Mozilla/5.0'})
    answer = json.load(urllib.request.urlopen(request, timeout=60))
    return int(answer['result'], 16) if 'result' in answer else answer


def main():
    code = run(os.path.join(FOUNDRY, 'forge'), 'inspect', 'ChogCareers', 'deployedBytecode',
               cwd=os.path.join(ROOT, 'contracts'))
    record = 'record(bytes32,uint8,(uint16,uint8,uint8)[])'
    key = '0x' + '11' * 32
    cases = {
        '1 Genesis': [(1795, 0, 0)],
        '3 Genesis': [(10, 0, 0), (500, 1, 1), (1500, 1, 0)],
        '6 Genesis on 6 pages': [(1, 1, 0), (200, 1, 1), (700, 0, 0), (900, 1, 2), (1795, 0, 1), (1969, 0, 2)],
        '6 Genesis on one page': [(1, 1, 0), (2, 1, 1), (7, 0, 0), (90, 1, 2), (100, 0, 1), (127, 0, 2)],
    }
    for name, entries in cases.items():
        data = calldata(record, key, '0', '[' + ','.join(f'({t},{team},{by})' for t, team, by in entries) + ']')
        tokens = [token for token, _, _ in entries]
        print(f'record, {name}: first match {estimate(code, data)} gas, already played {estimate(code, data, tokens)} gas')
    print('packedCareers(1, 1969):', estimate(code, calldata('packedCareers(uint256,uint256)', '1', '1969')), 'gas')
    every = '[' + ','.join(str(token) for token in range(1, 1970)) + ']'
    print('careersOf of the 1,969 ids:', estimate(code, calldata('careersOf(uint256[])', every)), 'gas')


if __name__ == '__main__':
    main()
