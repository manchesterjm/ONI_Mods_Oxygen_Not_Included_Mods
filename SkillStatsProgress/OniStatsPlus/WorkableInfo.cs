using HarmonyLib;
using Klei.AI;
using OniStatsShow;
using UnityEngine;

namespace OniStatsPlus;

[HarmonyPatch(typeof(Workable), "StartWork")]
internal class WorkableInfo
{
	public static string GetBool(bool b)
	{
		if (b)
		{
			return "*";
		}
		return "-";
	}

	public static void Postfix(Workable __instance)
	{
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)__instance.worker == (Object)null)
		{
			return;
		}
		float num = (__instance.GetEfficiencyMultiplier(__instance.worker) - 1f) * 100f;
		bool value = Traverse.Create((object)__instance).Field("lightEfficiencyBonus").GetValue<bool>();
		string text = Helper.ShrinkTo(((object)__instance).GetType().Name, 12) + ":\nEff:" + ((num < 0f) ? "" : "+") + num.ToString("F0") + "% " + GetBool(__instance.currentlyLit) + "/" + GetBool(value) + "\n";
		Attribute workAttribute = __instance.GetWorkAttribute();
		string AtId = null;
		if (((object)__instance).GetType() == typeof(Pickupable))
		{
			AtId = ((Resource)((ModifierSet)Db.Get()).Attributes.Athletics).Id;
		}
		else if (workAttribute != null)
		{
			AtId = ((Resource)workAttribute).Id;
		}
		AttributeLevels val = null;
		MinionResume val2 = (((Object)(object)__instance.worker != (Object)null) ? ((Component)__instance.worker).GetComponent<MinionResume>() : null);
		if ((Object)(object)val2 == (Object)null)
		{
			return;
		}
		if (AtId != null)
		{
			text = text + ((Resource)((ResourceSet<Attribute>)(object)((ModifierSet)Db.Get()).Attributes).Get(AtId)).Name + ":";
			val = ((Component)__instance.worker).GetComponent<AttributeLevels>();
			if ((Object)(object)val != (Object)null)
			{
				AttributeInstance val3 = ModifiersExtensions.GetAttributes((KMonoBehaviour)(object)val2).AttributeTable.Find((AttributeInstance ins) => ins.Id == AtId);
				if (val3 != null)
				{
					AttributeLevel attributeLevel = val.GetAttributeLevel(AtId);
					text = text + ((attributeLevel != null) ? attributeLevel.level.ToString() : "??") + "/" + val3.GetTotalValue();
				}
			}
		}
		if (!WorkInfo.WInfo.TryGetValue(val2, out var value2))
		{
			value2 = new WorkInfo();
		}
		value2.Time = GameClock.Instance.GetTime();
		value2.StartExp = (((Object)(object)val == (Object)null) ? 0f : ((val.GetAttributeLevel(AtId) != null) ? val.GetAttributeLevel(AtId).experience : 0f));
		value2.Wrk = __instance;
		WorkInfo.WInfo[val2] = value2;
		if (!Config.Cfg.WorkableShowOnlyResultReport)
		{
			Helper.ShowText(text, ((Component)__instance.worker).gameObject, Config.Cfg.WorkableInfoReport1Color, Config.Cfg.WorkableInfoReport1Speed, Config.Cfg.WorkableInfoReport1Time);
		}
	}
}
