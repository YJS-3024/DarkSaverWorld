"""Persistent local stdio MCP connection for reproducible REA/Ghidra work.

The bridge only forwards explicit JSON-RPC requests. It does not execute targets,
choose analysis tools, or change Codex configuration. Responses stay in files so
large evidence records need not be printed in the chat.
"""
import argparse
import json
import os
from pathlib import Path
import queue
import signal
import socket
import socketserver
import subprocess
import threading
import tomllib


class McpClient:
    def __init__(self, log_dir):
        config = tomllib.loads(Path('/home/agent/.codex/config.toml').read_text())['mcp_servers']['rea']
        environment = os.environ.copy()
        environment.update(config.get('env', {}))
        log_dir.mkdir(parents=True, exist_ok=True)
        self.stderr = (log_dir / 'mcp.stderr.log').open('a')
        self.process = subprocess.Popen([config['command'], *config['args']],
                                        stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                        stderr=self.stderr, text=True, bufsize=1,
                                        env=environment, start_new_session=True)
        self.responses = queue.Queue()
        self.next_id = 0
        self.lock = threading.Lock()
        threading.Thread(target=self.read, daemon=True).start()
        self.identity = self.request('initialize', {
            'protocolVersion': '2025-06-18', 'capabilities': {},
            'clientInfo': {'name': 'DarkSaver-Ghidra-analysis', 'version': '1.0'}})
        self.send({'jsonrpc': '2.0', 'method': 'notifications/initialized'})

    def read(self):
        try:
            for line in self.process.stdout:
                try:
                    message = json.loads(line)
                except json.JSONDecodeError:
                    continue
                if 'id' in message:
                    self.responses.put(message)
        finally:
            self.responses.put(None)

    def send(self, message):
        self.process.stdin.write(json.dumps(message) + '\n')
        self.process.stdin.flush()

    def request(self, method, params, timeout=900):
        with self.lock:
            self.next_id += 1
            request_id = self.next_id
            self.send({'jsonrpc': '2.0', 'id': request_id, 'method': method, 'params': params})
            response = self.responses.get(timeout=timeout)
            if response is None:
                raise RuntimeError('REA MCP stdout closed; inspect mcp.stderr.log')
            if response.get('id') != request_id:
                raise RuntimeError('Unexpected MCP response identity')
            return response

    def close(self):
        os.killpg(self.process.pid, signal.SIGTERM)
        try:
            self.process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            os.killpg(self.process.pid, signal.SIGKILL)
            self.process.wait()
        self.stderr.close()


def serve(args):
    client = McpClient(args.log_dir)

    class Handler(socketserver.StreamRequestHandler):
        def handle(self):
            request = json.loads(self.rfile.readline())
            try:
                if request['method'] == 'bridge/shutdown':
                    response = {'result': {'shutdown': True}}
                    threading.Thread(target=server.shutdown, daemon=True).start()
                else:
                    response = client.request(request['method'], request.get('params', {}),
                                              request.get('timeout', 900))
            except Exception as error:
                response = {'bridgeError': type(error).__name__, 'message': str(error)}
            self.wfile.write((json.dumps(response, ensure_ascii=False) + '\n').encode())

    if args.socket.exists():
        raise RuntimeError('Existing socket preserved; verify its server before choosing another path')
    with socketserver.UnixStreamServer(str(args.socket), Handler) as server:
        args.socket.chmod(0o600)
        (args.log_dir / 'mcp-server.json').write_text(json.dumps(client.identity, indent=2) + '\n')
        print(json.dumps({'socket': str(args.socket), 'pid': os.getpid(),
                          'initialize': client.identity.get('result', {}).get('serverInfo')}), flush=True)
        signal.signal(signal.SIGTERM, lambda *_: threading.Thread(target=server.shutdown, daemon=True).start())
        try:
            server.serve_forever()
        finally:
            client.close()
            args.socket.unlink(missing_ok=True)


def call(args):
    request = json.loads(args.request.read_text())
    with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as connection:
        connection.settimeout(request.get('timeout', 900) + 30)
        connection.connect(str(args.socket))
        connection.sendall((json.dumps(request) + '\n').encode())
        with connection.makefile('rb') as stream:
            response = json.loads(stream.readline())
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(response, ensure_ascii=False, indent=2) + '\n')
    result = response.get('result', {})
    print(json.dumps({'output': str(args.output), 'bytes': args.output.stat().st_size,
                      'error': bool(response.get('error') or response.get('bridgeError') or result.get('isError'))}))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    server = commands.add_parser('serve')
    server.add_argument('--socket', type=Path, required=True)
    server.add_argument('--log-dir', type=Path, required=True)
    request = commands.add_parser('call')
    request.add_argument('--socket', type=Path, required=True)
    request.add_argument('--request', type=Path, required=True)
    request.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    (serve if args.command == 'serve' else call)(args)


if __name__ == '__main__':
    main()
