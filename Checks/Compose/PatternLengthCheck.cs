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
    public class PatternLengthCheck : BeatmapSetCheck
    {
        private const string _MINOR = nameof(_MINOR);
        private const string _WARNING = nameof(_WARNING);
        private const bool _DEBUG_SEE_ALL_PATTERN_LENGTHS = false;

        private readonly Beatmap.Difficulty[] _DIFFICULTIES = new Beatmap.Difficulty[]
        {
            DIFF_KANTAN,
            DIFF_FUTSUU,
            DIFF_MUZU,
            DIFF_ONI
        };

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "SN707",
                Category = "Compose",
                Message = "Pattern Lengths",
                Difficulties = _DIFFICULTIES,
                Modes = new Beatmap.Mode[] { MODE_TAIKO },
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Preventing patterns from being too long based on each difficulty's Ranking Criteria."
                    },
                    {
                        "Reasoning",
                        @"
                    On lower difficulties, patterns of smaller snaps can get too straining if they surpass a certain length."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() => new Dictionary<string, IssueTemplate>()
        {
            {
                _MINOR,

                new IssueTemplate(LEVEL_MINOR,
                    "{0} {1} {2} pattern is {3} notes long.",
                    "start", "end", "snap", "number")
                .WithCause("Pattern length is equal to the RC guideline")
            },


            {
                _WARNING,
                new IssueTemplate(LEVEL_WARNING,
                    "{0} {1} {2} pattern is {3} notes long, ensure this makes sense.",
                    "start", "end", "snap", "number")
                .WithCause("Pattern length is surpassing the RC guideline.")
            }
        };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {

            // Difficulty, <snap size, snap count>
            var shortSnapParams = new Dictionary<Beatmap.Difficulty, Dictionary<double, int>>()
            {
                { DIFF_KANTAN, new Dictionary<double, int>() {
                    { 1.0 / 1, 7 },
                    { 1.0 / 2, 2 }
                }},
                { DIFF_FUTSUU, new Dictionary< double, int>() {
                    { 1.0 / 2, beatmapSet.IsBottomDiffKantan() ? 7 : 5 }, // If no kantan, then recommended maximum 1/2 length for futsuu is 5 instead of 7
                    { 1.0 / 3, 2 }
                }},
                { DIFF_MUZU, new Dictionary<double, int>() {
                    { 1.0 / 4, 5 }, 
                    { 1.0 / 6, 4 }, 
                }},
                { DIFF_ONI, new Dictionary<double, int>() {
                    { 1.0 / 4, 9 },
                    { 1.0 / 6, 4 },
                    { 1.0 / 8, 2 }, 
                }},
            };

            // Displays the fractions for output string
            var outputDict = new Dictionary<double, String>()
            {
                { 1.0 / 1, "1/1" },
                { 1.0 / 2, "1/2" },
                { 1.0 / 3, "1/3" },
                { 1.0 / 4, "1/4" },
                { 1.0 / 6, "1/6" },
                { 1.0 / 8, "1/8" }
            };

            foreach (var beatmap in beatmapSet.beatmaps)
            {
                var objects = beatmap.hitObjects.Where(x => x is Circle).ToList();

                foreach (var diff in _DIFFICULTIES)
                {
                    foreach (var snapValues in shortSnapParams[diff])
                    {
                        var currentPatternStartTimeMs = objects.FirstOrDefault()?.time ?? 0;
                        var currentPatternEndTimeMs = objects.FirstOrDefault()?.time ?? 0;

                        // variables to identify length of pattern
                        var patternStartIndex = 0;
                        var foundStartOfPattern = false;
                        var foundEndOfPattern = false;
                        for (int i = 0; i < objects.Count; i++)
                        {
                            var current = objects.SafeGetIndex(i);
                            var timing = beatmap.GetTimingLine<UninheritedLine>(current.time);
                            var normalizedMsPerBeat = timing.GetNormalizedMsPerBeat();

                            // convert minimal gap beats to milliseconds
                            var snapMs = snapValues.Key * normalizedMsPerBeat;


                            // check if this is end of pattern
                            if (i + 1 < objects.Count && foundStartOfPattern)
                            {
                                var gapBeginObject = objects.SafeGetIndex(i);
                                var gapEndObject = objects.SafeGetIndex(i + 1);

                                // Check if gap is greater than the snap size
                                var gap = gapEndObject.time - gapBeginObject.GetEndTime();
                                if (gap - MS_EPSILON > snapMs)
                                {
                                    foundEndOfPattern = true;
                                    currentPatternEndTimeMs = gapBeginObject.time;
                                }
                            } else if (i == objects.Count - 1 && foundStartOfPattern)
                            {
                                // last note, so forced end of pattern
                                foundEndOfPattern = true;
                                currentPatternEndTimeMs = objects.SafeGetIndex(i).time;
                            }

                            // check if this is start of pattern
                            if (i + 1 < objects.Count && !foundStartOfPattern)
                            {
                                var gapBeginObject = objects.SafeGetIndex(i);
                                var gapEndObject = objects.SafeGetIndex(i + 1);

                                // Check if gap is smaller than or equal to the snap size
                                var gap = gapEndObject.time - gapBeginObject.GetEndTime();
                                if (gap - MS_EPSILON <= snapMs)
                                {
                                    foundStartOfPattern = true;
                                    currentPatternStartTimeMs = gapBeginObject.time;
                                    patternStartIndex = i;
                                }
                            }

                            // check if this is the last note of a pattern, and if so check if it's too long
                            if (foundEndOfPattern && foundStartOfPattern)
                            {
                                foundEndOfPattern = false;
                                foundStartOfPattern = false; // resume checking for start of pattern
                                var durationOfPattern = i - patternStartIndex + 1;


                                if (durationOfPattern > 1 && _DEBUG_SEE_ALL_PATTERN_LENGTHS)
                                {
                                    yield return new Issue(
                                        GetTemplate(_WARNING),
                                        beatmap,
                                        Timestamp.Get(currentPatternStartTimeMs).Trim() + ">",
                                        Timestamp.Get(currentPatternEndTimeMs).Trim(),
                                        outputDict[snapValues.Key] ?? "unknown snap",
                                        durationOfPattern

                                    ).ForDifficulties(diff);
                                }
                                else if (durationOfPattern > snapValues.Value)
                                {
                                    yield return new Issue(
                                        GetTemplate(_WARNING),
                                        beatmap,
                                        Timestamp.Get(currentPatternStartTimeMs).Trim() + ">",
                                        Timestamp.Get(currentPatternEndTimeMs).Trim(),
                                        outputDict[snapValues.Key] ?? "unknown snap",
                                        durationOfPattern
                                    ).ForDifficulties(diff);
                                } else if (durationOfPattern == snapValues.Value)
                                {
                                    yield return new Issue(
                                        GetTemplate(_MINOR),
                                        beatmap,
                                        Timestamp.Get(currentPatternStartTimeMs).Trim() + ">",
                                        Timestamp.Get(currentPatternEndTimeMs).Trim(),
                                        outputDict[snapValues.Key] ?? "unknown snap",
                                        durationOfPattern
                                    ).ForDifficulties(diff);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
