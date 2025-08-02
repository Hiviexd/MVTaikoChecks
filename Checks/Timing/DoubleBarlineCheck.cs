using System.Collections.Generic;
using System.Linq;

using MapsetParser.objects;
using MapsetParser.objects.timinglines;
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
    public class DoubleBarlineCheck : BeatmapCheck
    {
        private const string _PROBLEM = nameof(_PROBLEM);
        private const string _WARNING = nameof(_WARNING);
        private const string _ROUNDING_ERROR_WARNING = "roundingErrorWarning";

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Hivie, Phob",
                Category = "Timing",
                Message = "Double barlines",
                Modes = new Beatmap.Mode[] { MODE_TAIKO },
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Ensuring that there are no two barlines within 50ms of each other."
                    },
                    {
                        "Reasoning",
                        @"
                    Double barlines are caused by rounding errors, visually disruptive and confusing in the representation of a song's downbeat."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>()
            {
                {
                    _PROBLEM,
                    new IssueTemplate(
                        LEVEL_PROBLEM,
                        "{0} Double barline",
                        "timestamp - "
                    ).WithCause(
                        "Red line is extremely close to a downbeat from the previous red line"
                    )
                },
                {
                    _WARNING,
                    new IssueTemplate(
                        LEVEL_WARNING,
                        "{0} Potential double barline, doublecheck manually",
                        "timestamp - "
                    ).WithCause(
                        "Red line is extremely close to a downbeat from the previous red line"
                    )
                },
                {
                    _ROUNDING_ERROR_WARNING,
                    new IssueTemplate(
                        LEVEL_WARNING,
                        "{0} Potential double barline due to rounding error, doublecheck manually",
                        "timestamp - "
                    ).WithCause(
                        "Rounding error"
                    )
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            const double threshold = 50;

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
                if (
                    next == null
                    || next.omitsBarLine
                    || (current.omitsBarLine && distance <= barlineGap)
                )
                    continue;

                var rest = distance % barlineGap;

                if (rest - barlineGap > -Global.ROUNDING_ERROR_MARGIN)
                {
                    yield return new Issue(
                        GetTemplate(_ROUNDING_ERROR_WARNING),
                        beatmap,
                        Timestamp.Get(next.offset)
                    );
                }
                else if (rest - threshold <= 0 && rest > 0)
                {
                    if (rest >= 0.5)
                    {
                        yield return new Issue(
                            GetTemplate(_PROBLEM),
                            beatmap,
                            Timestamp.Get(next.offset)
                        );
                    }
                    else
                    {
                        yield return new Issue(
                            GetTemplate(_WARNING),
                            beatmap,
                            Timestamp.Get(next.offset)
                        );
                    }
                }
            }
        }
    }
}
