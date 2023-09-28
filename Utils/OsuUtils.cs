using MapsetParser.objects;
using MapsetParser.objects.timinglines;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace MVTaikoChecks.Utils
{
    public static class OsuUtils
    {
        public static BeatmapSet GetMapset(this Beatmap beatmap)
        {
            string folderPath = Path.GetDirectoryName(beatmap.GetAudioFilePath());

            return new BeatmapSet(folderPath);
        }

        public static double GetNormalizedMsPerBeat(this UninheritedLine line)
        {
            double result = line.msPerBeat;

            while (result <= (60000 / 270)) // 270 BPM
                result *= 2;

            while (result >= (60000 / 110)) // 110 BPM
                result /= 2;

            while (result >= (60000 / 130)) // 130 BPM
                result /= 1.5;

            return result;
        }

        public static double NormalizeHpWithDrain(double hp, double drain)
        {
            if (drain <= 60 * 1000) // 1:00 or less gets a HP buff by 1
                return Math.Ceiling(hp + 1); // rounding up to avoid decimal values
            if (drain >= (4 * 60 * 1000) + (45 * 1000)) // 4:45 ore more gets a HP nerf by 2
                return Math.Ceiling(hp - 2);
            if (drain >= (3 * 60 * 1000) + (45 * 1000)) // 3:45 or more gets a HP nerf by 1
                return Math.Ceiling(hp - 1);
            return hp;
        }

        public static List<TimingLine> FindKiaiToggles(this List<TimingLine> timingLines)
        {
            List<TimingLine> kiaiToggles = new List<TimingLine>();

            TimingLine previousTimingLine = null;

            foreach (TimingLine line in timingLines)
            {
                if ((previousTimingLine == null && line.kiai) || (previousTimingLine != null && previousTimingLine.kiai != line.kiai))
                {
                    kiaiToggles.Add(line);
                }
                previousTimingLine = line;
            }
            return kiaiToggles;
        }
    }
}
