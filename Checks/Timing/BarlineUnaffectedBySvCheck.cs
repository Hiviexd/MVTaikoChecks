using System.Collections.Generic;

using MapsetParser.objects;
using MapsetParser.statics;

using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

using MVTaikoChecks.Utils;

using static MVTaikoChecks.Aliases.Mode;
using static MVTaikoChecks.Aliases.Level;

namespace MVTaikoChecks.Checks.Compose
{
    [Check]
    public class BarlineUnaffectedBySvCheck : BeatmapCheck
    {
        private const string _WARNING = nameof(_WARNING);

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Nostril",
                Category = "Timing",
                Message = "Barline is unaffected by a line very close to it.",
                Modes = new Beatmap.Mode[] { MODE_TAIKO },
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Preventing unintentional barline slider velocities caused by timing lines being slightly unsnapped."
                    },
                    {
                        "Reasoning",
                        @"
                    Barlines before a timing line (even if just by 1 ms or less), will not be affected by its slider velocity. With 1 ms unsnaps being common for due to rounding errors when copy pasting, this in turn becomes a common issue."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>()
            {
                {
                    _WARNING,

                    new IssueTemplate(
                        LEVEL_WARNING,
                        "{0} Barline is snapped {1} ms before a line which would modify its slider velocity.",
                        "timestamp - ",
                        "unsnap"
                    ).WithCause("The spinner/slider end is unsnapped 1ms early.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (var svChange in beatmap.FindSvChanges())
            {
                var unsnapMs = TaikoUtils.GetOffsetFromNearestBarlineMs(beatmap, svChange.offset);
                if (unsnapMs <= 1d && unsnapMs > 0d)
                {
                    yield return new Issue(
                       GetTemplate(_WARNING),
                       beatmap,
                       Timestamp.Get(svChange.offset - unsnapMs),
                       $"{unsnapMs:0.##}"
                   );
                }
            }
        }
    }
}
