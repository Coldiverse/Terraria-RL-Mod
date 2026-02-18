using Terraria;
using Terraria.ModLoader;

namespace TeRL
{
	public class RLSystem : ModSystem
	{
		public override void PostUpdateEverything()
		{
			if (Main.gameMenu) return;

			Player player = Main.LocalPlayer;
			if (player == null) return;

			BridgeServer.Update(player);
		}
	}
}
