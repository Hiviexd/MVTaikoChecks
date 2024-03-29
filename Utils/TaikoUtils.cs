﻿using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using System;

using static MVTaikoChecks.Utils.GeneralUtils;

namespace MVTaikoChecks.Utils
{
    public static class TaikoUtils
    {
        public static bool IsDon(this HitObject hitObject)
        {
            if (!(hitObject is Circle))
            {
                return false;
            }
            return !hitObject.HasHitSound(HitObject.HitSound.Whistle) && !hitObject.HasHitSound(HitObject.HitSound.Clap);
        }
        public static bool IsFinisher(this HitObject hitObject)
        {
            return hitObject.HasHitSound(HitObject.HitSound.Finish);
        }

        public static bool IsMono(this HitObject hitObject)
        {
            return (hitObject.Prev()?.IsDon() ?? null) == hitObject.IsDon();
        }

        public static bool IsAtBeginningOfPattern(this HitObject current)
        {
            var previous = current.Prev(true);
            var next = current.Next(true);

            // if there aren't circles immediately before this object, then this is the start of a pattern
            if (previous == null || !(previous is Circle))
            {
                return true;
            }

            // if there are circles immediately before but not after this object, then this is not the start of a pattern
            if (next == null || !(next is Circle))
            {
                return false;
            }

            var gapBeforeMs = current.time - previous.time;
            var gapAfterMs = next.time - current.time;

            // if there are circles both immediately before and after this object, then it's the start if the snap divisor after this note is lower than before it
            return gapAfterMs < gapBeforeMs;
        }

        public static bool IsAtEndOfPattern(this HitObject current)
        {
            var previous = current.Prev(true);
            var next = current.Next(true);

            // if there aren't circles immediately after this object, then this is the end of a pattern
            if (next == null || !(next is Circle))
            {
                return true;
            }

            // if there are circles immediately after but not before this object, then this is not the end of a pattern
            if (previous == null || !(previous is Circle))
            {
                return false;
            }

            var gapBeforeMs = current.time - previous.time;
            var gapAfterMs = next.time - current.time;

            // if there are circles both immediately before and after this object, then it's the end if the snap divisor before this note is lower than after it
            return gapBeforeMs < gapAfterMs;
        }

        public static bool IsInMiddleOfPattern(this HitObject current)
        {
            return !current.IsAtBeginningOfPattern() && !current.IsAtEndOfPattern();
        }

        public static bool IsNotInPattern(this HitObject current)
        {
            return current.IsAtBeginningOfPattern() && current.IsAtEndOfPattern();
        }

        public static double GetPatternSpacingMs(this HitObject current)
        {
            var previous = current.Prev(true);
            var next = current.Next(true);

            var gapBeforeMs = current.time - (previous?.time ?? 0);
            var gapAfterMs = (next?.time ?? double.MaxValue) - current.time;

            if (current.IsNotInPattern())
            {
                return 0;
            }

            if (current.IsAtBeginningOfPattern())
            {
                return gapAfterMs;
            }

            if (current.IsAtEndOfPattern())
            {
                return gapBeforeMs;
            }

            // if in middle of pattern, pattern spacing is based on which side has the smaller snap divisor
            return Math.Min(gapBeforeMs, gapAfterMs);
        }

        public static double GetOffsetFromPrevBarlineMs(Beatmap beatmap, double time)
        {
            var timing = beatmap.GetTimingLine<UninheritedLine>(time);
            var barlineGap = timing.msPerBeat * timing.meter;

            return (time - timing.offset) % barlineGap;
        }

        public static double GetOffsetFromNextBarlineMs(Beatmap beatmap, double time)
        {
            var timing = beatmap.GetTimingLine<UninheritedLine>(time);
            var barlineGap = timing.msPerBeat * timing.meter;

            var offsetFromNextImplicitBarline = ((time - timing.offset) % barlineGap) - barlineGap;
            var nextPotentialRedLine = beatmap.GetTimingLine<UninheritedLine>(time + offsetFromNextImplicitBarline);
            var offsetFromNextPotentialRedline = time - nextPotentialRedLine.offset;

            return TakeLowerAbsValue(offsetFromNextImplicitBarline, offsetFromNextPotentialRedline);
        }

        public static double GetOffsetFromNearestBarlineMs(Beatmap beatmap, double time)
        {
            return TakeLowerAbsValue(GetOffsetFromPrevBarlineMs(beatmap, time), GetOffsetFromNextBarlineMs(beatmap, time));
        }

        public static double GetHeadOffsetFromPrevBarlineMs(this HitObject current) => GetOffsetFromPrevBarlineMs(current.beatmap, current.time);

        public static double GetHeadOffsetFromNextBarlineMs(this HitObject current) => GetOffsetFromNextBarlineMs(current.beatmap, current.time);

        public static double GetHeadOffsetFromNearestBarlineMs(this HitObject current) => GetOffsetFromNearestBarlineMs(current.beatmap, current.time);

        public static double GetTailOffsetFromPrevBarlineMs(this HitObject current) => GetOffsetFromPrevBarlineMs(current.beatmap, current.GetEndTime());

        public static double GetTailOffsetFromNextBarlineMs(this HitObject current) => GetOffsetFromNextBarlineMs(current.beatmap, current.GetEndTime());

        public static double GetTailOffsetFromNearestBarlineMs(this HitObject current) => GetOffsetFromNearestBarlineMs(current.beatmap, current.GetEndTime());

        public static bool IsBottomDiffKantan(this BeatmapSet beatmapSet)
        {
            foreach (var beatmap in beatmapSet.beatmaps)
            {
                if (beatmap.GetDifficulty(true) == Beatmap.Difficulty.Easy)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
