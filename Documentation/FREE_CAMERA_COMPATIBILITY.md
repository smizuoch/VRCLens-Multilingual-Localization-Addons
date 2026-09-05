# Free Camera Add-ons v2.3.0との互換性修正・再検証

2026-09-05。本リポジトリに修正を反映し、修正したソースそのものを使ったUnity/SDK検証が成功しました。
前回の「互換性NG」は修正前の記録です。この結果は修正後の状態を対象にしています。

## 修正内容

- **assemblyを分離。** 翻訳Editor C# 11本をmetaと一緒に `Editor/Localization/` へ移動し、専用の `VRCLens.Localization.Editor` asmdefを追加。マーカーは `Runtime/` の `VRCLens.Localization.Runtime` asmdefに所属し、Editorから明示参照します。Free Cameraの `VRCLensCustom.Editor` に翻訳コードが巻き込まれるCS0246を解消しました。既存スクリプト・PrefabのGUIDは保持しています。
- **翻訳をビルド終盤へ移動。** Free Cameraと同じ -1025 だった翻訳callbackOrderを `Int32.MaxValue - 100` に変更。Free Camera・NDMF等のアバター加工後、VRCFuryの最後のEditor専用コンポーネント除去前に翻訳します。
- **生成先を分離。** `Assets/VRCLensLocalizationGenerated/LocalizedMenus/<locale>` に保存し、Free Cameraが削除する `Assets/VRCLens_Custom/Temp` と、そのsource-menu validatorの検索範囲から外しました。
- **ビルド開始時に設定を保存。** 言語とFavoritesのユーザー指定名・パス・submenu名を保持し、途中でコンポーネントが除去されても翻訳できます。別アバターへの状態の流用を拒否し、次のpreflightと処理終了時に状態を消去します。
- **Base版の構成差に対応。** 33 Prefab・30メニューというフル構成の個数固定を除き、実際に存在する全メニューとToggleパスを検査します。今回のBase版は31 Prefab・26メニュー・69 Toggleパスです。空の導入や未対応の表示名を黙認する変更ではありません。
- **READMEの衝突を回避。** 日本語説明書を `README.Localization.md` に変更し、Free Cameraの `README.md` と同一パス・異なるGUIDで衝突しないようにしました。
- **標準回帰検査を追加。** 専用assemblyへの所属、Free Camera外への出力、全26言語でのマーカー除去後の選択保持、別アバターへの状態漏れ、翻訳Prefabを外した後の非翻訳状態への復帰をValidate Packageで検査します。

Free Cameraのunitypackage・コード・asmdefは修正していません。元リポジトリのAssetsへFree Camera本体を展開していません。

## 検証環境と方法

- Unity **2022.3.22f1**、VRCLens **1.10.0**、VRCFury **1.1427.0**、VRChat SDK Avatars/Base **3.10.5**。
- 対象：`Logs/VRCLens_Custom_Base_v2.3.0.unitypackage`、SHA-256 `52C8EDF679AD651C8DE08B5AE5C46A1172E3BE9993165BFBDDC02123FF22CBB9`。
- Free Cameraとの統合は `Logs/free-camera-230-fix/project` と `combined-project` の独立プロジェクトで実行。元のShinanoアバターをケースごとに複製し、VRCLens直下に実Prefabを配置してSDK前処理を実行しました。シーン保存・アップロードは行っていません。
- 本リポジトリと両検証コピーのEditor/Runtime 82ファイル（C#・asmdef・meta）はハッシュ一致。検証用だけのコード差し替えやasmdef回避はありません。
- Free Cameraの227アセットの内容と元unitypackageのハッシュを照合し、変更ゼロ。翻訳リポジトリ外の既存Assets・ProjectSettingsもハッシュ一致です。
- SDKのtrue/falseだけでなく、処理完了後のメニュー参照、保存済みファイル、SubMenu参照、各メニュー8項目制限、言語別保存先、マーカー除去、ユーザー指定文字列、元descriptor不変を検査しました。
- 翻訳処理自身が、メニューグラフの表示名以外の全serialized fieldと、Expression Parametersの参照・内容・メモリコスト、Animator Controller参照を処理前後で照合します。

## 総合結果

