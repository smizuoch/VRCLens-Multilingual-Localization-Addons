# 既存4言語の用語・出典確認

確認日: 2026-09-05。対象は日本語、簡体字中国語、中国語繁体字（台湾）、韓国語。
現在の配置は `Assets/VRCLens_Custom`。Prefabの表示名データは対応するEditor内のC#辞書が供給します。

各253件、計1,012レコードを一覧化し、既存訳の意味・表記と取得できた一次資料を照合しました。
12件の表示名を修正しました。**全項目の外部出典確認が完了したという意味ではありません。**
対象言語の表記まで確認できたもの、機能説明を根拠に作成したもの、追加確認が必要なものを分けています。

| Locale | Canon・Adobe表記一致 | 根拠付き作成表現 | 機能確認済み・地域用例未確認 | その他出典未確認 | 合計 |
|---|---:|---:|---:|---:|---:|
| ja-JP | 39 | 117 | 0 | 97 | 253 |
| zh-Hans-CN | 39 | 6 | 111 | 97 | 253 |
| zh-Hant-TW | 37 | 7 | 112 | 97 | 253 |
| ko-KR | 32 | 9 | 115 | 97 | 253 |
| 合計 | 147 | 139 | 338 | 388 | 1,012 |

未確定は726件（338＋388）。一般操作、方向、機能付き空欄、センサー表記、規格・単位を含みます。
一般的に自然に読める語や既知の規格記号でも、今回適切な出典を個別に確認していなければ確定扱いしません。
既存22追加ロケールの3,946件と合わせると、未確定一覧の対象は4,672件です。

## 修正した表示名

