using System.Net.Sockets;
using Terraria.ModLoader;

namespace TeRL
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class TeRL : Mod
	{
		public override void Load()
		{
			BridgeServer.Instance.Init(this);
			try
			{
				BridgeServer.Instance.StartListening();
			}
			catch (SocketException ex)
			{
				Logger.Warn($"TeRL could not start bridge (e.g. port in use): {ex.Message}");
			}
		}
	}
}
