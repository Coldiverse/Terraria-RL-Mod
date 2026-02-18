import socket

HOST = "127.0.0.1"
PORT = 8765

s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.connect((HOST, PORT))

print("Connected to Terraria RL mod!")

data = s.recv(4096)
print("Received:", data.decode())

s.close()
