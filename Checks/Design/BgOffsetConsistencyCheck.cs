using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using MapsetParser.objects;

using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

using static MVTaikoChecks.Aliases.Level;

namespace MVTaikoChecks.Checks.Timing
{
    [Check]
    public class BgOffsetConsistencyCheck : GeneralCheck
    {
        private const string _MINOR = nameof(_MINOR);

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Nostril",
                Category = "Design",
                Message = "Background offset inconsistency",
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Pointing out differences in background offset between difficulties."
                    },
                    {
                        "Reasoning",
                        @"
                    Background offset should generally be consistent across difficulties."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() => new Dictionary<string, IssueTemplate>()
        {
            {
                _MINOR,
                new IssueTemplate(LEVEL_MINOR,
                    "\"{0}\" has {1} unique offsets: {2}",
                    "filename",
                    "# found",
                    "enumerated list")
                .WithCause("Background offset is inconsistent across difficulties. Make sure this is intentional.")
            }
        };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            // Record known offsets for each unique BG file
            var bgOffsets = new Dictionary<string, HashSet<Vector2?>>();
            foreach (var beatmap in beatmapSet.beatmaps)
            {
                foreach (var beatmapBg in beatmap.backgrounds) {
                    bgOffsets.TryAdd(beatmapBg.path, new HashSet<Vector2?>());
                    bgOffsets[beatmapBg.path].Add(beatmapBg.offset);
                }
            }

            // Check if any unique BG file has multiple recorded offsets (this means there is an inconsistency)
            foreach (var bgOffset in bgOffsets)
            {
                if (bgOffset.Value.Count > 1)
                {
                    yield return new Issue(
                        GetTemplate(_MINOR),
                        null,
                        bgOffset.Key,
                        bgOffset.Value.Count,
                        string.Join(", ", bgOffset.Value)
                    );
                }
            }
        }
    }
}
