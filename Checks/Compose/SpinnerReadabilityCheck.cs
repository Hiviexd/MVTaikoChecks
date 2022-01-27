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

namespace MVTaikoChecks.Checks.Compose
{
    [Check]
    public class SpinnerReadabilityCheck : BeatmapCheck
    {
        private const string _WARNING = nameof(_WARNING);

        private readonly Beatmap.Difficulty[] _DIFFICULTIES = new Beatmap.Difficulty[] { DIFF_KANTAN, DIFF_FUTSUU, DIFF_MUZU, DIFF_ONI, DIFF_INNER, DIFF_URA };

        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Author = "Phob",
            Category = "Compose",
            Message = "Hard to read spinner",

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
                    "{0} The note is too close to the spinner",
                    "timestamp - ")
                .WithCause("The note is too close to the spinner")
            }
        };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var hitObjects = beatmap.hitObjects;

            for (int i = 0; i < hitObjects.Count; i++)
            {
                var current = hitObjects[i];
                var next = hitObjects.SafeGetIndex(i + 1);

                var timing = beatmap.GetTimingLine<UninheritedLine>(current.time);
                var normalizedMsPerBeat = timing.GetNormalizedMsPerBeat();

                // for each diff: double minimalGap = ?;
                var minimalGap = new Dictionary<Beatmap.Difficulty, double>();
                minimalGap.AddRange(_DIFFICULTIES.Take(3), normalizedMsPerBeat / 2);
                minimalGap.AddRange(_DIFFICULTIES.Skip(3).Take(3), normalizedMsPerBeat / 4);

                if (!(next is Spinner))
                    continue;

                var currentEndTime = current is Slider ? (current as Slider).endTime : current.time;
                // null checking is not required, since if (!(next is Spinner)) is gonna filter everything out anyways
                var gap = next.time - currentEndTime;

                foreach (var diff in _DIFFICULTIES)
                {
                    if (gap < minimalGap[diff])
                    {
                        yield return new Issue(
                            GetTemplate(_WARNING),
                            beatmap,
                            Timestamp.Get(current, next)
                        ).ForDifficulties(diff);
                    }
                }
            }
        }
    }
}
