using HarmonyLib;
using Klei.AI;
using OniStatsShow;
using UnityEngine;

namespace OniStatsPlus;

[HarmonyPatch(typeof(Worker), "Work")]
internal class WorkableInfo4
{
	public unsafe static void Postfix(Worker __instance, WorkResult __result)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		if ((int)__result == 1 || (Object)(object)__instance == (Object)null)
		{
			return;
		}
		MinionResume component = ((Component)__instance).GetComponent<MinionResume>();
		if ((Object)(object)component == (Object)null || !WorkInfo.WInfo.TryGetValue(component, out var value) || value == null)
		{
			return;
		}
		string text = ((object)(*(WorkResult*)(&__result))/*cast due to .constrained prefix*/).ToString() + ":\n" + Helper.ShrinkTo(((object)value.Wrk).GetType().Name, 12) + "\nTime:";
		text = text + (GameClock.Instance.GetTime() - value.Time).ToString("F2") + "s\n";
		Attribute workAttribute = value.Wrk.GetWorkAttribute();
		string text2 = null;
		if (((object)value.Wrk).GetType() == typeof(Pickupable))
		{
			text2 = ((Resource)((ModifierSet)Db.Get()).Attributes.Athletics).Id;
		}
		else if (workAttribute != null)
		{
			text2 = ((Resource)workAttribute).Id;
		}
		if (text2 != null)
		{
			AttributeLevels component2 = ((Component)__instance).GetComponent<AttributeLevels>();
			if ((Object)(object)component2 != (Object)null)
			{
				AttributeLevel attributeLevel = component2.GetAttributeLevel(text2);
				if (attributeLevel != null)
				{
					float experience = attributeLevel.experience;
					text = text + Helper.ShrinkTo(((ResourceSet<Attribute>)(object)((ModifierSet)Db.Get()).Attributes).Get(text2).ProfessionName, 3) + ":";
					if (experience >= value.StartExp)
					{
						experience -= value.StartExp;
						text = text + (experience / attributeLevel.GetExperienceForNextLevel() * 100f).ToString("F3") + "%";
					}
					else
					{
						text += "Lvl UP";
					}
				}
			}
		}
		Helper.ShowText(text, ((Component)__instance).gameObject, Config.Cfg.WorkableInfoReport2Color, Config.Cfg.WorkableInfoReport2Speed, Config.Cfg.WorkableInfoReport2Time);
	}
}
