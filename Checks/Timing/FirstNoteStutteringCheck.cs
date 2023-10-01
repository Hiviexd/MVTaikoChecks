using System.Collections.Generic;
using System.Linq;

using MapsetParser.objects;
using MapsetParser.statics;

using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

using static MVTaikoChecks.Aliases.Mode;
using static MVTaikoChecks.Aliases.Level;

namespace MVTaikoChecks.Checks.Timing
{
    [Check]
    public class FirstNoteStuttering : BeatmapCheck
    {
        private const string _MINOR = nameof(_MINOR);
        private const string _WARNING = nameof(_WARNING);

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Hivie",
                Category = "Timing",
                Message = "Potential stuttering at the start of the map",
                Modes = new Beatmap.Mode[] { MODE_TAIKO },
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Check if the first object's offset may cause stuttering at the start of the map."
                    },
                    {
                        "Reasoning",
                        @"
                    If the first object's offset is very early (below 150ms), it may cause stuttering at the start of the map.
                    To fix this, extend the map's audio by at least 200ms.
                        "
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>()
            {
                {
                    _MINOR,

                    new IssueTemplate(
                        LEVEL_MINOR,
                        "{0} first object's offset is between {1}ms and {2}ms, which may cause stuttering at the start of the map. Ignore if there are no issues.",
                        "timestamp -",
                        "limit",
                        "recommended"
                    ).WithCause("The object's offset is between 150ms and 200ms.")
                },
                {
                    _WARNING,

                    new IssueTemplate(
                        LEVEL_WARNING,
                        "{0} first object's offset is below {1}ms, which may cause stuttering at the start of the map (recommended: {2}ms or more)",
                        "timestamp -",
                        "limit",
                        "recommended"
                    ).WithCause("The object's offset is below the 150ms limit.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            if (beatmap.hitObjects.Count == 0)
            {
                yield break;
            }

            var firstObject = beatmap.hitObjects.First();
            int limit = 150;
            int recommended = 200;

            if (firstObject.time < 150)
            {
                yield return new Issue(
                    GetTemplate(_WARNING),
                    beatmap,
                    Timestamp.Get(firstObject.time),
                    limit,
                    recommended
                );
            }
            else if (firstObject.time < 200)
            {
                yield return new Issue(
                    GetTemplate(_MINOR),
                    beatmap,
                    Timestamp.Get(firstObject.time),
                    limit,
                    recommended
                );
            }
        }
    }
}
