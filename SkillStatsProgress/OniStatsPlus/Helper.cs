using HarmonyLib;
using OniStatsShow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OniStatsPlus;

public static class Helper
{
	public static string ShrinkTo(string S, int MaxLength)
	{
		if (S.Length <= MaxLength)
		{
			return S;
		}
		return S.Substring(0, MaxLength);
	}

	public static bool CanShowInfo(GameObject G)
	{
		if (!Config.Cfg.ShowWorkableInfo)
		{
			return false;
		}
		if (Config.Cfg.ShowWorkableOnlyForSelectedDuplicant)
		{
			return IsSelected(G);
		}
		return true;
	}

	public static bool IsSelected(GameObject G)
	{
		if ((Object)(object)DetailsScreen.Instance == (Object)null || KMonoBehaviour.isLoadingScene || !((KScreen)DetailsScreen.Instance).IsActive())
		{
			return false;
		}
		if ((Object)(object)DetailsScreen.Instance.target == (Object)(object)G)
		{
			return true;
		}
		return false;
	}

	public static void ShowText(string Txt, GameObject G, Color C, float Speed, float Time)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		if (CanShowInfo(G))
		{
			PopFX val = PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Plus, Txt, G.transform, Time, false);
			RectTransform component = ((Component)val).GetComponent<RectTransform>();
			component.SetSizeWithCurrentAnchors((Axis)0, 250f);
			if ((Object)(object)val != (Object)null)
			{
				((Graphic)val.TextDisplay).color = C;
				Traverse.Create((object)val).Field("Speed").SetValue((object)Speed);
				Traverse.Create((object)val).Field("offset").SetValue((object)new Vector3(1f, 3.5f));
				((TMP_Text)val.TextDisplay).fontSize = Config.Cfg.WorkableReportFontSize;
				KMonoBehaviourExtensions.SetAlpha(val.IconDisplay, 0f);
			}
		}
	}
}
