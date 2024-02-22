using System.Collections.Generic;
using System.Linq;

using MapsetParser.objects;

using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

using MVTaikoChecks.Utils;

using static MVTaikoChecks.Aliases.Mode;
using static MVTaikoChecks.Aliases.Level;

namespace MVTaikoChecks.Checks.Timing
{
    [Check]
    public class KiaiConsistencyCheck : GeneralCheck
    {
        private const string _MINOR = nameof(_MINOR);

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "SN707, Nostril",
                Category = "Compose",
                Message = "Kiai consistency",
                Modes = new Beatmap.Mode[] { MODE_TAIKO },
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Pointing out differences in kiai between difficulties."
                    },
                    {
                        "Reasoning",
                        @"
                    Kiais should generally be consistent across difficulties (except in GDs)."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() => new Dictionary<string, IssueTemplate>()
        {
            {
                _MINOR,
                new IssueTemplate(LEVEL_MINOR,
                    "Kiai is inconsistent across difficulties.")
                .WithCause("Kiai start and end times are not aligned across difficulties.")
            }
        };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            // Store all kiai times in a 2D List
            var mapsetKiais = new List<List<double>>();

            foreach (var beatmap in beatmapSet.beatmaps)
            {
                var beatmapKiais = beatmap.timingLines.FindKiaiToggles().Select(x => x.offset).ToList();
                mapsetKiais.Add(beatmapKiais);
            }

            List<double> firstDiffKiaiSet = mapsetKiais[0];

            // Now we compare the first kiai times list to each of the remaining lists in the 2D array
            for (int i = 1; i < mapsetKiais.Count; i++)
            {
                List<double> currentDiffKiaiSet = mapsetKiais[i];
                var intersectKiaiSet = firstDiffKiaiSet.Intersect(currentDiffKiaiSet).ToList();
                if (firstDiffKiaiSet.Count != intersectKiaiSet.Count || currentDiffKiaiSet.Count != intersectKiaiSet.Count)
                {
                    // intersection, current diff, and lowest diff toggles are not equal, so kiai is inconsistent.
                    // Emit minor issue + finish.
                    yield return new Issue(
                        GetTemplate(_MINOR),
                        null
                    );
                    break;
                }
            }
        }
    }
}
