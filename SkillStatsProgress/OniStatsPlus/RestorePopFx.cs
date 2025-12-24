using System;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace OniStatsPlus;

[HarmonyPatch(typeof(PopFXManager), "SpawnFX", new Type[]
{
	typeof(Sprite),
	typeof(string),
	typeof(Transform),
	typeof(Vector3),
	typeof(float),
	typeof(bool),
	typeof(bool)
})]
public static class RestorePopFx
{
	public static void Postfix(PopFX __result)
	{
		if ((Object)(object)__result != (Object)null)
		{
			((TMP_Text)__result.TextDisplay).fontSize = 24f;
			KMonoBehaviourExtensions.SetAlpha(__result.IconDisplay, 1f);
		}
	}
}
