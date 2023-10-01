using System.Collections.Generic;
using System.Linq;

using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
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
    public class LastNoteHidingBarlineCheck : BeatmapCheck
    {
        private const string _MINOR = nameof(_MINOR);
        private const string _PROBLEM = nameof(_PROBLEM);

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Nostril",
                Category = "Timing",
                Message = "Unsnapped last note hiding barline",
                Modes = new Beatmap.Mode[] { MODE_TAIKO },
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Check if the last object in the beatmap is not 1ms earlier than a barline."
                    },
                    {
                        "Reasoning",
                        @"
                    This causes the last barline in the map to not be rendered."
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
                        "{0} Last spinner/slider end in the map is hiding its barline, due to being unsnapped 1ms early",
                        "timestamp - "
                    ).WithCause("The spinner/slider end is unsnapped 1ms early.")
                },
                {
                    _PROBLEM,

                    new IssueTemplate(
                        LEVEL_PROBLEM,
                        "{0} Last note in the map is hiding its barline, due to being unsnapped 1ms early",
                        "timestamp - "
                    ).WithCause("The note is unsnapped 1ms early.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            if (beatmap.hitObjects.Count == 0)
            {
                yield break;
            }

            var lastObject = beatmap.hitObjects.Last();

            var unsnapFromLastBarline = lastObject.GetTailOffsetFromNextBarlineMs();

            if (unsnapFromLastBarline > -2.0 && unsnapFromLastBarline <= -1.0)
            {
                if (lastObject is Circle)
                {
                    yield return new Issue(
                        GetTemplate(_PROBLEM),
                        beatmap,
                        Timestamp.Get(lastObject.GetEndTime())
                    );
                }
                else
                {
                    yield return new Issue(
                        GetTemplate(_MINOR),
                        beatmap,
                        Timestamp.Get(lastObject.GetEndTime())
                    );
                }
            }
        }
    }
}
