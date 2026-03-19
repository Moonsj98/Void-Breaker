using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Stage Data", fileName = "StageData_")]
public class StageData : ScriptableObject
{
    public int stageId = 1;
    public List<MiniStageData> miniStages = new List<MiniStageData>();
}

[System.Serializable]
public class MiniStageData
{
    [Header("Generation")]
    public bool useFixedPattern;
    [Min(1)] public int supportRowCount = 6;
    public bool spawnBuffCard = true;
    public bool spawnInfiniteBlock = true;
    [Min(0)] public int infiniteBlockCount = 1;

    [Header("Fixed Pattern")]
    public List<StageBlockData> fixedBlocks = new List<StageBlockData>();

    [Header("Gimmicks")]
    public List<StageGimmickData> gimmicks = new List<StageGimmickData>();
}

[System.Serializable]
public class StageBlockData
{
    [Min(0)] public int row;
    [Range(0, 7)] public int col;
    public Block.BlockType blockType = Block.BlockType.Normal;
    [Min(0)] public int hp = 1;

    [Header("Debuff Block Only")]
    public StatType debuffStatType = StatType.Cost;
    [Min(1)] public int debuffAmount = 1;
}

public enum StageGimmickTriggerType
{
    OnStageStart,
    OnTurnStart
}

public enum StatType
{
    Cost,
    AttackPower,
    RemainShotCount
}

[System.Serializable]
public class StageGimmickData
{
    public StageGimmickTriggerType triggerType = StageGimmickTriggerType.OnStageStart;
    public StatType statType = StatType.Cost;
    [Min(1)] public int amount = 1;
}
