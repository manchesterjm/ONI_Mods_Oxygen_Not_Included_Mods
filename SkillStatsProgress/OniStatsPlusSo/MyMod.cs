using HarmonyLib;
using KMod;
using OniStatsShow;

namespace OniStatsPlusSo;

internal class MyMod : UserMod2
{
	private static bool Init;

	public override void OnLoad(Harmony harmony)
	{
		((UserMod2)this).OnLoad(harmony);
		if (!Init)
		{
			Init = true;
			Debug.Log((object)("Mod:Stats Info OnLoad::Version:" + Config.Cfg.Ver + $" @Build: {typeof(MyMod).Assembly.GetName().Version})"));
		}
	}
}
