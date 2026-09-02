namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    public sealed class NpcInteractionResult // NPC 상호작용 한 번의 결과값
    {
        public NpcInteractionResult( // 결과 생성자
            NpcInteractionResultType resultType, // 다음에 이어질 화면 종류
            string message) // 사용자에게 보여줄 안내 문구
        {
            ResultType = // 결과 종류 저장
                resultType; // 매개변수로 받은 값 대입

            Message = // 안내 문구 저장
                message ?? string.Empty; // null이면 빈 문자열로 대체
        }

        public NpcInteractionResultType ResultType { get; } // 다음 화면 종류
        public string Message { get; } // 안내 문구
    }
}
