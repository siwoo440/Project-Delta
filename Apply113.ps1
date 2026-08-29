$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = Join-Path $root 'Assets\ProjectDelta\Scripts\Presentation\DungeonFloorController.cs'

if (-not (Test-Path $target))
{
    throw "DungeonFloorController.cs not found: $target"
}

$text = [System.IO.File]::ReadAllText($target)
$text = $text.Replace("`r`n", "`n")

function Replace-Required
{
    param(
        [string]$Source,
        [string]$Old,
        [string]$New,
        [string]$Name
    )

    if ($Source.Contains($New))
    {
        return $Source
    }

    if (-not $Source.Contains($Old))
    {
        throw "Patch target not found: $Name"
    }

    return $Source.Replace($Old, $New)
}

$old = @'
            if (encounters.Count == 0)
            {
                Debug.LogWarning(
                    "[Project Delta] 40일차 EncounterDefinition이 지정되지 않아 몬스터 방 배정을 건너뜁니다.",
                    this);
                return;
            }
'@
$new = @'
            if (encounters.Count == 0)
            {
                Debug.LogWarning(
                    "[Project Delta] EncounterDefinition 필드가 비어 있어 로드된 인카운터 자산으로 Combat 방 보장을 시도합니다.",
                    this);

                EnsureCombatRoomsHaveMonsters(
                    dungeon,
                    seed);

                return;
            }
'@
$text = Replace-Required $text $old $new 'empty encounter fallback'

$old = @'
            if (dungeon?.Layout == null
                || defaultMonsterEncounter == null)
            {
                return;
            }
'@
$new = @'
            if (dungeon?.Layout == null)
            {
                return;
            }
'@
$text = Replace-Required $text $old $new 'combat guarantee early return'

$old = @'
            if (dungeonState == null)
            {
                return;
            }

            MonsterSpawnPositionService spawnPositionService =
                new MonsterSpawnPositionService();
'@
$new = @'
            if (dungeonState == null)
            {
                return;
            }

            List<EncounterDefinition> fallbackEncounters =
                CollectCombatGuaranteeEncounters();

            if (fallbackEncounters.Count == 0)
            {
                Debug.LogError(
                    "[Project Delta] Combat 방 최소 몬스터 보장 실패: 사용할 EncounterDefinition이 하나도 없습니다.",
                    this);
                return;
            }

            MonsterSpawnPositionService spawnPositionService =
                new MonsterSpawnPositionService();
'@
$text = Replace-Required $text $old $new 'combat guarantee encounter collection'

$old = @'
                MonsterGroupCompositionService.Result group =
                    MonsterGroupCompositionService.Build(
                        defaultMonsterEncounter,
                        seed,
                        room.RoomId);

                if (group.Representative == null
                    || group.Slots.Count == 0)
                {
                    continue;
                }

                string[] monsterDefinitionIds =
                    new string[group.Slots.Count];

                for (int slotIndex = 0; slotIndex < group.Slots.Count; slotIndex++)
                {
                    monsterDefinitionIds[slotIndex] =
                        group.Slots[slotIndex].Id;
                }

                RoomEncounterAssignment assignment =
                    new RoomEncounterAssignment(
                        room.RoomId,
                        RoomContentType.Monster,
                        defaultMonsterEncounter.Id,
                        monsterDefinitionIds,
                        group.Representative.Id);
'@
$new = @'
                if (!TryBuildCombatGuaranteeAssignment(
                        room.RoomId,
                        seed,
                        fallbackEncounters,
                        out RoomEncounterAssignment assignment))
                {
                    Debug.LogError(
                        $"[Project Delta] Combat 방 최소 몬스터 보장 실패: 유효한 몬스터 그룹 없음. RoomId={room.RoomId}",
                        roomView);
                    continue;
                }
'@
$text = Replace-Required $text $old $new 'combat guarantee assignment'

$old = @'
                if (!spawnPositionService.TryChoosePosition(
                        definition.MinX,
                        definition.MaxX,
                        definition.MinZ,
                        definition.MaxZ,
                        connectedExits,
                        occupiedPositions,
                        seed,
                        room.RoomId,
                        assignment.MonsterDefinitionId,
                        out GridPosition spawnPosition))
                {
                    Debug.LogWarning(
                        $"[Project Delta] 112일차 전투 방 몬스터 보장 배치 실패(빈 칸 없음). RoomId={room.RoomId}",
                        roomView);
                    continue;
                }
'@
$new = @'
                bool hasSpawnPosition =
                    spawnPositionService.TryChoosePosition(
                        definition.MinX,
                        definition.MaxX,
                        definition.MinZ,
                        definition.MaxZ,
                        connectedExits,
                        occupiedPositions,
                        seed,
                        room.RoomId,
                        assignment.MonsterDefinitionId,
                        out GridPosition spawnPosition);

                // 113일차: 다른 콘텐츠 때문에 빈 칸이 없으면 겹침을 허용해서라도 Combat 방 1마리를 우선 보장한다.
                if (!hasSpawnPosition)
                {
                    hasSpawnPosition =
                        spawnPositionService.TryChoosePosition(
                            definition.MinX,
                            definition.MaxX,
                            definition.MinZ,
                            definition.MaxZ,
                            connectedExits,
                            null,
                            seed,
                            room.RoomId,
                            assignment.MonsterDefinitionId,
                            out spawnPosition);
                }

                if (!hasSpawnPosition)
                {
                    spawnPosition =
                        ChooseEmergencyMonsterPosition(
                            definition,
                            connectedExits);

                    Debug.LogWarning(
                        $"[Project Delta] Combat 방 빈 칸이 없어 비상 위치에 최소 몬스터를 배치합니다. RoomId={room.RoomId} / Position={spawnPosition}",
                        roomView);
                }
