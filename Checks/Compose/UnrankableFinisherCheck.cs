using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using MVTaikoChecks.Utils;
using System.Collections.Generic;
using System.Linq;
using static MVTaikoChecks.Global;
using static MVTaikoChecks.Aliases.Difficulty;
using static MVTaikoChecks.Aliases.Level;
using static MVTaikoChecks.Aliases.Mode;

namespace MVTaikoChecks.Checks.Compose
{
    [Check]
    public class UnrankableFinisherCheck : BeatmapCheck
    {
        private const string _WARNING = nameof(_WARNING);
        private const string _PROBLEM = nameof(_PROBLEM);

        private readonly Beatmap.Difficulty[] _DIFFICULTIES = new Beatmap.Difficulty[]
        {
            DIFF_KANTAN,
            DIFF_FUTSUU,
            DIFF_MUZU,
            DIFF_ONI,
            DIFF_INNER,
            DIFF_HELL
        };

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Hivie & Nostril",
                Category = "Compose",
                Message = "Unrankable finishers",
                Difficulties = _DIFFICULTIES,
                Modes = new Beatmap.Mode[] { MODE_TAIKO },
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Ensuring that finishers abide by each difficulty's Ranking Criteria."
                    },
                    {
                        "Reasoning",
                        @"
                    Improper finisher usage can lead to significant gameplay issues."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>()
            {
                {
                    _WARNING,
                    new IssueTemplate(
                        LEVEL_WARNING,
                        "{0} Abnormal finisher, ensure this makes sense",
                        "timestamp - "
                    ).WithCause("Finisher is potentially violating the Ranking Criteria")
                },
                {
                    _PROBLEM,
                    new IssueTemplate(
                        LEVEL_PROBLEM,
                        "{0} Unrankable finisher",
                        "timestamp - "
                    ).WithCause("Finisher is violating the Ranking Criteria")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var finishers = beatmap.hitObjects.Where(x => x is Circle && x.IsFinisher()).ToList();

            // any finisher pattern spacing equal to or smaller than this gap is a problem
            var maximalGapBeats = new Dictionary<Beatmap.Difficulty, double>()
            {
                { DIFF_KANTAN,  1.0/2 },
                { DIFF_FUTSUU,  1.0/3 },
                { DIFF_MUZU,    1.0/4 },
                { DIFF_ONI,     1.0/6 }
            };

            // any finisher pattern spacing equal to or smaller than this gap is a problem
            var maximalGapBeatsWarning = new Dictionary<Beatmap.Difficulty, double>() { };

            // any finisher pattern spacing equal to or smaller than this gap without a color change before is a problem
            var maximalGapBeatsRequiringColorChangeBefore = new Dictionary<Beatmap.Difficulty, double>()
            {
                { DIFF_ONI,     1.0/4 }
            };

            // any finisher pattern spacing equal to or smaller than this gap without a color change after is a problem
            var maximalGapBeatsRequiringColorChangeAfter = new Dictionary<Beatmap.Difficulty, double>() { };

            // any finisher pattern spacing equal to or smaller than this gap without a color change before is a warning
            var maximalGapBeatsRequiringColorChangeBeforeWarning = new Dictionary<Beatmap.Difficulty, double>()
            {
                { DIFF_INNER,   1.0/3 },
                { DIFF_HELL,    1.0/3 }
            };

            // any finisher pattern spacing equal to or smaller than this gap without a color change after is a warning
            var maximalGapBeatsRequiringColorChangeAfterWarning = new Dictionary<Beatmap.Difficulty, double>()
            {
                { DIFF_INNER,   1.0/3 },
                { DIFF_HELL,    1.0/3 }
            };

            // any finisher pattern spacing equal to or smaller than this gap while not being at the end of the pattern is a problem
            var maximalGapBeatsRequiringFinalNote = new Dictionary<Beatmap.Difficulty, double>()
            {
                { DIFF_ONI,     1.0/4 }
            };

            // any finisher pattern spacing equal to or smaller than this gap while not being at the end of the pattern is a warning
            var maximalGapBeatsRequiringFinalNoteWarning = new Dictionary<Beatmap.Difficulty, double>()
            {
                { DIFF_INNER,   1.0/3 },
                { DIFF_HELL,    1.0/3 }
            };

            foreach (var diff in _DIFFICULTIES)
            {
                foreach (var current in finishers)
                {
                    var sameColorBefore = current.IsMono();
                    var sameColorAfter = current.Next()?.IsMono() ?? false;
                    var isInPattern = !current.IsNotInPattern();
                    var isFirstNote = current.IsAtBeginningOfPattern();
                    var isFinalNote = current.IsAtEndOfPattern();

                    // check for unrankable finishers (problem)
                    if ((checkGap(diff, maximalGapBeats, beatmap, current) && isInPattern) ||
                        (checkGap(diff, maximalGapBeatsRequiringColorChangeBefore, beatmap, current) && sameColorBefore && !isFirstNote) ||
                        (checkGap(diff, maximalGapBeatsRequiringColorChangeAfter, beatmap, current) && sameColorAfter && !isFinalNote) ||
                        (checkGap(diff, maximalGapBeatsRequiringFinalNote, beatmap, current) && !isFinalNote)) {
                        yield return new Issue(
                            GetTemplate(_PROBLEM),
                            beatmap,
                            Timestamp.Get(current.time)
                        ).ForDifficulties(diff);
                        continue;
                    }

                    // check for abnormal finishers (warning)
                    if ((checkGap(diff, maximalGapBeatsWarning, beatmap, current) && isInPattern) ||
                        (checkGap(diff, maximalGapBeatsRequiringColorChangeBeforeWarning, beatmap, current) && sameColorBefore && !isFirstNote) ||
                        (checkGap(diff, maximalGapBeatsRequiringColorChangeAfterWarning, beatmap, current) && sameColorAfter && !isFinalNote) ||
                        (checkGap(diff, maximalGapBeatsRequiringFinalNoteWarning, beatmap, current) && !isFinalNote)) {
                        yield return new Issue(
                            GetTemplate(_WARNING),
                            beatmap,
                            Timestamp.Get(current.time)
                        ).ForDifficulties(diff);
                        continue;
                    }
                }
            }
            yield break;
        }

        private bool checkGap(
            Beatmap.Difficulty diff,
            Dictionary<Beatmap.Difficulty, double> maximalGapBeats,
            Beatmap beatmap,
            HitObject current)
        {
            // if check isn't relevant to this diff, don't check it
            if (!maximalGapBeats.ContainsKey(diff))
            {
                return false;
            }

            // convert maximal gap from # beats to milliseconds
            var timing = beatmap.GetTimingLine<UninheritedLine>(current.time);
            var maximalGapMs = maximalGapBeats[diff] * timing.msPerBeat;

            if (current.GetPatternSpacingMs() <= maximalGapMs + MS_EPSILON)
            {
                return true;
            }

            return false;
        }
    }
}
