using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using MVTaikoChecks.Utils;
using System.Collections.Generic;
using System.Linq;
using static MVTaikoChecks.Aliases.Difficulty;
using static MVTaikoChecks.Aliases.Level;
using static MVTaikoChecks.Aliases.Mode;

namespace MVTaikoChecks.Checks.Compose
#warning: TODO: improve error detection in oni and inner + implement finisher and preceding note same color detection
{
    [Check]
    public class UnrankableFinisherCheck : BeatmapCheck
    {
        private const string _WARNING = nameof(_WARNING);
        private const string _PROBLEM = nameof(_PROBLEM);

        private readonly Beatmap.Difficulty[] _DIFFICULTIES = new Beatmap.Difficulty[] { DIFF_KANTAN, DIFF_FUTSUU, DIFF_MUZU, DIFF_ONI, DIFF_INNER, DIFF_URA };

        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Author = "Hivie",
            Category = "Compose",
            Message = "Unrankable finishers",

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
                _WARNING,

                new IssueTemplate(
                    LEVEL_WARNING,
                    "{0} Abnormal finisher, ensure this makes sense",
                    "timestamp - ")
            },
            {
                _PROBLEM,

                new IssueTemplate(
                    LEVEL_PROBLEM,
                    "{0} Unrankable finisher",
                    "timestamp - ")
            }
        };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var circles = beatmap.hitObjects.Where(x => x is Circle).ToList();

            // for each diff: var violatingGroup = new List<HitObject>();
            // lambda is used, because bare "new List<HitObject>()" would set the same instance in each pair
            var violatingGroup = new Dictionary<Beatmap.Difficulty, List<HitObject>>();
            violatingGroup.AddRange(_DIFFICULTIES, () => new List<HitObject>());

            for (int i = 0; i < circles.Count; i++)
            {
                var current = circles[i];
                var next = circles.SafeGetIndex(i + 1);
                var previous = i == 0 ? null : circles.SafeGetIndex(i - 1);

                var timing = beatmap.GetTimingLine<UninheritedLine>(current.time);
                var normalizedMsPerBeat = timing.GetNormalizedMsPerBeat();

                // for each diff: double minimalGap = ?;
                var minimalGap = new Dictionary<Beatmap.Difficulty, double>()
                {
                    { DIFF_KANTAN, normalizedMsPerBeat / 2 },
                    { DIFF_FUTSUU, normalizedMsPerBeat / 3 },
                    { DIFF_MUZU, normalizedMsPerBeat / 3 },
                    { DIFF_ONI, normalizedMsPerBeat / 4 },
                    { DIFF_INNER, normalizedMsPerBeat / 4 },
                    { DIFF_URA, normalizedMsPerBeat / 4 },
                };

                var nextGap = (next?.time ?? double.MaxValue) - current.time;
                var previousGap = i == 0 ? normalizedMsPerBeat : current.time - (previous?.time ?? double.MaxValue);

                // for each diff: bool violatingGroupEnded = false;
                var violatingGroupEnded = new Dictionary<Beatmap.Difficulty, bool>();
                violatingGroupEnded.AddRange(_DIFFICULTIES, false);

                foreach (var diff in _DIFFICULTIES)
                {
                    CheckAndHandleIssues(diff, minimalGap, violatingGroup, violatingGroupEnded, current, nextGap, previousGap);

                    if (violatingGroupEnded[diff])
                    {
                        if (diff == DIFF_KANTAN || diff == DIFF_FUTSUU || diff == DIFF_MUZU ||
                            (diff == DIFF_ONI && nextGap < minimalGap[DIFF_ONI]))
                        {
                            yield return new Issue(
                                GetTemplate(_PROBLEM),
                                beatmap,
                                Timestamp.Get(violatingGroup[diff].ToArray())
                            ).ForDifficulties(diff);

                            violatingGroup[diff].Clear();
                        }
                        else if (diff == DIFF_INNER || diff == DIFF_URA)
                        {
                            if (nextGap < minimalGap[diff])
                            {
                                yield return new Issue(
                                GetTemplate(_WARNING),
                                beatmap,
                                Timestamp.Get(violatingGroup[diff].ToArray())
                                ).ForDifficulties(diff);

                                violatingGroup[diff].Clear();
                            }
                        }
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
            double nextGap,
            double previousGap)
        {
            if (current.HasHitSound(HitObject.HitSound.Finish))
            {
                if (nextGap < minimalGap[diff] || previousGap < minimalGap[diff])
                {
                    violatingGroup[diff].Add(current);
                    violatingGroupEnded[diff] = true;
                }
            }
        }
    }
}
