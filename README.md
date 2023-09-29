# MVTaikoChecks

A set of osu!taiko specific [Mapset Verifier](https://github.com/Naxesss/MapsetVerifier) checks

> **Warning**
> This plugin is still in heavy development, so [false positives and false negatives](https://en.wikipedia.org/wiki/False_positives_and_false_negatives) may occur. If you find something that isn't right, please refer to the *[Feature requests & bug reports](#feature-requests--bug-reports)* section.
> **ALWAYS APPLY YOUR OWN JUDGEMENT ON EVERY CHECK AND DON'T BLINDLY FOLLOW THEM.**

## Features

### BPM scaling

Every check tries to compensate for BPM scaling, however this is not perfect and may cause false positives in very high or low BPM maps. **Always use your manual judgement with every check.**

| BPM | Action |
| :-- | :-- |
| BPM <= 110 | effective BPM is multiplied by 2 |
| 110 < BPM <= 130 | effective BPM multiplied by 1.5 |
| 130 < BPM < 270 | effective BPM is unchanged |
| BPM >= 270 | effective BPM is divided by 2 |

> **Important**
> This unfortunately **does not** work on double/half BPM-style maps that don't change the actual BPM value, so it will cause false positives.

### Available checks

- Double barlines
- Rest moments
- Unrankable finishers[^note-unstable]
- Abnormal note gaps
- Spinner gap
- OD/HP settings
- Kiai flashes

### Planned checks

- Extreme SV changes/SV jumps in lower difficulties

## Installing

- Download the [latest release](https://github.com/Hiviexd/MVTaikoChecks/releases/latest) of `MVTaikoChecks.dll` 
- Click the settings icon (top right) in Mapset Verifier
- Scroll down to the `Shortcuts` section
- Click the `Open externals folder` icon
- Open the `checks` folder
- Place the `MVTaikoChecks.dll` file in this folder
- Restart Mapset Verifier

## Known issues

- Difficulties with custom names are always marked as "Easy" so make sure you manually change that to the correct diff, else you'll have an insane amount of flase positives

## Feature requests & bug reports

If you have any feature requests or an issue to report, please open an issue or reach out to one of the active maintainers below:

- **Hivie** (osu!: [Hivie](https://osu.ppy.sh/users/14102976) | Discord: `hivie`)
- **Nostril** (osu!: [Nostril](https://osu.ppy.sh/users/11479122) | Discord: `nostril`)

## Contributing

If you're here to contribute, please open an issue to discuss your idea before you start working on it.

### Special thanks

- [phob144](https://github.com/phob144) for being integral to development in early stages

## Notes

[^note-unstable]: This check is currently unstable and may cause false positives.
