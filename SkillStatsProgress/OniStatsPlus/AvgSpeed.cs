using System.Collections.Generic;
using OniStatsShow;
using UnityEngine;

namespace OniStatsPlus;

public class AvgSpeed
{
	public static Navigator LastNavigator;

	public static LinkedList<AvgSpeedInfo> L = new LinkedList<AvgSpeedInfo>();

	public static Stack<LinkedListNode<AvgSpeedInfo>> Pool = new Stack<LinkedListNode<AvgSpeedInfo>>();

	public static string GetAvgSpeed(Navigator Nav)
	{
		if ((Object)(object)LastNavigator != (Object)(object)Nav)
		{
			while (L.Last != null)
			{
				LinkedListNode<AvgSpeedInfo> last = L.Last;
				L.RemoveLast();
				RecycleNode(last);
			}
		}
		LastNavigator = Nav;
		int totalDistance = GetTotalDistance(Nav);
		float TimeInterval;
		int D;
		float andSet = GetAndSet(totalDistance, out TimeInterval, out D);
		string text = (Config.Cfg.DebugInfo ? $"List:{L.Count}, Pool: {Pool.Count}.\n" : "");
		return text + $"Avg.Speed:<b>{andSet:f3}</b> tile/s Dist:<b>{D}</b> Last <b>{TimeInterval:f0}</b> s.";
	}

	private static float GetAndSet(int totalDistance, out float TimeInterval, out int D)
	{
		float time = GameClock.Instance.GetTime();
		if (L.First == null || L.First.Value.Time != time)
		{
			L.AddFirst(GetNode(new AvgSpeedInfo(time, totalDistance)));
		}
		float num = 0f;
		int num2 = 0;
		LinkedListNode<AvgSpeedInfo> linkedListNode = L.First;
		float time2 = linkedListNode.Value.Time;
		int distance = linkedListNode.Value.Distance;
		while (linkedListNode != null)
		{
			if (time2 - linkedListNode.Value.Time > Config.Cfg.AvgSpeedInterval + 1f)
			{
				RemoveFrom(linkedListNode);
				break;
			}
			num = time2 - linkedListNode.Value.Time;
			num2 = distance - linkedListNode.Value.Distance;
			linkedListNode = linkedListNode.Next;
		}
		TimeInterval = num;
		D = num2;
		return (num == 0f) ? 0f : ((float)num2 / num);
	}

	private static void RemoveFrom(LinkedListNode<AvgSpeedInfo> i)
	{
		LinkedListNode<AvgSpeedInfo> linkedListNode = L.First;
		while (linkedListNode != null && linkedListNode != i)
		{
			linkedListNode = linkedListNode.Next;
		}
		if (linkedListNode == null)
		{
			Debug.Log((object)"OniStatsShow: Can not find LinkListNode in RemoveFrom!");
			return;
		}
		bool flag = false;
		while (!flag)
		{
			LinkedListNode<AvgSpeedInfo> last = L.Last;
			if (last == linkedListNode)
			{
				flag = true;
			}
			L.RemoveLast();
			RecycleNode(last);
		}
	}

	private static void RecycleNode(LinkedListNode<AvgSpeedInfo> R)
	{
		Pool.Push(R);
	}

	private static LinkedListNode<AvgSpeedInfo> GetNode(AvgSpeedInfo avgSpeedInfo)
	{
		if (Pool.Count > 0)
		{
			LinkedListNode<AvgSpeedInfo> linkedListNode = Pool.Pop();
			linkedListNode.Value = avgSpeedInfo;
			return linkedListNode;
		}
		return new LinkedListNode<AvgSpeedInfo>(avgSpeedInfo);
	}

	private static int GetTotalDistance(Navigator Nav)
	{
		int num = 0;
		foreach (KeyValuePair<NavType, int> item in Nav.distanceTravelledByNavType)
		{
			num += item.Value;
		}
		return num;
	}
}
