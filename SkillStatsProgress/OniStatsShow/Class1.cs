using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Klei.AI;
using OniStatsPlus;
using STRINGS;
using TMPro;
using UnityEngine;

namespace OniStatsShow;

[HarmonyPatch(typeof(MinionStatsPanel), "RefreshAttributes")]
public static class Class1
{
	[HarmonyPatch(typeof(DetailsScreen), "Refresh")]
	public class Class5
	{
		public static void Prefix(GameObject go)
		{
			if (Config.Cfg.AlterSortOrder)
			{
				int value = Traverse.Create((object)DetailsScreen.Instance).Field("previouslyActiveTab").GetValue<int>();
				if ((Object)(object)go.GetComponent<MinionIdentity>() != (Object)null && value < 0)
				{
					Traverse.Create((object)DetailsScreen.Instance).Field("previouslyActiveTab").SetValue((object)2);
				}
			}
		}
	}

	private static Dictionary<string, float> OldValue = new Dictionary<string, float>();

	public static Dictionary<NavType, int> TempTI = new Dictionary<NavType, int>();

	public static Dictionary<NavType, int> x = new Dictionary<NavType, int>();

	public static GameObject LastTarget;

	public static float OldTime = 0f;

	public static float OldRad = 0f;

	public static string LastChange;