| 検査 | 結果 |
|---|---|
| 本リポジトリ単体・Free CameraなしのValidate Package | PASS・終了コード0 |
| 未修正Free Cameraのasmdefと同居したValidate Package | PASS・終了コード0 |
| 26言語のカタログ・実Prefab・競合・共有/循環メニュー等の標準検査 | PASS |
| 31 Free Camera Prefabの個別SDK検査（日本語） | **31/31 PASS** |
| 29 Prefab同時導入 × 26言語のSDK検査 | **26/26 PASS** |
| 29 Prefab同時導入・翻訳なし | PASS |
| CustomResolution・翻訳なし | PASS |
| Pins 1・2・13本 × Dolly有無 | **6/6 PASS** |
| ピン項目の番号範囲とDolly無効時のメニューパラメーター除去 | **6/6 PASS** |
| Favoritesの任意パス・alias・submenu・ページ分割 | PASS |
| Favoritesと言語マーカーを途中で除去したSDK検査 | PASS |
| CustomResolutionの言語マーカーを途中で除去したSDK検査 | PASS |
| Free Camera validatorが新しい翻訳生成物を誤検出しないこと | PASS |

29 Prefabの同時導入では、代替設定のManualFocus (9m)とDroneSpeed (Slower)を除外しました。この2個も個別検査には含めています。全組み合わせの総当たりではありません。

全26言語で最終メニュー数70、Control数376、同期パラメータコスト102 bit。翻訳なしの同時導入も同じ数です。

## Prefab別結果

修正前に最終メニューが消えた18 Prefabを含め、すべて成功しました。PASSは今回のSDK前処理・最終メニュー構造の範囲です。

| Prefab | SDK検査 | メニュー数 | Control数 |
|---|---|---:|---:|
| [Camera] CustomResolution.prefab | PASS | 25 | 141 |
| [Camera] FarClipPlane.prefab | PASS | 26 | 143 |
| [Camera] PlayerVisibility.prefab | PASS | 27 | 145 |
| [Camera] SmoothDoFEdges.prefab | PASS | 27 | 145 |
| [Camera] SmoothRotate.prefab | PASS | 26 | 143 |
| [Camera] SmoothZoom.prefab | PASS | 25 | 141 |
| [Drone] AvatarOffset.prefab | PASS | 27 | 146 |
| [Drone] CameraPinsDolly.prefab | PASS | 43 | 237 |
| [Drone] DroneSpeed (Slower and Faster).prefab | PASS | 25 | 141 |
| [Drone] DroneSpeed (Slower).prefab | PASS | 25 | 141 |
| [Drone] MoveDroneVertical.prefab | PASS | 26 | 143 |
| [Filter] AnamorphicBokeh.prefab | PASS | 26 | 143 |
| [Filter] ChromaticAberration.prefab | PASS | 28 | 149 |
| [Filter] ColorGrading.prefab | PASS | 30 | 166 |
| [Filter] DepthFog.prefab | PASS | 27 | 149 |
| [Filter] FilmGrain.prefab | PASS | 27 | 148 |
| [Filter] FisheyeLens.prefab | PASS | 28 | 152 |
| [Filter] Letterbox.prefab | PASS | 28 | 154 |
| [Filter] Pixelation.prefab | PASS | 27 | 148 |
| [Filter] TiltShift.prefab | PASS | 27 | 149 |
| [Filter] Vignette.prefab | PASS | 27 | 148 |
| [Filter] ZoomBlur.prefab | PASS | 28 | 151 |
| [Focus] ManualFocus (0.1m to 9m).prefab | PASS | 25 | 141 |
| [Focus] ManualFocus (9m).prefab | PASS | 25 | 141 |
| [Focus] ManualFocusAssist.prefab | PASS | 27 | 149 |
| [Focus] MaxBlurSize.prefab | PASS | 26 | 143 |
| [Utility] FixAvatarDrop.prefab | PASS | 25 | 141 |
| [Utility] MenuExtra.prefab | PASS | 25 | 142 |
| [Utility] MenuFavorites.prefab | PASS | 27 | 146 |
| [Utility] PresetSaver.prefab | PASS | 29 | 158 |
| [Utility] VRCLensOptimizer.prefab | PASS | 25 | 141 |

## 26言語の同時導入結果

