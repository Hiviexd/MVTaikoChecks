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
    public class SmallestSnapCheck : BeatmapCheck
    {
        private const string _PROBLEM = nameof(_PROBLEM);

        private readonly Beatmap.Difficulty[] _DIFFICULTIES = new Beatmap.Difficulty[] { DIFF_KANTAN, DIFF_FUTSUU, DIFF_MUZU, DIFF_ONI };

        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Author = "Phob",
            Category = "Compose",
            Message = "Unrankable snapping",

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

                new IssueTemplate(
                    LEVEL_PROBLEM,
                    "{0} Too small gap between notes",
                    "timestamp - ")
                .WithCause("Too small gap between notes")
            }
        };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var circles = beatmap.hitObjects.Where(x => x is Circle).ToList();

            // for each diff: var violatingGroup = new List<HitObject>();
            // lambda is used, because bare "new List<HitObject>()" would set the same instance in each pair
            var violatingGroup = new Dictionary<Beatmap.Difficulty, List<HitObject>>();
            violatingGroup.AddRange(_DIFFICULTIES, () => new List<HitObject>() );

            for (int i = 0; i < circles.Count; i++)
            {
                var current = circles[i];
                var next = circles.SafeGetIndex(i + 1);

                var timing = beatmap.GetTimingLine<UninheritedLine>(current.time);
                var normalizedMsPerBeat = timing.GetNormalizedMsPerBeat();

                // for each diff: double minimalGap = ?;
                var minimalGap = new Dictionary<Beatmap.Difficulty, double>()
                {
                    { DIFF_KANTAN, normalizedMsPerBeat / 2 },
                    { DIFF_FUTSUU, normalizedMsPerBeat / 3 },
                    { DIFF_MUZU, normalizedMsPerBeat / 6 },
                    { DIFF_ONI, normalizedMsPerBeat / 8 }
                };

                var gap = (next?.time ?? double.MaxValue) - current.time;

                // for each diff: bool violatingGroupEnded = false;
                var violatingGroupEnded = new Dictionary<Beatmap.Difficulty, bool>();
                violatingGroupEnded.AddRange(_DIFFICULTIES, false);

                foreach (var diff in _DIFFICULTIES)
                {
                    CheckAndHandleIssues(diff, minimalGap, violatingGroup, violatingGroupEnded, current, next, gap);

                    if (violatingGroupEnded[diff])
                    {
                        yield return new Issue(
                            GetTemplate(_PROBLEM),
                            beatmap,
                            Timestamp.Get(violatingGroup[diff].ToArray())
                        ).ForDifficulties(diff);

                        violatingGroup[diff].Clear();
                    }
                }
            }
        }

        private static void CheckAndHandleIssues(
            Beatmap.Difficulty diff,
            Dictionary<Beatmap.Difficulty, double> minimalGap,
            Dictionary<Beatmap.Difficulty, List<HitObject>> violatingGroup,
            Dictionary<Beatmap.Difficulty, bool> violatingGroupEnded,
            HitObject current,
            HitObject next,
            double gap)
        {
            if (gap < minimalGap[diff])
            {
                // add both the current and the next object only at the beginning of a group, add only the next one otherwise
                if (violatingGroup[diff].Count == 0)
                    violatingGroup[diff].Add(current);

                violatingGroup[diff].Add(next);
            }
            else if (violatingGroup[diff].Count != 0)
            {
                violatingGroupEnded[diff] = true;
            }
        }
    }
}
