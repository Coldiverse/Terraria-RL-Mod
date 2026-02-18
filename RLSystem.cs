using Terraria;
using Terraria.ModLoader;

namespace TeRL
{
	public override void PostUpdateEverything()
		{
			if (Main.gameMenu) return;

			Mod.Logger.Info("RLSystem running");

			BridgeServer.Instance.Init(Mod);
			BridgeServer.Instance.Update(Main.LocalPlayer);
		}
}
