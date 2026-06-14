# Bundled example fonts

These Google **Noto** fonts are bundled **only for the example projects** — they
let the typography examples (CustomFonts, EmojisAndUnicode) embed real TrueType
fonts with broad Unicode coverage and run deterministically on any platform,
including Linux CI, without depending on machine-installed fonts (Arial, Segoe UI).

| File | Used for |
| ---- | -------- |
| `NotoSans-Regular.ttf` | Latin, Greek, currency, Latin-1 symbols |
| `NotoSansMath-Regular.ttf` | Arrows (U+2190..) and mathematical operators (U+2200..) |
| `NotoSansSymbols2-Regular.ttf` | Dingbats and miscellaneous symbols (U+2600..U+27FF) |
| `NotoEmoji-Regular.ttf` | Supplementary-plane emoji (monochrome outlines) |

**Not shipped to library consumers.** These files live under `Examples/` and are
copied into each example's build output (`CopyToOutputDirectory`). They are **not**
compiled into, or packed with, the published `BonniePDF` NuGet package — adopter
package size is unaffected.

## Licence

The Noto fonts are © The Noto Project Authors and licensed under the
**SIL Open Font License, Version 1.1** (see [`OFL.txt`](OFL.txt)), which permits
bundling and embedding. Source: https://github.com/notofonts
