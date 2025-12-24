using System.Collections.Generic;
using OniStatsShow;

namespace OniStatsPlus;

public class ComplexRecord
{
	public SimpleRecord Delta;

	public MinionIdentity Minion;

	public int Time;

	private static Stack<ComplexRecord> S = new Stack<ComplexRecord>();

	private ComplexRecord(SimpleRecord D, MinionIdentity M, int T)
	{
		Delta = D;
		Minion = M;
		Time = T;
	}

	public static ComplexRecord Create(SimpleRecord D, MinionIdentity M, int T)
	{
		if (S.Count > 0)
		{
			ComplexRecord complexRecord = S.Pop();
			complexRecord.Delta = D;
			complexRecord.Minion = M;
			complexRecord.Time = T;
			return complexRecord;
		}
		return new ComplexRecord(D, M, T);
	}

	public static void Recycle(ComplexRecord R)
	{
		S.Push(R);
	}
}
