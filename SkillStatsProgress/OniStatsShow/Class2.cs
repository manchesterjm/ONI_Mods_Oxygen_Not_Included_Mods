using HarmonyLib;
using OniStatsPlus;
using UnityEngine;

namespace OniStatsShow;

[HarmonyPatch(typeof(GameClock), "Render1000ms")]
public static class Class2
{
	public static float d;

	public static int Time;

	public static void Postfix(float dt)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Expected O, but got Unknown
		if (!Config.Cfg.EnableComplexFeature || SpeedControlScreen.Instance.IsPaused)
		{
			return;
		}
		d += dt * Time.timeScale;
		if (!(d >= (float)Config.Cfg.GetEveryXSecond))
		{
			return;
		}
		Time++;
		d -= Config.Cfg.GetEveryXSecond;
		foreach (MinionIdentity liveMinionIdentity in Components.LiveMinionIdentities)
		{
			MinionIdentity val = liveMinionIdentity;
			if (MinionManager.LastUpdChange.TryGetValue(val, out var value))
			{
				value.ClearValue();
			}
			MinionManager.AddData(val, new SimpleRecord(val), Time);
			TravelInfo.UpdateTodayTravelInfo(val);
		}
		int num = Config.Cfg.IntervalSecond / Config.Cfg.GetEveryXSecond;
		int time = Time - num;
		MinionManager.RemoveDataOlderThen(time);
	}
}
