using MapsetParser.objects;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MapsetParser.objects.timinglines;

namespace MVTaikoChecks.Utils
{
    public static class OsuUtils
    {
        public static BeatmapSet GetMapset(this Beatmap beatmap)
        {
            string folderPath = Path.GetDirectoryName(beatmap.GetAudioFilePath());

            return new BeatmapSet(folderPath);
        }

#if RELEASE
#warning TODO: 100-280 BPM
#endif

        public static double GetNormalizedMsPerBeat(this UninheritedLine line)
        {
            double result = line.msPerBeat;

            while (result < (60000/270)) // 270 BPM
                result *= 2;

            while (result > (60000/110)) // 110 BPM
                result /= 2;

            return result;
        }
    }
}
