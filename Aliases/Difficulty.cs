using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


namespace MVTaikoChecks.Aliases
{
    public static class Difficulty
    {
        public const Beatmap.Difficulty DIFF_KANTAN = Beatmap.Difficulty.Easy;
        public const Beatmap.Difficulty DIFF_FUTSUU = Beatmap.Difficulty.Normal;
        public const Beatmap.Difficulty DIFF_MUZU = Beatmap.Difficulty.Hard;
        public const Beatmap.Difficulty DIFF_ONI = Beatmap.Difficulty.Insane;
        public const Beatmap.Difficulty DIFF_INNER = Beatmap.Difficulty.Expert;
        public const Beatmap.Difficulty DIFF_URA = Beatmap.Difficulty.Ultra;
    }
}
