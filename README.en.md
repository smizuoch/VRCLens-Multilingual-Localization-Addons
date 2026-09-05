# VRCLens Multilingual Localization Add-ons
[日本語版 (README.md)](README.md)

This VRCFury add-on translates the VRCLens Expressions Menu and any installed
Free Camera Add-ons at build time only. It includes Japanese, Simplified Chinese,
Traditional Chinese (Taiwan), and Korean localizations.

## Requirements

- VRCLens (tested with 1.10.0) — [https://hirabiki.gumroad.com/l/rpnel](https://hirabiki.gumroad.com/l/rpnel)
- VRCFury (tested with 1.1426.0) — [https://vrcfury.com/](https://vrcfury.com/)
- Free Camera Add-ons (optional) — [https://booth.pm/ja/items/8375173](https://booth.pm/ja/items/8375173)
- VRChat SDK Avatars (a prerequisite for VRCLens and VRCFury)
- Unity 2022.3.22f1

Free Camera Add-ons are optional. If they are not installed, only VRCLens itself
is translated. If they are installed, the `VRCL_Custom/*` menus integrated by
VRCFury are translated as part of the same process. This unitypackage does not
include VRCLens, VRCFury, Free Camera Add-ons, paid assets, menus, animations, or
icons. It also omits the asmdef shared with Free Camera Add-ons to avoid
overwriting an existing environment. When Free Camera Add-ons are not installed,
the localization logic is compiled as part of the regular Editor assembly.
Prefab marker components live in `Runtime` and compile into a runtime assembly.
The SDK strips them before upload through `IEditorOnly`. Keep these scripts outside
`Editor` folders so Unity can load their serialized prefab components.

## Usage

Place exactly one of the following prefabs directly under the avatar's `VRCLens`
object:

- `[Utility] JapaneseLocalization.prefab` — Japanese (`ja-JP`)
- `[Utility] ChineseSimplifiedLocalization.prefab` — Simplified Chinese (`zh-Hans-CN`)
- `[Utility] ChineseTraditionalLocalization.prefab` — Traditional Chinese, Taiwan (`zh-Hant-TW`)
- `[Utility] KoreanLocalization.prefab` — Korean (`ko-KR`)

Remove the prefab and rebuild to restore the English labels. If two or more
localization prefabs are present, including duplicates for the same language, the
build stops before VRCFury processing and displays every language, locale, and
Hierarchy path involved. Inactive or disabled prefabs are also detected as
installed.

## Non-destructive Processing

After VRCFury and any optional Free Camera Add-ons processing is complete, the
final Expressions Menu is duplicated to
`Assets/VRCLens_Custom/Temp/LocalizedMenus/<locale>`. Only `Control.name` and
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
checks the four catalogs, the supported VRCLens menus, installed Free Camera
Add-ons when present, shared and circular menus, Puppet labels, conflict
detection, the four installer prefabs, and the empty VRCFury Full Controller.

## License

This software is released under the MIT License. See [LICENSE.txt](LICENSE.txt)
for details.
