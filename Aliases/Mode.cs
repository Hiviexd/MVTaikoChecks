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


namespace MVTaikoChecks.Aliases
{
    public static class Mode
    {
        public const Beatmap.Mode MODE_TAIKO = Beatmap.Mode.Taiko;
    }
}
