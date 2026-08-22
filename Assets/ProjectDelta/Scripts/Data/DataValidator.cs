using System.Collections.Generic;

namespace ProjectDelta.Data
{
    // Day 3 scope: duplicate/empty ID only.
    // Missing-reference, localization-key and probability-sum checks are added
    // once the corresponding data (skills, events, loot tables) exists.
    public sealed class DataValidator : IDataValidator
    {
        public DataValidationReport Validate(DataRepository repository)
        {
            var report = new DataValidationReport();

            ValidateIds(repository.Monsters.All, "Monster", report);
            ValidateIds(repository.Items.All, "Item", report);

            return report;
        }

        private static void ValidateIds<TDefinition>(IEnumerable<TDefinition> definitions, string category, DataValidationReport report)
            where TDefinition : DefinitionBase
        {
            var seenIds = new HashSet<string>();

            foreach (var definition in definitions)
            {
                if (string.IsNullOrEmpty(definition.Id))
                {
                    report.Errors.Add($"{category} definition '{definition.name}' has an empty Id.");
                    continue;
                }

                if (!seenIds.Add(definition.Id))
                {
                    report.Errors.Add($"{category} definition Id '{definition.Id}' is duplicated.");
                }
            }
        }
    }
}
