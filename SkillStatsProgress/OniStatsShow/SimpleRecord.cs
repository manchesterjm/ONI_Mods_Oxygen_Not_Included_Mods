using System;
using System.Text;
using Klei.AI;
using UnityEngine;

namespace OniStatsShow;

public class SimpleRecord
{
	private int[] data = new int[12];

	public static SimpleRecord Empty = new SimpleRecord();

	public int this[DataEnum D]
	{
		get
		{
			return data[(int)D];
		}
		set
		{
			data[(int)D] = value;
		}
	}

	public int this[string S]
	{
		get
		{
			return this[Helper.ConvertStringToEnum(S)];
		}
		set
		{
			this[Helper.ConvertStringToEnum(S)] = value;
		}
	}

	public SimpleRecord()
	{
	}

	public SimpleRecord(int SkillExp, int Constr, int Digging, int Tinkering, int Athletics, int Learning, int Cooking, int Creativity, int Strength, int Kindness, int Farming, int Ranching)
	{
		SetValue(SkillExp, Constr, Digging, Tinkering, Athletics, Learning, Cooking, Creativity, Strength, Kindness, Farming, Ranching);
	}

	public SimpleRecord(MinionIdentity M)
	{
		SetValue(M);
	}

	public void SetValue(MinionIdentity m)
	{
		MinionResume component = ((Component)m).GetComponent<MinionResume>();
		int num = (int)MinionResume.CalculatePreviousExperienceBar(component.TotalSkillPointsGained);
		int num2 = (int)MinionResume.CalculateNextExperienceBar(component.TotalSkillPointsGained);
		int value = (int)component.TotalExperienceGained - num;
		this[DataEnum.Skillexp] = value;
		AttributeLevels component2 = ((Component)m).GetComponent<AttributeLevels>();
		foreach (DataEnum value2 in Enum.GetValues(typeof(DataEnum)))
		{
			if (value2 != DataEnum.Skillexp)
			{
				this[value2] = (int)component2.GetAttributeLevel(Helper.ConvertEnumToString(value2)).experience;
			}
		}
	}

	public void SetValue(int skillExp, int constr, int digging, int tinkering, int athletics, int learning, int cooking, int creativity, int strength, int kindness, int farming, int ranching)
	{
		this[DataEnum.Skillexp] = skillExp;
		this[DataEnum.Construction] = constr;
		this[DataEnum.Digging] = digging;
		this[DataEnum.Tinkering] = tinkering;
		this[DataEnum.Athletics] = athletics;
		this[DataEnum.Learning] = learning;
		this[DataEnum.Cooking] = cooking;
		this[DataEnum.Creativity] = creativity;
		this[DataEnum.Strength] = strength;
		this[DataEnum.Kindness] = kindness;
		this[DataEnum.Farming] = farming;
		this[DataEnum.Ranching] = ranching;
	}

	public void ClearValue()
	{
		for (int i = 0; i < data.Length; i++)
		{
			data[i] = 0;
		}
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append($"SkillExp: {this[DataEnum.Skillexp]}, ");
		foreach (DataEnum value in Enum.GetValues(typeof(DataEnum)))
		{
			if (value != DataEnum.Skillexp)
			{
				stringBuilder.Append(Helper.ConvertEnumToString(value) + " " + this[value] + " ");
			}
		}
		return stringBuilder.ToString();
	}
}
