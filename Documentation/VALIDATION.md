# 26言語拡張の検証結果 / Validation results

## 既存4言語の用語監査後の追記（2026-09-05）

以下の26言語拡張時の結果は変更前の記録です。今回、[既存4言語の監査](LEGACY_TERMINOLOGY.md)で
12件を修正したため、「既存4言語の出力一致」は「記録済み12件以外の出力一致」へ更新されます。
全26言語の未確定件数は4,672件（従来3,946＋既存4言語726）です。

- Unity 2022.3.22f1で全26言語のPackage検証に成功。
- 既存4言語の監査JSON・CSV計1,012件を、実際の固定名・方向・動的出力・機能付き空欄と照合。
- 変更前の全出力スナップショットとの差は記録済み12件のみ。
- Shinanoの複製4体でSDK/VRCFury前処理、メニューの言語、マーカー除去、元descriptorの保持を確認。4/4成功。
- [今回のSDK統合ログ](../../../Logs/legacy-localization-audit/integration.log): 終了コード0。
- [最終Package検証ログ](../../../Logs/legacy-localization-audit/final-package.log): PASS、終了コード0、C#コンパイルエラーなし。

今回はこのプロジェクトをbatchmodeで開き、検証用アバターを複製しています。シーンの保存・アップロードは行っていません。
最初のインポートで追加した型の認識前にCS0103が記録されましたが、再コンパイル後の検証は成功しています。
最終Package検証でコンパイルエラーが残っていないことを確認しました。
起動時のAndroid ADBパス由来の例外は従来同様に記録されますが、今回の検証は終了コード0です。

## 26言語拡張時の記録

検証日: 2026-09-05。技術実装・Unity検証は成功。訳語の出典確認は未完了です。
出典・語義・地域が未確定の3,946レコードを、翻訳完了には数えていません。
詳細は [TERMINOLOGY.md](TERMINOLOGY.md) と [未確定一覧](TranslationReviewQueue.csv) を参照してください。

## 実装した内容

- 既存4言語に22言語を追加。ノルウェー語はBokmålとNynorskを別登録。
- 新規22辞書・22導入Prefab・22 Runtimeマーカーを追加。各Prefabは空のVRCFury Full Controllerを保持。
- 中国語の辞書アダプターを共通化し、新規言語も同じ意味判定・メニュー複製・翻訳・前処理を使用。
- 206固定項目、8方向、11動的・文脈依存レコード、28機能付き空欄を各新規辞書へ格納。
- ピン番号と時間は言語別テンプレート。例: ハンガリー語は番号が先、他の多くの言語はマーカー名が先。
- レジストリ・必要26ロケールの検証・日英README・出典表を更新。追加パッケージ依存なし。

## 実行環境とログ

Unity 2022.3.22f1、VRCLens 1.10.0、VRCFury 1.1426.0、VRChat SDK Avatars 3.10.4。
元プロジェクトのUnityが開かれていたため、最終実行はLogs/localization-validation-projectへ
Assets・Packages・ProjectSettingsをコピーした独立プロジェクトで行いました。
検証対象はAssets/Shinano/Shinano.unityのアバターを毎回複製したものです。
作業シーンの保存、実アップロード、外部公開は行っていません。

- [26言語SDK統合ログ](../../../Logs/localization-26-final-integration.log): 終了コード0、26/26成功、既存4言語の出力一致。
- [最終Package検証ログ](../../../Logs/localization-26-final-package.log): 終了コード0、最新の出典CSVと実Prefabの競合検証を含め成功。
- [回帰検証用Editorスクリプト](../../Editor/VRCLensLocalizationRegression.cs): このワークスペース専用。配布アドオンのフォルダー外。
- [元アセットのハッシュ記録](../../../Logs/localization-26-original-assets.json): アセット・パッケージ設定1,330ファイルに変更なし。

最終の用語メタデータ修正は採用文字列を変更しません。全26言語SDK検証の後に、
追加した実Prefab競合チェックと出典表の再生成結果を最終Package検証で確認しています。

## 確認できたこと

