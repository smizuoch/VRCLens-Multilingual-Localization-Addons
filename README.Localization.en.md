# VRCLens Multilingual Localization Add-ons

Standalone Composition Guides support adds 76 labels for all 26 locales. Existing language prefabs,
including Japanese, select the guide language as well. A selected localization overrides the guide's
built-in Japanese/English setting on the build copy. No localization prefab means the standalone
language setting remains in effect. Additional non-Japanese translations are authored drafts pending
native review; Japanese matches the supplied author's labels.
See [compatibility notes](Documentation/COMPOSITION_GUIDES_COMPATIBILITY.md).
[日本語版 (README.Localization.md)](README.Localization.md)

This VRCFury add-on translates the VRCLens Expressions Menu and any installed
Free Camera Add-ons at build time only. It supports 26 languages and locales.
Some translations still need source, sense, or regional usage review.
See [terminology status](Documentation/TERMINOLOGY.md).

## Requirements

- VRCLens (tested with 1.10.0) — [https://hirabiki.gumroad.com/l/rpnel](https://hirabiki.gumroad.com/l/rpnel)
- VRCFury (tested with 1.1427.0) — [https://vrcfury.com/](https://vrcfury.com/)
- Free Camera Add-ons (optional) — [https://booth.pm/ja/items/8375173](https://booth.pm/ja/items/8375173)
- VRChat SDK Avatars (a prerequisite for VRCLens and VRCFury)
- Unity 2022.3.22f1

Free Camera Add-ons are optional. If they are not installed, only VRCLens itself
is translated. If they are installed, the `VRCL_Custom/*` menus integrated by
VRCFury are translated as part of the same process. This unitypackage does not
include VRCLens, VRCFury, Free Camera Add-ons, paid assets, menus, animations, or
icons. Localization uses its own Editor assembly under `Editor/Localization` and
a separate marker assembly under `Runtime`. These assembly definitions remain
independent of Free Camera Add-ons; its asmdef does not need to be changed.
The SDK strips them before upload through `IEditorOnly`. Keep these scripts outside
`Editor` folders so Unity can load their serialized prefab components.

## Usage

Place exactly one of the following prefabs directly under the avatar's `VRCLens`
object:

- `[Utility] JapaneseLocalization.prefab` — Japanese (`ja-JP`)
- `[Utility] ChineseSimplifiedLocalization.prefab` — Simplified Chinese (`zh-Hans-CN`)
- `[Utility] ChineseTraditionalLocalization.prefab` — Traditional Chinese, Taiwan (`zh-Hant-TW`)
- `[Utility] KoreanLocalization.prefab` — Korean (`ko-KR`)
- `[Utility] FrenchLocalization.prefab` — Français (`fr-FR`)
- `[Utility] GermanLocalization.prefab` — Deutsch (`de-DE`)
- `[Utility] CzechLocalization.prefab` — Čeština (`cs-CZ`)
- `[Utility] SpanishSpainLocalization.prefab` — Español (España) (`es-ES`)
- `[Utility] SpanishLatinAmericaLocalization.prefab` — Español (Latinoamérica) (`es-419`)
- `[Utility] RussianLocalization.prefab` — Русский (`ru-RU`)
- `[Utility] ItalianLocalization.prefab` — Italiano (`it-IT`)
- `[Utility] DanishLocalization.prefab` — Dansk (`da-DK`)
- `[Utility] DutchLocalization.prefab` — Nederlands (`nl-NL`)
- `[Utility] FinnishLocalization.prefab` — Suomi (`fi-FI`)
- `[Utility] NorwegianBokmalLocalization.prefab` — Norsk bokmål (`nb-NO`)
- `[Utility] NorwegianNynorskLocalization.prefab` — Norsk nynorsk (`nn-NO`)
- `[Utility] PolishLocalization.prefab` — Polski (`pl-PL`)
- `[Utility] PortuguesePortugalLocalization.prefab` — Português (Portugal) (`pt-PT`)
- `[Utility] SwedishLocalization.prefab` — Svenska (`sv-SE`)
- `[Utility] BulgarianLocalization.prefab` — Български (`bg-BG`)
- `[Utility] GreekLocalization.prefab` — Ελληνικά (`el-GR`)
- `[Utility] HungarianLocalization.prefab` — Magyar (`hu-HU`)
- `[Utility] RomanianLocalization.prefab` — Română (`ro-RO`)
- `[Utility] ThaiLocalization.prefab` — ไทย (`th-TH`)
- `[Utility] TurkishLocalization.prefab` — Türkçe (`tr-TR`)
- `[Utility] UkrainianLocalization.prefab` — Українська (`uk-UA`)

Remove the prefab and rebuild to restore the English labels. If two or more
localization prefabs are present, including duplicates for the same language, the
build stops before VRCFury processing and displays every language, locale, and
Hierarchy path involved. Inactive or disabled prefabs are also detected as
installed.

## Non-destructive Processing

After VRCFury and any optional Free Camera Add-ons processing is complete, the
final Expressions Menu is duplicated to
`Assets/VRCLensLocalizationGenerated/LocalizedMenus/<locale>`. Only `Control.name` and
`Control.labels[].name` in the duplicated copy are changed. Parameters, values,
types, styles, icons, ordering, Expression Parameters, Animator Controllers, and
animations remain unchanged, and no additional synced memory is used. Shared
submenus and circular references are preserved as well.

If Menu Favorites is installed, user-entered aliases, menu paths, and submenu
names are preserved. Public serialized fields are detected optionally for
compatibility with older Free Camera Add-ons versions, so their C# types are not
required dependencies.

## Validation

In the Unity Editor, run `Tools > VRCLens Localization > Validate Package`. This
checks all registered catalogs (currently 26), the supported VRCLens menus, installed Free Camera
Add-ons when present, shared and circular menus, Puppet labels, conflict
detection, all registered installer prefabs, and the empty VRCFury Full Controller.

## Catalogs and sources

Translations are stored in C# catalogs under `Editor/Localization` and in `Editor/Catalogs/<locale>.json`. The shared resolver consumes 206 fixed
labels, 8 directions, 11 contextual/dynamic records, and 28 functional blank buttons per locale.
Number and time labels use locale-specific `{0}` templates. Five layout spacers remain blank.
No additional package dependencies are introduced.

Bokmål and Nynorsk have separate dictionaries and prefabs, as do Spain and Latin American Spanish.
Portuguese targets Portugal. Shared regional terms are not artificially differentiated. Official
spellings are never shortened for display width.

[Terminology status](Documentation/TERMINOLOGY.md) provides review results by locale, source tables,
and lists of terms awaiting confirmation. Validate Package also verifies table/catalog agreement.
See [Unity validation](Documentation/VALIDATION.md) for tested behavior and unverified client rendering.

## License

This software is released under the MIT License. See [LICENSE.txt](LICENSE.txt)
for details.

## Free Camera v2.3.0 compatibility

Preflight captures the locale and user-authored Favorites settings. Localization runs after Free Camera and avatar optimization, even when editor-only components have already been stripped. Generated menus live outside the Free Camera folder, so its Temp cleanup and source-menu validator do not touch them.

The Japanese README is named `README.Localization.md` to avoid overwriting the Free Camera README. Keep the Editor/Localization and Runtime asmdefs, and move scripts together with their existing meta files. Do not leave duplicate localization scripts in the old Editor folder. Validation checks all installed menus and Toggle paths without requiring the full optional bundle asset count.

[Free Camera v2.3.0 fixes and 31-prefab / 26-locale validation](Documentation/FREE_CAMERA_COMPATIBILITY.md)
