using System.Collections.Generic;

using MapsetParser.objects;
using MapsetParser.statics;

using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

using MVTaikoChecks.Utils;

using static MVTaikoChecks.Aliases.Mode;
using static MVTaikoChecks.Aliases.Level;

namespace MVTaikoChecks.Checks.Timing
{
    [Check]
    public class ConflictingKiaisCheck : BeatmapCheck
    {
        private const string _WARNING = nameof(_WARNING);

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Nostril",
                Category = "Timing",
                Message = "Conflicting Kiai enablement",
                Modes = new Beatmap.Mode[] { MODE_TAIKO },
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Ensuring that Kiai enablement does not conflict at a single timestamp."
                    },
                    {
                        "Reasoning",
                        @"
                    In this situation, the green line Kiai overrides red line Kiai. However, this is fragile and may not be the intended behavior."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>()
            {
                {
                    _WARNING,
                    new IssueTemplate(LEVEL_WARNING, "{0} Conflicting Kiais", "timestamp - ").WithCause(
                        "Conflicting Kiai states are set at the same time. Check the timing panel."
                    )
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            // making a hashset for the results to de-dupe any duplicate issues
            HashSet<double> conflictTimestamps = new HashSet<double>();

            for (int i = 0; i < beatmap.timingLines.Count; i++)
            {
                var line = beatmap.timingLines.SafeGetIndex(i);
                var nextLine = beatmap.timingLines.SafeGetIndex(i + 1);

                // check if this line is concurrent with the next one
                if (line.offset == nextLine?.offset)
                {
                    // check if kiai enablement doesn't match
                    if (nextLine.kiai != line.kiai)
                    {
                        conflictTimestamps.Add(line.offset);
                    }
                }
            }

            foreach (var conflictTimestamp in conflictTimestamps)
            {
                yield return new Issue(
                    GetTemplate(_WARNING),
                    beatmap,
                    Timestamp.Get(conflictTimestamp)
                );
            }
        }
    }
}
