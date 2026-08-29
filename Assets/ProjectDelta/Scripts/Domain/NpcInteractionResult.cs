namespace ProjectDelta.Domain
{
    public sealed class NpcInteractionResult
    {
        public NpcInteractionResult(
            NpcInteractionResultType resultType,
            string message)
        {
            ResultType =
                resultType;

            Message =
                message ?? string.Empty;
        }

        public NpcInteractionResultType ResultType { get; }
        public string Message { get; }
    }
}
