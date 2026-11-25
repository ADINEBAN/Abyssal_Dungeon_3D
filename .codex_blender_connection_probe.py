import json
import socket


with socket.create_connection(("127.0.0.1", 9876), timeout=3) as client:
    client.sendall(json.dumps({"type": "get_addon_info", "params": {}}).encode("utf-8"))
    client.shutdown(socket.SHUT_WR)
    chunks = []
    while True:
        chunk = client.recv(65536)
        if not chunk:
            break
        chunks.append(chunk)

print(b"".join(chunks).decode("utf-8"))
