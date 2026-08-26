using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectDelta.Data
{
    // Progress for the single active run. Deleted on run end (기획서 9.1).
    [Serializable]
    public sealed class RunData
    {
        public RunBasicInfo BasicInfo =
            new RunBasicInfo();

        public PlayerRunStats PlayerStats =
            new PlayerRunStats();

        public RunInventory Inventory =
            new RunInventory();

        public DungeonRunState DungeonState =
            new DungeonRunState();

        public BattleEncounterCheckpointData BattleEncounterCheckpoint =
            new BattleEncounterCheckpointData();

        public List<CharacterRunState> CharacterStates =
            new List<CharacterRunState>();
    }

    [Serializable]
    public sealed class RunBasicInfo
    {
        public string RunId;
        public string GameMode;
        public string Difficulty;
        public string PlayerName;
        public string StartedAtIso8601;
        public float PlaytimeSeconds;
        public string CurrentSceneName;
        public int CurrentFloor;
        public string CurrentFloorThemeId;
        public Vector2Int CurrentRoomCoordinate;
        public int FacingDirection;
        public int DungeonSeed;

        public string CurrentRoomId;
        public Vector2Int CurrentGridPositionInRoom;
    }

    [Serializable]
    public sealed class PlayerRunStats
    {
        public int Level;
        public int Experience;
        public int UnspentStatPoints;
        public int SpentStatPoints;

        public int MaxHealth;
        public int CurrentHealth;
        public int MaxMana;
        public int CurrentMana;

        // 83일차: 도주 후 현재 정력까지 그대로 이어가기 위한 저장 필드.
        public int MaxStamina;
        public int CurrentStamina;

        public int MaxArousal;
        public int CurrentArousal;
        public int Attack;
        public int Defense;
        public int Speed;
        public int Charm;
        public int Evasion;
        public int Resistance;

        public List<string> RunStatModifierIds =
            new List<string>();

        public List<string> ActiveStatusEffectIds =
            new List<string>();

        // 83일차: 상태 ID뿐 아니라 남은 지속·중첩·수치까지 저장한다.
        public List<PlayerStatusEffectRunState> ActiveStatusEffects =
            new List<PlayerStatusEffectRunState>();

        public int Gold;

        public List<string> AcquiredRunSkillIds =
            new List<string>();

        public List<string> EquippedSkillIds =
            new List<string>();

        public List<string> StartingSkillIds =
            new List<string>();
    }

    [Serializable]
    public sealed class PlayerStatusEffectRunState
    {
        public string DefinitionId;
        public string SourceInstanceId;
        public int RemainingDuration;
        public int StackCount;
        public int AppliedValue;
        public int EffectKind;
        public int TargetStat;
    }

    [Serializable]
    public sealed class RunInventory
    {
        public List<string> InventoryItemIds =
            new List<string>();

        public List<string> ConsumableStackIds =
            new List<string>();

        public List<string> ExplorationToolIds =
            new List<string>();

        public List<string> KeyItemIds =
            new List<string>();

        public List<string> TreasureIds =
            new List<string>();

        public List<string> EquippedItemIds =
            new List<string>();

        public List<string> EquipmentRolledOptionIds =
            new List<string>();

        public List<string> BagIds =
            new List<string>();

        public List<string> RelicIds =
            new List<string>();

        public List<string> CursedItemIds =
            new List<string>();
    }

    [Serializable]
    public sealed class DungeonRunState
    {
        public List<string> FloorThemeIds =
            new List<string>();

        public List<RoomRunState> Rooms =
            new List<RoomRunState>();

        // 39일차: 현재 층의 확정된 생성 그래프와 Seed 재현 정보.
        public ProjectDelta.Domain.DungeonLayoutSnapshot LayoutSnapshot;

        // 37일차 Fog of War에서 한 번이라도 밝혀진 방 목록.
        public List<string> RevealedRoomIds =
            new List<string>();
    }

    [Serializable]
    public sealed class RoomRunState
    {
        public string RoomId;
        public Vector2Int Coordinate;

        public List<int> ConnectedDirections =
            new List<int>();

        public bool Visited;
        public bool Discovered;
        public bool Completed;
        public bool SecretFound;
        public bool TrapTriggered;
        public bool ChestOpened;
        public bool IsStairs;
        public bool StairsDiscovered;
        public bool RestRoomUsed;
    }

    [Serializable]
    public sealed class CharacterRunState
    {
        public string InstanceId;
        public string DefinitionId;
        public int CurrentHealth;
        public int CurrentArousal;
        public int RunAffinity;
        public bool IsRelationshipTarget;
        public Vector2Int SpawnCoordinate;
        public bool IsAlive;
        public bool HasRetreatedFromBattle;

        public bool IsHostile;
        public bool IsUsingService;
    }
}
