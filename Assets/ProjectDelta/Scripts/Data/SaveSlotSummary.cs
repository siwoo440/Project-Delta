namespace ProjectDelta.Data
{
    // 저장 슬롯 UI가 슬롯 목록을 그릴 때 쓰는 요약 정보다.
    // RunData 전체를 파싱하지 않고도 슬롯 카드를 채울 수 있게 분리했다.
    public sealed class SaveSlotSummary
    {
        public int Slot { get; set; }

        public bool HasData { get; set; }

        public string RunId { get; set; }

        // 실제로 파일에 마지막으로 기록된 시각(SaveEnvelope.ModifiedAtIso8601).
        public string SavedAtIso8601 { get; set; }

        // RunData.BasicInfo.PlaytimeSeconds를 그대로 옮긴 값이다.
        // 지금은 이 필드를 실제로 갱신하는 곳이 없어 항상 0으로 표시된다 -
        // 플레이타임 누적 추적은 별도 작업이 필요하다.
        public float PlaytimeSeconds { get; set; }

        public static SaveSlotSummary Empty(
            int slot)
        {
            return new SaveSlotSummary
            {
                Slot = slot,
                HasData = false
            };
        }
    }
}