| Locale | 英語キー | 修正前 | 修正後 | 理由・出典 |
|---|---|---|---|---|
| ja-JP | Response | 被写体切替 | 被写体追従特性 | [Canon サーボAF特性](https://cam.start.canon/ja/C017/manual/html/UG-04_AF-Drive_0090.html)。Locked-on／Responsiveの選択肢を持つ追従特性として解釈。 |
| ja-JP | Responsive | 敏感 | 俊敏 | 同じCanon資料の追従特性の表記に統一。 |
| ja-JP | Posterize | 階調化 | ポスタリゼーション | [Adobe フィルター効果](https://helpx.adobe.com/jp/photoshop/using/filter-effects-reference.html)。色数・階調数を減らす効果を明示。 |
| ja-JP | 4:5 Portrait | 4:5 ポートレート | 4:5 縦位置 | [作者のLetterbox機能説明](https://github.com/gummidot/VRCLens-Addons/blob/main/README_JP.md#letterbox)。人物撮影スタイルではなく縦向きの比率。既存のPortrait訳とも統一。 |
| ja-JP | 2.35:1 Anamorphic | 2.35:1 アナモフィック | 2.35:1 アナモルフィック | [作者のAnamorphic Bokeh説明](https://github.com/gummidot/VRCLens-Addons/blob/main/README_JP.md#anamorphic-bokeh)。同じ形容語を既存のボケ名称と統一。比率は元の設定値を維持。 |
| zh-Hans-CN | Response | 被摄体切换 | 追踪灵敏度 | [Canon 伺服自动对焦特性](https://cam.start.canon/zh/C017/manual/html/UG-04_AF-Drive_0090.html)。追従感度として解釈。 |
| zh-Hans-CN | Responsive | 灵敏 | 敏感 | 同じCanon中国語資料の選択肢に統一。 |
| zh-Hant-TW | Response | 主體切換 | 追蹤靈敏度 | [Canon 伺服自動對焦特性](https://cam.start.canon/tc/C017/manual/html/UG-04_AF-Drive_0090.html)。台湾向け表記を採用。 |
| zh-Hant-TW | Vibrance | 鮮艷度 | 自然飽和度 | [Adobe台湾 色の調整](https://helpx.adobe.com/tw/lightroom-cc/web/edit-photos/apply-effects/adjust-color.html)。Saturation（飽和度）と区別。 |
| zh-Hant-TW | Midtones | 中間色調 | 中間調 | 同じAdobe台湾資料の階調域の表記に統一。 |
| zh-Hant-TW | Highlights | 高光 | 亮部 | 同じAdobe台湾資料の階調域の表記に統一。簡体字の「高光」は維持。 |
| ko-KR | Vibrance | 생동감 | 활기 | [Adobe韓国 オブジェクト調整](https://helpx.adobe.com/kr/photoshop/web/edit-images/retouch/adjust-objects.html)、[色の活気の説明](https://www.adobe.com/kr/learn/photoshop/web/photo-enhancement-basics)。画像編集UI用語に統一。 |

Responseの意味判定は既存カタログの選択肢と対応機能からの解釈です。CanonとVRCLensのAF実装が同一という主張ではありません。
韓国語の `픽쳐스타일` は[Canon R6のメニュー表記](https://cam.start.canon/ko/C004/manual/html/UG-03_Shooting-1_0140.html)にもあるため維持しました。
本文で空白入り表記が使われていても、空白なしの既存表記を誤りとは扱いません。

## 記録と判定基準

- [全出典対応表](LegacyTranslationSources.csv): `locale + group + key` で全1,012件を識別。
- [未確定一覧](LegacyTranslationReviewQueue.csv): 対象言語の用例が未確認の726件。
- [監査データ](LegacyCatalogs): 実C#出力との一致検査用JSON。実際の翻訳データ供給元は引き続きC#。
- [変更一覧](LegacyTerminologyChanges.json): 修正前後を記録し、回帰検証で許容する差分を限定。

`canon-exact` / `adobe-exact` は、同じ意味の用語表記が一次資料に存在するもの。
単語の一致が、そのままVRCLensの公式翻訳であることを意味するわけではありません。
`canon-adapted` はCanon機能説明に基づく作成表現、`author-adapted` は作者の日本語説明に基づく作成表現です。
`function-reviewed` は作者の説明で機能を確認できても、中国語・韓国語の地域用例が未確認のもの。
`review-required` とともに未確定一覧へ残します。未確定にしたこと自体は誤訳の判定ではありません。

Canon C017の `/ko/` 配下で英語本文にフォールバックするページを確認しました。
韓国語の根拠には採用せず、韓国語本文のあるEOS R6（C004）の該当機能ページを使用しています。
中国語は `/zh/` と `/tc/`、Adobeは `/cn/` と `/tw/` の本文を区別しました。
作者のREADMEは2026-09-05時点のmainを参照しており、Free Camera Add-ons v2.3.0の全実装を検証したという意味ではありません。

## 検証・再実行

`Tools > VRCLens Localization > Validate Package` は従来の26言語検証に加えて、
既存4言語の全固定名・方向・機能付き空欄・動的ラベルと監査JSON・両CSVの一致を検査します。
ピン番号と秒・分は1、2、12、60の各値で実際の出力を照合します。
未確定件数のConsole警告にも既存4言語を加算します。

ワークスペースの `tools/legacy-localization-audit.cjs --check` はCSV・未確定一覧の再現性を検査します。
初期生成は `node tools/legacy-localization-audit.cjs --write`。変更前の
`Logs/localization-26-backup/*.txt` を保持し、CaptureBaselineで上書きしないでください。
調査スクリプトは `tools/legacy-research.cjs`、取得記録は `Logs/legacy-localization-audit/research/`。

修正後のSDK統合検証メソッドは
`VRCLensCustom.VRCLensLocalizationRegression.ValidateReviewedAndBuildSceneCopies`。
変更前のスナップショットから、記録済み12件だけが変わったことも検査します。
元のVerifyBaselineは従来どおり完全一致を要求するため、今回の意図した修正後には不一致になります。

実行結果: 26言語Package検証PASS、既存4言語SDK統合4/4 PASS、記録済み12件以外の出力不変PASS。
[SDK統合ログ](../../../Logs/legacy-localization-audit/integration.log)、
[最終Packageログ](../../../Logs/legacy-localization-audit/final-package.log)はいずれも終了コード0です。

VRChatクライアントのフォントや円形メニュー上の折り返しは未確認です。
Free Camera Add-ons本体はこのプロジェクトに存在しないため、その実Prefabを組み込んだ検証は含みません。
