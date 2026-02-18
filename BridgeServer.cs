using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Terraria;
using Terraria.ModLoader;

namespace TeRL
{
	internal class BridgeServer
	{
		private const int Port = 8765;
		private const int ReadBufferSize = 256;
		private const int ReceiveTimeoutMs = 100;

		private static BridgeServer _instance;
		public static BridgeServer Instance => _instance ??= new BridgeServer();

		private Mod _mod;
		private TcpListener _listener;
		private TcpClient _client;
		private NetworkStream _stream;
		private readonly byte[] _readBuffer = new byte[ReadBufferSize];
		private readonly List<byte> _lineBuffer = new List<byte>();

		private class ActionMessage
		{
			public int action { get; set; }
		}

		private BridgeServer() { }

		/// <summary>Sets the Mod reference for logging. Idempotent.</summary>
		public void Init(Mod mod)
		{
			_mod ??= mod;
		}

		/// <summary>Starts the TCP listener. Idempotent: safe to call multiple times. Catches port-in-use etc. so the game does not crash.</summary>
		public void StartListening()
		{
			if (_listener != null) return;
			try
			{
				_listener = new TcpListener(IPAddress.Loopback, Port);
				_listener.Start();
				_mod?.Logger.Info($"TeRL bridge listening on localhost:{Port}");
			}
			catch (SocketException ex)
			{
				_mod?.Logger.Warn($"TeRL bridge could not bind to port {Port}: {ex.Message}");
			}
		}

		private void AcceptConnection()
		{
			if (_client != null) return;
			if (_listener == null) StartListening();
			if (_listener == null || !_listener.Pending()) return;

			_client = _listener.AcceptTcpClient();
			_client.ReceiveTimeout = ReceiveTimeoutMs;
			_stream = _client.GetStream();
			_lineBuffer.Clear();
			_mod?.Logger.Info("RL client connected.");
		}

		private void ReceiveAction(Player player)
		{
			if (_client == null || !_client.Connected || _stream == null) return;

			player.controlLeft = false;
			player.controlRight = false;
			player.controlJump = false;
			player.controlUseItem = false;

			while (_stream.DataAvailable)
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
					catch (JsonException ex)
					{
						_mod?.Logger.Debug($"TeRL: malformed action JSON: {ex.Message}");
					}
				}
			}
		}

		/// <summary>Serializes the state to JSON with System.Text.Json and sends it over the bridge. Called once per tick.</summary>
		private void SendState(StateDTO state)
		{
			if (_client == null || !_client.Connected || _stream == null) return;
			if (state == null) return;

			string json = JsonSerializer.Serialize(state);
			byte[] payload = Encoding.UTF8.GetBytes(json + "\n");
			_stream.Write(payload, 0, payload.Length);
		}

		/// <summary>Accepts connection, receives actions, and sends state once. Call once per tick from PostUpdatePlayers.</summary>
		public void Update(Player player, StateDTO state)
		{
			if (player == null) return;

			try
			{
				AcceptConnection();
				if (_client == null) return;

				SendState(state);
				ReceiveAction(player);
			}
			catch (Exception ex)
			{
				_mod?.Logger.Warn($"TeRL bridge error: {ex.Message}");
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

		private void CloseClient()
		{
			try { _stream?.Close(); } catch { }
			try { _client?.Close(); } catch { }
			_stream = null;
			_client = null;
			_lineBuffer.Clear();
			_mod?.Logger.Info("RL client disconnected.");
		}
	}
}
