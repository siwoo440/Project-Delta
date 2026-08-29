DungeonMinimapController.cs CS0104 수정 패키지

수정 대상
Assets\ProjectDelta\Scripts\Presentation\DungeonMinimapController.cs

수정 내용
using DungeonRunState = ProjectDelta.Domain.DungeonRunState;
별칭을 추가하여 ProjectDelta.Data.DungeonRunState와
ProjectDelta.Domain.DungeonRunState 사이의 모호한 참조를 제거합니다.

사용 방법
1. 이 ZIP을 Unity 프로젝트 루트 폴더에 압축 해제합니다.
2. ApplyFix.bat을 실행합니다.
3. 원본 파일은 DungeonMinimapController.cs.bak으로 백업됩니다.
4. Unity로 돌아가 스크립트 재컴파일을 확인합니다.

중요
이 패키지는 현재 로컬 파일 전체를 교체하지 않습니다.
기존 최신 코드를 유지하면서 문제의 using alias 한 줄만 삽입합니다.
GitHub main의 파일이 로컬 작업본보다 오래된 상태라 전체 파일 덮어쓰기는
최근 수정 내용을 잃을 위험이 있어 안전 패치 방식으로 구성했습니다.
