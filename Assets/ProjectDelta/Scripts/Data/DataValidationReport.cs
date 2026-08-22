using System.Collections.Generic;

namespace ProjectDelta.Data
{
    public sealed class DataValidationReport
    {
        public List<string> Errors { get; } = new List<string>();

        public bool HasErrors => Errors.Count > 0;
    }
}
