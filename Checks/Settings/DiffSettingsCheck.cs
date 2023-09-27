using MapsetParser.objects;

using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

using System;
using System.Collections.Generic;

using MVTaikoChecks.Utils;

using static MVTaikoChecks.Aliases.Difficulty;
using static MVTaikoChecks.Aliases.Level;
using static MVTaikoChecks.Aliases.Mode;

namespace MVTaikoChecks.Checks.Settings
{
    [Check]
    public class DiffSettingsCheck : BeatmapCheck
    {
        private const string _MINOR = nameof(_MINOR);
        private const string _WARNING = nameof(_WARNING);
        // private const bool _DEBUG_SEE_ALL_HP = false;
        // private const bool _DEBUG_SEE_ALL_OD = false;
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
                Author = "Hivie",
                Category = "Settings",
                Message = "OD/HP values too high/low",
                Difficulties = _DIFFICULTIES,
                Modes = new Beatmap.Mode[] { MODE_TAIKO },
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                        Preventing difficulties from going beyond or below the
                        recommended OD/HP values present in the ranking criteria.
                        "
                    },
                    {
                        "Reasoning",
                        @"
                        OD/HP values that are too high or low can cause gameplay imbalances.
                        <note>
                            The recommended settings below are based on the current ranking criteria with
                            adjustments based on mapping standards and drain time, 
                            so make sure you apply your own judgment as well.
                        </note>

                        <style type='text/css' scoped>
                            table, th, td {
                                border: 1px solid;
                                border-collapse: collapse;
                                padding: 5px;
                            }
                        </style>

                        <h3>Recommended OD settings</h3>
                        <table>
                            <tr>
                                <th>Difficulty</th>
                                <th>Value</th>
                            </tr>
                            <tr>
                                <td>Kantan OD</td>
                                <td>3</td>
                            </tr>
                            <tr>
                                <td>Futsuu OD</td>
                                <td>4</td>
                            </tr>
                            <tr>
                                <td>Muzukashii OD</td>
                                <td>5</td>
                            </tr>
                            <tr>
                                <td>Oni OD</td>
                                <td>5.5</td>
                            </tr>
                        </table>
                        
                        <h3>Recommended HP settings</h3>
                        <table>
                            <tr>
                                <th>Difficulty</th>
                                <th>Drain <= 1:00</th>
                                <th>1:00 < Drain < 3:45</th>
                                <th>3:45 <= Drain < 4:45</th>
                                <th>Drain >= 4:45</th>
                            </tr>
                            <tr>
                                <td>Kantan HP</td>
                                <td>9~10</td>
                                <td>8~9</td>
                                <td>7~8</td>
                                <td>6~7</td>
                            </tr>
                            <tr>
                                <td>Futsuu HP</td>
                                <td>8~9</td>
                                <td>7~8</td>
                                <td>6~7</td>
                                <td>5~6</td>
                            </tr>
                            <tr>
                                <td>Muzukashii HP</td>
                                <td>7~8</td>
                                <td>6~7</td>
                                <td>5~6</td>
                                <td>4~5</td>
                            </tr>
                            <tr>
                                <td>Oni HP</td>
                                <td>7~8</td>
                                <td>5.5~6.5</td>
                                <td>5~6</td>
                                <td>4~5</td>
                            </tr>
                        </table>
                        "
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>()
            {
                {
                    "hpMinor",
                    new IssueTemplate(
                        LEVEL_MINOR,
                        "HP is different from suggested value {0}, currently {1}.",
                        "limit",
                        "current"
                    ).WithCause("Current value is slightly different from the recommended limits.")
                },
                {
                    "hpWarning",
                    new IssueTemplate(
                        LEVEL_WARNING,
                        "HP is different from suggested value {0}, currently {1}. Ensure this makes sense.",
                        "limit",
                        "current"
                    ).WithCause(
                        "Current value is considerably different from the recommended limits."
                    )
                },
                {
                    "odMinor",
                    new IssueTemplate(
                        LEVEL_MINOR,
                        "OD is different from suggested value {0}, currently {1}.",
                        "limit",
                        "current"
                    ).WithCause("Current value is slightly different from the recommended limits.")
                },
                {
                    "odWarning",
                    new IssueTemplate(
                        LEVEL_WARNING,
                        "OD is different from suggested value {0}, currently {1}. Ensure this makes sense.",
                        "limit",
                        "current"
                    ).WithCause(
                        "Current value is considerably different from the recommended limits."
                    )
                },
                /*{
                    "debug",
                    new IssueTemplate(
                        LEVEL_WARNING,
                        "{0} - limit: {1} - current: {2}",
                        "setting",
                        "limit",
                        "current"
                    ).WithCause("Debug")
                }*/
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            double HP = Math.Round(beatmap.difficultySettings.hpDrain, 2, MidpointRounding.ToEven);
            double OD = Math.Round(
                beatmap.difficultySettings.overallDifficulty,
                2,
                MidpointRounding.ToEven
            );

            var recommendedOd = new Dictionary<Beatmap.Difficulty, double>()
            {
                { DIFF_KANTAN, 3 },
                { DIFF_FUTSUU, 4 },
                { DIFF_MUZU, 5 },
                { DIFF_ONI, 5.5 }
            };

            var recommendedHp = new Dictionary<Beatmap.Difficulty, double>()
            {
                { DIFF_KANTAN, 8 },
                { DIFF_FUTSUU, 7 },
                { DIFF_MUZU, 6 },
                { DIFF_ONI, 5.5 }
            };

            foreach (var diff in _DIFFICULTIES)
            {
                double drain = beatmap.GetDrainTime();
                double normalizedRecommendedHp = OsuUtils.NormalizeHpWithDrain(
                    recommendedHp[diff],
                    drain
                );

                /*if (_DEBUG_SEE_ALL_HP)
                {
                    yield return new Issue(
                        GetTemplate("debug"),
                        beatmap,
                        "HP",
                        normalizedRecommendedHp,
                        HP
                    ).ForDifficulties(diff);
                };*/

                if (Math.Abs(HP - normalizedRecommendedHp) > 1)
                {
                    yield return new Issue(
                        GetTemplate("hpWarning"),
                        beatmap,
                        normalizedRecommendedHp,
                        HP
                    ).ForDifficulties(diff);
                }
                else if (Math.Abs(HP - normalizedRecommendedHp) > 0)
                {
                    Console.WriteLine(HP);
                    yield return new Issue(
                        GetTemplate("hpMinor"),
                        beatmap,
                        normalizedRecommendedHp,
                        HP
                    ).ForDifficulties(diff);
                };

                /*if (_DEBUG_SEE_ALL_OD)
                {
                    yield return new Issue(
                        GetTemplate("debug"),
                        beatmap,
                        "OD",
                        recommendedOd[diff],
                        OD
                    ).ForDifficulties(diff);
                };*/

                if (Math.Abs(OD - recommendedOd[diff]) > 0.5)
                {
                    yield return new Issue(
                        GetTemplate("odWarning"),
                        beatmap,
                        recommendedOd[diff],
                        OD
                    ).ForDifficulties(diff);
                }
                else if (Math.Abs(OD - recommendedOd[diff]) > 0)
                {
                    yield return new Issue(
                        GetTemplate("odMinor"),
                        beatmap,
                        recommendedOd[diff],
                        OD
                    ).ForDifficulties(diff);
                };
            }
        }
    }
}
