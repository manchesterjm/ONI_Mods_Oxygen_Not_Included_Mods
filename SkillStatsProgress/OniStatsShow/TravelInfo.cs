using System.Collections.Generic;
using UnityEngine;

namespace OniStatsShow;

public class TravelInfo
{
	public static Dictionary<MinionIdentity, TravelInfo> TD = new Dictionary<MinionIdentity, TravelInfo>();

	public int Today = -1;

	public Dictionary<NavType, int> TodayTravelInfo = new Dictionary<NavType, int>();

	public static Dictionary<NavType, int> Temp = new Dictionary<NavType, int>();

	public static string[] NavTypeName = new string[11]
	{
		"Floor", "Left wall", "Right wall", "Ceiling", "Ladder", "Hover", "Swim", "Pole", "Tube", "Solid",
		""
	};

	public static void UpdateTodayTravelInfo(MinionIdentity M)
	{
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		if (!Config.Cfg.ShowTravelPath)
		{
			return;
		}
		if (!TD.TryGetValue(M, out var value))
		{
			value = new TravelInfo();
			TD[M] = value;
		}
		if (GameClock.Instance.GetCycle() == value.Today)
		{
			return;
		}
		value.Today = GameClock.Instance.GetCycle();
		value.TodayTravelInfo.Clear();
		Navigator component = ((Component)M).GetComponent<Navigator>();
		if (!((Object)(object)component != (Object)null))
		{
			return;
		}
		foreach (KeyValuePair<NavType, int> item in component.distanceTravelledByNavType)
		{
			value.TodayTravelInfo.Add(item.Key, item.Value);
		}
	}

	public static void PrintDictionary(Dictionary<NavType, int> Nav)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		if (Nav == null)
		{
			Debug.Log((object)"Dictionary is null.");
			return;
		}
		Debug.Log((object)$"Dictionary count: {Nav.Count}.");
		foreach (KeyValuePair<NavType, int> item in Nav)
		{
			Debug.Log((object)$"Pairs: {item.Key}, {item.Value}.");
		}
	}

	public static TravelInfo GetTodayTravelInfo(MinionIdentity M)
	{
		if (TD.TryGetValue(M, out var value))
		{
			return value;
		}
		return null;
	}

	public static void GetTodayTravelInfo(Dictionary<NavType, int> Source, MinionIdentity M, Dictionary<NavType, int> Dest)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		Dest.Clear();
		TravelInfo todayTravelInfo = GetTodayTravelInfo(M);
		if (Dest == null || todayTravelInfo == null || todayTravelInfo.Today != GameClock.Instance.GetCycle())
		{
			return;
		}
		foreach (KeyValuePair<NavType, int> item in Source)
		{
			todayTravelInfo.TodayTravelInfo.TryGetValue(item.Key, out var value);
			value = item.Value - value;
			Dest[item.Key] = value;
		}
	}

	internal static void ShowTravelInfo(DetailsPanelDrawer a, Dictionary<NavType, int> tempTI, string v)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		foreach (KeyValuePair<NavType, int> item in tempTI)
		{
			num += item.Value;
		}
		a.NewLabel(string.Format(v + " (<b>{0,8:N3}</b> Km)", (float)num / 1000f));
		foreach (KeyValuePair<NavType, int> item2 in tempTI)
		{
			if (item2.Value != 0)
			{
				a.NewLabel($"{NavTypeName[item2.Key].ToString(),15}: <b>{(float)item2.Value / 1000f,8:N3}</b> Km.");
			}
		}
	}

	internal static void GetTotal(Dictionary<NavType, int> tempTI, bool GetTotalTraveledInfo = false)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		if (tempTI == null)
		{
			return;
		}
		tempTI.Clear();
		foreach (MinionIdentity minionIdentity in Components.MinionIdentities)
		{
			MinionIdentity val = minionIdentity;
			Navigator component = ((Component)val).GetComponent<Navigator>();
			if (!((Object)(object)component != (Object)null))
			{
				continue;
			}
			if (GetTotalTraveledInfo)
			{
				foreach (KeyValuePair<NavType, int> item in component.distanceTravelledByNavType)
				{
					if (tempTI.TryGetValue(item.Key, out var value))
					{
						tempTI[item.Key] = value + item.Value;
					}
					else
					{
						tempTI[item.Key] = item.Value;
					}
				}
				continue;
			}
			Temp.Clear();
			UpdateTodayTravelInfo(val);
			GetTodayTravelInfo(component.distanceTravelledByNavType, val, Temp);
			if (!TD.TryGetValue(val, out var value2))
			{
				continue;
			}
			Dictionary<NavType, int> todayTravelInfo = value2.TodayTravelInfo;
			foreach (KeyValuePair<NavType, int> item2 in todayTravelInfo)
			{
				if (component.distanceTravelledByNavType.TryGetValue(item2.Key, out var value3))
				{
					value3 -= item2.Value;
					if (tempTI.TryGetValue(item2.Key, out var value4))
					{
						tempTI[item2.Key] = value4 + value3;
					}
					else
					{
						tempTI[item2.Key] = value3;
					}
				}
			}
		}
	}
}
