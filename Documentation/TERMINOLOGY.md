# 用語・出典の確認状況 / Terminology status

22追加ロケールについて、実装対象は各253レコード、合計5,566レコードです。
現時点で公式資料との表記一致は802レコード、出典・語義・地域の追加確認が必要なのは3946レコードです。
**新規22言語を「翻訳の出典確認まで完了」とは扱っていません。** 技術検証が成功してもこの状態は変わりません。

All 22 new catalogs are populated, but source/sense/region review is incomplete for 3946 records.
No new locale is represented as fully signed off. The catalogs can be selected and built; provisional
strings are explicitly identified in the source table and review queue.

## 対応表の読み方

[TranslationSources.csv](TranslationSources.csv) は実JSONと同じ文字列とメタデータを持ちます。
識別子は `locale + group + key`。同じ英語でも用途が違う項目は別キーです。

| basis | 意味 |
|---|---|
| canon-exact | 関連するCanon機能ページに採用表記が完全一致。sourceTermも保存。 |
| adobe-exact | 対象言語のAdobe機能説明に完全一致。Canonの公式表記とは呼ばない。 |
| canon-adapted | Canonの機能説明を根拠に作成した訳。表記そのものの引用ではない。 |
| dictionary-composition | 語義を読んだ辞書項目を根拠にした合成表現。製品公式訳ではない。 |
| education-composition | 公的教育資料の撮影用語を根拠にした合成表現。 |
| standard-identifier | APS-H、APS-C、HLG、s、minなどの規格名・単位記号。数値はVRCLensの設定値。 |
| dictionary-candidate | 辞書の候補ページを取得した段階。対象言語の節・語義・複合表現の確認が必要。**未確定**。 |
| adobe-region-review | Adobeのポルトガル語本文の地域用法が未確認。**未確定**。 |
| review-required | 適切な対象言語の出典を未確認。**未確定**。 |

未確定項目の全件は [TranslationReviewQueue.csv](TranslationReviewQueue.csv) にあります。
辞書候補は語の一致から収集したもので、同綴異義語・別言語の節・定義未記入のページがあるため、
単にページが存在するだけで訳語を確定しません。候補ページを公式の翻訳として扱うこともありません。

## 言語別件数

下表の「根拠付き作成等」は公式の完全一致とは別で、合成表現および規格名を含みます。
各行253件。方向の別キーや機能付き空欄にも個別レコードがあるため、異なる単語の数ではありません。

| Locale | 公式表記一致 | 根拠付き作成等 | 未確定 |
|---|---:|---:|---:|
| fr-FR | 47 | 35 | 171 |
| de-DE | 56 | 29 | 168 |
| cs-CZ | 34 | 45 | 174 |
| es-ES | 39 | 41 | 173 |
| es-419 | 39 | 41 | 173 |
| ru-RU | 38 | 41 | 174 |
| it-IT | 47 | 36 | 170 |
| da-DK | 43 | 33 | 177 |
| nl-NL | 45 | 35 | 173 |
| fi-FI | 41 | 38 | 174 |
| nb-NO | 14 | 23 | 216 |
| nn-NO | 0 | 38 | 215 |
| pl-PL | 42 | 41 | 170 |
| pt-PT | 30 | 40 | 183 |
| sv-SE | 44 | 36 | 173 |
| bg-BG | 24 | 46 | 183 |
| el-GR | 23 | 47 | 183 |
| hu-HU | 40 | 35 | 178 |
| ro-RO | 31 | 40 | 182 |
| th-TH | 41 | 27 | 185 |
| tr-TR | 48 | 31 | 174 |
| uk-UA | 36 | 40 | 177 |

## 意味・地域についての判断

- `Portrait` は縦向き。人物撮影のピクチャースタイルには割り当てません。
- `dynamic/WorldDrop` はカメラのワールド固定、`dynamic/CameraPinDrop` は位置マーカーの設置。
- `Softness` はぼけの遷移、`VignetteSoftness` は周辺減光の境界、`Edge Softness` は効果の縁、
  `Zone Softness` はフォーカス域の遷移です。同じ形容詞を使う言語でも処理側の意味判定を維持します。
- `Increase/Decrease` は絞り値の増減に対応する絞りを閉じる／開く操作です。
- `Reverse` はドリーの往復再生、`Smooth/Fitted` は経路の補間／近似、`Playback` は再生位置です。
- スペイン語2地域は独立JSONです。Canonの共通スペイン語とAdobeのes/la資料を区別し、
  根拠のない差は作っていません。Canon共通資料はラテンアメリカ固有の表現の証拠ではありません。
- ポルトガル語はcâmara、objetiva、ecrã、ficheiro等のポルトガルでの用法を基準にします。
  AdobeのURLやHTML言語属性だけではポルトガル版の証拠にしません。
- ニーノシュクは別辞書として作成。絞りは[公式辞書blendar](https://ordbokene.no/nn/6640)、
  マーカーは[markørの語義2・4](https://ordbokene.no/nn/48461)、被写界深度は
  [NDLAのdjupneskarpleik](https://ndla.no/nn/r/medie--og-informasjonskunnskap-1/blenderapning-og-dybdeskarphet/6a49fbca81)を確認しました。
  BokmålのmarkørerとNynorskのmarkørar等の差を保持します。
- HUD、AF、MF、3D、センサー規格・アスペクト比の固有名は用途や規格を示すトークンです。
  保持理由は各レコードのnoteに記載。小数・比率はメニューの元設定を表します。
- 公式表記を画面幅に合わせて削る処理はありません。従来4言語は互換性のため既存出力を維持し、
  今回の出典CSVの対象は新規22言語です。

## 調査と再生成

Canon EOS R5 Mark II C017の各言語マニュアルで、絞り、AF、ホワイトバランス、露出補正、
手ブレ補正、表示設定等の機能別ページを確認しました。Adobeは画像の色調・フィルター・ぼかしの資料を調査。
対象言語の本文が英語にフォールバックするページは対象言語の出典にしません。
Wiktionaryは候補収集のみで、採用確定には語義の追加確認が必要です。

このワークスペースの `Logs/localization-tools/` に生成・調査スクリプト、
`Logs/localization-translations/` に原稿、`Logs/localization-research/` に取得記録があります。
再生成順は `node Logs/localization-tools/generate.cjs`、
`node Logs/localization-tools/docs.cjs`、UnityのValidate Packageです。
調査用Node.jsは配布アドオンの依存ではありません。実行時のWebアクセスもありません。
調査日: 2026-09-05。
