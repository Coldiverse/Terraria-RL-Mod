using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Terraria;
using Terraria.ModLoader;

namespace TeRL
{
	internal class BridgeServer
	{
		private const int Port = 8765;
		private const int ReceiveTimeoutMs = 100;

		private static BridgeServer _instance;
		public static BridgeServer Instance => _instance ??= new BridgeServer();

		private Mod _mod;
		private TcpListener _listener;
		private TcpClient _client;
		private NetworkStream _stream;
		private volatile ActionDTO latestAction = new ActionDTO();

		private BridgeServer() { }

		internal ActionDTO GetLatestAction() => latestAction;

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
			_mod?.Logger.Info("RL client connected.");

			var recvThread = new Thread(ReceiveLoop)
			{
				IsBackground = true
			};
			recvThread.Start();
		}

		private void ReceiveLoop()
		{
			try
			{
				using (var reader = new StreamReader(_stream, Encoding.UTF8))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						try
						{
							var dto = JsonSerializer.Deserialize<ActionDTO>(line);
							if (dto != null)
								latestAction = dto;
						}
						catch (JsonException ex)
						{
							_mod?.Logger.Debug($"TeRL: malformed action JSON: {ex.Message}");
						}
					}
				}
			}
			catch (Exception)
			{
				// Stream closed or error; fall through to CloseClient
			}
			CloseClient();
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

		/// <summary>Accepts connection and sends state once. Call once per tick from PostUpdatePlayers.</summary>
		public void Update(Player player, StateDTO state)
		{
			if (player == null) return;

			try
			{
				AcceptConnection();
				if (_client == null) return;

				SendState(state);
			}
			catch (Exception ex)
			{
				_mod?.Logger.Warn($"TeRL bridge error: {ex.Message}");
				CloseClient();
			}
		}

		private void CloseClient()
		{
			try { _stream?.Close(); } catch { }
			try { _client?.Close(); } catch { }
			_stream = null;
			_client = null;
			_mod?.Logger.Info("RL client disconnected.");
		}
	}
}
