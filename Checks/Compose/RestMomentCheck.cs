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
                    "start", "end", "length")
            },

            {
                _WARNING,

                new IssueTemplate(LEVEL_WARNING,
                    "{0} {1} No rest moments for {2}, ensure this makes sense",
                    "start", "end", "length")
            }
        };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var objects = beatmap.hitObjects.ToList();

            // for each diff: double previousGap = circles.FirstOrDefault()?.time ?? 0;
            var previousGap = new Dictionary<Beatmap.Difficulty, double>();
            previousGap.AddRange(_DIFFICULTIES, objects.FirstOrDefault()?.time ?? 0);

            for (int i = 0; i < objects.Count; i++)
            {
                var current = objects[i];
                var next = objects.SafeGetIndex(i + 1);

                var timing = beatmap.GetTimingLine<UninheritedLine>(current.time);
                var normalizedMsPerBeat = timing.GetNormalizedMsPerBeat();

                // for each diff: double minimalGap = ?;
                var minimalGap = new Dictionary<Beatmap.Difficulty, double>()
                {
                    { DIFF_KANTAN, 3 * normalizedMsPerBeat },
                    { DIFF_FUTSUU, 2 * normalizedMsPerBeat },
                    { DIFF_MUZU, 1.5 * normalizedMsPerBeat },
                    { DIFF_ONI, normalizedMsPerBeat }
                };

                var gap = (next?.time ?? double.MaxValue) - current.time;

                // for each diff: bool isRestMoment = false;
                var isRestMoment = new Dictionary<Beatmap.Difficulty, bool>();
                isRestMoment.AddRange(_DIFFICULTIES, false);

                foreach (var diff in _DIFFICULTIES)
                {
                    CheckAndHandleIssues(diff, minimalGap, isRestMoment, gap);

                    if (isRestMoment[diff])
                    {
                        double beatsWithoutBreaks = Math.Floor((current.time - previousGap[diff]) / normalizedMsPerBeat);

                        if ((diff == DIFF_KANTAN || diff == DIFF_FUTSUU) && beatsWithoutBreaks > 44 ||
                            (diff != DIFF_KANTAN && diff != DIFF_FUTSUU && beatsWithoutBreaks >= 32))
                        {
                            yield return new Issue(
                                GetTemplate(_WARNING),
                                beatmap,
                                Timestamp.Get(previousGap[diff]).Trim() + ">",
                                Timestamp.Get(current.time),
                                $"{beatsWithoutBreaks}/1"
                            ).ForDifficulties(diff);
                        }
                        else if ((diff == DIFF_KANTAN || diff == DIFF_FUTSUU) && beatsWithoutBreaks > 36 ||
                                (diff != DIFF_KANTAN && diff != DIFF_FUTSUU && beatsWithoutBreaks > 20))
                        {
                            yield return new Issue(
                                GetTemplate(_MINOR),
                                beatmap,
                                Timestamp.Get(previousGap[diff]).Trim() + ">",
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
            if (gap + MS_EPSILON >= minimalGap[diff])
                isRestMoment[diff] = true;
        }
    }
}
