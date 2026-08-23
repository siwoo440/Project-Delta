using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectDelta.Data
{
    // Progress for the single active run. Deleted on run end (기획서 9.1).
    // RunData is saved independently from ProfileData/SettingsData so a
    // corrupted run never takes permanent progress or settings down with it.
    [Serializable]
    public sealed class RunData
    {
        public RunBasicInfo BasicInfo = new RunBasicInfo();
        public PlayerRunStats PlayerStats = new PlayerRunStats();
        public RunInventory Inventory = new RunInventory();
        public DungeonRunState DungeonState = new DungeonRunState();
        public List<CharacterRunState> CharacterStates = new List<CharacterRunState>();
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

        // 26일차: 방-방 연결 그래프가 없는 지금은 CurrentRoomCoordinate로 이어하기 위치를 못 찾는다.
        // RoomId로 대신 찾는 테스트용 필드. 실제 좌표/연결 데이터가 생기면 이 필드는 걷어낸다.
        public string CurrentRoomId;

        // 26일차: 방을 다시 찾아간 뒤, 그 방 안 정확히 어느 칸에 서 있었는지 복원하기 위한 필드.
        public Vector2Int CurrentGridPositionInRoom;

        // TODO 3.2.8절 던전 시드 구현 시 추가: 난수 생성기별 현재 상태
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
        public int MaxArousal;
        public int CurrentArousal;
        public int Attack;
        public int Defense;
        public int Speed;
        public int Charm;
        public int Evasion;
        public int Resistance;
        public List<string> RunStatModifierIds = new List<string>();
        public List<string> ActiveStatusEffectIds = new List<string>();
        public int Gold;
        public List<string> AcquiredRunSkillIds = new List<string>();
        public List<string> EquippedSkillIds = new List<string>();
        public List<string> StartingSkillIds = new List<string>();
    }

    [Serializable]
    public sealed class RunInventory
    {
        // TODO 6.4절 인벤토리·장비·유물 구현 시 실제 슬롯 구조로 교체
        public List<string> InventoryItemIds = new List<string>();
        public List<string> ConsumableStackIds = new List<string>();
        public List<string> ExplorationToolIds = new List<string>();
        public List<string> KeyItemIds = new List<string>();
        public List<string> TreasureIds = new List<string>();
        public List<string> EquippedItemIds = new List<string>();
        public List<string> EquipmentRolledOptionIds = new List<string>();
        public List<string> BagIds = new List<string>();
        public List<string> RelicIds = new List<string>();
        public List<string> CursedItemIds = new List<string>();
    }

    [Serializable]
    public sealed class DungeonRunState
    {
        // TODO 3.1~3.2절 던전 생성 구현 시 실제 방/층 데이터 구조로 교체
        public List<string> FloorThemeIds = new List<string>();
        public List<RoomRunState> Rooms = new List<RoomRunState>();
    }

    [Serializable]
    public sealed class RoomRunState
    {
        public string RoomId;
        public Vector2Int Coordinate;
        public List<int> ConnectedDirections = new List<int>();
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

        // NPC 전용 상태 (몬스터 개체에는 사용하지 않음)
        public bool IsHostile;
        public bool IsUsingService;
    }
}
