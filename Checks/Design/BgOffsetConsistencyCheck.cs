using System.Collections.Generic;
using System.Numerics;
using System.Text;

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
            var files = new Dictionary<string, Dictionary<Vector2?, HashSet<string>>>();
            foreach (var beatmap in beatmapSet.beatmaps)
            {
                foreach (var beatmapBg in beatmap.backgrounds) {
                    files.TryAdd(beatmapBg.path, new Dictionary<Vector2?, HashSet<string>>());
                    var file = files[beatmapBg.path];
                    file.TryAdd(beatmapBg.offset, new HashSet<string>());
                    var diffNames = file[beatmapBg.offset];
                    diffNames.Add(beatmap.metadataSettings.version);
                }
            }

            // Print any inconsistencies
            foreach (var file in files)
            {
                var fileName = file.Key;
                var offsets = file.Value;

                // If the file only has a single recorded offset, there is no inconsistency
                if (offsets.Count <= 1)
                {
                    continue;
                }

                var outputString = convertOffsetsToString(offsets);

                yield return new Issue(
                    GetTemplate(_MINOR),
                    null,
                    fileName,
                    offsets.Count,
                    outputString
                );
            }
        }

        private string convertOffsetsToString(Dictionary<Vector2?, HashSet<string>> offsets)
        {
            if (offsets == null)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            foreach (var offset in offsets) {
                sb.Append(offset.Key);
                sb.Append(" (");
                sb.Append(string.Join(", ", offset.Value));
                sb.Append("), ");
            }
            sb.Remove(sb.Length - 2, 2);    // Remove last trailing comma + space

            return sb.ToString();
        }
    }
}
