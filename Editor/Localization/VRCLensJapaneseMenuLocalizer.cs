#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace VRCLensCustom
{
    /// <summary>
    /// Makes a build-only copy of the final expression-menu graph and localizes the VRCLens branch.
    ///
    /// This intentionally runs after VRCFury has merged and paginated menus. VRCFury's menu rename
    /// feature does not rename puppet labels, and a global "Next" override would also affect menus
    /// that do not belong to VRCLens. No source menu is ever written by this class.
    /// </summary>
    internal static class VRCLensMenuLocalizer
    {
        private const string LogPrefix = "[VRCLens Custom]";
        private const string FeatureToggle = "VRCLFeatureToggle";
        private const string CustomParameterPrefix = "VRCL_Custom/";
        private const string FavoriteTokenPrefix = "__VRCL10N_";

        private sealed class FavoriteToken
        {
            public string CatalogId;
            public string ResolvedName;
            public string SourceName;
            public string ParentLabel;
            public string ChosenAlias;
            public bool Consumed;
        }

        private static readonly object FavoriteTokenLock = new object();
        private sealed class FavoriteBridge
        {
            public int AvatarId;
            public string LocaleCode;
            public int Serial;
            public readonly Dictionary<string, FavoriteToken> Tokens =
                new Dictionary<string, FavoriteToken>(StringComparer.Ordinal);
        }
        private static readonly Dictionary<int, FavoriteBridge> FavoriteBridges =
            new Dictionary<int, FavoriteBridge>();

        // Canon terminology references (Japanese camera manuals):
        //   Exposure / aperture value / exposure compensation:
        //   https://cam.start.canon/ja/C021/manual/html/UG-03_ShootingStill_0070.html
        //   MF peaking:
        //   https://cam.start.canon/ja/C011/manual/html/UG-06_AF-Drive_0100.html
        //   Image stabilization:
        //   https://cam.start.canon/ja/C021/manual/html/UG-04_Shooting_0410.html
        //   Picture Style:
        //   https://cam.start.canon/ja/C004/manual/html/UG-03_Shooting-1_0160.html
        // Add-on effect terminology follows the author's Japanese documentation:
        //   https://github.com/gummidot/VRCLens-Addons/blob/main/README_JP.md
        private static readonly Dictionary<string, string> KnownNames =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // VRCLens 1.10.0
                { "Enable", "有効" },
                { "Advanced", "詳細設定" },
                { "Zoom", "ズーム" },
                { "Drone", "ドローン" },
                { "Settings", "設定" },
                { "Focus", "フォーカス" },
                { "Reset", "初期化" },
                { "Exposure", "露出" },
                { "Filters", "フィルター" },
                { "Stabilize", "手ブレ補正" },
                { "Aperture", "絞り" },
                { "Portrait", "縦位置" },
                { "Display", "表示" },
                { "Normal", "標準" },
                { "Depth Cool", "深度・クール" },
                { "Depth Cute", "深度・キュート" },
                { "Spotlight", "スポットライト" },
                { "Green Back", "グリーンバック" },
                { "Blue Back", "ブルーバック" },
                { "RIGHT", "右" },
                { "UP", "上" },
                { "DOWN", "下" },
                { "LEFT", "左" },
                { "Show/Hide", "表示切替" },
                { "Color", "色" },
                { "Hexagon", "六角形" },
                { "Octagon", "八角形" },
                { "Square", "四角形" },
                { "Look At Face", "顔へ向ける" },
                { "Look Up", "上へ向ける" },
                { "Look Down", "下へ向ける" },
                { "Look At Feet", "足元へ向ける" },
                { "Track Self", "自分を追尾" },
                { "Track Self...", "自分を追尾..." },
                { "Infinity", "無限遠" },
                { "Auto Focus", "AF" },
                { "Manual Focus", "MF" },
                { "Macro", "マクロ" },
                { "Picture Style", "ピクチャースタイル" },
                { "Quick Selfie", "自撮り" },
                { "Extra...", "その他..." },
                { "Movie Mode", "動画モード" },
                { "Avatar-Drop", "アバター固定" },
                { "3D Mode", "3Dモード" },
                { "Mount Swap", "マウント切替" },
                { "Hide Lens", "レンズ非表示" },
                { "Increase", "絞る" },
                { "Decrease", "開く" },
                { "Sensor Type", "センサーサイズ" },
                { "DoF Mode", "被写界深度" },
                { "Av Dial", "絞り数値" },
                { "Bokeh Shape", "ボケ形状" },
                { "V. Horizon", "水準器" },
                { "Grid", "グリッド" },
                { "Monitor", "モニター" },
                { "Move HUD", "表示位置" },
                { "Focus Peaking", "MFピーキング" },
                { "Hand-Rotate", "手動回転" },
                { "Change Angle", "向き変更" },
                { "Tracking", "追尾" },
                { "Move Camera", "カメラ移動" },
                { "Drone Speed", "ドローン速度" },
                { "Move Pivot", "ピボット移動" },
                { "Track Pivot", "ピボット追尾" },
                { "Smoothing", "追尾スムージング" },
                { "Drop Pivot", "ピボット設置" },
                { "Vertical Lock", "上下固定" },
                { "EV Dial", "露出補正" },
                { "Auto Exposure", "自動露出" },
                { "White Bal.", "ホワイトバランス" },
                { "Avatar AF", "アバターAF" },
                { "Move Focus", "ピント位置" },

                // Pro Autofocus
                { "Pro Autofocus", "高性能AF" },
                { "Operation", "動作モード" },
                { "AF Area", "AFエリア" },
                { "Response", "被写体追従特性" },
                { "Drive Speed", "フォーカス速度" },
                { "AF-ON", "AF作動" },
                { "AF Lock", "AFロック" },
                { "Focus Guide", "フォーカスガイド" },
                { "One-Shot AF", "ワンショットAF" },
                { "Servo AF", "サーボAF" },
                { "Spot", "スポット" },
                { "Zone", "ゾーン" },
                { "Avatar Priority", "アバター優先" },
                { "Locked-on", "粘る" },
                { "Standard", "標準" },
                { "Responsive", "俊敏" },

                // Free Camera Add-ons v2.3.0
                { "Custom", "追加機能" },
                { "Far Clip Plane", "遠クリップ距離" },
                { "Smooth DoF Edges", "被写界深度境界補正" },
                { "Softness", "柔らかさ" },
                { "Smooth Rotate", "回転スムージング" },
                { "Anamorphic Bokeh", "アナモルフィックボケ" },
                { "Chromatic Aberration", "色収差" },
                { "Enabled", "有効" },
                { "Transverse CA", "倍率色収差" },
                { "Axial CA", "軸上色収差" },
                { "Axial Focus-Aware", "ピント連動" },
                { "Magenta-Green", "マゼンタ／グリーン" },
                { "Color Grading", "カラーグレーディング" },
                { "Saturation", "彩度" },
                { "Vibrance", "自然な彩度" },
                { "Contrast", "コントラスト" },
                { "Shadows", "シャドウ" },
                { "Midtones", "中間調" },
                { "Highlights", "ハイライト" },
                { "Brightness", "明るさ" },
                { "Temperature", "色温度" },
                { "Red", "赤" },
                { "Green", "緑" },
                { "Blue", "青" },
                { "Depth Fog", "大気フォグ" },
                { "Density", "濃さ" },
                { "Start Distance", "開始距離" },
                { "Color R", "赤" },
                { "Color G", "緑" },
                { "Color B", "青" },
                { "Film Grain", "フィルムグレイン" },
                { "Intensity", "強さ" },
                { "Size", "サイズ" },
                { "Speed", "速度" },
                { "Fisheye Lens", "魚眼レンズ" },
                { "Strength", "強さ" },
                { "Roundness", "真円度" },
                { "Lens Size", "レンズサイズ" },
                { "Edge Softness", "周辺ぼかし" },
                { "Reverse Fisheye", "反転魚眼" },
                { "Center", "中心" },
                { "X", "横位置" },
                { "Y", "縦位置" },
                { "Letterbox", "レターボックス" },
                { "2.39:1 Cinemascope", "2.39:1 シネマスコープ" },
                { "2.35:1 Anamorphic", "2.35:1 アナモルフィック" },
                { "2.00:1 Univisium", "2.00:1 ユニビジウム" },
                { "1.85:1 Theatrical", "1.85:1 シアトリカル" },
                { "16:9 Widescreen", "16:9 ワイド" },
                { "3:2 DSLR", "3:2 DSLR" },
                { "4:3 Classic", "4:3 クラシック" },
                { "1:1 Square", "1:1 スクエア" },
                { "4:5 Portrait", "4:5 縦位置" },
                { "9:16 Vertical", "9:16 縦型" },
                { "Pixelation", "ピクセル化" },
                { "Block Size", "ブロックサイズ" },
                { "Dither", "ディザ" },
                { "Posterize", "ポスタリゼーション" },
                { "Aspect Ratio", "縦横比" },
                { "Tilt-Shift", "チルトシフト" },
                { "Depth Mode", "距離モード" },
                { "Blur", "ぼかし" },
                { "Position", "位置" },
                { "Width", "幅" },
                { "Angle", "角度" },
                { "Vignette", "周辺減光" },
                { "Radius", "半径" },
                { "Zoom Blur", "ズームブラー" },
                { "Center X", "中心・横" },
                { "Center Y", "中心・縦" },
                { "Focus Zone", "ピント範囲" },
                { "Spread", "広がり" },
                { "Focus Distance", "撮影距離" },
                { "Manual Focus Assist", "MFアシスト" },
                { "Zone Size", "有効範囲" },
                { "Zone Softness", "範囲ぼかし" },
                { "Edge Feather", "輪郭ぼかし" },
                { "Peaking", "ピーキング" },
                { "Max Blur Size", "最大ぼかしサイズ" },
                { "Player Visibility", "プレイヤー表示" },
                { "Hide Remote Players", "他人を非表示" },
                { "Hide Self", "自分を非表示" },
                { "Avatar Offset", "アバターオフセット" },
                { "Rotate With Avatar", "アバター連動回転" },
                { "Drop (Reset to Hand)", "手元へ戻す" },
                { "Move Drone Vertical", "ドローン上下移動" },
                { "Camera Pins & Dolly", "カメラピン＆ドリー" },
                { "Teleport", "ピン移動" },
                { "Dolly", "ドリー" },
                { "Reset to Hand", "手元へ戻す" },
                { "Show Pins", "ピン表示" },
                { "Show Path", "軌道表示" },
                { "Show Numbers", "番号表示" },
                { "Play", "再生" },
                { "Stop", "停止" },
                { "Playback", "再生位置" },
                { "Move", "手動移動" },
                { "Go To Pin", "ピンへ移動" },
                { "Duration", "所要時間" },
                { "Loop", "ループ" },
                { "Delay", "開始待ち" },
                { "Path", "軌道" },
                { "Hand Rotate (Pins)", "手動回転（ピン）" },
                { "Hand Offset", "手位置連動" },
                { "Drop Next", "次のピンを設置" },
                { "Clear All (Hold)", "全解除（長押し）" },
                { "Full", "全方向" },
                { "Vertical", "上下のみ" },
                { "Teleport Next", "次のピンへ" },
                { "Reverse", "往復" },
                { "Linear", "直線" },
                { "Smooth", "全点補間" },
                { "Fitted", "近似曲線" },
                { "Preset Saver", "プリセット保存" },
                { "Save", "保存" },
                { "Load", "読込" },
                { "Load Defaults", "初期値読込" },
                { "Favorites", "お気に入り" },
            };

        private static readonly Dictionary<string, string> DirectionLabels =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "TOP", "上" },
                { "UP", "上" },
                { "RIGHT", "右" },
                { "BOTTOM", "下" },
                { "DOWN", "下" },
                { "LEFT", "左" },
                { "FORWARD", "前" },
                { "BACK", "後" },
            };

        private static readonly Regex PinName = new Regex(@"^Pin\s+(\d+)$",
            RegexOptions.CultureInvariant);
        private static readonly Regex SecondsName = new Regex(@"^(\d+)s$",
            RegexOptions.CultureInvariant);
        private static readonly Regex MinutesName = new Regex(@"^(\d+)\s+min$",
            RegexOptions.CultureInvariant);

        internal static Dictionary<string, string> CreateJapaneseNames()
        {
            return new Dictionary<string, string>(KnownNames, StringComparer.Ordinal);
        }

        internal static Dictionary<string, string> CreateJapaneseDirectionLabels()
        {
            return new Dictionary<string, string>(DirectionLabels, StringComparer.OrdinalIgnoreCase);
        }

        internal static bool TryGetJapaneseContextualName(
            string sourceName,
            string parameterName,
            VRCExpressionsMenu.Control.ControlType controlType,
            float value,
            bool submenuContainsVrclensParameters,
            bool isGeneratedNextWithinVrclens,
            out string localized)
        {
            localized = null;
            sourceName = sourceName ?? "";
            parameterName = parameterName ?? "";
            if (sourceName == "Drop")
            {
                if (controlType == VRCExpressionsMenu.Control.ControlType.SubMenu
                    && submenuContainsVrclensParameters)
                {
                    localized = "ピン設置";
                    return true;
                }
                if (parameterName == FeatureToggle && Approximately(value, 251))
                {
                    localized = "ワールド固定";
                    return true;
                }
                return false;
            }
            if (parameterName == FeatureToggle)
            {
                if (sourceName == "Zoom In" && Approximately(value, 107))
                {
                    localized = "望遠側 速度4";
                    return true;
                }
                if (sourceName == "Zoom Out" && Approximately(value, 103))
                {
                    localized = "広角側 速度4";
                    return true;
                }
            }
            if (sourceName == "Next" && isGeneratedNextWithinVrclens)
            {
                localized = "次へ";
                return true;
            }
            Match match = PinName.Match(sourceName);
            if (match.Success && IsCameraPinParameter(parameterName))
            {
                localized = "ピン" + match.Groups[1].Value;
                return true;
            }
            match = SecondsName.Match(sourceName);
            if (match.Success && (parameterName == "VRCL_Custom/DollyDelay"
                                  || parameterName == "VRCL_Custom/DollyDuration"))
            {
                localized = match.Groups[1].Value + "秒";
                return true;
            }
            match = MinutesName.Match(sourceName);
            if (match.Success && parameterName == "VRCL_Custom/DollyDuration")
            {
                localized = match.Groups[1].Value + "分";
                return true;
            }
            if (sourceName == "Softness"
                && parameterName == "VRCL_Custom/VignetteSoftness")
            {
                localized = "境界ぼかし";
                return true;
            }
            if (sourceName == "X" && parameterName == "VRCL_Custom/FisheyeLensCenterX")
            {
                localized = "横位置";
                return true;
            }
            if (sourceName == "Y" && parameterName == "VRCL_Custom/FisheyeLensCenterY")
            {
                localized = "縦位置";
                return true;
            }
            return false;
        }

        internal static bool TryGetJapaneseFunctionalBlankName(
            string parameterName,
            VRCExpressionsMenu.Control.ControlType controlType,
            float value,
            out string localized)
        {
            localized = null;
            int integralValue = Mathf.RoundToInt(value);
            if (!Approximately(value, integralValue)) return false;
            if (parameterName == FeatureToggle
                && controlType == VRCExpressionsMenu.Control.ControlType.Button)
            {
                switch (integralValue)
                {
                    case 129: localized = "少し上を見る"; return true;
                    case 130: localized = "少し下を見る"; return true;
                    case 121: localized = "遠距離側 中"; return true;
                    case 120: localized = "遠距離側 小"; return true;
                    case 118: localized = "近距離側 小"; return true;
                    case 117: localized = "近距離側 中"; return true;
                    case 106: localized = "望遠側 速度3"; return true;
                    case 105: localized = "望遠側 速度2"; return true;
                    case 104: localized = "望遠側 速度1"; return true;
                    case 100: localized = "広角側 速度1"; return true;
                    case 101: localized = "広角側 速度2"; return true;
                    case 102: localized = "広角側 速度3"; return true;
                    case 110: localized = "露出補正 +1/3段"; return true;
                    case 108: localized = "露出補正 -1/3段"; return true;
                    case 115: localized = "WB補正：アンバー側"; return true;
                    case 114: localized = "WB補正：リセット"; return true;
                    case 113: localized = "WB補正：ブルー側"; return true;
                }
            }
            if (parameterName == "VRCLS_SensorSize"
                && controlType == VRCExpressionsMenu.Control.ControlType.Toggle)
            {
                switch (integralValue)
                {
                    case 0: localized = "35mmフルサイズ"; return true;
                    case 1: localized = "APS-H"; return true;
                    case 2: localized = "APS-C（1.5倍）"; return true;
                    case 3: localized = "APS-C（1.6倍）"; return true;
                    case 4: localized = "マイクロフォーサーズ"; return true;
                    case 5: localized = "1.0型"; return true;
                }
            }
            if (parameterName == "VRCLS_TonemapFilter"
                && controlType == VRCExpressionsMenu.Control.ControlType.Toggle)
            {
                switch (integralValue)
                {
                    case 0: localized = "切"; return true;
                    case 1: localized = "フィルミック1"; return true;
                    case 2: localized = "フィルミック2"; return true;
                    case 3: localized = "HLG"; return true;
                    case 4: localized = "ニュートラル"; return true;
                }
            }
            return false;
        }

        internal static bool TryGetJapaneseImpliedPivotDirection(
            float featureValue,
            bool hasFaceBlendHorizontal,
            bool hasFaceBlendVertical,
            int labelCount,
            int labelIndex,
            out string localized)
        {
            localized = null;
            if (!Approximately(featureValue, 214)
                || !hasFaceBlendHorizontal || !hasFaceBlendVertical
                || labelCount != 4 || labelIndex < 0 || labelIndex >= 4)
                return false;
            localized = new[] { "前", "右", "後", "左" }[labelIndex];
            return true;
        }

        /// <summary>
        /// Clones the descriptor's complete menu graph, localizes only its unique VRCLens subtree,
        /// and assigns the cloned root to the build avatar. Returns false without assigning anything
        /// when the avatar/menu/VRCLens root cannot be identified safely.
        /// </summary>
        internal static bool Apply(GameObject avatarGameObject, string tempDir)
        {
            return Apply(avatarGameObject, tempDir,
                VRCLensLocalizationRegistry.GetRequiredProfile("ja-JP"));
        }

        internal static bool Apply(GameObject avatarGameObject, string tempDir,
                                   VRCLensLocalizationProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var catalog = VRCLensLocalizationCatalog.ForLocale(profile.LocaleCode);
            if (avatarGameObject == null)
            {
                Debug.LogError($"{LogPrefix} {profile.NativeName} localization received no avatar GameObject.");
                return false;
            }

            var descriptor = avatarGameObject.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null)
            {
                Debug.LogError($"{LogPrefix} {profile.NativeName} localization could not find a VRCAvatarDescriptor " +
                               $"on '{avatarGameObject.name}'.");
                EndFavoriteBridge(avatarGameObject);
                return false;
            }
            if (descriptor.expressionsMenu == null)
            {
                Debug.LogError($"{LogPrefix} {profile.NativeName} localization cannot run because " +
                               $"'{avatarGameObject.name}' has no Expressions Menu.");
                EndFavoriteBridge(avatarGameObject);
                return false;
            }

            var allMenus = CollectMenus(descriptor.expressionsMenu);
            var candidates = allMenus.Where(IsVRCLensRoot).ToList();
            if (candidates.Count == 0)
            {
                Debug.LogError($"{LogPrefix} {profile.NativeName} localization could not find the VRCLens menu root on " +
                               $"'{avatarGameObject.name}'. Expected one menu containing VRCLT_Enabled and " +
                               "VRCLZoomRadial controls after VRCFury finished merging.");
                EndFavoriteBridge(avatarGameObject);
                return false;
            }
            if (candidates.Count != 1)
            {
                Debug.LogError($"{LogPrefix} {profile.NativeName} localization found {candidates.Count} possible VRCLens " +
                               $"menu roots on '{avatarGameObject.name}' ({DescribeMenus(candidates)}). " +
                               "Localization was stopped so an unrelated menu is not renamed.");
                EndFavoriteBridge(avatarGameObject);
                return false;
            }

            string localizedDir;
            if (!TryPrepareOutputFolder(tempDir, profile.LocaleCode, out localizedDir))
            {
                EndFavoriteBridge(avatarGameObject);
                return false;
            }

            try
            {
                var copies = new Dictionary<VRCExpressionsMenu, VRCExpressionsMenu>();
                int serial = 0;
                var clonedDescriptorRoot = CloneMenuGraph(
                    descriptor.expressionsMenu, localizedDir, copies, ref serial, true);
                var clonedVRCLensRoot = copies[candidates[0]];

                // Verify independent managed storage before any translated string is written. If a
                // future SDK clone implementation ever shares a Control/Parameter/Label object,
                // stop before the source graph can be touched through that alias.
                RestoreClonedMenuObjectNames(copies);
                AssertDisplayOnlyClone(descriptor.expressionsMenu, clonedDescriptorRoot);

                // A FavoriteItem name or path is user-authored. Record those exact controls on the
                // clone before translating so an alias such as "Reset" remains exactly what was typed.
                int restoredFavoriteNames;
                bool usedFavoriteBridge;
                var protectedNames = RestoreFavoriteTokens(avatarGameObject, clonedDescriptorRoot,
                                                            catalog,
                                                            out restoredFavoriteNames,
                                                            out usedFavoriteBridge);
                int localizedFavoritePagination;
                protectedNames.UnionWith(CollectFavoriteProtections(
                    avatarGameObject, clonedDescriptorRoot, clonedVRCLensRoot,
                    !usedFavoriteBridge, catalog, out localizedFavoritePagination));
                var unknown = new HashSet<string>(StringComparer.Ordinal);
                var excluded = clonedDescriptorRoot == clonedVRCLensRoot ? null : clonedDescriptorRoot;
                int changed = restoredFavoriteNames + localizedFavoritePagination
                              + TranslateGraph(clonedVRCLensRoot, excluded, protectedNames, unknown,
                                               catalog);

                // CreateAsset derives Object.name from a temporary filename. Every menu now exists,
                // so restore the complete graph before comparing it with the source or assigning it.
                RestoreClonedMenuObjectNames(copies);
                // Prove on the actual build graph—not only in a synthetic test—that cloning and
                // translation changed no serialized field except Control.name and Label.name.
                AssertDisplayOnlyClone(descriptor.expressionsMenu, clonedDescriptorRoot);

                // A first import can still be deferred until saving the last asset in a large graph.
                // Stabilize names only after every menu exists so any filename-derived Object.name
                // normalization is repaired before the uploaded menu graph is accepted.
                // Save only the build-only clones we own. AssetDatabase.SaveAssets() would also
                // persist unrelated user assets that happened to be dirty when an avatar build
                // started, which is outside this add-on's non-destructive scope.
                SaveClonedMenusWithStableObjectNames(copies);
                // Re-run the complete invariant after persistence. This catches an importer that
                // changed anything beyond the harmless, repaired filename-derived Object.name.
                AssertDisplayOnlyClone(descriptor.expressionsMenu, clonedDescriptorRoot);

                // Keep the operation transactional: do not point the build descriptor at the copy
                // until that copy has been saved and has passed every structural invariant.
                descriptor.expressionsMenu = clonedDescriptorRoot;
                EditorUtility.SetDirty(descriptor);

                if (unknown.Count > 0)
                {
                    Debug.LogWarning($"{LogPrefix} {profile.NativeName} localization kept {unknown.Count} unknown " +
                                     "VRCLens/Add-ons label(s) in English. This is safe, but may indicate a " +
                                     $"newer version: {string.Join(", ", unknown.OrderBy(n => n))}.");
                }
                Debug.Log($"{LogPrefix} Localized {changed} VRCLens menu name/label(s) to " +
                          $"{profile.NativeName} ({profile.LocaleCode}) for '{avatarGameObject.name}' " +
                          $"using {copies.Count} build-only menu copy/copies.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} {profile.NativeName} localization could not clone/localize the Expressions " +
                               $"Menu for '{avatarGameObject.name}'. No source menu was edited.");
                Debug.LogError(exception);
                return false;
            }
            finally
            {
                EndFavoriteBridge(avatarGameObject);
            }
        }

        /// <summary>
        /// Localizes a copied known VRCLens control without traversing a menu. Menu Favorites may call
        /// this only when FavoriteItem.name is blank; an explicit alias must always win.
        /// </summary>
        internal static bool LocalizeKnownControlCopy(VRCExpressionsMenu.Control control)
        {
            if (control == null) return false;
            return TranslateControl(control, false, null,
                VRCLensLocalizationCatalog.ForLocale("ja-JP")) > 0;
        }

        internal static bool LocalizeKnownControlCopy(
            VRCExpressionsMenu.Control control, VRCLensLocalizationProfile profile)
        {
            if (control == null) return false;
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            return TranslateControl(control, false, null,
                VRCLensLocalizationCatalog.ForLocale(profile.LocaleCode)) > 0;
        }

        /// <summary>Starts one per-avatar Menu Favorites hand-off before its pages are built.</summary>
        internal static void BeginFavoriteBridge(GameObject avatar)
        {
            BeginFavoriteBridge(avatar,
                VRCLensLocalizationRegistry.GetRequiredProfile("ja-JP"));
        }

        internal static void BeginFavoriteBridge(GameObject avatar,
                                                 VRCLensLocalizationProfile profile)
        {
            if (avatar == null) throw new ArgumentNullException(nameof(avatar));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            int avatarId = avatar.GetInstanceID();
            lock (FavoriteTokenLock)
            {
                // A previous failed build of this same avatar cannot leak tokens into this build.
                FavoriteBridges.Remove(avatarId);
                FavoriteBridges[avatarId] = new FavoriteBridge
                {
                    AvatarId = avatarId,
                    LocaleCode = profile.LocaleCode,
                };
            }
        }

        /// <summary>
        /// Replaces one already-resolved Menu Favorites leaf name with a unique build token. Call this
        /// after MenuFavoritesNaming.Resolve and only in a build for which BeginFavoriteBridge ran.
        /// Apply restores explicit aliases verbatim and localizes blank aliases from the copied
        /// control's signature/source/catalog id. The token is never allowed into the uploaded menu.
        /// </summary>
        internal static void TagFavoriteControl(
            GameObject avatar,
            VRCExpressionsMenu.Control control,
            string catalogId,
            string sourceName,
            string parentLabel,
            string chosenAlias)
        {
            if (avatar == null) throw new ArgumentNullException(nameof(avatar));
            if (control == null) throw new ArgumentNullException(nameof(control));
            lock (FavoriteTokenLock)
            {
                FavoriteBridge bridge;
                if (!FavoriteBridges.TryGetValue(avatar.GetInstanceID(), out bridge))
                    throw new InvalidOperationException(
                        "TagFavoriteControl was called without BeginFavoriteBridge.");

                string token = FavoriteTokenPrefix
                               + unchecked((uint)bridge.AvatarId).ToString("X8") + "_"
                               + (++bridge.Serial).ToString("X4") + "__";
                bridge.Tokens[token] = new FavoriteToken
                {
                    CatalogId = catalogId ?? "",
                    ResolvedName = control.name ?? "",
                    SourceName = sourceName ?? "",
                    ParentLabel = parentLabel ?? "",
                    ChosenAlias = chosenAlias ?? "",
                };
                control.name = token;
            }
        }

        private static string LocalizeFavoriteQualifier(string english,
                                                         VRCLensLocalizationCatalog catalog)
        {
            switch (english)
            {
                case "Drop": return catalog.CameraPinDrop;
            }
            string localized;
            return catalog.KnownNames.TryGetValue(english, out localized) ? localized : english;
        }

        private static HashSet<VRCExpressionsMenu.Control> RestoreFavoriteTokens(
            GameObject avatar,
            VRCExpressionsMenu descriptorRoot,
            VRCLensLocalizationCatalog catalog,
            out int restored,
            out bool usedBridge)
        {
            restored = 0;
            int avatarId = avatar.GetInstanceID();
            FavoriteBridge bridge;
            lock (FavoriteTokenLock) FavoriteBridges.TryGetValue(avatarId, out bridge);
            usedBridge = bridge != null;
            if (bridge != null
                && !string.Equals(bridge.LocaleCode, catalog.LocaleCode,
                                  StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Menu Favorites localization bridge locale '{bridge.LocaleCode}' does not match " +
                    $"the selected locale '{catalog.LocaleCode}'.");
            var preserve = new HashSet<VRCExpressionsMenu.Control>();
            foreach (var menu in CollectMenus(descriptorRoot))
            {
                foreach (var control in menu.controls ?? new List<VRCExpressionsMenu.Control>())
                {
                    if (control == null || string.IsNullOrEmpty(control.name)
                        || !control.name.StartsWith(FavoriteTokenPrefix, StringComparison.Ordinal))
                        continue;
                    FavoriteToken token;
                    if (bridge == null || !bridge.Tokens.TryGetValue(control.name, out token))
                        throw new InvalidOperationException(
                            "A foreign or stale Menu Favorites localization token remained in the " +
                            "merged menu: " + control.name);
                    if (token.Consumed)
                        throw new InvalidOperationException(
                            "A Menu Favorites localization token appeared more than once: " + control.name);

                    bool explicitAlias = !string.IsNullOrWhiteSpace(token.ChosenAlias);
                    control.name = explicitAlias
                        ? token.ChosenAlias.Trim()
                        : ResolveFavoriteBlankName(control, token, catalog);
                    token.Consumed = true;
                    if (explicitAlias) preserve.Add(control);
                    restored++;
                    // A page may live outside the VRCLens subtree. Its copied puppet still needs the
                    // same localized direction labels, even when its display name is an explicit alias.
                    restored += TranslateDirectionLabels(control, null, catalog);
                }
            }

            if (bridge != null)
            {
                var missing = bridge.Tokens.Where(pair => !pair.Value.Consumed)
                    .Select(pair => pair.Key).ToArray();
                if (missing.Length > 0)
                    throw new InvalidOperationException(
                        "VRCFury did not hand these Menu Favorites localization token(s) to the final " +
                        "menu: " + string.Join(", ", missing));
            }

            // Belt-and-suspenders scan: a token from any bridge/domain-loss must stop the build.
            var residual = CollectMenus(descriptorRoot)
                .SelectMany(menu => menu.controls ?? new List<VRCExpressionsMenu.Control>())
                .Where(control => control != null && (control.name ?? "")
                    .StartsWith(FavoriteTokenPrefix, StringComparison.Ordinal))
                .Select(control => control.name).ToArray();
            if (residual.Length > 0)
                throw new InvalidOperationException(
                    "Unresolved Menu Favorites localization token(s) remain: "
                    + string.Join(", ", residual));
            return preserve;
        }

        private static string ResolveFavoriteBlankName(VRCExpressionsMenu.Control control,
                                                        FavoriteToken token,
                                                        VRCLensLocalizationCatalog catalog)
        {
            string sourceName = token.SourceName;
            if (string.IsNullOrEmpty(sourceName) && !string.IsNullOrEmpty(token.CatalogId))
            {
                // Menu Favorites belongs to the optional Free Camera Add-ons package. Resolve its
                // catalog only when that package is present so the localization-only unitypackage
                // has no compile-time dependency on any Free Add-ons class.
                sourceName = ResolveOptionalFavoriteCatalogName(token.CatalogId);
            }
            if (string.IsNullOrWhiteSpace(sourceName))
                sourceName = token.ResolvedName ?? "";

            string tokenName = control.name;
            control.name = sourceName;
            string leaf;
            bool localizedLeaf = TryLocalizedName(control, catalog, out leaf);
            control.name = tokenName;
            if (!localizedLeaf) leaf = sourceName.Trim();

            bool wasQualified = !string.Equals(token.ResolvedName.Trim(), sourceName.Trim(),
                                                StringComparison.Ordinal);
            if (wasQualified && !string.IsNullOrWhiteSpace(token.ParentLabel))
            {
                string parent = LocalizeFavoriteQualifier(token.ParentLabel.Trim(), catalog);
                return string.IsNullOrEmpty(parent) ? leaf : parent + " " + leaf;
            }
            return leaf;
        }

        private static void EndFavoriteBridge(GameObject avatar)
        {
            if (avatar == null) return;
            int avatarId = avatar.GetInstanceID();
            lock (FavoriteTokenLock)
            {
                FavoriteBridges.Remove(avatarId);
            }
        }

        internal static void CancelFavoriteBridge(GameObject avatar)
        {
            EndFavoriteBridge(avatar);
        }

        internal static void ClearAllFavoriteBridges()
        {
            lock (FavoriteTokenLock)
            {
                FavoriteBridges.Clear();
            }
        }

        private static List<VRCExpressionsMenu> CollectMenus(VRCExpressionsMenu root)
        {
            var result = new List<VRCExpressionsMenu>();
            var seen = new HashSet<VRCExpressionsMenu>();
            Action<VRCExpressionsMenu> visit = null;
            visit = menu =>
            {
                if (menu == null || !seen.Add(menu)) return;
                result.Add(menu);
                foreach (var control in menu.controls ?? new List<VRCExpressionsMenu.Control>())
                {
                    if (control != null && control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                        visit(control.subMenu);
                }
            };
            visit(root);
            return result;
        }

        private static bool IsVRCLensRoot(VRCExpressionsMenu menu)
        {
            if (menu == null || menu.controls == null) return false;
            bool enable = menu.controls.Any(control =>
                control != null
                && control.type == VRCExpressionsMenu.Control.ControlType.Toggle
                && ParameterName(control) == "VRCLT_Enabled");
            bool zoom = menu.controls.Any(control =>
                control != null
                && control.type == VRCExpressionsMenu.Control.ControlType.RadialPuppet
                && HasSubParameter(control, "VRCLZoomRadial"));
            return enable && zoom;
        }

        private static string DescribeMenus(IEnumerable<VRCExpressionsMenu> menus)
        {
            return string.Join(", ", menus.Select(menu =>
            {
                var path = AssetDatabase.GetAssetPath(menu);
                return string.IsNullOrEmpty(path) ? menu.name : path;
            }));
        }

        private static void RestoreClonedMenuObjectNames(
            Dictionary<VRCExpressionsMenu, VRCExpressionsMenu> copies)
        {
            foreach (var pair in copies)
            {
                if (pair.Key == null || pair.Value == null)
                    throw new InvalidOperationException(
                        "VRCLens localization lost a source or cloned menu while restoring object names.");
                if (!string.Equals(pair.Value.name, pair.Key.name, StringComparison.Ordinal))
                {
                    pair.Value.name = pair.Key.name;
                    EditorUtility.SetDirty(pair.Value);
                }
            }
        }

        private static void SaveClonedMenusWithStableObjectNames(
            Dictionary<VRCExpressionsMenu, VRCExpressionsMenu> copies)
        {
            // CreateAsset imports most generated menus immediately, but Unity can defer the final
            // asset in a large graph until SaveAssetIfDirty. That late first import derives
            // Object.name from the numbered filename after RestoreClonedMenuObjectNames already ran.
            // A bounded second pass occurs after that import and persists only affected clones.
            const int maxPasses = 3;
            for (int pass = 1; pass <= maxPasses; pass++)
            {
                RestoreClonedMenuObjectNames(copies);
                foreach (var clone in copies.Values)
                    AssetDatabase.SaveAssetIfDirty(clone);

                var mismatches = copies.Where(pair => pair.Key == null || pair.Value == null
                    || !string.Equals(pair.Key.name, pair.Value.name, StringComparison.Ordinal))
                    .ToArray();
                if (mismatches.Length == 0) return;

                if (pass < maxPasses)
                    Debug.LogWarning($"{LogPrefix} Unity deferred the import of " +
                                     $"{mismatches.Length} generated menu asset(s); stabilizing " +
                                     $"their internal names (pass {pass + 1}/{maxPasses}).");
            }

            AssertClonedMenuObjectNames(copies);
        }

        private static void AssertClonedMenuObjectNames(
            Dictionary<VRCExpressionsMenu, VRCExpressionsMenu> copies)
        {
            foreach (var pair in copies)
            {
                if (pair.Key == null || pair.Value == null
                    || !string.Equals(pair.Key.name, pair.Value.name, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "VRCLens localization could not preserve a cloned menu's internal object " +
                        $"name after saving ('{pair.Key?.name ?? "<missing>"}' -> " +
                        $"'{pair.Value?.name ?? "<missing>"}').");
            }
        }

        private static VRCExpressionsMenu CloneMenuGraph(
            VRCExpressionsMenu source,
            string destinationFolder,
            Dictionary<VRCExpressionsMenu, VRCExpressionsMenu> copies,
            ref int serial,
            bool writeAssets)
        {
            if (source == null) return null;
            VRCExpressionsMenu existing;
            if (copies.TryGetValue(source, out existing)) return existing;

            // Instantiate serializes all present and future SDK fields. The explicit submenu rewrite
            // below is the only graph-level change and preserves shared references/cycles through memo.
            var clone = UnityEngine.Object.Instantiate(source);
            clone.name = source.name;
            copies[source] = clone; // before recursion: required for cyclic menu graphs

            int thisSerial = serial++;
            if (writeAssets)
            {
                string safeName = SanitizeFileName(
                    string.IsNullOrEmpty(source.name) ? "Menu" : source.name);
                // Put the uniqueness token in a directory rather than in the asset filename.
                // Unity derives Object.name from that filename during the first import, so a
                // numbered filename can overwrite the source menu name when the final import is
                // deferred until SaveAssetIfDirty.
                string serialFolder = $"{destinationFolder}/{thisSerial:D3}";
                if (!EnsureAssetFolder(serialFolder))
                    throw new InvalidOperationException(
                        $"Could not create temporary menu folder '{serialFolder}'.");
                string path = AssetDatabase.GenerateUniqueAssetPath(
                    $"{serialFolder}/{safeName}.asset");
                AssetDatabase.CreateAsset(clone, path);
                // Blank names and names containing filesystem-invalid characters can still differ
                // from safeName, so preserve the explicit restoration as a fallback.
                clone.name = source.name;
                EditorUtility.SetDirty(clone);
            }

            var sourceControls = source.controls ?? new List<VRCExpressionsMenu.Control>();
            if (clone.controls == null || clone.controls.Count != sourceControls.Count)
                throw new InvalidOperationException($"Cloning menu '{source.name}' changed its control count.");

            for (int i = 0; i < sourceControls.Count; i++)
            {
                var sourceControl = sourceControls[i];
                var cloneControl = clone.controls[i];
                if (sourceControl == null || cloneControl == null) continue;
                if (sourceControl.type == VRCExpressionsMenu.Control.ControlType.SubMenu
                    && sourceControl.subMenu != null)
                {
                    cloneControl.subMenu = CloneMenuGraph(sourceControl.subMenu, destinationFolder,
                                                         copies, ref serial, writeAssets);
                }
            }
            return clone;
        }

        private static bool TryPrepareOutputFolder(string tempDir, string localeCode,
                                                   out string outputFolder)
        {
            outputFolder = "";
            string normalized = (tempDir ?? "").Replace('\\', '/').TrimEnd('/');
            if (normalized != "Assets" && !normalized.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Debug.LogError($"{LogPrefix} Localization needs a project-relative temporary " +
                               $"folder below Assets, but received '{tempDir ?? "(null)"}'.");
                return false;
            }
            if (!EnsureAssetFolder(normalized)) return false;
            string root = normalized + "/LocalizedMenus";
            if (!EnsureAssetFolder(root)) return false;
            outputFolder = root + "/" + SanitizeFileName(localeCode);
            // This directory contains build-only copies from a previous run. Removing only the
            // selected locale keeps the translator independent from optional add-ons that also use
            // Assets/VRCLens_Custom/Temp, while preventing names such as "... 1.asset" from
            // accumulating across builds.
            if (AssetDatabase.IsValidFolder(outputFolder)
                && !AssetDatabase.DeleteAsset(outputFolder))
            {
                Debug.LogError($"{LogPrefix} Could not clear the previous localization output " +
                               $"folder '{outputFolder}'.");
                return false;
            }
            return EnsureAssetFolder(outputFolder);
        }

        private static void AssertDisplayOnlyClone(
            VRCExpressionsMenu sourceRoot,
            VRCExpressionsMenu cloneRoot)
        {
            AssertDisplayOnlySchema();
            if (sourceRoot == null || cloneRoot == null || ReferenceEquals(sourceRoot, cloneRoot))
                throw new InvalidOperationException(
                    "VRCLens localization did not produce an independent menu root clone.");

            var sourceToClone = new Dictionary<VRCExpressionsMenu, VRCExpressionsMenu>();
            var cloneToSource = new Dictionary<VRCExpressionsMenu, VRCExpressionsMenu>();
            var pending = new Queue<KeyValuePair<VRCExpressionsMenu, VRCExpressionsMenu>>();
            sourceToClone.Add(sourceRoot, cloneRoot);
            cloneToSource.Add(cloneRoot, sourceRoot);
            pending.Enqueue(new KeyValuePair<VRCExpressionsMenu, VRCExpressionsMenu>(
                sourceRoot, cloneRoot));

            while (pending.Count > 0)
            {
                var pair = pending.Dequeue();
                var source = pair.Key;
                var clone = pair.Value;
                if (!string.Equals(source.name, clone.name, StringComparison.Ordinal)
                    || source.hideFlags != clone.hideFlags
                    || !ReferenceEquals(source.Parameters, clone.Parameters))
                    throw new InvalidOperationException(
                        $"VRCLens localization changed non-display menu data on '{source.name}'.");

                var sourceControls = source.controls;
                var cloneControls = clone.controls;
                if ((sourceControls == null) != (cloneControls == null)
                    || (sourceControls != null && sourceControls.Count != cloneControls.Count))
                    throw new InvalidOperationException(
                        $"VRCLens localization changed the control list on '{source.name}'.");
                if (sourceControls == null) continue;
                if (ReferenceEquals(sourceControls, cloneControls))
                    throw new InvalidOperationException(
                        $"VRCLens localization shared a mutable control list with '{source.name}'.");

                for (int index = 0; index < sourceControls.Count; index++)
                {
                    var sourceControl = sourceControls[index];
                    var cloneControl = cloneControls[index];
                    if ((sourceControl == null) != (cloneControl == null))
                        throw new InvalidOperationException(
                            $"VRCLens localization changed null control #{index} on '{source.name}'.");
                    if (sourceControl == null) continue;

                    if (ReferenceEquals(sourceControl, cloneControl))
                        throw new InvalidOperationException(
                            $"VRCLens localization shared control #{index} with '{source.name}'.");

                    var behavioralDifferences = new List<string>();
                    if (sourceControl.icon != cloneControl.icon)
                        behavioralDifferences.Add("icon");
                    if (sourceControl.type != cloneControl.type)
                        behavioralDifferences.Add("type");
                    if (FloatBits(sourceControl.value) != FloatBits(cloneControl.value))
                        behavioralDifferences.Add(
                            $"value ({sourceControl.value:R} -> {cloneControl.value:R})");
                    if (sourceControl.style != cloneControl.style)
                        behavioralDifferences.Add("style");
                    if (!SameMenuParameter(sourceControl.parameter, cloneControl.parameter))
                        behavioralDifferences.Add("parameter");
                    if (!SameMenuParameters(sourceControl.subParameters,
                                            cloneControl.subParameters))
                        behavioralDifferences.Add("subParameters");
                    if (!SameLabelsExceptName(sourceControl.labels, cloneControl.labels))
                        behavioralDifferences.Add("labels");
                    if (behavioralDifferences.Count > 0)
                        throw new InvalidOperationException(
                            $"VRCLens localization changed behavioral data on control #{index} " +
                            $"in '{source.name}': {string.Join(", ", behavioralDifferences)}.");

                    var sourceSubMenu = sourceControl.subMenu;
                    var cloneSubMenu = cloneControl.subMenu;
                    if ((sourceSubMenu == null) != (cloneSubMenu == null))
                        throw new InvalidOperationException(
                            $"VRCLens localization changed submenu link #{index} in '{source.name}'.");
                    if (sourceSubMenu == null) continue;
                    if (ReferenceEquals(sourceSubMenu, cloneSubMenu))
                        throw new InvalidOperationException(
                            $"VRCLens localization retained a source submenu on '{source.name}'.");

                    VRCExpressionsMenu mappedClone;
                    if (sourceToClone.TryGetValue(sourceSubMenu, out mappedClone))
                    {
                        if (!ReferenceEquals(mappedClone, cloneSubMenu))
                            throw new InvalidOperationException(
                                "VRCLens localization did not preserve a shared submenu or cycle.");
                        continue;
                    }
                    VRCExpressionsMenu mappedSource;
                    if (cloneToSource.TryGetValue(cloneSubMenu, out mappedSource)
                        && !ReferenceEquals(mappedSource, sourceSubMenu))
                        throw new InvalidOperationException(
                            "VRCLens localization merged two distinct source submenus.");
                    sourceToClone.Add(sourceSubMenu, cloneSubMenu);
                    cloneToSource.Add(cloneSubMenu, sourceSubMenu);
                    pending.Enqueue(new KeyValuePair<VRCExpressionsMenu, VRCExpressionsMenu>(
                        sourceSubMenu, cloneSubMenu));
                }
            }
        }

        private static bool SameMenuParameter(
            VRCExpressionsMenu.Control.Parameter source,
            VRCExpressionsMenu.Control.Parameter clone)
        {
            if (source == null || clone == null)
            {
                // Unity serializes a missing nested Parameter as a default Parameter with an empty
                // name. VRC treats both forms as "no parameter".
                var present = source ?? clone;
                return present == null || string.IsNullOrEmpty(present.name);
            }
            return !ReferenceEquals(source, clone)
                   && string.Equals(source.name ?? "", clone.name ?? "",
                                    StringComparison.Ordinal);
        }

        private static bool SameMenuParameters(
            VRCExpressionsMenu.Control.Parameter[] source,
            VRCExpressionsMenu.Control.Parameter[] clone)
        {
            int sourceLength = source == null ? 0 : source.Length;
            int cloneLength = clone == null ? 0 : clone.Length;
            // Unity normalizes null serialized arrays to empty arrays. Both mean no sub-parameters.
            if (sourceLength != cloneLength) return false;
            if (sourceLength > 0 && ReferenceEquals(source, clone)) return false;
            for (int index = 0; index < sourceLength; index++)
                if (!SameMenuParameter(source[index], clone[index])) return false;
            return true;
        }

        private static bool SameLabelsExceptName(
            VRCExpressionsMenu.Control.Label[] source,
            VRCExpressionsMenu.Control.Label[] clone)
        {
            int sourceLength = source == null ? 0 : source.Length;
            int cloneLength = clone == null ? 0 : clone.Length;
            // Unity normalizes null serialized arrays to empty arrays. Both mean no labels.
            if (sourceLength != cloneLength) return false;
            if (sourceLength > 0 && ReferenceEquals(source, clone)) return false;
            for (int index = 0; index < sourceLength; index++)
            {
                object sourceLabel = source[index];
                object cloneLabel = clone[index];
                if ((sourceLabel == null) != (cloneLabel == null)) return false;
                if (sourceLabel != null
                    && (ReferenceEquals(sourceLabel, cloneLabel)
                        || source[index].icon != clone[index].icon)) return false;
            }
            return true;
        }

        private static void AssertDisplayOnlySchema()
        {
            AssertSerializableFields(typeof(VRCExpressionsMenu), "Parameters", "controls");
            AssertSerializableFields(typeof(VRCExpressionsMenu.Control),
                "icon", "labels", "name", "parameter", "style", "subMenu",
                "subParameters", "type", "value");
            AssertSerializableFields(typeof(VRCExpressionsMenu.Control.Parameter), "name");
            AssertSerializableFields(typeof(VRCExpressionsMenu.Control.Label), "icon", "name");
        }

        private static void AssertSerializableFields(Type type, params string[] expectedNames)
        {
            var actual = type.GetFields(BindingFlags.Instance | BindingFlags.Public
                                        | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(field => !field.IsStatic && !field.IsNotSerialized
                                && (field.IsPublic
                                    || field.IsDefined(typeof(SerializeField), true)
                                    || field.IsDefined(typeof(SerializeReference), true)))
                .Select(field => field.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            var expected = expectedNames.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
                throw new InvalidOperationException(
                    $"VRCLens localization cannot prove display-only behavior for the current " +
                    $"{type.FullName} schema. Expected [{string.Join(", ", expected)}], found " +
                    $"[{string.Join(", ", actual)}].");
        }

        private static int FloatBits(float value)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        }

        private static bool EnsureAssetFolder(string folder)
        {
            folder = (folder ?? "").Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(folder)) return true;
            var parts = folder.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || parts[0] != "Assets") return false;
            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    string guid = AssetDatabase.CreateFolder(current, parts[i]);
                    if (string.IsNullOrEmpty(guid))
                    {
                        Debug.LogError($"{LogPrefix} Could not create temporary menu folder '{next}'.");
                        return false;
                    }
                }
                current = next;
            }
            return true;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
            name = name.Replace('/', '_').Replace('\\', '_');
            return string.IsNullOrWhiteSpace(name) ? "Menu" : name.Trim();
        }

        private static HashSet<VRCExpressionsMenu.Control> CollectFavoriteProtections(
            GameObject avatar,
            VRCExpressionsMenu descriptorRoot,
            VRCExpressionsMenu vrclensRoot,
            bool protectAliasesByName,
            VRCLensLocalizationCatalog catalog,
            out int localizedPagination)
        {
            localizedPagination = 0;
            var protectedNames = new HashSet<VRCExpressionsMenu.Control>();
            var favoritePageRoots = new HashSet<VRCExpressionsMenu>();
            foreach (var page in EnumerateOptionalFavoritePages(avatar))
            {
                string normalizedPagePath = NormalizeMenuPath(page.MenuPath);
                if (normalizedPagePath.Length == 0) continue;

                bool isDefaultPath = string.Equals(
                    normalizedPagePath,
                    "VRCLens/Custom/Favorites",
                    StringComparison.Ordinal);
                var pageSegments = normalizedPagePath.Split('/');
                int protectFromSegment = 0;
                if (pageSegments.Length > 0
                    && string.Equals(pageSegments[0], "VRCLens", StringComparison.OrdinalIgnoreCase))
                    protectFromSegment = 1;
                if (pageSegments.Length > 1
                    && string.Equals(pageSegments[0], "VRCLens", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(pageSegments[1], "Custom", StringComparison.OrdinalIgnoreCase))
                    protectFromSegment = 2;
                if (isDefaultPath) protectFromSegment = int.MaxValue;
                VRCExpressionsMenu pageMenu;
                if (!TryFollowMenuPath(descriptorRoot, vrclensRoot, normalizedPagePath,
                                       protectFromSegment, protectedNames, out pageMenu))
                    continue;
                favoritePageRoots.Add(pageMenu);

                foreach (var item in page.Items)
                {
                    VRCExpressionsMenu itemMenu = pageMenu;
                    string submenuPath = NormalizeMenuPath(item.Submenu);
                    if (submenuPath.Length > 0)
                    {
                        VRCExpressionsMenu found;
                        if (TryFollowRelativePath(pageMenu, submenuPath, true, protectedNames,
                                                  favoritePageRoots, out found))
                        {
                            itemMenu = found;
                            favoritePageRoots.Add(found);
                        }
                    }

                    // Compatibility fallback for a Free Add-ons build whose Menu Favorites hook
                    // predates the localization token bridge. Explicit aliases remain user text.
                    if (protectAliasesByName && !string.IsNullOrWhiteSpace(item.Name))
                    {
                        string chosen = item.Name.Trim();
                        // A chosen name is never qualified by MenuFavoritesNaming. Search the page
                        // family because VRCFury may have moved the row behind one or more Next links.
                        foreach (var control in ControlsAcrossGeneratedPages(itemMenu))
                        {
                            if (control != null && string.Equals(control.name, chosen,
                                                                  StringComparison.Ordinal))
                                protectedNames.Add(control);
                        }
                    }
                }
            }
            var seenPaginationMenus = new HashSet<VRCExpressionsMenu>();
            var vrclensMenus = new HashSet<VRCExpressionsMenu>(CollectMenus(vrclensRoot));
            foreach (var pageRoot in favoritePageRoots)
            {
                localizedPagination += LocalizeFavoritePagination(
                    pageRoot, seenPaginationMenus, catalog);
                if (protectAliasesByName)
                    localizedPagination += LocalizeFavoriteQualifiedNames(
                        pageRoot, protectedNames, catalog);
                // A Favorites page may live outside the VRCLens subtree. Translate only controls
                // carrying VRCLens/VRCL_Custom semantics, so blank aliases still follow the selected
                // language even with an older Free Add-ons hook that has no token bridge.
                if (!vrclensMenus.Contains(pageRoot))
                    localizedPagination += TranslateGraph(
                        pageRoot, vrclensRoot, protectedNames, null, catalog);
            }
            return protectedNames;
        }

        /// <summary>
        /// Older Menu Favorites builds qualify duplicate blank aliases (for example "Drop Pin 1",
        /// "Shadows Contrast" or "Save 1") before this package sees them. Resolve only those
        /// controls on proven Favorites pages; user-entered aliases and unrelated avatar controls
        /// remain protected.
        /// </summary>
        private static int LocalizeFavoriteQualifiedNames(
            VRCExpressionsMenu pageRoot,
            HashSet<VRCExpressionsMenu.Control> protectedNames,
            VRCLensLocalizationCatalog catalog)
        {
            int changed = 0;
            foreach (var control in ControlsAcrossGeneratedPages(pageRoot))
            {
                if (control == null || protectedNames.Contains(control)) continue;
                string localized;
                if (!TryLocalizedName(control, catalog, out localized)
                    && TryLocalizedFavoriteQualifiedName(control, catalog, out localized)
                    && !string.Equals(control.name, localized, StringComparison.Ordinal))
                {
                    control.name = localized;
                    changed++;
                }
            }
            return changed;
        }

        private static bool TryLocalizedFavoriteQualifiedName(
            VRCExpressionsMenu.Control control,
            VRCLensLocalizationCatalog catalog,
            out string localized)
        {
            localized = null;
            if (control == null || !IsVRCLensSemanticControl(control)) return false;
            string name = (control.name ?? "").Trim();
            if (name.Length == 0) return false;

            var prefixes = catalog.KnownNames.Keys.Concat(new[] { "Drop" })
                .Distinct(StringComparer.Ordinal)
                .Where(prefix => name.StartsWith(prefix + " ", StringComparison.Ordinal))
                .OrderByDescending(prefix => prefix.Length);
            foreach (string prefix in prefixes)
            {
                string suffix = name.Substring(prefix.Length).TrimStart();
                string localizedSuffix;
                if (Regex.IsMatch(suffix, @"^[+-]?\d+(?:\.\d+)?(?:/\d+)?$",
                                  RegexOptions.CultureInvariant))
                {
                    localizedSuffix = suffix;
                }
                else
                {
                    string original = control.name;
                    control.name = suffix;
                    bool recognized = TryLocalizedName(control, catalog, out localizedSuffix);
                    control.name = original;
                    if (!recognized) continue;
                }

                string localizedPrefix = LocalizeFavoriteQualifier(prefix, catalog);
                if (string.Equals(localizedPrefix, prefix, StringComparison.Ordinal)
                    && prefix != "Drop" && !catalog.KnownNames.ContainsKey(prefix))
                    continue;
                localized = localizedPrefix + " " + localizedSuffix;
                return true;
            }
            return false;
        }

        private sealed class OptionalFavoritePage
        {
            internal string MenuPath;
            internal readonly List<OptionalFavoriteItem> Items =
                new List<OptionalFavoriteItem>();
        }

        private sealed class OptionalFavoriteItem
        {
            internal string Name;
            internal string Submenu;
        }

        private static GameObject favoriteSnapshotAvatar;
        private static List<OptionalFavoritePage> favoritePageSnapshot;

        internal static void CaptureFavoritePages(GameObject avatar)
        {
            ClearFavoritePageSnapshot();
            var pages = ReadOptionalFavoritePages(avatar).ToList();
            favoriteSnapshotAvatar = avatar;
            favoritePageSnapshot = pages;
        }

        internal static void ClearFavoritePageSnapshot()
        {
            favoriteSnapshotAvatar = null;
            favoritePageSnapshot = null;
        }

        private static IEnumerable<OptionalFavoritePage> EnumerateOptionalFavoritePages(GameObject avatar)
        {
            return ReferenceEquals(avatar, favoriteSnapshotAvatar) && favoritePageSnapshot != null
                ? favoritePageSnapshot : ReadOptionalFavoritePages(avatar);
        }

        /// <summary>
        /// Reads Menu Favorites when Free Camera Add-ons is installed without linking against that
        /// optional package. Public serialized member names are its compatibility contract, and an
        /// unknown/newer shape stops localization rather than risking changes to user aliases or
        /// paths. The optional component itself remains completely absent from the compile graph.
        /// </summary>
        private static IEnumerable<OptionalFavoritePage> ReadOptionalFavoritePages(
            GameObject avatar)
        {
            if (avatar == null) yield break;
            foreach (var behaviour in avatar.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null
                    || !string.Equals(behaviour.GetType().FullName,
                                      "VRCLensCustom.VRCLensMenuFavorites",
                                      StringComparison.Ordinal))
                    continue;

                foreach (var rawPage in EnumerateObjects(
                             ReadRequiredMember(behaviour, "pages")))
                {
                    if (rawPage == null) continue;
                    var page = new OptionalFavoritePage
                    {
                        MenuPath = ReadRequiredStringMember(rawPage, "menuPath"),
                    };
                    foreach (var rawItem in EnumerateObjects(
                                 ReadRequiredMember(rawPage, "items")))
                    {
                        if (rawItem == null) continue;
                        page.Items.Add(new OptionalFavoriteItem
                        {
                            Name = ReadRequiredStringMember(rawItem, "name"),
                            Submenu = ReadRequiredStringMember(rawItem, "submenu"),
                        });
                    }
                    yield return page;
                }
            }
        }

        private static string ResolveOptionalFavoriteCatalogName(string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId)) return "";
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type catalogType;
                try
                {
                    catalogType = assembly.GetType(
                        "VRCLensCustom.VRCLensMenuCatalog", false, false);
                }
                catch
                {
                    continue;
                }
                if (catalogType == null) continue;
                var find = catalogType.GetMethod(
                    "Find", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    null, new[] { typeof(string) }, null);
                if (find == null) return "";
                try
                {
                    var entry = find.Invoke(null, new object[] { catalogId });
                    if (entry == null) return "";
                    string nameOverride = ReadStringMember(entry, "NameOverride");
                    return !string.IsNullOrEmpty(nameOverride)
                        ? nameOverride
                        : ReadStringMember(entry, "DisplayName");
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"{LogPrefix} Could not read the optional Menu Favorites " +
                                     $"catalog entry '{catalogId}': {exception.GetBaseException().Message}");
                    return "";
                }
            }
            return "";
        }

        private static IEnumerable<object> EnumerateObjects(object value)
        {
            if (value == null) yield break;
            var enumerable = value as IEnumerable;
            if (enumerable == null || value is string)
                throw new InvalidOperationException(
                    "The installed Menu Favorites pages/items member is not an enumerable " +
                    "collection. Update the localization add-on or remove Menu Favorites before " +
                    "building.");
            foreach (var item in enumerable) yield return item;
        }

        private static object ReadMember(object target, string memberName)
        {
            object value;
            return TryReadMember(target, memberName, out value) ? value : null;
        }

        private static object ReadRequiredMember(object target, string memberName)
        {
            object value;
            if (TryReadMember(target, memberName, out value)) return value;
            throw new InvalidOperationException(
                $"The installed Menu Favorites schema has no readable '{memberName}' member on " +
                $"'{target?.GetType().FullName ?? "<null>"}'. Update the localization add-on or " +
                "remove Menu Favorites before building.");
        }

        private static bool TryReadMember(object target, string memberName, out object value)
        {
            value = null;
            if (target == null || string.IsNullOrEmpty(memberName)) return false;
            var type = target.GetType();
            var field = type.GetField(memberName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                value = field.GetValue(target);
                return true;
            }
            var property = type.GetProperty(memberName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null || property.GetIndexParameters().Length != 0
                || !property.CanRead) return false;
            value = property.GetValue(target, null);
            return true;
        }

        private static string ReadStringMember(object target, string memberName)
        {
            return ReadMember(target, memberName) as string ?? "";
        }

        private static string ReadRequiredStringMember(object target, string memberName)
        {
            object value = ReadRequiredMember(target, memberName);
            if (value == null) return "";
            var text = value as string;
            if (text != null) return text;
            throw new InvalidOperationException(
                $"The installed Menu Favorites member '{target.GetType().FullName}.{memberName}' " +
                "is not a string. Update the localization add-on or remove Menu Favorites before " +
                "building.");
        }

        /// <summary>
        /// Translates pagination only below a page proven to come from VRCLensMenuFavorites. This is
        /// intentionally separate from the VRCLens-subtree pass: a filmer may place Favorites at an
        /// arbitrary avatar-menu path, while an unrelated VRCFury page called Next must stay English.
        /// Only the generated page chain is followed; ordinary submenu links may have existed on a
        /// shared destination page before Favorites merged into it.
        /// </summary>
        private static int LocalizeFavoritePagination(VRCExpressionsMenu menu,
                                                       HashSet<VRCExpressionsMenu> seen,
                                                       VRCLensLocalizationCatalog catalog)
        {
            if (menu == null || !seen.Add(menu)) return 0;
            int changed = 0;
            foreach (var control in menu.controls ?? new List<VRCExpressionsMenu.Control>())
            {
                if (!IsGeneratedNext(control)) continue;
                if (!string.Equals(control.name, catalog.Next, StringComparison.Ordinal))
                {
                    control.name = catalog.Next;
                    changed++;
                }
                changed += LocalizeFavoritePagination(control.subMenu, seen, catalog);
            }
            return changed;
        }

        private static string NormalizeMenuPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            return string.Join("/", path.Replace('\\', '/')
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => segment.Trim())
                .Where(segment => segment.Length > 0));
        }

        private static bool TryFollowMenuPath(
            VRCExpressionsMenu descriptorRoot,
            VRCExpressionsMenu vrclensRoot,
            string path,
            int protectFromSegment,
            HashSet<VRCExpressionsMenu.Control> protectedNames,
            out VRCExpressionsMenu result)
        {
            var segments = NormalizeMenuPath(path).Split(new[] { '/' },
                StringSplitOptions.RemoveEmptyEntries);
            VRCExpressionsMenu current = descriptorRoot;
            int first = 0;
            if (descriptorRoot == vrclensRoot && segments.Length > 0
                && string.Equals(segments[0], "VRCLens", StringComparison.OrdinalIgnoreCase))
                first = 1;

            for (int i = first; i < segments.Length; i++)
            {
                VRCExpressionsMenu.Control link;
                if (!TryFindSubMenuLink(current, segments[i], out link))
                {
                    result = null;
                    return false;
                }
                // VRCLens/Custom is the add-ons' own known prefix and should become
                // VRCLens/追加機能. Everything after it is the user's page path.
                if (i >= protectFromSegment) protectedNames.Add(link);
                current = link.subMenu;
                if (current == null)
                {
                    result = null;
                    return false;
                }
            }
            result = current;
            return true;
        }

        private static bool TryFollowRelativePath(
            VRCExpressionsMenu root,
            string path,
            bool protectPath,
            HashSet<VRCExpressionsMenu.Control> protectedNames,
            HashSet<VRCExpressionsMenu> ownedMenus,
            out VRCExpressionsMenu result)
        {
            VRCExpressionsMenu current = root;
            if (ownedMenus != null) ownedMenus.Add(current);
            foreach (string segment in NormalizeMenuPath(path).Split(new[] { '/' },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                VRCExpressionsMenu.Control link;
                if (!TryFindSubMenuLink(current, segment, out link))
                {
                    result = null;
                    return false;
                }
                if (protectPath) protectedNames.Add(link);
                current = link.subMenu;
                if (ownedMenus != null && current != null) ownedMenus.Add(current);
            }
            result = current;
            return true;
        }

        private static bool TryFindSubMenuLink(VRCExpressionsMenu menu, string segment,
                                                out VRCExpressionsMenu.Control found)
        {
            foreach (var control in ControlsAcrossGeneratedPages(menu))
            {
                if (control == null || control.type != VRCExpressionsMenu.Control.ControlType.SubMenu)
                    continue;
                if (IsGeneratedNext(control)) continue;
                if (string.Equals((control.name ?? "").Trim(), segment.Trim(),
                                  StringComparison.OrdinalIgnoreCase))
                {
                    found = control;
                    return true;
                }
            }
            found = null;
            return false;
        }

        private static IEnumerable<VRCExpressionsMenu.Control> ControlsAcrossGeneratedPages(
            VRCExpressionsMenu first)
        {
            var page = first;
            var seen = new HashSet<VRCExpressionsMenu>();
            while (page != null && seen.Add(page))
            {
                var controls = page.controls ?? new List<VRCExpressionsMenu.Control>();
                foreach (var control in controls) yield return control;
                page = controls.FirstOrDefault(IsGeneratedNext)?.subMenu;
            }
        }

        private static bool IsGeneratedNext(VRCExpressionsMenu.Control control)
        {
            return control != null
                   && control.type == VRCExpressionsMenu.Control.ControlType.SubMenu
                   && string.Equals(control.name, "Next", StringComparison.Ordinal)
                   && control.subMenu != null
                   && (control.subMenu.name ?? "").IndexOf("(Page ", StringComparison.Ordinal) >= 0;
        }

        private static int TranslateGraph(
            VRCExpressionsMenu root,
            VRCExpressionsMenu excluded,
            HashSet<VRCExpressionsMenu.Control> protectedNames,
            HashSet<string> unknown,
            VRCLensLocalizationCatalog catalog)
        {
            int changed = 0;
            var seen = new HashSet<VRCExpressionsMenu>();
            Action<VRCExpressionsMenu> visit = null;
            visit = menu =>
            {
                if (menu == null || menu == excluded || !seen.Add(menu)) return;
                foreach (var control in menu.controls ?? new List<VRCExpressionsMenu.Control>())
                {
                    if (control == null) continue;
                    changed += TranslateControl(control,
                        protectedNames != null && protectedNames.Contains(control), unknown,
                        catalog);
                    if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                        visit(control.subMenu);
                }
            };
            visit(root);
            return changed;
        }

        private static int TranslateControl(VRCExpressionsMenu.Control control, bool protectName,
                                            HashSet<string> unknown,
                                            VRCLensLocalizationCatalog catalog)
        {
            int changed = 0;
            bool recognizedName = false;
            string localized;
            if (TryLocalizedName(control, catalog, out localized))
            {
                recognizedName = true;
                if (!protectName && !string.Equals(control.name, localized, StringComparison.Ordinal))
                {
                    control.name = localized;
                    changed++;
                }
            }

            bool semantic = recognizedName || IsVRCLensSemanticControl(control);
            if (semantic) changed += TranslateDirectionLabels(control, unknown, catalog);

            if (!recognizedName && !protectName && unknown != null
                && ShouldReportUnknown(control))
                unknown.Add(control.name.Trim());
            return changed;
        }

        private static int TranslateDirectionLabels(VRCExpressionsMenu.Control control,
                                                     HashSet<string> unknown,
                                                     VRCLensLocalizationCatalog catalog)
        {
            if (control == null || control.labels == null) return 0;
            int changed = 0;
            for (int i = 0; i < control.labels.Length; i++)
            {
                var label = control.labels[i];
                if (string.IsNullOrWhiteSpace(label.name))
                {
                    // VRCLens 1.10.0 leaves all four Move Pivot captions empty even though the
                    // puppet directions are functional. Add captions on the build copy only. The
                    // disabled horizontal axes of Move Drone Vertical intentionally remain blank.
                    string implied;
                    if (TryImpliedDirectionLabel(control, i, catalog, out implied))
                    {
                        label.name = implied;
                        control.labels[i] = label;
                        changed++;
                    }
                    continue;
                }
                string localized;
                if (catalog.DirectionLabels.TryGetValue(label.name.Trim(), out localized))
                {
                    if (!string.Equals(label.name, localized, StringComparison.Ordinal))
                    {
                        label.name = localized;
                        control.labels[i] = label;
                        changed++;
                    }
                }
                else if (unknown != null && ContainsLatinLetter(label.name))
                {
                    unknown.Add("label: " + label.name);
                }
            }
            return changed;
        }

        private static bool TryImpliedDirectionLabel(VRCExpressionsMenu.Control control,
                                                     int labelIndex,
                                                     VRCLensLocalizationCatalog catalog,
                                                     out string localized)
        {
            localized = null;
            if (control == null
                || control.type != VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet
                || ParameterName(control) != FeatureToggle
                || control.labels == null)
                return false;

            return catalog.TryImpliedPivotDirection(
                control.value,
                HasSubParameter(control, "VRCFaceBlendH"),
                HasSubParameter(control, "VRCFaceBlendV"),
                control.labels.Length,
                labelIndex,
                out localized);
        }

        private static bool IsIntentionalBlankDirectionLabel(
            VRCExpressionsMenu.Control control,
            int labelIndex)
        {
            return control != null
                   && control.type == VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet
                   && ParameterName(control) == FeatureToggle
                   && Approximately(control.value, 212)
                   && control.labels != null && control.labels.Length == 4
                   && HasSubParameter(control, "VRCLDroneV")
                   && (labelIndex == 1 || labelIndex == 3);
        }

        private static bool TryLocalizedName(VRCExpressionsMenu.Control control,
                                             VRCLensLocalizationCatalog catalog,
                                             out string localized)
        {
            localized = null;
            if (control == null) return false;

            string functionalBlank;
            if (TryFunctionalBlankName(control, catalog, out functionalBlank))
            {
                localized = functionalBlank;
                return true;
            }

            string name = control.name ?? "";
            string parameter = ParameterName(control);

            bool submenuContainsVrclensParameters =
                control.type == VRCExpressionsMenu.Control.ControlType.SubMenu
                && SubtreeHasVRCLensParameter(control.subMenu,
                                              new HashSet<VRCExpressionsMenu>());
            bool generatedNextWithinVrclens = IsGeneratedNext(control)
                && SubtreeHasVRCLensParameter(control.subMenu,
                                              new HashSet<VRCExpressionsMenu>());
            if (catalog.TryContextualName(
                    name, parameter, control.type, control.value,
                    submenuContainsVrclensParameters, generatedNextWithinVrclens,
                    out localized))
                return true;

            string known;
            if (catalog.KnownNames.TryGetValue(name, out known)
                && (IsVRCLensSemanticControl(control)
                    || (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu
                        && SubtreeHasVRCLensParameter(control.subMenu,
                                                     new HashSet<VRCExpressionsMenu>()))))
            {
                localized = known;
                return true;
            }
            return false;
        }

        private static bool TryFunctionalBlankName(VRCExpressionsMenu.Control control,
                                                   VRCLensLocalizationCatalog catalog,
                                                   out string localized)
        {
            localized = null;
            if (control == null || !string.IsNullOrWhiteSpace(control.name)) return false;
            return catalog.TryFunctionalBlankName(
                ParameterName(control), control.type, control.value, out localized);
        }

        private static bool IsCameraPinParameter(string parameter)
        {
            if (string.IsNullOrEmpty(parameter)) return false;
            return parameter == "VRCL_Custom/DollyToPin"
                   || parameter == "VRCL_Custom/TelePin"
                   || parameter.StartsWith("VRCL_Custom/DropPin", StringComparison.Ordinal);
        }

        private static bool IsVRCLensSemanticControl(VRCExpressionsMenu.Control control)
        {
            if (control == null) return false;
            if (IsVRCLensParameter(ParameterName(control))) return true;
            if (control.subParameters != null
                && control.subParameters.Any(parameter => IsVRCLensParameter(ParameterName(parameter))))
                return true;
            return false;
        }

        private static bool IsVRCLensParameter(string parameter)
        {
            if (string.IsNullOrEmpty(parameter)) return false;
            if (parameter.StartsWith(CustomParameterPrefix, StringComparison.Ordinal)
                || parameter.StartsWith("VRCLS_", StringComparison.Ordinal)
                || parameter.StartsWith("VRCLT_", StringComparison.Ordinal)
                || parameter.StartsWith("VRCLInt_", StringComparison.Ordinal))
                return true;

            switch (parameter)
            {
                case "VRCLApertureRadial":
                case "VRCLDroneV":
                case "VRCLExposureRadial":
                case FeatureToggle:
                case "VRCLFloatX":
                case "VRCLFloatY":
                case "VRCLFocusRadial":
                case "VRCLPeakingHueRadial":
                case "VRCLZoomRadial":
                    return true;
                default:
                    return false;
            }
        }

        private static bool LooksLikeVRCLensParameter(string parameter)
        {
            return !string.IsNullOrEmpty(parameter)
                   && parameter.StartsWith("VRCL", StringComparison.Ordinal);
        }

        private static bool LooksLikeVRCLensSemanticControl(VRCExpressionsMenu.Control control)
        {
            if (control == null) return false;
            if (LooksLikeVRCLensParameter(ParameterName(control))) return true;
            return control.subParameters != null
                   && control.subParameters.Any(parameter =>
                       LooksLikeVRCLensParameter(ParameterName(parameter)));
        }

        private static bool ShouldReportUnknown(VRCExpressionsMenu.Control control)
        {
            if (control == null || string.IsNullOrWhiteSpace(control.name)) return false;
            if (!ContainsLatinLetter(control.name) || IsLanguageNeutral(control.name)) return false;
            return LooksLikeVRCLensSemanticControl(control)
                   || (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu
                       && SubtreeLooksLikeVRCLens(control.subMenu,
                                                 new HashSet<VRCExpressionsMenu>()));
        }

        private static bool SubtreeLooksLikeVRCLens(VRCExpressionsMenu menu,
                                                     HashSet<VRCExpressionsMenu> seen)
        {
            if (menu == null || !seen.Add(menu)) return false;
            foreach (var control in menu.controls ?? new List<VRCExpressionsMenu.Control>())
            {
                if (LooksLikeVRCLensSemanticControl(control)) return true;
                if (control != null && control.type == VRCExpressionsMenu.Control.ControlType.SubMenu
                    && SubtreeLooksLikeVRCLens(control.subMenu, seen)) return true;
            }
            return false;
        }

        private static bool SubtreeHasVRCLensParameter(VRCExpressionsMenu menu,
                                                        HashSet<VRCExpressionsMenu> seen)
        {
            if (menu == null || !seen.Add(menu)) return false;
            foreach (var control in menu.controls ?? new List<VRCExpressionsMenu.Control>())
            {
                if (IsVRCLensSemanticControl(control)) return true;
                if (control != null && control.type == VRCExpressionsMenu.Control.ControlType.SubMenu
                    && SubtreeHasVRCLensParameter(control.subMenu, seen)) return true;
            }
            return false;
        }

        private static bool ContainsLatinLetter(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Any(character =>
                (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z'));
        }

        private static bool IsLanguageNeutral(string value)
        {
            string trimmed = (value ?? "").Trim();
            if (trimmed.Length == 0) return true;
            if (trimmed == "VRCLens" || trimmed == "AF" || trimmed == "MF" || trimmed == "WB"
                || trimmed == "EV" || trimmed == "HLG" || trimmed == "DSLR" || trimmed == "APS-H")
                return true;
            return Regex.IsMatch(trimmed, @"^[0-9\s.:+\-/×]+$", RegexOptions.CultureInvariant);
        }

        private static string ParameterName(VRCExpressionsMenu.Control control)
        {
            return control == null ? "" : ParameterName(control.parameter);
        }

        private static string ParameterName(VRCExpressionsMenu.Control.Parameter parameter)
        {
            return parameter == null ? "" : parameter.name ?? "";
        }

        private static bool HasSubParameter(VRCExpressionsMenu.Control control, string name)
        {
            return control != null && control.subParameters != null
                   && control.subParameters.Any(parameter => ParameterName(parameter) == name);
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) < 0.001f;
        }

        /// <summary>
        /// Checks the menus and VRCFury Toggle paths shipped with the supported versions. This is
        /// deliberately strict: adding an English control without adding a translation must fail the
        /// release validator rather than silently ship a partly localized prefab.
        /// </summary>
        internal static bool ValidateShippedCoverage(out string error)
        {
            var failures = new List<string>();
            var catalogs = VRCLensLocalizationCatalog.All
                .OrderBy(item => item.LocaleCode, StringComparer.Ordinal).ToArray();
            if (catalogs.Length != VRCLensLocalizationRegistry.Profiles.Count)
                failures.Add($"expected {VRCLensLocalizationRegistry.Profiles.Count} locale catalogs, " +
                             $"found {catalogs.Length}");
            foreach (var profile in VRCLensLocalizationRegistry.Profiles)
            {
                if (!catalogs.Any(item => item.LocaleCode == profile.LocaleCode))
                    failures.Add($"[{profile.LocaleCode}] registered installer has no catalog");
            }
            foreach (var catalog in catalogs)
            {
                if (!VRCLensLocalizationRegistry.TryGet(catalog.LocaleCode, out var profile))
                    failures.Add($"[{catalog.LocaleCode}] catalog has no registered installer");
                else if (!string.Equals(catalog.NativeName, profile.NativeName,
                                        StringComparison.Ordinal))
                    failures.Add($"[{catalog.LocaleCode}] catalog language name " +
                                 $"'{catalog.NativeName}' does not match registry '{profile.NativeName}'");
            }

            var expectedKeys = new HashSet<string>(KnownNames.Keys, StringComparer.Ordinal);
            foreach (var catalog in catalogs)
            {
                string locale = "[" + catalog.LocaleCode + "] ";
                var actualKeys = new HashSet<string>(catalog.KnownNames.Keys,
                                                     StringComparer.Ordinal);
                foreach (string missing in expectedKeys.Except(actualKeys).OrderBy(item => item))
                    failures.Add(locale + "missing catalog key '" + missing + "'");
                foreach (string extra in actualKeys.Except(expectedKeys).OrderBy(item => item))
                    failures.Add(locale + "unexpected catalog key '" + extra + "'");
                foreach (var pair in catalog.KnownNames)
                    if (string.IsNullOrWhiteSpace(pair.Value))
                        failures.Add(locale + "empty translation for '" + pair.Key + "'");

                int baseMenuCount = 0;
                int addOnMenuCount = 0;
                int functionalBlanks = 0;
                int layoutSpacers = 0;
                ValidateMenuAssets("Assets/Hirabiki/VRCLens/Prefabs/Menus2", true,
                                   failures, catalog, ref baseMenuCount,
                                   ref functionalBlanks, ref layoutSpacers);
                bool freeAddOnsInstalled =
                    AssetDatabase.IsValidFolder("Assets/VRCLens_Custom/Mods");
                if (freeAddOnsInstalled)
                {
                    ValidateMenuAssets("Assets/VRCLens_Custom/Mods", false,
                                       failures, catalog, ref addOnMenuCount,
                                       ref functionalBlanks, ref layoutSpacers);
                }

                if (baseMenuCount != 22)
                    failures.Add(locale + $"expected 22 VRCLens 1.10.0 menu assets, found {baseMenuCount}");
                if (freeAddOnsInstalled && addOnMenuCount == 0)
                    failures.Add(locale + "Free Add-ons Mods folder contains no readable menu assets");
                if (functionalBlanks != 28)
                    failures.Add(locale + $"expected translations for 28 functional blank controls, found {functionalBlanks}");
                if (layoutSpacers != 5)
                    failures.Add(locale + $"expected 5 true layout spacers, found {layoutSpacers}");

                // Free Camera Add-ons is an optional enhancement, not a compile/install dependency
                // of the localization-only package. When present, retain the strict release coverage
                // that proves all of its current menus and generated VRCFury paths are translated.
                if (freeAddOnsInstalled)
                {
                    // Base and optional bundles have different counts; inspect every installed path.
                    ValidateVRCFuryTogglePaths(failures, catalog);
                }
            }

            error = string.Join("; ", failures);
            return failures.Count == 0;
        }

        private static void ValidateMenuAssets(
            string folder,
            bool countBaseBlanks,
            List<string> failures,
            VRCLensLocalizationCatalog catalog,
            ref int menuCount,
            ref int functionalBlanks,
            ref int layoutSpacers)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                failures.Add("menu folder is missing: " + folder);
                return;
            }
            var guids = AssetDatabase.FindAssets("t:VRCExpressionsMenu", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var menu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(path);
                if (menu == null) continue;
                menuCount++;
                foreach (var control in menu.controls ?? new List<VRCExpressionsMenu.Control>())
                {
                    if (control == null) continue;
                    if (string.IsNullOrWhiteSpace(control.name))
                    {
                        string functional;
                        if (TryFunctionalBlankName(control, catalog, out functional))
                        {
                            if (countBaseBlanks) functionalBlanks++;
                        }
                        else if (IsLayoutSpacer(control))
                        {
                            if (countBaseBlanks) layoutSpacers++;
                        }
                        else failures.Add($"unmapped functional blank at {path}: " +
                                          $"{ParameterName(control)}={control.value}");
                    }
                    else
                    {
                        string localized;
                        if (!TryLocalizedName(control, catalog, out localized)
                            && !IsLanguageNeutral(control.name))
                            failures.Add($"[{catalog.LocaleCode}] unmapped control '{control.name}' at {path}");
                    }

                    var labels = control.labels ?? new VRCExpressionsMenu.Control.Label[0];
                    for (int labelIndex = 0; labelIndex < labels.Length; labelIndex++)
                    {
                        var label = labels[labelIndex];
                        if (string.IsNullOrWhiteSpace(label.name))
                        {
                            string implied;
                            if (!TryImpliedDirectionLabel(control, labelIndex, catalog, out implied)
                                && !IsIntentionalBlankDirectionLabel(control, labelIndex))
                                failures.Add($"[{catalog.LocaleCode}] unmapped blank puppet label #{labelIndex} at {path}");
                            continue;
                        }
                        if (!catalog.DirectionLabels.ContainsKey(label.name.Trim())
                            && !IsLanguageNeutral(label.name))
                            failures.Add($"[{catalog.LocaleCode}] unmapped puppet label '{label.name}' at {path}");
                    }
                }
            }
        }

        private static bool IsLayoutSpacer(VRCExpressionsMenu.Control control)
        {
            return control != null
                   && control.type == VRCExpressionsMenu.Control.ControlType.Button
                   && string.IsNullOrEmpty(ParameterName(control))
                   && (control.subParameters == null
                       || control.subParameters.All(parameter =>
                           string.IsNullOrEmpty(ParameterName(parameter))))
                   && control.subMenu == null
                   && (control.labels == null || control.labels.Length == 0);
        }

        private static int ValidateVRCFuryTogglePaths(List<string> failures,
                                                      VRCLensLocalizationCatalog catalog)
        {
            const string root = "Assets/VRCLens_Custom";
            if (!Directory.Exists(root))
            {
                failures.Add("add-on prefab folder is missing: " + root);
                return 0;
            }

            int count = 0;
            var uniquePaths = new HashSet<string>(StringComparer.Ordinal);
            var pathPattern = new Regex(@"(?m)^\s{8}name:\s+(VRCLens/Custom/[^\r\n]+)$",
                                        RegexOptions.CultureInvariant);
            var prefabPaths = Directory.GetFiles(root, "*.prefab", SearchOption.TopDirectoryOnly);
            int coveredFreePrefabs = prefabPaths.Count(path =>
                !VRCLensLocalizationRegistry.Profiles.Any(profile =>
                    string.Equals(path.Replace('\\', '/'), profile.PrefabPath,
                                  StringComparison.OrdinalIgnoreCase)));
            if (coveredFreePrefabs == 0)
                failures.Add("Free Add-ons Mods folder is installed but no installer prefabs were found");

            foreach (string prefabPath in prefabPaths)
            {
                string source = File.ReadAllText(prefabPath);
                foreach (Match match in pathPattern.Matches(source))
                {
                    count++;
                    string fullPath = match.Groups[1].Value.Trim();
                    if (!uniquePaths.Add(fullPath))
                        failures.Add($"duplicate VRCFury menu path '{fullPath}' in {prefabPath}");
                    foreach (string segment in fullPath.Split('/'))
                    {
                        string ignored;
                        if (!catalog.KnownNames.TryGetValue(segment, out ignored)
                            && !IsLanguageNeutral(segment))
                            failures.Add($"[{catalog.LocaleCode}] unmapped VRCFury path segment " +
                                         $"'{segment}' in {prefabPath}");
                    }
                }
            }
            return count;
        }

        /// <summary>Fast, asset-free regression checks for matching, protection and cycle handling.</summary>
        internal static bool SelfTest(out string error)
        {
            var made = new List<VRCExpressionsMenu>();
            GameObject bridgeAvatar = null;
            Texture2D cloneTestIcon = null;
            try
            {
                var japaneseCatalog = VRCLensLocalizationCatalog.ForLocale("ja-JP");
                cloneTestIcon = new Texture2D(1, 1) { name = "VRCLensJapaneseCloneSelfTestIcon" };
                RunStructuralCloneSelfTest(made, cloneTestIcon);
                AssertOptionalFavoritesReflectionSchema();

                var root = ScriptableObject.CreateInstance<VRCExpressionsMenu>(); made.Add(root);
                var shared = ScriptableObject.CreateInstance<VRCExpressionsMenu>(); made.Add(shared);
                var outside = ScriptableObject.CreateInstance<VRCExpressionsMenu>(); made.Add(outside);
                var unrelatedInside = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                made.Add(unrelatedInside);
                var unrelatedPage = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                unrelatedPage.name = "Unrelated (Page 2)";
                made.Add(unrelatedPage);

                var enable = Control("Enable", VRCExpressionsMenu.Control.ControlType.Toggle,
                                     "VRCLT_Enabled", 1);
                var zoom = Control("Zoom", VRCExpressionsMenu.Control.ControlType.RadialPuppet, "", 0,
                                   "VRCLZoomRadial");
                var reset = Control("Reset", VRCExpressionsMenu.Control.ControlType.Button,
                                    FeatureToggle, 254);
                var protectedAlias = Control("Reset", VRCExpressionsMenu.Control.ControlType.Button,
                                             FeatureToggle, 254);
                var move = Control("Move Camera", VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet,
                                   FeatureToggle, 212, "VRCFaceBlendH", "VRCFaceBlendV");
                move.labels = new[]
                {
                    new VRCExpressionsMenu.Control.Label { name = "FORWARD" },
                    new VRCExpressionsMenu.Control.Label { name = "RIGHT" },
                    new VRCExpressionsMenu.Control.Label { name = "BACK" },
                    new VRCExpressionsMenu.Control.Label { name = "LEFT" },
                };
                var movePivot = Control(
                    "Move Pivot", VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet,
                    FeatureToggle, 214, "VRCFaceBlendH", "VRCFaceBlendV");
                movePivot.labels = new VRCExpressionsMenu.Control.Label[4];
                shared.controls.Add(move);
                shared.controls.Add(movePivot);
                shared.controls.Add(new VRCExpressionsMenu.Control
                {
                    name = "戻る",
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = root,
                });
                root.controls.Add(enable);
                root.controls.Add(zoom);
                root.controls.Add(reset);
                root.controls.Add(protectedAlias);
                root.controls.Add(new VRCExpressionsMenu.Control
                {
                    name = "Settings",
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = shared,
                });
                root.controls.Add(new VRCExpressionsMenu.Control
                {
                    name = "Advanced",
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = shared, // shared reference
                });
                var unrelatedSettings = Control(
                    "Settings", VRCExpressionsMenu.Control.ControlType.Toggle, "User/Settings", 1);
                var unrelatedReset = Control(
                    "Reset", VRCExpressionsMenu.Control.ControlType.Button, "User/Reset", 1);
                var misleadingPrefix = Control(
                    "Settings", VRCExpressionsMenu.Control.ControlType.Toggle,
                    "VRCLocal/UserSettings", 1);
                var unrelatedNext = new VRCExpressionsMenu.Control
                {
                    name = "Next",
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = unrelatedPage,
                };
                unrelatedInside.controls.Add(unrelatedSettings);
                unrelatedInside.controls.Add(unrelatedReset);
                unrelatedInside.controls.Add(misleadingPrefix);
                unrelatedInside.controls.Add(unrelatedNext);
                root.controls.Add(new VRCExpressionsMenu.Control
                {
                    name = "User Tools",
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = unrelatedInside,
                });
                outside.controls.Add(new VRCExpressionsMenu.Control
                {
                    name = "Settings",
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "User/Settings" },
                });

                var protectedNames = new HashSet<VRCExpressionsMenu.Control> { protectedAlias };
                TranslateGraph(root, outside, protectedNames, new HashSet<string>(),
                               japaneseCatalog);

                if (!IsVRCLensRoot(root)) throw new InvalidOperationException("root signature was not found");
                if (reset.name != "初期化") throw new InvalidOperationException("known Reset was not localized");
                if (protectedAlias.name != "Reset")
                    throw new InvalidOperationException("an explicit favorite alias was changed");
                if (move.name != "カメラ移動" || move.labels[0].name != "前"
                    || move.labels[2].name != "後")
                    throw new InvalidOperationException("puppet name/labels were not localized");
                if (movePivot.name != "ピボット移動" || movePivot.labels[0].name != "前"
                    || movePivot.labels[1].name != "右" || movePivot.labels[2].name != "後"
                    || movePivot.labels[3].name != "左")
                    throw new InvalidOperationException(
                        "functional blank Move Pivot labels were not localized");
                if (outside.controls[0].name != "Settings")
                    throw new InvalidOperationException("an unrelated menu control was localized");
                if (unrelatedSettings.name != "Settings" || unrelatedReset.name != "Reset"
                    || misleadingPrefix.name != "Settings" || unrelatedNext.name != "Next")
                    throw new InvalidOperationException(
                        "an unrelated Settings/Reset/Next below the VRCLens root was localized");

                var blank = Control("    ", VRCExpressionsMenu.Control.ControlType.Toggle,
                                    "VRCLS_SensorSize", 3);
                if (!LocalizeKnownControlCopy(blank) || blank.name != "APS-C（1.6倍）")
                    throw new InvalidOperationException("functional blank mapping failed");

                bridgeAvatar = new GameObject("VRCLensJapaneseFavoriteBridgeSelfTest");
                var favoriteMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                made.Add(favoriteMenu);
                var favoritePage2 = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                favoritePage2.name = "Favorite Outside (Page 2)";
                made.Add(favoritePage2);
                var preexistingSubmenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                made.Add(preexistingSubmenu);
                var preexistingPage2 = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                preexistingPage2.name = "Preexisting (Page 2)";
                made.Add(preexistingPage2);
                var explicitFavorite = Control(
                    "My Camera", VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet,
                    FeatureToggle, 212, "VRCFaceBlendH", "VRCFaceBlendV");
                var blankFavorite = Control(
                    "Drone Move Camera", VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet,
                    FeatureToggle, 212, "VRCFaceBlendH", "VRCFaceBlendV");
                explicitFavorite.labels = new[]
                {
                    new VRCExpressionsMenu.Control.Label { name = "FORWARD" },
                    new VRCExpressionsMenu.Control.Label { name = "RIGHT" },
                    new VRCExpressionsMenu.Control.Label { name = "BACK" },
                    new VRCExpressionsMenu.Control.Label { name = "LEFT" },
                };
                blankFavorite.labels = explicitFavorite.labels.ToArray();
                favoriteMenu.controls.Add(explicitFavorite);
                favoriteMenu.controls.Add(blankFavorite);
                var favoriteNext = new VRCExpressionsMenu.Control
                {
                    name = "Next",
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = favoritePage2,
                };
                favoriteMenu.controls.Add(favoriteNext);
                var preexistingNext = new VRCExpressionsMenu.Control
                {
                    name = "Next",
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = preexistingPage2,
                };
                preexistingSubmenu.controls.Add(preexistingNext);
                favoriteMenu.controls.Add(new VRCExpressionsMenu.Control
                {
                    name = "Preexisting Tools",
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = preexistingSubmenu,
                });
                BeginFavoriteBridge(bridgeAvatar);
                TagFavoriteControl(bridgeAvatar, explicitFavorite, "DroneMove", "Move Camera",
                                   "Drone", "My Camera");
                TagFavoriteControl(bridgeAvatar, blankFavorite, "DroneMove", "Move Camera",
                                   "Drone", "");
                int restored;
                bool usedBridge;
                var preciseProtection = RestoreFavoriteTokens(
                    bridgeAvatar, favoriteMenu, japaneseCatalog, out restored, out usedBridge);
                if (!usedBridge || restored != 10 || explicitFavorite.name != "My Camera"
                    || !preciseProtection.Contains(explicitFavorite)
                    || explicitFavorite.labels[0].name != "前"
                    || blankFavorite.name != "ドローン カメラ移動"
                    || blankFavorite.labels[2].name != "後")
                    throw new InvalidOperationException("Menu Favorites token bridge failed");
                if (LocalizeFavoritePagination(favoriteMenu,
                                               new HashSet<VRCExpressionsMenu>(),
                                               japaneseCatalog) != 1
                    || favoriteNext.name != "次へ" || unrelatedNext.name != "Next"
                    || preexistingNext.name != "Next")
                    throw new InvalidOperationException(
                        "scoped Menu Favorites pagination localization failed");
                EndFavoriteBridge(bridgeAvatar);

                RunLocaleCatalogSelfTest(made);
                RunFavoriteBridgeLocaleSelfTest(bridgeAvatar, made);

                error = "";
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                if (bridgeAvatar != null)
                {
                    EndFavoriteBridge(bridgeAvatar);
                    UnityEngine.Object.DestroyImmediate(bridgeAvatar);
                }
                foreach (var menu in made)
                    if (menu != null) UnityEngine.Object.DestroyImmediate(menu);
                if (cloneTestIcon != null) UnityEngine.Object.DestroyImmediate(cloneTestIcon);
            }
        }

        private static void RunLocaleCatalogSelfTest(List<VRCExpressionsMenu> made)
        {
            var golden = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                { "ja-JP", new[] { "初期化", "ピクチャースタイル", "水準器",
                    "カラーグレーディング", "フィルムグレイン", "周辺減光",
                    "ワールド固定", "次へ", "35mmフルサイズ" } },
                { "zh-Hans-CN", new[] { "重置", "照片风格", "电子水准仪",
                    "色彩分级", "胶片颗粒", "暗角",
                    "固定在世界", "下一页", "35mm全画幅" } },
                { "zh-Hant-TW", new[] { "重設", "相片風格", "電子水平儀",
                    "調色", "底片顆粒", "暗角",
                    "固定於世界", "下一頁", "35mm全片幅" } },
                { "ko-KR", new[] { "초기화", "픽쳐스타일", "전자 수평계",
                    "컬러 그레이딩", "필름 그레인", "비네팅",
                    "월드 고정", "다음", "35mm 풀프레임" } },
            };

            foreach (var profile in VRCLensLocalizationRegistry.Profiles)
            {
                var catalog = VRCLensLocalizationCatalog.ForLocale(profile.LocaleCode);
                if (!golden.TryGetValue(profile.LocaleCode, out var expected)
                    && !VRCLensAdditionalGolden.Values.TryGetValue(profile.LocaleCode, out expected))
                    throw new InvalidOperationException(
                        "No localization golden set exists for " + profile.LocaleCode);
                var keys = new[]
                {
                    "Reset", "Picture Style", "V. Horizon", "Color Grading",
                    "Film Grain", "Vignette",
                };
                for (int index = 0; index < keys.Length; index++)
                {
                    if (!catalog.KnownNames.TryGetValue(keys[index], out var actual)
                        || actual != expected[index])
                        throw new InvalidOperationException(
                            $"{profile.LocaleCode} golden translation failed for '{keys[index]}': " +
                            $"'{actual ?? "<missing>"}'.");
                }

                var worldDrop = Control("Drop", VRCExpressionsMenu.Control.ControlType.Button,
                                        FeatureToggle, 251);
                if (!TryLocalizedName(worldDrop, catalog, out var dropName)
                    || dropName != expected[6])
                    throw new InvalidOperationException(
                        profile.LocaleCode + " contextual world Drop mapping failed");

                var generatedPage = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                generatedPage.name = "Locale Smoke (Page 2)";
                generatedPage.controls.Add(Control(
                    "Enable", VRCExpressionsMenu.Control.ControlType.Toggle,
                    "VRCLT_Enabled", 1));
                made.Add(generatedPage);
                var generatedNext = new VRCExpressionsMenu.Control
                {
                    name = "Next",
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = generatedPage,
                };
                if (!TryLocalizedName(generatedNext, catalog, out var nextName)
                    || nextName != expected[7])
                    throw new InvalidOperationException(
                        profile.LocaleCode + " generated Next mapping failed");

                var sensor = Control("", VRCExpressionsMenu.Control.ControlType.Toggle,
                                     "VRCLS_SensorSize", 0);
                if (!TryLocalizedName(sensor, catalog, out var sensorName)
                    || sensorName != expected[8])
                    throw new InvalidOperationException(
                        profile.LocaleCode + " functional sensor label mapping failed");

                var pinLeaf = Control("Pin 3",
                    VRCExpressionsMenu.Control.ControlType.Button,
                    "VRCL_Custom/DropPin3", 1);
                if (!TryLocalizedName(pinLeaf, catalog, out var localizedPin))
                    throw new InvalidOperationException(
                        profile.LocaleCode + " dynamic favorite Pin mapping failed");
                var qualifiedPin = Control("Drop Pin 3",
                    VRCExpressionsMenu.Control.ControlType.Button,
                    "VRCL_Custom/DropPin3", 1);
                if (!TryLocalizedFavoriteQualifiedName(
                        qualifiedPin, catalog, out var localizedQualifiedPin)
                    || localizedQualifiedPin != catalog.CameraPinDrop + " " + localizedPin)
                    throw new InvalidOperationException(
                        profile.LocaleCode + " qualified favorite Pin mapping failed");

                var qualifiedDollyPin = Control("Go To Pin 3",
                    VRCExpressionsMenu.Control.ControlType.Button,
                    "VRCL_Custom/DollyToPin", 3);
                if (!TryLocalizedFavoriteQualifiedName(
                        qualifiedDollyPin, catalog, out var localizedDollyPin)
                    || localizedDollyPin != catalog.KnownNames["Go To Pin"] + " 3")
                    throw new InvalidOperationException(
                        profile.LocaleCode + " overlap-qualified favorite Pin mapping failed");

                var qualifiedTone = Control("Shadows Contrast",
                    VRCExpressionsMenu.Control.ControlType.RadialPuppet, "", 0,
                    "VRCL_Custom/ColorGradingContrastShadows");
                if (!TryLocalizedFavoriteQualifiedName(
                        qualifiedTone, catalog, out var localizedTone)
                    || localizedTone != catalog.KnownNames["Shadows"] + " "
                                        + catalog.KnownNames["Contrast"])
                    throw new InvalidOperationException(
                        profile.LocaleCode + " qualified favorite color mapping failed");

                var qualifiedPreset = Control("Save 1",
                    VRCExpressionsMenu.Control.ControlType.Button,
                    "VRCL_Custom/PresetSave", 1);
                if (!TryLocalizedFavoriteQualifiedName(
                        qualifiedPreset, catalog, out var localizedPreset)
                    || localizedPreset != catalog.KnownNames["Save"] + " 1")
                    throw new InvalidOperationException(
                        profile.LocaleCode + " qualified favorite preset mapping failed");

                var puppet = Control("Move Camera",
                    VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet,
                    FeatureToggle, 212, "VRCFaceBlendH", "VRCFaceBlendV");
                puppet.labels = new[]
                {
                    new VRCExpressionsMenu.Control.Label { name = "FORWARD" },
                    new VRCExpressionsMenu.Control.Label { name = "RIGHT" },
                    new VRCExpressionsMenu.Control.Label { name = "BACK" },
                    new VRCExpressionsMenu.Control.Label { name = "LEFT" },
                };
                if (TranslateDirectionLabels(puppet, null, catalog) != 4
                    || puppet.labels.Any(label => string.IsNullOrWhiteSpace(label.name)))
                    throw new InvalidOperationException(
                        profile.LocaleCode + " puppet direction mapping failed");
            }
        }

        /// <summary>
        /// When the optional Free Camera Add-ons assembly is present, prove that the serialized
        /// Favorites contract used by the reflection fallback is still readable. Standalone
        /// localization installs deliberately have no such type and therefore skip this check.
        /// </summary>
        private static void AssertOptionalFavoritesReflectionSchema()
        {
            Type componentType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    componentType = assembly.GetType(
                        "VRCLensCustom.VRCLensMenuFavorites", false, false);
                }
                catch
                {
                    componentType = null;
                }
                if (componentType != null) break;
            }
            if (componentType == null) return;
            if (!typeof(MonoBehaviour).IsAssignableFrom(componentType))
                throw new InvalidOperationException(
                    "The optional VRCLensMenuFavorites type is no longer a MonoBehaviour.");

            Type pageType = RequiredEnumerableElementType(componentType, "pages");
            RequiredStringMemberType(pageType, "menuPath");
            Type itemType = RequiredEnumerableElementType(pageType, "items");
            RequiredStringMemberType(itemType, "name");
            RequiredStringMemberType(itemType, "submenu");
        }

        private static Type RequiredEnumerableElementType(Type ownerType, string memberName)
        {
            Type memberType = RequiredMemberType(ownerType, memberName);
            if (memberType.IsArray) return memberType.GetElementType();
            var enumerable = new[] { memberType }.Concat(memberType.GetInterfaces())
                .FirstOrDefault(candidate => candidate.IsGenericType
                    && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (enumerable == null)
                throw new InvalidOperationException(
                    $"The optional Menu Favorites member '{ownerType.FullName}.{memberName}' " +
                    "is not an enumerable collection.");
            return enumerable.GetGenericArguments()[0];
        }

        private static void RequiredStringMemberType(Type ownerType, string memberName)
        {
            Type memberType = RequiredMemberType(ownerType, memberName);
            if (memberType != typeof(string))
                throw new InvalidOperationException(
                    $"The optional Menu Favorites member '{ownerType.FullName}.{memberName}' " +
                    "is not a string.");
        }

        private static Type RequiredMemberType(Type ownerType, string memberName)
        {
            var field = ownerType.GetField(memberName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null) return field.FieldType;
            var property = ownerType.GetProperty(memberName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.GetIndexParameters().Length == 0
                && property.CanRead) return property.PropertyType;
            throw new InvalidOperationException(
                $"The optional Menu Favorites schema has no readable '{memberName}' member on " +
                $"'{ownerType.FullName}'.");
        }

        private static void RunFavoriteBridgeLocaleSelfTest(
            GameObject avatar, List<VRCExpressionsMenu> made)
        {
            foreach (var profile in VRCLensLocalizationRegistry.Profiles)
            {
                var catalog = VRCLensLocalizationCatalog.ForLocale(profile.LocaleCode);
                var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                made.Add(menu);
                var blankAlias = Control(
                    "Drone Move Camera", VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet,
                    FeatureToggle, 212, "VRCFaceBlendH", "VRCFaceBlendV");
                blankAlias.labels = new[]
                {
                    new VRCExpressionsMenu.Control.Label { name = "FORWARD" },
                    new VRCExpressionsMenu.Control.Label { name = "RIGHT" },
                    new VRCExpressionsMenu.Control.Label { name = "BACK" },
                    new VRCExpressionsMenu.Control.Label { name = "LEFT" },
                };
                menu.controls.Add(blankAlias);

                BeginFavoriteBridge(avatar, profile);
                TagFavoriteControl(avatar, blankAlias, "DroneMove", "Move Camera",
                                   "Drone", "");
                int restored;
                bool usedBridge;
                RestoreFavoriteTokens(avatar, menu, catalog, out restored, out usedBridge);
                string expected = catalog.KnownNames["Drone"] + " "
                                  + catalog.KnownNames["Move Camera"];
                var expectedDirections = new[] { "FORWARD", "RIGHT", "BACK", "LEFT" }
                    .Select(direction => catalog.DirectionLabels[direction]);
                if (!usedBridge || blankAlias.name != expected
                    || !blankAlias.labels.Select(label => label.name).SequenceEqual(expectedDirections))
                    throw new InvalidOperationException(
                        profile.LocaleCode + " Menu Favorites blank-alias bridge failed");
                EndFavoriteBridge(avatar);
            }

            // A bridge created for one language must never be consumed by another language.
            var mismatchMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            made.Add(mismatchMenu);
            var mismatchControl = Control(
                "Move Camera", VRCExpressionsMenu.Control.ControlType.Button,
                FeatureToggle, 212);
            mismatchMenu.controls.Add(mismatchControl);
            var japanese = VRCLensLocalizationRegistry.GetRequiredProfile("ja-JP");
            BeginFavoriteBridge(avatar, japanese);
            TagFavoriteControl(avatar, mismatchControl, "DroneMove", "Move Camera", "Drone", "");
            try
            {
                int restored;
                bool usedBridge;
                RestoreFavoriteTokens(
                    avatar, mismatchMenu, VRCLensLocalizationCatalog.ForLocale("ko-KR"),
                    out restored, out usedBridge);
                throw new InvalidOperationException(
                    "a Menu Favorites bridge was accepted by the wrong locale");
            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.IndexOf("does not match", StringComparison.Ordinal) < 0)
                    throw;
            }
            finally
            {
                EndFavoriteBridge(avatar);
            }
        }

        /// <summary>
        /// Exercises the same memoized recursion used by Apply, with asset persistence disabled. This
        /// catches a tempting but destructive "copy each link" rewrite: shared menus must remain
        /// shared, cycles must close on the cloned graph, and serialized control data must be copied.
        /// </summary>
        private static void RunStructuralCloneSelfTest(List<VRCExpressionsMenu> made,
                                                       Texture2D testIcon)
        {
            var sourceRoot = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            sourceRoot.name = "Clone Source Root";
            var sourceShared = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            sourceShared.name = "Clone Source Shared";
            made.Add(sourceRoot);
            made.Add(sourceShared);

            var firstLink = new VRCExpressionsMenu.Control
            {
                name = "Shared A",
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = sourceShared,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = "" },
                value = 1f,
                subParameters = new VRCExpressionsMenu.Control.Parameter[0],
                labels = new VRCExpressionsMenu.Control.Label[0],
            };
            var secondLink = new VRCExpressionsMenu.Control
            {
                name = "Shared B",
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = sourceShared,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = "" },
                value = 1f,
                subParameters = new VRCExpressionsMenu.Control.Parameter[0],
                labels = new VRCExpressionsMenu.Control.Label[0],
            };
            var representative = Control(
                "Representative Puppet",
                VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet,
                FeatureToggle,
                212,
                "VRCFaceBlendH",
                "VRCFaceBlendV");
            representative.icon = testIcon;
            representative.labels = new[]
            {
                new VRCExpressionsMenu.Control.Label { name = "FORWARD", icon = testIcon },
                new VRCExpressionsMenu.Control.Label { name = "RIGHT", icon = testIcon },
                new VRCExpressionsMenu.Control.Label { name = "BACK", icon = testIcon },
                new VRCExpressionsMenu.Control.Label { name = "LEFT", icon = testIcon },
            };
            sourceRoot.controls.Add(firstLink);
            sourceRoot.controls.Add(secondLink);
            sourceRoot.controls.Add(representative);
            var sparseSubmenu = new VRCExpressionsMenu.Control
            {
                name = "Sparse Submenu",
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = sourceShared,
                parameter = null,
                subParameters = null,
                labels = null,
                value = 1f,
            };
            sourceRoot.controls.Add(sparseSubmenu);
            sourceShared.controls.Add(new VRCExpressionsMenu.Control
            {
                name = "Cycle Back",
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = sourceRoot,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = "" },
                value = 1f,
                subParameters = new VRCExpressionsMenu.Control.Parameter[0],
                labels = new VRCExpressionsMenu.Control.Label[0],
            });

            var copies = new Dictionary<VRCExpressionsMenu, VRCExpressionsMenu>();
            int serial = 0;
            try
            {
                var clonedRoot = CloneMenuGraph(sourceRoot, null, copies, ref serial, false);
                AssertDisplayOnlyClone(sourceRoot, clonedRoot);
                if (ReferenceEquals(clonedRoot, sourceRoot)
                    || ReferenceEquals(copies[sourceShared], sourceShared))
                    throw new InvalidOperationException("menu clone reused a source object");
                if (copies.Count != 2 || serial != 2)
                    throw new InvalidOperationException("menu clone memo did not visit exactly two menus");

                var firstClonedShared = clonedRoot.controls[0].subMenu;
                var secondClonedShared = clonedRoot.controls[1].subMenu;
                if (!ReferenceEquals(firstClonedShared, secondClonedShared)
                    || !ReferenceEquals(firstClonedShared, copies[sourceShared]))
                    throw new InvalidOperationException("a shared submenu was cloned more than once");
                if (!ReferenceEquals(firstClonedShared.controls[0].subMenu, clonedRoot))
                    throw new InvalidOperationException("a submenu cycle did not point to the cloned root");

                var clonedSparseSubmenu = clonedRoot.controls[3];
                if (FloatBits(clonedSparseSubmenu.value) != FloatBits(sparseSubmenu.value)
                    || !ReferenceEquals(clonedSparseSubmenu.subMenu, firstClonedShared))
                    throw new InvalidOperationException(
                        "a sparse submenu did not preserve its value or cloned submenu link");

                var clonedRepresentative = clonedRoot.controls[2];
                if (ReferenceEquals(clonedRepresentative, representative)
                    || clonedRepresentative.icon != representative.icon
                    || clonedRepresentative.type != representative.type
                    || clonedRepresentative.parameter == null
                    || ReferenceEquals(clonedRepresentative.parameter, representative.parameter)
                    || clonedRepresentative.parameter.name != representative.parameter.name
                    || !Approximately(clonedRepresentative.value, representative.value)
                    || clonedRepresentative.style != representative.style
                    || clonedRepresentative.subMenu != representative.subMenu
                    || clonedRepresentative.subParameters == null
                    || clonedRepresentative.subParameters.Length != representative.subParameters.Length
                    || ReferenceEquals(clonedRepresentative.subParameters,
                                       representative.subParameters)
                    || clonedRepresentative.subParameters[0].name
                       != representative.subParameters[0].name
                    || ReferenceEquals(clonedRepresentative.subParameters[0],
                                       representative.subParameters[0])
                    || clonedRepresentative.labels == null
                    || clonedRepresentative.labels.Length != representative.labels.Length
                    || ReferenceEquals(clonedRepresentative.labels, representative.labels)
                    || clonedRepresentative.labels[0].icon != representative.labels[0].icon)
                    throw new InvalidOperationException(
                        "a representative control lost a serialized field while cloning");

                clonedRepresentative.name = "Changed Clone Name";
                var changedLabel = clonedRepresentative.labels[0];
                changedLabel.name = "Changed Clone Label";
                clonedRepresentative.labels[0] = changedLabel;
                clonedRepresentative.parameter.name = "ChangedCloneParameter";
                if (representative.name != "Representative Puppet"
                    || representative.labels[0].name != "FORWARD"
                    || representative.parameter.name != FeatureToggle)
                    throw new InvalidOperationException("editing cloned text changed its source control");
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(clonedRoot)))
                    throw new InvalidOperationException("the in-memory clone self-test wrote an asset");
            }
            finally
            {
                foreach (var clone in copies.Values)
                    if (clone != null && !made.Contains(clone)) made.Add(clone);
            }
        }

        private static VRCExpressionsMenu.Control Control(
            string name,
            VRCExpressionsMenu.Control.ControlType type,
            string parameter,
            float value,
            params string[] subParameters)
        {
            return new VRCExpressionsMenu.Control
            {
                name = name,
                type = type,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = parameter },
                value = value,
                subParameters = (subParameters ?? new string[0])
                    .Select(item => new VRCExpressionsMenu.Control.Parameter { name = item }).ToArray(),
                labels = new VRCExpressionsMenu.Control.Label[0],
            };
        }
    }

    /// <summary>
    /// Compatibility seam for the original internal type name. New code uses
    /// VRCLensMenuLocalizer; existing package tooling can keep calling the Japanese-named API.
    /// </summary>
    internal static class VRCLensJapaneseMenuLocalizer
    {
        internal static bool Apply(GameObject avatar, string tempDir) =>
            VRCLensMenuLocalizer.Apply(avatar, tempDir);

        internal static bool Apply(GameObject avatar, string tempDir,
                                   VRCLensLocalizationProfile profile) =>
            VRCLensMenuLocalizer.Apply(avatar, tempDir, profile);

        internal static bool LocalizeKnownControlCopy(VRCExpressionsMenu.Control control) =>
            VRCLensMenuLocalizer.LocalizeKnownControlCopy(control);

        internal static bool LocalizeKnownControlCopy(
            VRCExpressionsMenu.Control control, VRCLensLocalizationProfile profile) =>
            VRCLensMenuLocalizer.LocalizeKnownControlCopy(control, profile);

        internal static void BeginFavoriteBridge(GameObject avatar) =>
            VRCLensMenuLocalizer.BeginFavoriteBridge(avatar);

        internal static void BeginFavoriteBridge(
            GameObject avatar, VRCLensLocalizationProfile profile) =>
            VRCLensMenuLocalizer.BeginFavoriteBridge(avatar, profile);

        internal static void TagFavoriteControl(
            GameObject avatar,
            VRCExpressionsMenu.Control control,
            string catalogId,
            string sourceName,
            string parentLabel,
            string chosenAlias) =>
            VRCLensMenuLocalizer.TagFavoriteControl(
                avatar, control, catalogId, sourceName, parentLabel, chosenAlias);

        internal static void CancelFavoriteBridge(GameObject avatar) =>
            VRCLensMenuLocalizer.CancelFavoriteBridge(avatar);

        internal static void ClearAllFavoriteBridges() =>
            VRCLensMenuLocalizer.ClearAllFavoriteBridges();

        internal static bool ValidateShippedCoverage(out string error) =>
            VRCLensMenuLocalizer.ValidateShippedCoverage(out error);

        internal static bool SelfTest(out string error) =>
            VRCLensMenuLocalizer.SelfTest(out error);
    }

    /// <summary>Command-line entry point used by the package's Unity batch validation.</summary>
    public static class VRCLensJapaneseLocalizationBatch
    {
        public static void RunValidationForBatchMode()
        {
            // Exercise coverage, semantics and the authored installers without depending on the
            // optional Free Add-ons validator, which is deliberately not in the translation package.
            var prefabIssues = VRCLensLocalizationPackageValidator.ValidateAll().ToArray();
            if (prefabIssues.Length != 0)
                throw new InvalidOperationException(
                    "Localization prefab validation failed:\n" +
                    string.Join("\n", prefabIssues));

            Debug.Log("[VRCLens Custom] Four-language localization coverage, structural self-test, " +
                      "and installer prefab validation passed.");
        }
    }

    /// <summary>Language-neutral batch entry; the Japanese name above remains compatible.</summary>
    public static class VRCLensLocalizationBatch
    {
        public static void RunValidationForBatchMode()
        {
            VRCLensJapaneseLocalizationBatch.RunValidationForBatchMode();
        }
    }
}
#endif
