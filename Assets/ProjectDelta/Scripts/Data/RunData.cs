using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectDelta.Data
{
    // 단일 활성 회차의 저장 데이터다. 회차 종료 시 제거된다.
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

        // 109일차: 발생·확정된 이벤트 플래그 목록(EventRunState.Flags 그대로).
        public List<string> EventFlags =
            new List<string>();
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

        // 도주 후 현재 정력까지 그대로 이어가기 위한 저장 필드다.
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

    // 89일차: 저장 파일에서도 빈칸 위치를 포함한 슬롯 순서를 그대로 보존한다.
    [Serializable]
    public sealed class RunInventorySlotData
    {
        public string ItemId;
        public string DisplayName;
        public int Quantity;

        // 95일차: HUD가 초기화되기 전 이어하기에서도 Stack 수량이 잘리지 않도록 저장한다.
        // 0은 95일차 이전 저장 데이터로 간주한다.
        public int MaxStackSize;
    }

    [Serializable]
    public sealed class RunInventory
    {
        // 89일차: 앞으로 실제 보유 아이템의 저장 기준이 되는 슬롯 목록이다.
        public List<RunInventorySlotData> Slots =
            new List<RunInventorySlotData>();

        // 135일차 영구 강화에서 사용할 슬롯 증가량을 미리 저장할 수 있게 한다.
        public int PermanentSlotBonus;

        // 99일차 가방 장비에서 사용할 슬롯 증가량을 미리 저장할 수 있게 한다.
        public int BagSlotBonus;

        // 89일차 이전 저장과 기존 코드를 읽기 위한 호환 목록을 유지한다.
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

        // 현재 층의 확정된 생성 그래프와 Seed 재현 정보다.
        public ProjectDelta.Domain.DungeonLayoutSnapshot LayoutSnapshot;

        // Fog of War에서 한 번이라도 밝혀진 방 목록이다.
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

        // 95일차: 이전 저장과 "새 저장에서 남은 아이템 0개"를 구분하기 위한 표식.
        public bool HasChestContentsSnapshot;

        // 현재 상자에 실제로 남아 있는 아이템 키를 순서대로 저장한다.
        public List<string> ChestRemainingItems =
            new List<string>();

        public bool IsStairs;
        public bool StairsDiscovered;
        public bool RestRoomUsed;

        // 110일차: 방 종류. TrapTriggered는 이미 위에 있던 필드를 그대로 쓴다
        // (이번 일차 전까지는 아무도 읽거나 쓰지 않던 필드였다).
        public ProjectDelta.Domain.RoomType RoomType;
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
