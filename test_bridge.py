import socket
import json
import time

HOST = "localhost"
PORT = 8765

def main():
    print("Connecting to Terraria bridge...")
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.connect((HOST, PORT))
    s.settimeout(1.0)

    print("Connected.")

    try:
        while True:
            # Send move-right action
            action_payload = {"action": 1}
            s.sendall((json.dumps(action_payload) + "\n").encode())

            # Receive state
            data = s.recv(4096)
            if not data:
                print("No data received.")
                break

            decoded = data.decode().strip()
            print("Raw:", decoded)

            try:
                state = json.loads(decoded)
                print("Parsed state:", state)
            except json.JSONDecodeError:
                print("Invalid JSON received!")

            time.sleep(0.1)

    except KeyboardInterrupt:
        print("Stopping test.")

    finally:
        print("Closing socket.")
        s.close()

if __name__ == "__main__":
    main()
