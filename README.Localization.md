# VRCLens Multilingual Localization Add-ons
[English (README.Localization.en.md)](README.Localization.en.md)

構図ガイド単独版の76項目を26言語に追加対応しました。日本語を含め既存の言語Prefabで使えます。
[構図ガイドとの併用方法・処理順・翻訳確認状況](Documentation/COMPOSITION_GUIDES_COMPATIBILITY.md)

VRCLensのExpressions Menuと、導入済みのFree Camera Add-onsをビルド時だけ翻訳する
VRCFury用アドオンです。26言語・ロケールに対応しています。
出典・語義の確認が未完了の訳が含まれます。
[用語と出典の確認状況](Documentation/TERMINOLOGY.md)を参照してください。

## 必要環境

- VRCLens（検証対象: 1.10.0）— [https://hirabiki.gumroad.com/l/rpnel](https://hirabiki.gumroad.com/l/rpnel)
- VRCFury（検証対象: 1.1427.0）— [https://vrcfury.com/](https://vrcfury.com/)
- Free Camera Add-ons（任意）— [https://booth.pm/ja/items/8375173](https://booth.pm/ja/items/8375173)
- VRChat SDK Avatars（VRCLens／VRCFuryの前提依存）
- Unity 2022.3.22f1

Free Camera Add-onsは任意です。入っていない場合はVRCLens本体だけを翻訳し、入っている場合は
VRCFuryが統合した`VRCL_Custom/*`メニューも同じ処理で翻訳します。本unitypackageには
VRCLens、VRCFury、Free Camera Add-ons、有料素材、メニュー、Animation、Iconを同梱していません。
翻訳コードは`Editor/Localization`の専用Editor assembly、言語マーカーは`Runtime`の専用runtime
assemblyでコンパイルします。Free Camera Add-onsのasmdefが存在しても所属と参照が変わらず、
Free Camera側のasmdefを書き換えたり、必須依存に追加したりする必要はありません。
マーカーは`IEditorOnly`によりアップロード前に除去されます。`Editor`フォルダーへ移動すると
UnityがPrefabのコンポーネントを読み込めなくなるため、フォルダー構成を維持してください。

## 使い方

次のPrefabから1つだけをアバターの`VRCLens`オブジェクト直下へ配置します。

- `[Utility] JapaneseLocalization.prefab` — 日本語 (`ja-JP`)
- `[Utility] ChineseSimplifiedLocalization.prefab` — 简体中文 (`zh-Hans-CN`)
- `[Utility] ChineseTraditionalLocalization.prefab` — 繁體中文・台灣 (`zh-Hant-TW`)
- `[Utility] KoreanLocalization.prefab` — 한국어 (`ko-KR`)
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

Prefabを削除して再ビルドすると英語表示へ戻ります。翻訳Prefabが同一言語の重複を含め2個以上
ある場合は、VRCFury処理前に全言語・ロケール・Hierarchyパスを表示してビルドを停止します。
非アクティブ／無効状態のPrefabも「導入済み」として検出します。

## 非破壊処理

VRCFuryと任意のFree Camera Add-ons処理が終わった最終Expressions Menuを
`Assets/VRCLensLocalizationGenerated/LocalizedMenus/<locale>`へ複製します。変更対象は複製側の
`Control.name`と`Control.labels[].name`だけです。Parameter、値、型、Style、Icon、順序、
Expression Parameters、Animator Controller、Animationには変更を加えず、同期メモリも増えません。
共有SubMenuと循環参照も維持します。

Menu Favoritesが導入されている場合は、ユーザーが入力したalias、menu path、submenu名を保持します。
古いFree Camera Add-onsとの組み合わせでも公開されたserialized fieldを任意検出するため、
Free Camera Add-onsのC#型は必須依存ではありません。

## 検証

Unity Editorで`Tools > VRCLens Localization > Validate Package`を実行できます。
登録済みの全カタログ（現在26）、VRCLens本体の対応メニュー、導入済みならFree Camera Add-ons、共有／循環メニュー、
Puppetラベル、競合検出、登録済みの全installer Prefabと空のVRCFury Full Controllerを検査します。
さらに、マーカーのruntime assembly所属と、各Prefabを配置したアバターの複製を検査します。

## 言語データと出典

翻訳データは `Editor/Localization` 内のC#辞書と `Editor/Catalogs/<locale>.json` で管理しています。206個の固定項目、8方向、
11個の動的・文脈依存表示、28個の機能付き空欄を共通処理へ渡します。番号や時間は言語別の
`{0}` テンプレートです。レイアウト用の空欄5個はそのままです。追加パッケージは不要です。

ノルウェー語のブークモールとニーノシュク、スペインとラテンアメリカは別のPrefabです。
ポルトガル語はポルトガル向けです。根拠のない地域差を作らず、公式表記を表示幅のために短縮しません。

[用語と出典の確認状況](Documentation/TERMINOLOGY.md)に、言語別の確認結果、出典対応表、未確定一覧をまとめています。
Validate Packageは翻訳データと出典表の一致も検査します。
[Unity検証結果](Documentation/VALIDATION.md)にはクライアントで未確認の文字表示も分けて記載しています。

## ライセンス

本ソフトウェアはMIT Licenseの下で公開されています。詳細は[LICENSE.txt](LICENSE.txt)を参照してください。

## Free Camera v2.3.0との併用修正

言語とFavoritesの設定はビルド開始時に保存し、Free Camera Add-onsとアバター最適化の後に最終メニューを翻訳します。途中でEditor専用コンポーネントが除去されても設定を保持します。生成先はFree Camera側のTemp削除・元メニュー検査の対象外です。

この説明書はFree Cameraの`README.md`との上書き衝突を避けるため`README.Localization.md`に変更しました。更新時はC#とmetaを組にして移動し、`Editor/Localization`と`Runtime`のasmdefを含むフォルダー構成を維持してください。元の`Editor`直下に翻訳C#の重複を残さないでください。

Validate PackageはBase・任意追加分の個数差を許容し、実際に導入されている全メニュー項目とToggleパスを検査します。

[Free Camera v2.3.0の修正内容・31 Prefab／26言語の検証結果](Documentation/FREE_CAMERA_COMPATIBILITY.md)
