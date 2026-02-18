using Terraria;
using Terraria.ModLoader;

namespace TeRL
{
	public class RLSystem : ModSystem
	{
		public override void PostUpdateEverything()
		{
			if (Main.gameMenu) return;

			BridgeServer.Instance.Init(Mod);
			BridgeServer.Instance.Update(Main.LocalPlayer);
		}
	}
}
