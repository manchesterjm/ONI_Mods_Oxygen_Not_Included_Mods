using System.Collections.Generic;

namespace OniStatsPlus;

public class WorkInfo
{
	public static Dictionary<MinionResume, WorkInfo> WInfo = new Dictionary<MinionResume, WorkInfo>();

	public float Time;

	public float StartExp;

	public Workable Wrk;
}
