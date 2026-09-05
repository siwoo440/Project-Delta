# 133일차 : CG 갤러리 구현

## 목표
- 기획서 7.4절 "CG" 항목을 실제 화면으로 구현 - 몬스터·NPC 관계 이벤트 CG 해금 체계와
  메인 메뉴에서 열람 가능한 갤러리 UI를 완성

## 구현 내용

### 1. CG 해금 규칙
- `MonsterCgRule`(Domain) - 몬스터 호감도 20/40/60/80/100 구간별 CG ID(`{몬스터ID}_CG_{구간}`)
- `NpcCgRule`(Domain) - NPC는 기존 `NpcRelationshipRules` 관계 단계 경계(34/67/85/100)를
  그대로 재사용해 CG ID 생성
- `ApplicationFlow.UnlockCg()`/`IsCgUnlocked()` 범용 메서드로 어떤 CG든 같은 방식으로 기록

### 2. 해금 시점 연결
- `EventBattleParticipantState.AddFavor()` - 몬스터 호감도가 구간을 넘길 때마다 자동 해금
- `NpcInteractionService.ResolveGift()`/`ResolveRescue()` - NPC 호감도가 오르는 두 경로에서 자동 해금

### 3. NPC 역할 목록 공유
- `NpcRuntimeBootstrapController` 안에만 있던 4개 역할(상인/치료사/지도사/보물사냥꾼) 정의를
  `NpcRosterCatalog`로 분리해 CG 갤러리도 같은 목록을 참조하도록 정리

### 4. CG 갤러리 화면 (Canvas/UGUI)
- 전용 씬 `CgGalleryScene.unity` + 타이틀 화면 "CG 목록" 버튼으로 진입
- 왼쪽: 몬스터(보스 제외) + NPC 전체를 스크롤 버튼 목록으로, 초상화 자리는 고유색 박스로 대체
- 오른쪽: 선택한 캐릭터의 CG를 3열×2행(6장)씩 페이지 단위로 표시, 미해금은 "?"로 잠금
- 진입 시 목록 맨 위 캐릭터 자동 선택

### 5. 버그 수정 두 건 (사용자 피드백으로 발견)
- **몬스터 목록이 비어있던 문제**: `Resources.FindObjectsOfTypeAll`은 "이미 메모리에 로드된"
  에셋만 찾는데, 타이틀에서 바로 들어가는 CG 갤러리는 던전 씬을 거친 적이 없어 몬스터 정의가
  로드된 적이 없었다 - 몬스터 정의 22개를 `Resources/Monster Definition/`으로 이동(`git mv`로
  GUID 보존)해 `Resources.LoadAll`로 확실히 불러오도록 수정
- **왼쪽 목록이 안 보이던 문제**: 스크롤 영역을 자르는 `Mask`의 그래픽을 `Color.clear`로
  만들었더니 마스크가 알파값 기준으로 "보이는 영역 없음"으로 판정해 자식(버튼 목록 전체)이
  통째로 사라졌다 - 알파에 의존하지 않는 `RectMask2D`로 교체

### 6. 레이아웃 조정 (사용자 피드백)
- CG 칸 크기 확대(220→280) 및 오른쪽 패널 중앙 정렬 컨테이너 도입
- 페이지 이동 버튼(이전/페이지 표시/다음)도 같은 방식으로 중앙 하단 정렬