	public static bool Prefix(ref MinionStatsPanel __instance)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Expected O, but got Unknown
		//IL_0821: Unknown result type (might be due to invalid IL or missing references)
		//IL_0826: Unknown result type (might be due to invalid IL or missing references)
		//IL_0828: Unknown result type (might be due to invalid IL or missing references)
		string text = "";
		bool flag = false;
		Type type = ((object)__instance).GetType();
		FieldInfo field = type.GetField("selectedTarget", BindingFlags.Instance | BindingFlags.NonPublic);
		GameObject val = (GameObject)field.GetValue(__instance);
		MinionIdentity component = val.GetComponent<MinionIdentity>();
		field = type.GetField("attributesPanel", BindingFlags.Instance | BindingFlags.NonPublic);
		GameObject val2 = (GameObject)field.GetValue(__instance);
		if (!Object.op_Implicit((Object)(object)val))
		{
			val2.SetActive(false);
		}
		else
		{
			val2.SetActive(true);
			((TMP_Text)val2.GetComponent<CollapsibleDetailContentPanel>().HeaderLabel).text = LocString.op_Implicit(STATS.GROUPNAME_ATTRIBUTES);
			List<AttributeInstance> list = new List<AttributeInstance>(ModifiersExtensions.GetAttributes(val).AttributeTable);
			MinionResume component2 = val.GetComponent<MinionResume>();
			MinionIdentity component3 = val.GetComponent<MinionIdentity>();
			int num = (int)MinionResume.CalculatePreviousExperienceBar(component2.TotalSkillPointsGained);
			int num2 = (int)MinionResume.CalculateNextExperienceBar(component2.TotalSkillPointsGained);
			int num3 = (int)component2.TotalExperienceGained - num;
			int num4 = num2 - num;
			float num5 = 100f * (float)num3 / (float)num4;
			string text2 = string.Format("Exp: <b>{0}</b>{1} <b>{2:F2}%</b> SP:{3}/{4}  ", num3, Config.Cfg.ShowMaxExpForSkill ? ("/" + num4) : "", num5, component2.AvailableSkillpoints, component2.TotalSkillPointsGained);
			text = "";
			if (Config.Cfg.EnableComplexFeature && (Object)(object)component3 != (Object)null)
			{
				if (MinionManager.LastUpdChange.TryGetValue(component3, out var value))
				{
					int tempi = value[DataEnum.Skillexp];
					text = Print(tempi);
				}
				if (MinionManager.Change.TryGetValue(component3, out var value2))
				{
					text = "  D: (" + text + value2[DataEnum.Skillexp] + ")";
				}
			}
			AttributeLevels component4 = val.GetComponent<AttributeLevels>();
			List<AttributeInstance> list2 = list.FindAll((AttributeInstance a) => (int)a.Attribute.ShowInUI == 1);
			AttributeInstance val3 = list.Find((AttributeInstance a) => a.Id == ((Resource)((ModifierSet)Db.Get()).Attributes.SpaceNavigation).Id);
			if (val3 != null)
			{
				list2.Add(val3);
			}
			field = type.GetField("attributesDrawer", BindingFlags.Instance | BindingFlags.NonPublic);
			DetailsPanelDrawer val4 = (DetailsPanelDrawer)field.GetValue(__instance);
			val4.BeginDrawing();
			val4.NewLabel(text2 + text + "\n").Tooltip(string.Format("Skillpoint expirience and avaible/max skillpoints." + UI.HORIZONTAL_BR_RULE + "Exp needed: <b>{0}</b>", num4 - num3));
			if (Config.Cfg.EnableComplexFeature && Config.Cfg.EnableAdditionalInfo)
			{
				text2 = $"T: {Class2.Time}/{Class2.d}  N1: {MinionManager.L.Count} N2: {MinionManager.Change.Count}\n\n";
				val4.NewLabel(text2).Tooltip("T: Current and subcurrent time. \n\n N1: Linked list count.\n N2: Dictionary count.");
			}
			if (Config.Cfg.ShowRadiationInfo && DlcManager.IsExpansion1Active())
			{
				val4.NewLabel(GetRadiationInfo(component2, component3));
			}
			if (list2.Count > 0)
			{
				foreach (AttributeInstance item in list2)
				{
					text2 = item.Id;
					string s = text2;
					AttributeLevel attributeLevel = component4.GetAttributeLevel(text2);
					flag = false;
					float num6 = 0f;
					string text3;
					if (attributeLevel != null)
					{
						float experience = attributeLevel.experience;
						if (OldValue.TryGetValue(text2, out var value3))
						{
							if (value3 != experience)
							{
								flag = true;
								num6 = experience - value3;
								if (!SpeedControlScreen.Instance.IsPaused)
								{
									OldValue[text2] = experience;
								}
							}
						}
						else
						{
							flag = true;
							OldValue.Add(text2, experience);
						}
						flag = flag && Config.Cfg.EnabledFirstFeature;
						if (!flag)
						{
							text2 = string.Format("/{0} <b>{1:F0}</b>{2:F0} <b>{3:F2}</b>%", item.GetFormattedValue(), Config.Cfg.ShowRequiredXp ? (attributeLevel.GetExperienceForNextLevel() - attributeLevel.experience) : attributeLevel.experience, Config.Cfg.ShowMaxExpForStats ? ("/" + attributeLevel.GetExperienceForNextLevel()) : "", attributeLevel.GetPercentComplete() * 100f);
							text3 = $"Exp needed: <b>{attributeLevel.GetExperienceForNextLevel() - attributeLevel.experience:F2}</b>";
						}
						else
						{
							text2 = string.Format("/{0} {1:F0}{2:F0} {3:F2}%", item.GetFormattedValue(), Config.Cfg.ShowRequiredXp ? (attributeLevel.GetExperienceForNextLevel() - attributeLevel.experience) : attributeLevel.experience, Config.Cfg.ShowMaxExpForStats ? ("/" + attributeLevel.GetExperienceForNextLevel()) : "", attributeLevel.GetPercentComplete() * 100f);
							text3 = $"Exp needed: {attributeLevel.GetExperienceForNextLevel() - attributeLevel.experience:F2}";
						}
					}
					else
					{
						text2 = (text3 = "");
					}
					string text4 = ((Config.Cfg.ShrinkStatNameToXchar <= 0) ? item.Name : item.Name.Substring(0, (item.Name.Length > Config.Cfg.ShrinkStatNameToXchar) ? Config.Cfg.ShrinkStatNameToXchar : item.Name.Length));
					text = "";
					if (Config.Cfg.EnableComplexFeature && (Object)(object)component3 != (Object)null)
					{
						string text5 = MinionManager.GetLastAttribSum(component3)[s].ToString();
						string text6;
						if (MinionManager.LastUpdChange.TryGetValue(component3, out var value4))
						{
							int tempi2 = value4[s];
							text6 = Print(tempi2);
						}
						else
						{
							text6 = "0";
						}
						if (MinionManager.Change.TryGetValue(component3, out var value5))
						{
							text = "  (" + text5 + "/" + text6 + value5[s] + ")";
						}
					}
					if (!flag)
					{
						val4.NewLabel($"  {text4} {attributeLevel.GetLevel()} {text2} <b>{text}</b>").Tooltip(item.GetAttributeValueTooltip() + UI.HORIZONTAL_BR_RULE + text3);
					}
					else
					{
						val4.NewLabel($"=><b>{text4} {attributeLevel.GetLevel()} {text2} {text}</b>").Tooltip(item.GetAttributeValueTooltip() + UI.HORIZONTAL_BR_RULE + text3);
					}
				}
			}
			if (Config.Cfg.ShowActualSpeed)
			{
				Navigator component5 = ((Component)component3).GetComponent<Navigator>();
				if ((Object)(object)component5 != (Object)null)
				{
					text = "Speed: <b>";
					if (component5.transitionDriver != null && component5.transitionDriver.GetTransition != null)
					{
						Vector3 position = TransformExtensions.GetPosition(((Component)component3).GetComponent<Transform>());
						int num7 = default(int);
						int num8 = default(int);
						Grid.PosToXY(position, ref num7, ref num8);
						text = text + (((double)component5.transitionDriver.GetTransition.speed != 1.0 && component5.IsMoving()) ? $"{component5.transitionDriver.GetTransition.speed:f3} " : "0.000") + $"</b> x:<b>{num7}</b> y:<b> {num8}</b> Cell: <b>{Grid.PosToCell(((Component)component5).gameObject)}</b>\n";
						if (Config.Cfg.AvgSpeedInterval >= 0f)
						{
							text += AvgSpeed.GetAvgSpeed(component5);
						}
					}
					val4.NewLabel(text).Tooltip("Speed info.");
				}
			}
			if (Config.Cfg.ShowTravelPath)
			{
				Navigator component6 = ((Component)component3).GetComponent<Navigator>();
				if ((Object)(object)component6 != (Object)null)
				{
					TravelInfo.GetTodayTravelInfo(component6.distanceTravelledByNavType, component3, TempTI);
					TravelInfo.ShowTravelInfo(val4, TempTI, "Distance traveled today: ");
					TravelInfo.ShowTravelInfo(val4, component6.distanceTravelledByNavType, "Total traveled distance: ");
					TravelInfo.GetTotal(x);
					TravelInfo.ShowTravelInfo(val4, x, "Total Distance today: ");
					TravelInfo.GetTotal(x, GetTotalTraveledInfo: true);
					TravelInfo.ShowTravelInfo(val4, x, "Total Distance: ");
				}
			}
			val4.EndDrawing();
		}
		return false;
	}

	private static string GetRadiationInfo(MinionResume mr, MinionIdentity m)
	{
		string text = "Radiation info:\n";
		Amounts amounts = ModifiersExtensions.GetAmounts(((Component)mr).gameObject);
		float value = amounts.GetValue(((Resource)((ModifierSet)Db.Get()).Amounts.RadiationBalance).Id);
		int num = Grid.PosToCell(((Component)mr).gameObject);
		float num2 = ((!Grid.IsValidCell(num)) ? 0f : ((RadiationIndexer)(ref Grid.Radiation))[num]);
		float totalValue = ((ModifierSet)Db.Get()).Attributes.RadiationRecovery.Lookup((Component)(object)mr).GetTotalValue();
		float totalValue2 = ((ModifierSet)Db.Get()).Attributes.RadiationResistance.Lookup((Component)(object)mr).GetTotalValue();
		float time = GameClock.Instance.GetTime();
		float num3 = time - OldTime;
		string text2 = (LastChange = ((OldTime != 0f && num3 != 0f) ? ((value - OldRad) / num3 * 600f).ToString("F2") : ((LastChange == null) ? "??" : LastChange)));
		OldRad = value;
		OldTime = time;
		text = "Rad: <b>" + value.ToString("F0") + "</b>  Ch:<b>" + text2 + "</b>/cycle Rec: <b>" + (totalValue * 600f).ToString("F0") + "</b>\n";
		return text + "Cur.Exp.: <b>" + num2.ToString("F0") + "</b>/<b>" + (num2 * (1f - totalValue2)).ToString("F0") + "</b> Res:<b>" + (totalValue2 * 100f).ToString("F0") + " </b>%\n\n";
	}

	private static string Print(int tempi)
	{
		if (tempi == 0)
		{
			return "0/";
		}
		return ((tempi >= 0) ? '+' : '-').ToString() + tempi + "/";
	}
}
