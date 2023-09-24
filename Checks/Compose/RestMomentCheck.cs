using System;
using System.Collections.Generic;
using System.Linq;

using MapsetParser.objects;
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
    public class RestMomentCheck : BeatmapCheck
    {
        private const string _MINOR = nameof(_MINOR);
        private const string _WARNING = nameof(_WARNING);
        private const bool _DEBUG_SEE_ALL_CONTINUOUS_MAPPING = false;

        private readonly Beatmap.Difficulty[] _DIFFICULTIES = new Beatmap.Difficulty[] { DIFF_KANTAN, DIFF_FUTSUU, DIFF_MUZU, DIFF_ONI };

        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Author = "Hivie, Phob, Nostril",
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
                    @"
                    Ensuring that a chain of objects doesn't exceed a certain threshold without a required rest moment."
                },
                {
                    "Reasoning",
                    @"
                    Abnormally long chains without proper rest moments can be very straining to play."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates() => new Dictionary<string, IssueTemplate>()
        {
            {
                _MINOR,

                new IssueTemplate(LEVEL_MINOR,
                    "{0} {1} No {2} rest moments for {3}, ensure this makes sense",
                    "start", "end", "break", "length")
                .WithCause("Chain length is surpassing the RC guideline, but not excessively")
            },

            {
                _WARNING,

                new IssueTemplate(LEVEL_WARNING,
                    "{0} {1} No {2} rest moments for {3}, ensure this makes sense",
                    "start", "end", "break", "length")
                .WithCause("Chain length is excessively surpassing the RC guideline")
            }
        };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var objects = beatmap.hitObjects.ToList();

            // for each diff: acceptable versions of { # of consecutive gaps required, number of beats required per gap }
            var minimalGapBeats = new Dictionary<Beatmap.Difficulty, Dictionary<int, double>>()
            {
                { DIFF_KANTAN, new Dictionary<int, double>() {
                    { 1, 3.0 }
                }},
                { DIFF_FUTSUU, new Dictionary<int, double>() {
                    { 1, 2.0 }
                }},
                { DIFF_MUZU, new Dictionary<int, double>() {
                    { 1, 1.5 },
                    { 3, 1.0 },
                }},
                { DIFF_ONI, new Dictionary<int, double>() {
                    { 1, 1.0 }
                }}
            };

            // for each diff: string to output describing rest moment requirements
            var breakTypes = new Dictionary<Beatmap.Difficulty, string>()
            {
                { DIFF_KANTAN, "3/1"},
                { DIFF_FUTSUU, "2/1"},
                { DIFF_MUZU, "3/2 or three consecutive 1/1"},
                { DIFF_ONI, "1/1"}
            };

            // for each diff: string to output describing continuous mapping limitations (minor issue severity)
            var continuousMappingMinorLimit = new Dictionary<Beatmap.Difficulty, double>()
            {
                { DIFF_KANTAN, 36},
                { DIFF_FUTSUU, 36},
                { DIFF_MUZU, 20},
                { DIFF_ONI, 20}
            };

            // for each diff: string to output describing continuous mapping limitations (warning severity)
            var continuousMappingWarningLimit = new Dictionary<Beatmap.Difficulty, double>()
            {
                { DIFF_KANTAN, 44},
                { DIFF_FUTSUU, 44},
                { DIFF_MUZU, 32},
                { DIFF_ONI, 32}
            };

            foreach (var diff in _DIFFICULTIES)
            {
                var currentContinuousSectionStartTimeMs = objects.FirstOrDefault()?.time ?? 0;
                for (int i = 0; i < objects.Count; i++)
                {
                    var current = objects.SafeGetIndex(i);
                    var timing = beatmap.GetTimingLine<UninheritedLine>(current.time);
                    var normalizedMsPerBeat = timing.GetNormalizedMsPerBeat();

                    // identify boundaries of continuous mapping
                    var isBeginningOfContinuousMapping = false;
                    var isEndOfContinuousMapping = false;
                    foreach (var acceptableRestMoment in minimalGapBeats[diff])
                    {
                        // convert minimal gap beats to milliseconds
                        var minimalRestMomentGapMs = acceptableRestMoment.Value * normalizedMsPerBeat;

                        // check if this is beginning of continuous mapping
                        var smallestConsecutiveGapMs = double.MaxValue;
                        for (int j = 0; j < acceptableRestMoment.Key; j++)
                        {
                            // out of bounds check
                            if (i - j - 1 < 0)
                            {
                                continue;
                            }

                            var gapBeginObject = objects.SafeGetIndex(i - j - 1);
                            var gapEndObject = objects.SafeGetIndex(i - j);

                            var gap = gapEndObject.time - gapBeginObject.GetEndTime();
                            smallestConsecutiveGapMs = Math.Min(smallestConsecutiveGapMs, gap);
                        }

                        // check if the backward-looking current string of notes is a rest moment
                        if (smallestConsecutiveGapMs + MS_EPSILON >= minimalRestMomentGapMs)
                        {
                            isBeginningOfContinuousMapping = true;
                        }

                        // check if this is end of continuous mapping
                        smallestConsecutiveGapMs = double.MaxValue;
                        for (int j = 0; j < acceptableRestMoment.Key; j++)
                        {
                            // out of bounds check
                            if (i + j + 1 >= objects.Count)
                            {
                                continue;
                            }

                            var gapBeginObject = objects.SafeGetIndex(i + j);
                            var gapEndObject = objects.SafeGetIndex(i + j + 1);

                            var gap = gapEndObject.time - gapBeginObject.GetEndTime();
                            smallestConsecutiveGapMs = Math.Min(smallestConsecutiveGapMs, gap);
                        }

                        // check if the forward-looking current string of notes is a rest moment
                        if (smallestConsecutiveGapMs + MS_EPSILON >= minimalRestMomentGapMs)
                        {
                            isEndOfContinuousMapping = true;
                        }
                    }

                    // check if this is the first note of a continuously mapped section, if so record the timestamp for later once we find the end
                    if (isBeginningOfContinuousMapping)
                    {
                        currentContinuousSectionStartTimeMs = current.time;
                    }

                    // check if this is the last note of a continuously mapped section, if so check if it's too long
                    if (isEndOfContinuousMapping)
                    {
                        var continuouslyMappedDurationMs = current.GetEndTime() - currentContinuousSectionStartTimeMs;

                        double beatsWithoutBreaks = Math.Floor((continuouslyMappedDurationMs + MS_EPSILON) / normalizedMsPerBeat);

                        if (beatsWithoutBreaks > 0 && _DEBUG_SEE_ALL_CONTINUOUS_MAPPING)
                        {
                            yield return new Issue(
                                GetTemplate(_WARNING),
                                beatmap,
                                Timestamp.Get(currentContinuousSectionStartTimeMs).Trim() + ">",
                                Timestamp.Get(current.GetEndTime()),
                                breakTypes[diff],
                                $"{beatsWithoutBreaks}/1"
                            ).ForDifficulties(diff);
                        }
                        else if (beatsWithoutBreaks > continuousMappingWarningLimit[diff])
                        {
                            yield return new Issue(
                                GetTemplate(_WARNING),
                                beatmap,
                                Timestamp.Get(currentContinuousSectionStartTimeMs).Trim() + ">",
                                Timestamp.Get(current.time),
                                breakTypes[diff],
                                $"{beatsWithoutBreaks}/1"
                            ).ForDifficulties(diff);
                        }
                        else if (beatsWithoutBreaks > continuousMappingMinorLimit[diff])
                        {
                            yield return new Issue(
                                GetTemplate(_MINOR),
                                beatmap,
                                Timestamp.Get(currentContinuousSectionStartTimeMs).Trim() + ">",
                                Timestamp.Get(current.time),
                                breakTypes[diff],
                                $"{beatsWithoutBreaks}/1"
                            ).ForDifficulties(diff);
                        }
                    }
                }
            }
        }
    }
}
