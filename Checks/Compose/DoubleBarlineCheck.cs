using System.Collections.Generic;
using System.Linq;

using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;

using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

using MVTaikoChecks.Utils;

using static MVTaikoChecks.Global;
using static MVTaikoChecks.Aliases.Difficulty;
using static MVTaikoChecks.Aliases.Mode;
using static MVTaikoChecks.Aliases.Level;

namespace MVTaikoChecks.Checks.Compose
{
    [Check]
    public class DoubleBarlineCheck : BeatmapCheck
    {
        private const string _PROBLEM = nameof(_PROBLEM);

        private readonly Beatmap.Difficulty[] _DIFFICULTIES = new Beatmap.Difficulty[] { DIFF_KANTAN, DIFF_FUTSUU, DIFF_MUZU, DIFF_ONI, DIFF_INNER, DIFF_URA };

        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Author = "Phob",
            Category = "Compose",
            Message = "Double barlines",

            Difficulties = _DIFFICULTIES,

            Modes = new Beatmap.Mode[]
            {
                MODE_TAIKO
            },

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    "TODO"
                },
                {
                    "Reasoning",
                    "TODO"
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates() => new Dictionary<string, IssueTemplate>()
        {
            {
                _PROBLEM,

#if RELEASE
#warning TODO: WORDING
#endif

                new IssueTemplate(LEVEL_PROBLEM,
                    "{0} Double barlines",
                    "timestamp - ")
            }
        };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            const double threshold = 50;
            const double lower_threshold = 2;

            var redLines = beatmap.timingLines
                .Where(x => x is UninheritedLine)
                .Select(x => x as UninheritedLine)
                .ToList();

            for (int i = 0; i < redLines.Count; i++)
            {
                var current = redLines[i];
                var next = redLines.SafeGetIndex(i + 1);

                var barlineGap = current.msPerBeat * current.meter;
                var distance = (next?.offset ?? double.MaxValue) - current.offset;

                // if the next line has an omit, double barlines can't happen
                // if the current line has an omit and lasts only 1 measure, double barlines can't happen either
                // true for not insanely high bpms, but who cares ^
                if (next == null || next.omitsBarLine || (current.omitsBarLine && distance <= barlineGap))
                    continue;

                var rest = distance % barlineGap;

                // rest >= lower_threshold is to avoid rounding errors and false positives on red lines placed at the same time as a big white tick
                if (rest - threshold <= 0 && rest >= lower_threshold)
                {
                    yield return new Issue(
                        GetTemplate(_PROBLEM),
                        beatmap,
                        Timestamp.Get(next.offset)
                    ).ForDifficulties(_DIFFICULTIES);
                }
            }
        }
    }
}