| 検査 | 結果 |
|---|---|
| 必要26ロケール、カタログ・型・Prefabパスの重複 | PASS |
| 206固定項目と各グループの欠落・空文字・重複 | PASS |
| 28機能付き空欄と5レイアウト空欄 | PASS |
| 方向、ピン1/2/12/60、秒・分、文脈判定、無関係なパラメーター除外 | PASS |
| 出典CSVとJSONの全レコード一致、公式採用表記とsourceTerm一致 | PASS |
| 26実Prefabの配置とアバター複製、有効・無効・非アクティブでの選択 | PASS |
| 同一言語26組＋異言語325組の実Prefab競合、各3状態（計1,053ケース） | PASS: 選択を拒否し、言語を含む理由を返す |
| Runtime assembly所属、Prefabのマーカー参照、空のFull Controller | PASS |
| Shinano複製26体でSDK/VRCFury前処理、言語別メニュー生成、マーカー除去 | PASS |
| 日本語・簡体字・繁体字・韓国語の変更前スナップショット比較 | PASS: 全4言語一致 |
| 共有SubMenu・循環、値・パラメーター・型・Style・Icon・順序 | PASS: 複製の構造不変検査 |
| ユーザー指定Favorites名・パス・サブメニュー名の保護 | PASS: 各言語の自己テスト |
| 元アバターdescriptor（Animator参照等を含む）・元メニュー参照 | PASS: SDK処理前後で元descriptorのJSONと参照が一致 |
| 既存スクリプト等のmeta、追加GUID | 既存12 metaファイルがバックアップと一致。追加22組のGUIDが一意でPrefabから正しく参照 |

SDKの各翻訳処理は、実際のメニューを保存した後にも、表示名以外のシリアライズされたフィールドが
変わっていないことを検査します。VRCFury自体がビルド複製側に行うAnimatorの統合とは区別しています。

## 全26言語のSDK結果

「変更名数」はこのShinanoで実際に名称が変わった数であり、辞書の全項目数ではありません。
原文と同じ綴りの正規語や、アバターにない機能によって言語ごとに異なります。

| Locale | SDK前処理・マーカー除去 | 変更名数 | 生成メニュー数 |
|---|---|---:|---:|
| ja-JP | PASS | 119 | 25 |
| zh-Hans-CN | PASS | 118 | 25 |
| zh-Hant-TW | PASS | 118 | 25 |
| ko-KR | PASS | 119 | 25 |
| fr-FR | PASS | 111 | 25 |
| de-DE | PASS | 111 | 25 |
| cs-CZ | PASS | 113 | 25 |
| es-ES | PASS | 110 | 25 |
| es-419 | PASS | 110 | 25 |
| ru-RU | PASS | 119 | 25 |
| it-IT | PASS | 112 | 25 |
| da-DK | PASS | 109 | 25 |
| nl-NL | PASS | 111 | 25 |
| fi-FI | PASS | 119 | 25 |
| nb-NO | PASS | 111 | 25 |
| nn-NO | PASS | 111 | 25 |
| pl-PL | PASS | 113 | 25 |
| pt-PT | PASS | 110 | 25 |
| sv-SE | PASS | 112 | 25 |
| bg-BG | PASS | 119 | 25 |
| el-GR | PASS | 118 | 25 |
| hu-HU | PASS | 113 | 25 |
| ro-RO | PASS | 110 | 25 |
| th-TH | PASS | 119 | 25 |
| tr-TR | PASS | 117 | 25 |
| uk-UA | PASS | 119 | 25 |

## 検証範囲の限界

- Free Camera Add-onsの206項目内の訳と、そのパラメーター・Favoritesに対する自己テストは含みます。
  このプロジェクトにはFree Camera Add-ons本体がないため、実アドオン全PrefabのVRCFury統合は未実行です。
- VRChatクライアントのフォント、タイ語の結合文字、キリル文字・ギリシャ文字等の表示、
  円形メニューでの折り返し・はみ出しは未確認です。Unityのシリアライズ成功は表示品質の保証ではありません。
- [LabelLengths.csv](LabelLengths.csv)は全5,566レコードのUnicodeコードポイント数とUTF-8バイト数です。
  ピクセル幅ではありません。最長はフランス語のWBリセット51文字。公式表記の例にはチェコ語の
  One-Shot AF 37文字、フィンランド語とウクライナ語のFocus Peaking 33文字があります。
  自動短縮・文字切り捨ては実装していません。
- 起動ログにはUnityのAndroid ADBパス未設定に由来する例外があります。翻訳コードのC#コンパイルエラーはなく、
  上記バッチはどちらも終了コード0です。Android実機ビルドを確認したという意味ではありません。
- 公式表記一致802件、機能資料・辞書等を根拠にした作成表現・規格名818件、未確定3,946件。
  これらは同義語の重複や機能別キーを含むレコード件数で、未確定を含めて翻訳完了とは呼びません。

## 再実行

通常はUnityのTools > VRCLens Localization > Validate Packageで技術検証します。
全26言語のSDK検証は、検証コピーを対象にUnityのbatchmodeで次のexecuteMethodを実行します。

    VRCLensCustom.VRCLensLocalizationRegression.ValidateAllAndBuildSceneCopies

既存4言語の比較に必要なLogs/localization-26-backupのtxtファイルを保持してください。
初回のCaptureBaselineを実行し直すと比較の基準が変わるので、更新後の検証ではVerifyBaselineを使用します。
