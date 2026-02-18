using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Terraria;

namespace TeRL
{
	internal static class BridgeServer
	{
		private const int Port = 8765;
		private const int ReadBufferSize = 256;

		private static TcpListener _listener;
		private static TcpClient _client;
		private static NetworkStream _stream;
		private static readonly byte[] _readBuffer = new byte[ReadBufferSize];
		private static readonly List<byte> _lineBuffer = new List<byte>();

		private class ActionMessage
		{
			public int action { get; set; }
		}

		/// <summary>Starts the TCP listener. Idempotent: safe to call multiple times.</summary>
		public static void Start()
		{
			if (_listener != null) return;
			_listener = new TcpListener(IPAddress.Loopback, Port);
			_listener.Start();
		}

		public static void Update(Player player)
		{
			if (player == null) return;

			try
			{
				if (_client == null)
				{
					if (_listener == null) Start();
					if (!_listener.Pending()) return;
					_client = _listener.AcceptTcpClient();
					_stream = _client.GetStream();
					_lineBuffer.Clear();
				}

				if (!_client.Connected)
				{
					CloseClient();
					return;
				}

				player.controlLeft = false;
				player.controlRight = false;
				player.controlJump = false;
				player.controlUseItem = false;

				if (_stream.DataAvailable)
				{
					int read = _stream.Read(_readBuffer, 0, _readBuffer.Length);
					for (int i = 0; i < read; i++)
						_lineBuffer.Add(_readBuffer[i]);

					int newlineIndex = _lineBuffer.IndexOf((byte)'\n');
					if (newlineIndex >= 0)
					{
						string line = Encoding.UTF8.GetString(_lineBuffer.GetRange(0, newlineIndex).ToArray());
						_lineBuffer.RemoveRange(0, newlineIndex + 1);

						try
						{
							var msg = JsonSerializer.Deserialize<ActionMessage>(line);
							if (msg != null)
								ApplyAction(player, msg.action);
						}
						catch { /* ignore malformed JSON */ }
					}
				}

				var state = new StateDTO
				{
					player_x = player.position.X,
					player_y = player.position.Y,
					health = player.statLife,
					is_night = !Main.dayTime
				};
				string json = JsonSerializer.Serialize(state);
				byte[] payload = Encoding.UTF8.GetBytes(json + "\n");
				_stream.Write(payload, 0, payload.Length);
			}
			catch
			{
				CloseClient();
			}
		}

		private static void ApplyAction(Player player, int action)
		{
			switch (action)
			{
				case 0: player.controlLeft = true; break;
				case 1: player.controlRight = true; break;
				case 2: player.controlJump = true; break;
				case 3: player.controlUseItem = true; break;
				case 4: break;
			}
		}

		private static void CloseClient()
		{
			try { _stream?.Close(); } catch { }
			try { _client?.Close(); } catch { }
			_stream = null;
			_client = null;
			_lineBuffer.Clear();
		}
	}
}