'@
$text = Replace-Required $text $old $new 'combat guarantee emergency position'

$marker = @'
        // 111일차: RoomType.Combat이 아닌 방은 몬스터 조우 배정에서 제외한다.
'@
$helpers = @'
        // 113일차: 기본 인카운터가 비어 있어도 추가 인카운터와 현재 로드된 인카운터 자산까지 후보로 사용한다.
        private List<EncounterDefinition> CollectCombatGuaranteeEncounters()
        {
            List<EncounterDefinition> encounters =
                CollectFloorEncounters();

            HashSet<EncounterDefinition> known =
                new HashSet<EncounterDefinition>(encounters);

            EncounterDefinition[] loaded =
                Resources.FindObjectsOfTypeAll<EncounterDefinition>();

            for (int index = 0; index < loaded.Length; index++)
            {
                EncounterDefinition encounter =
                    loaded[index];

                if (encounter != null
                    && known.Add(encounter))
                {
                    encounters.Add(encounter);
                }
            }

            return encounters;
        }

        private static bool TryBuildCombatGuaranteeAssignment(
            string roomId,
            int seed,
            IReadOnlyList<EncounterDefinition> encounters,
            out RoomEncounterAssignment assignment)
        {
            assignment = null;

            if (encounters == null)
            {
                return false;
            }

            for (int encounterIndex = 0;
                 encounterIndex < encounters.Count;
                 encounterIndex++)
            {
                EncounterDefinition encounter =
                    encounters[encounterIndex];

                if (encounter == null)
                {
                    continue;
                }

                MonsterGroupCompositionService.Result group =
                    MonsterGroupCompositionService.Build(
                        encounter,
                        seed,
                        roomId);

                if (group.Representative == null
                    || group.Slots.Count == 0)
                {
                    continue;
                }

                string[] monsterDefinitionIds =
                    new string[group.Slots.Count];

                for (int slotIndex = 0;
                     slotIndex < group.Slots.Count;
                     slotIndex++)
                {
                    monsterDefinitionIds[slotIndex] =
                        group.Slots[slotIndex].Id;
                }

                assignment =
                    new RoomEncounterAssignment(
                        roomId,
                        RoomContentType.Monster,
                        encounter.Id,
                        monsterDefinitionIds,
                        group.Representative.Id);

                return true;
            }

            return false;
        }

        private static GridPosition ChooseEmergencyMonsterPosition(
            RoomDefinition definition,
            IReadOnlyList<RoomExit> connectedExits)
        {
            GridPosition center =
                new GridPosition(
                    Mathf.Clamp(
                        0,
                        definition.MinX,
                        definition.MaxX),
                    Mathf.Clamp(
                        0,
                        definition.MinZ,
                        definition.MaxZ));

            HashSet<GridPosition> doorPositions =
                new HashSet<GridPosition>();

            if (connectedExits != null)
            {
                for (int exitIndex = 0;
                     exitIndex < connectedExits.Count;
                     exitIndex++)
                {
                    doorPositions.Add(
                        connectedExits[exitIndex].LocalPosition);
                }
            }

            if (!doorPositions.Contains(center))
            {
                return center;
            }

            for (int z = definition.MinZ;
                 z <= definition.MaxZ;
                 z++)
            {
                for (int x = definition.MinX;
                     x <= definition.MaxX;
                     x++)
                {
                    GridPosition candidate =
                        new GridPosition(x, z);

                    if (!doorPositions.Contains(candidate))
                    {
                        return candidate;
                    }
                }
            }

            return center;
        }

'@
if (-not $text.Contains('private List<EncounterDefinition> CollectCombatGuaranteeEncounters()'))
{
    if (-not $text.Contains($marker))
    {
        throw 'Patch target not found: combat helper insertion point'
    }

    $text = $text.Replace($marker, $helpers + $marker)
}

$old = @'
            List<string> chestRoomIds =
                placementService.SelectRoomIds(
                    dungeon,
                    seed);

            MonsterSpawnPositionService spawnPositionService =
                new MonsterSpawnPositionService();
'@
$new = @'
            List<string> chestRoomIds =
                placementService.SelectRoomIds(
                    dungeon,
                    seed);

            // 113일차: 상자가 문 사이 이동 경로를 끊지 않는 후보만 선택한다.
            RoomBlockingPlacementService spawnPositionService =
                new RoomBlockingPlacementService();
'@
$text = Replace-Required $text $old $new 'path-safe chest service'

$old = @'
                if (!spawnPositionService.TryChoosePosition(
                        definition.MinX,
                        definition.MaxX,
                        definition.MinZ,
                        definition.MaxZ,
                        connectedExits,
                        occupiedPositions,
                        seed,
                        roomId,
                        "CHEST",
                        out GridPosition spawnPosition))
'@
$new = @'
                if (!spawnPositionService.TryChoosePosition(
                        definition.MinX,
                        definition.MaxX,
                        definition.MinZ,
                        definition.MaxZ,
                        connectedExits,
                        occupiedPositions,
                        roomView.PassageController.CanPass,
                        seed,
                        roomId,
                        "CHEST",
                        out GridPosition spawnPosition))
'@
$text = Replace-Required $text $old $new 'path-safe chest invocation'

$utf8 = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($target, $text, $utf8)
Write-Host 'Project Delta 113 patch applied.'
