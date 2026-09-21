# 構図ガイド単独版との併用

2026-09-21 更新。`VRCLens_CompositionGuides_Standalone.unitypackage` の32種類・2レイヤーの構図ガイドに対応しました。

## 使い方

1. VRCLens 1.10.0、VRCFury、構図ガイド単独版をインポートします。
2. `[Camera] CompositionGuides.prefab` を動作するアバターのVRCLens直下に1個配置します。
3. 同じVRCLens直下に、使用する言語の `[Utility] ...Localization.prefab` を1個だけ配置します。

日本語は既存の `[Utility] JapaneseLocalization.prefab` をそのまま使用できます。構図ガイド側のInspectorがJapaneseでもEnglishでも、多言語アドオンで選んだ言語を優先します。翻訳Prefabを配置しない場合は、構図ガイド内蔵のJapanese/English設定がそのまま有効です。

構図ガイドだけ日本語で使いたい場合は、日本語専用書き出しに含まれる `[Camera] CompositionGuides_Japanese.prefab` を1個配置してください。これは日本語設定を保存したPrefabで、通常版と重ねて配置する必要はありません。日本語専用パッケージは構図ガイド一式を含み、多言語アドオンへの依存はありません。VRCLens本体のメニューも日本語にする場合は、上記のJapaneseLocalizationを併用してください。

## 対応範囲

- 登録済み26言語・ロケールすべてで、構図32種、分類6種、2つのガイドと表示・配置・色など、合計76個の表示名を追加。
- 新しい辞書は `Editor/CompositionCatalogs/<locale>.json`。従来の206項目の辞書は変更しません。
- `VRCL_Custom/Composition` パラメーター、またはそのパラメーターを含むSubMenuだけが翻訳対象です。同名の無関係なメニューには適用しません。
- ビルド時の複製メニューの表示名だけを翻訳します。元のメニュー、シェーダー、Animator、パラメーター名・値・同期設定は変更しません。
- 構図ガイドのassemblyは必須参照にしていません。構図ガイド未導入でも翻訳アドオンを使用できます。

## 内蔵日本語化との処理順

翻訳アドオンのpreflightを `-20003` にし、構図ガイドのpreflight (`-20002`) より前に、ビルド対象のインストーラーだけをEnglishに設定します。後段の構図ガイド内蔵日本語化が、選んだ言語やFavoritesの名前を上書きすることを防ぎます。保存されたPrefab・シーンの言語設定には書き込みません。

翻訳を選ばないビルドはこの処理を通らず、内蔵日本語化が動きます。

## 翻訳データの位置づけ

日本語76項目は同梱された `CompositionGuides.labels.json` の作者表記を維持しています。それ以外の25ロケールの追加訳は、この更新で作成した訳案です。JSONの `basis` に `authored-translation-review-required` と記録しています。母語話者による校閲・実機での表示幅とフォントの確認は未実施です。スペイン語2ロケールは、この機能では同じ表記を採用しています。

## 検証

`Tools > VRCLens Localization > Validate Package` に、76項目×26ロケールの欠落・重複・空欄検査、無関係な同名項目の保持、RadialのsubParameters、循環SubMenu、日本語作者表記との一致、導入済みの構図ガイド全メニューの対応検査を追加しました。従来の元メニュー保護・表示以外の不変条件・言語競合検査も継続します。

VRChat内での表示・操作と、全言語の翻訳品質を保証するものではありません。
