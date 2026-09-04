# VRCLens Multilingual Localization Add-ons
[English (README.en.md)](README.en.md)

VRCLensのExpressions Menuと、導入済みのFree Camera Add-onsをビルド時だけ翻訳する
VRCFury用アドオンです。日本語、簡体字中国語、繁体字中国語（台湾）、韓国語を収録しています。

## 必要環境

- VRCLens（検証対象: 1.10.0）— [https://hirabiki.gumroad.com/l/rpnel](https://hirabiki.gumroad.com/l/rpnel)
- VRCFury（検証対象: 1.1426.0）— [https://vrcfury.com/](https://vrcfury.com/)
- Free Camera Add-ons（任意）— [https://booth.pm/ja/items/8375173](https://booth.pm/ja/items/8375173)
- VRChat SDK Avatars（VRCLens／VRCFuryの前提依存）
- Unity 2022.3.22f1

Free Camera Add-onsは任意です。入っていない場合はVRCLens本体だけを翻訳し、入っている場合は
VRCFuryが統合した`VRCL_Custom/*`メニューも同じ処理で翻訳します。本unitypackageには
VRCLens、VRCFury、Free Camera Add-ons、有料素材、メニュー、Animation、Iconを同梱していません。
また、Free Camera Add-onsと共有するasmdefは既存環境を上書きしないよう同梱せず、未導入環境では
翻訳スクリプトが通常のEditor assemblyとしてコンパイルされます。

## 使い方

次のPrefabから1つだけをアバターの`VRCLens`オブジェクト直下へ配置します。

- `[Utility] JapaneseLocalization.prefab` — 日本語 (`ja-JP`)
- `[Utility] ChineseSimplifiedLocalization.prefab` — 简体中文 (`zh-Hans-CN`)
- `[Utility] ChineseTraditionalLocalization.prefab` — 繁體中文・台灣 (`zh-Hant-TW`)
- `[Utility] KoreanLocalization.prefab` — 한국어 (`ko-KR`)

Prefabを削除して再ビルドすると英語表示へ戻ります。翻訳Prefabが同一言語の重複を含め2個以上
ある場合は、VRCFury処理前に全言語・ロケール・Hierarchyパスを表示してビルドを停止します。
非アクティブ／無効状態のPrefabも「導入済み」として検出します。

## 非破壊処理

VRCFuryと任意のFree Camera Add-ons処理が終わった最終Expressions Menuを
`Assets/VRCLens_Custom/Temp/LocalizedMenus/<locale>`へ複製します。変更対象は複製側の
`Control.name`と`Control.labels[].name`だけです。Parameter、値、型、Style、Icon、順序、
Expression Parameters、Animator Controller、Animationには変更を加えず、同期メモリも増えません。
共有SubMenuと循環参照も維持します。

Menu Favoritesが導入されている場合は、ユーザーが入力したalias、menu path、submenu名を保持します。
古いFree Camera Add-onsとの組み合わせでも公開されたserialized fieldを任意検出するため、
Free Camera Add-onsのC#型は必須依存ではありません。

## 検証

Unity Editorで`Tools > VRCLens Localization > Validate Package`を実行できます。
4カタログ、VRCLens本体の対応メニュー、導入済みならFree Camera Add-ons、共有／循環メニュー、
Puppetラベル、競合検出、4つのinstaller Prefabと空のVRCFury Full Controllerを検査します。

## ライセンス

本ソフトウェアはMIT Licenseの下で公開されています。詳細は[LICENSE.txt](LICENSE.txt)を参照してください。
