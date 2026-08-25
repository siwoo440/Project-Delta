Project Delta 70일차 CS1056 / CS1002 / CS1003 복구 패키지

원인
- Day70SurrenderInstaller.cs가 줄바꿈을 실제 줄바꿈(\n)이 아니라
  문자 그대로의 백슬래시+n(\\n)으로 ExplorationMonsterEncounterController.cs에 삽입했습니다.
- 일반 공격 1곳, 스킬 공격 1곳이 손상되었습니다.

적용
1. 이 ZIP의 내용물을 Project Delta 프로젝트 루트에 덮어씁니다.
2. Fix-Day70-CorruptedEncounter.bat 를 실행합니다.
3. 완료 메시지를 확인합니다.
4. Unity로 돌아가 재컴파일합니다.
5. Console의 기존 오류를 Clear 후 다시 확인합니다.

복구 스크립트는 수정 전 파일을
ExplorationMonsterEncounterController.cs.day70-backup
이름으로 한 번 백업합니다.

수정된 Day70SurrenderInstaller.cs도 함께 포함되어 있어
같은 자동 패치를 다시 실행해도 이번 \n 문법 오류가 재발하지 않습니다.
