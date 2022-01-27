using System;
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
using System.Diagnostics;

namespace MVTaikoChecks.Checks.Compose
{
    [Check]
    public class RestMomentCheck : BeatmapCheck
    {
        private const string _MINOR = nameof(_MINOR);
        private const string _WARNING = nameof(_WARNING);

        private readonly Beatmap.Difficulty[] _DIFFICULTIES = new Beatmap.Difficulty[] { DIFF_KANTAN, DIFF_FUTSUU, DIFF_MUZU, DIFF_ONI };

        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Author = "Phob",
            Category = "Compose",
            Message = "Rest moments",

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
                _MINOR,

                new IssueTemplate(LEVEL_MINOR,
                    "{0} {1} No rest moments for {2}, ensure this makes sense",
                    "start - ", "end - ", "length")
            },

            {
                _WARNING,

                new IssueTemplate(LEVEL_WARNING,
                    "{0} {1} No rest moments for {2}, ensure this makes sense",
                    "start - ", "end - ", "length")
            }
        };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var circles = beatmap.hitObjects.Where(x => x is Circle).ToList();

            // for each diff: double minimalGap = ?;
            var minimalGap = new Dictionary<Beatmap.Difficulty, double>()
            {
                { DIFF_KANTAN, 3 * FULL_BEAT_180 },
                { DIFF_FUTSUU, 2 * FULL_BEAT_180 },
                { DIFF_MUZU, 1.5 * FULL_BEAT_180 },
                { DIFF_ONI, FULL_BEAT_180 }
            };

            // for each diff: double previousGap = circles.FirstOrDefault()?.time ?? 0;
            var previousGap = new Dictionary<Beatmap.Difficulty, double>();
            previousGap.AddRange(_DIFFICULTIES, circles.FirstOrDefault()?.time ?? 0);

            for (int i = 0; i < circles.Count; i++)
            {
                var current = circles[i];
                var next = i + 1 < circles.Count ? circles[i + 1] : null;

                var gap = (next?.time ?? double.MaxValue) - current.time;

                // for each diff: bool isRestMoment = false;
                var isRestMoment = new Dictionary<Beatmap.Difficulty, bool>();
                isRestMoment.AddRange(_DIFFICULTIES, false);

                foreach (var diff in _DIFFICULTIES)
                {
                    CheckAndHandleIssues(diff, minimalGap, isRestMoment, gap);

                    if (isRestMoment[diff])
                    {
                        double beatsWithoutBreaks = Math.Floor((current.time - previousGap[diff]) / FULL_BEAT_180);

                        if (beatsWithoutBreaks >= 32)
                        {
                            yield return new Issue(
                                GetTemplate(_WARNING),
                                beatmap,
                                Timestamp.Get(previousGap[diff]),
                                Timestamp.Get(current.time),
                                $"{beatsWithoutBreaks}/1"
                            ).ForDifficulties(diff);
                        }
                        else if (beatsWithoutBreaks >= 20)
                        {
                            yield return new Issue(
                                GetTemplate(_MINOR),
                                beatmap,
                                Timestamp.Get(previousGap[diff]),
                                Timestamp.Get(current.time),
                                $"{beatsWithoutBreaks}/1"
                            ).ForDifficulties(diff);
                        }

                        previousGap[diff] = next?.time ?? double.MaxValue;
                    }
                }
            }
        }

        private static void CheckAndHandleIssues(
            Beatmap.Difficulty diff,
            Dictionary<Beatmap.Difficulty, double> minimalGap,
            Dictionary<Beatmap.Difficulty, bool> isRestMoment,
            double gap)
        {
            if (diff == DIFF_MUZU)
                Debugger.Break();

            if (gap >= minimalGap[diff])
                isRestMoment[diff] = true;
        }
    }
}
