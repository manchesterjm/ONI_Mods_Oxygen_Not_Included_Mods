using System;
using System.Collections.Generic;
using OniStatsShow;
using UnityEngine;

namespace OniStatsPlus;

public static class MinionManager
{
	public static Dictionary<MinionIdentity, SimpleRecord> LastValue = new Dictionary<MinionIdentity, SimpleRecord>();

	public static Dictionary<MinionIdentity, SimpleRecord> Change = new Dictionary<MinionIdentity, SimpleRecord>();

	public static Dictionary<MinionIdentity, SimpleRecord> LastUpdChange = new Dictionary<MinionIdentity, SimpleRecord>();

	public static LinkedList<ComplexRecord> L = new LinkedList<ComplexRecord>();

	public static void AddData(MinionIdentity M, SimpleRecord S, int Time)
	{
		SimpleRecord simpleRecord = new SimpleRecord();
		if (LastValue.TryGetValue(M, out var value))
		{
			SimpleRecord simpleRecord2 = Change[M];
			SimpleRecord simpleRecord3 = LastUpdChange[M];
			foreach (DataEnum value3 in Enum.GetValues(typeof(DataEnum)))
			{
				if (value[value3] != S[value3])
				{
					int num = S[value3] - value[value3];
					if (num < 0)
					{
						num = 0;
					}
					simpleRecord2[value3] += num;
					simpleRecord3[value3] += num;
					simpleRecord[value3] = num;
					value[value3] = S[value3];
				}
			}
			ComplexRecord value2 = ComplexRecord.Create(simpleRecord, M, Time);
			L.AddFirst(value2);
		}
		else
		{
			LastValue.Add(M, S);
			Change.Add(M, new SimpleRecord());
			LastUpdChange.Add(M, new SimpleRecord());
		}
	}

	public static SimpleRecord GetLastAttribSum(MinionIdentity M)
	{
		foreach (ComplexRecord item in L)
		{
			if ((Object)(object)M == (Object)(object)item.Minion)
			{
				return item.Delta;
			}
		}
		return SimpleRecord.Empty;
	}

	public static void RemoveDataOlderThen(int Time)
	{
		int num = 0;
		int num2 = 0;
		if (L.Last == null)
		{
			return;
		}
		while (L.Last.Value.Time <= Time)
		{
			num2++;
			ComplexRecord value = L.Last.Value;
			L.RemoveLast();
			SimpleRecord simpleRecord = Change[value.Minion];
			SimpleRecord simpleRecord2 = LastUpdChange[value.Minion];
			foreach (DataEnum value2 in Enum.GetValues(typeof(DataEnum)))
			{
				simpleRecord[value2] -= value.Delta[value2];
				num += value.Delta[value2];
				simpleRecord2[value2] -= value.Delta[value2];
			}
			ComplexRecord.Recycle(value);
		}
	}
}
