using System.Collections.Generic;
using System.Linq;

using MapsetParser.objects;

using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

using MVTaikoChecks.Utils;

using static MVTaikoChecks.Aliases.Mode;
using static MVTaikoChecks.Aliases.Level;
using System;

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
                Message = "Kiai inconsistencies",
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
                    "{0}",
                    "List of Difficulty Names")
                .WithCause("Kiai start and end times are not aligned across difficulties.")
            }
        };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            // Store all kiai times in a Dictionary
            var mapsetKiais = new Dictionary<string, List<string>>();

            foreach (var beatmap in beatmapSet.beatmaps)
            {
                var beatmapKiais = string.Join(',', beatmap.timingLines.FindKiaiToggles().Select(x => x.offset));
                mapsetKiais.TryAdd(beatmapKiais, new List<string>());
                mapsetKiais[beatmapKiais].Add(beatmap.metadataSettings.version);
            }

            int mapsetKiaiSetCount = mapsetKiais.Count;
            if (mapsetKiaiSetCount > 1)
            {
                // At least 2 different 'sets' of kiais
                // Emit each of them and exit
                var groupStrings = new List<string>();
                List<List<string>> diffNameStrings = mapsetKiais.Values.ToList();
                for (int i = 0; i < mapsetKiaiSetCount; i++)
                {
                    yield return new Issue(
                    GetTemplate(_MINOR),
                    null,
                    "Group " + (i + 1).ToString() + ": (" + string.Join(", ", diffNameStrings[i]) + ")"
                    );
                }
            }
        }

    }
}