| Locale | SDK検査 | メニュー数 | Control数 | 変更された名前・ラベル数 | 同期コスト |
|---|---|---:|---:|---:|---:|
| ja-JP | PASS | 70 | 376 | 347 | 102 bit |
| zh-Hans-CN | PASS | 70 | 376 | 345 | 102 bit |
| zh-Hant-TW | PASS | 70 | 376 | 345 | 102 bit |
| ko-KR | PASS | 70 | 376 | 347 | 102 bit |
| fr-FR | PASS | 70 | 376 | 329 | 102 bit |
| de-DE | PASS | 70 | 376 | 331 | 102 bit |
| cs-CZ | PASS | 70 | 376 | 337 | 102 bit |
| es-ES | PASS | 70 | 376 | 333 | 102 bit |
| es-419 | PASS | 70 | 376 | 333 | 102 bit |
| ru-RU | PASS | 70 | 376 | 348 | 102 bit |
| it-IT | PASS | 70 | 376 | 334 | 102 bit |
| da-DK | PASS | 70 | 376 | 330 | 102 bit |
| nl-NL | PASS | 70 | 376 | 333 | 102 bit |
| fi-FI | PASS | 70 | 376 | 344 | 102 bit |
| nb-NO | PASS | 70 | 376 | 334 | 102 bit |
| nn-NO | PASS | 70 | 376 | 334 | 102 bit |
| pl-PL | PASS | 70 | 376 | 337 | 102 bit |
| pt-PT | PASS | 70 | 376 | 332 | 102 bit |
| sv-SE | PASS | 70 | 376 | 335 | 102 bit |
| bg-BG | PASS | 70 | 376 | 346 | 102 bit |
| el-GR | PASS | 70 | 376 | 343 | 102 bit |
| hu-HU | PASS | 70 | 376 | 339 | 102 bit |
| ro-RO | PASS | 70 | 376 | 332 | 102 bit |
| th-TH | PASS | 70 | 376 | 348 | 102 bit |
| tr-TR | PASS | 70 | 376 | 345 | 102 bit |
| uk-UA | PASS | 70 | 376 | 348 | 102 bit |

名前・ラベルの変更数は、原文と同じ綴りの語を含むかどうかによって言語ごとに異なります。全26言語の統合ログに未対応ラベルを英語で保持した警告はありません。

## 実施範囲の限界と残る注意点

- VRChatクライアントへの実アップロード、カメラ映像・シェーダー表示、Dollyの実際の移動、PresetSaverの実保存/復元、26言語のフォント・折り返しは未確認です。SDK検証の成功を、あらゆる実行環境でバグがない保証とはしていません。
- Free Camera Base自体のPresetSaver stampは132項目、同梱LocalParams由来の期待値は90項目であり、元からのvalidator警告が残ります。差分42項目はBaseに含まれないGhostLens・SoftGlow・SwirlyBokehです。翻訳コードから第三者のコントローラーやstampを再生成することはしていません。今回のPresetSaver個別・同時導入SDK検査は成功しています。
- 既存の `Assets/VRCLens_Custom/Temp/LocalizedMenus` は過去の生成物です。今回の出力先変更は新しいビルドから有効で、既存の生成物を自動削除する処理は追加していません。
- 初回の個別検査では隔離プロジェクトのVRCFury Temp Files package初期化が不足し、全ケースが翻訳処理の前に停止しました。検証環境に正規のpackage.jsonをコピーした後、全個別ケースを最初から再実行して成功しています。修正本体の変更で隠したエラーではありません。
- 起動時のAndroid ADBパス由来の例外は従来通りログにあります。上記の最終検査はC#コンパイルエラーなし・終了コード0です。

## 証拠と再実行

- [Free Camera併用のPackage検証](../../../Logs/free-camera-230-fix/01-package.log)
- [本リポジトリ単体のPackage検証](../../../Logs/free-camera-230-fix/03-standalone-package.log)
- [31 Prefabの個別SDK検査](../../../Logs/free-camera-230-fix/04-individuals.log)
- [26言語の同時導入SDK検査](../../../Logs/free-camera-230-fix/05-combined.log)
- [Pins・Favorites・途中のコンポーネント除去](../../../Logs/free-camera-230-fix/06-focused.log)
- [個別・追加ケースJSONL](../../../Logs/free-camera-230-fix/results.jsonl)、[26言語JSONL](../../../Logs/free-camera-230-fix/combined-results.jsonl)
- [ピン番号・Dollyメニューの照合](../../../Logs/free-camera-230-fix/pin-menu-checks.json)
- [ソース同期のハッシュ照合](../../../Logs/free-camera-230-fix/source-sync.json)、[Free Camera内容の照合](../../../Logs/free-camera-230-fix/free-camera-integrity.json)、[保護対象アセットの照合](../../../Logs/free-camera-230-fix/protected-assets.json)
- [隔離環境用ハーネス](../../../Logs/free-camera-230-fix/project/Assets/Editor/FreeCameraCompatibilityAudit.cs)。Unity batchmodeのexecuteMethodは `VRCLensCustom.FreeCameraCompatibilityAudit.RunFixedIndividuals` / `RunFixedCombined` / `RunFixedFocused`。ハーネスは配布リポジトリに含めていません。

普段の技術検査はUnityの `Tools > VRCLens Localization > Validate Package` を使用してください。Free Cameraを導入せずに実行できます。