#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace VRCLensCustom
{
    /// <summary>
    /// Language data consumed by the shared semantic menu resolver.  The resolver owns all
    /// VRCLens scoping; catalogs only turn an already-proven semantic control into display text.
    /// </summary>
    internal sealed class VRCLensLocalizationCatalog
    {
        internal delegate bool ContextualNameResolver(
            string sourceName,
            string parameterName,
            VRCExpressionsMenu.Control.ControlType controlType,
            float value,
            bool submenuContainsVrclensParameters,
            bool isGeneratedNextWithinVrclens,
            out string localized);

        internal delegate bool FunctionalBlankResolver(
            string parameterName,
            VRCExpressionsMenu.Control.ControlType controlType,
            float value,
            out string localized);

        internal delegate bool ImpliedPivotDirectionResolver(
            float featureValue,
            bool hasFaceBlendHorizontal,
            bool hasFaceBlendVertical,
            int labelCount,
            int labelIndex,
            out string localized);

        internal string LocaleCode { get; }
        internal string NativeName { get; }
        internal IReadOnlyDictionary<string, string> KnownNames { get; }
        internal IReadOnlyDictionary<string, string> DirectionLabels { get; }
        internal string CameraPinDrop { get; }
        internal string Next { get; }
        internal ContextualNameResolver TryContextualName { get; }
        internal FunctionalBlankResolver TryFunctionalBlankName { get; }
        internal ImpliedPivotDirectionResolver TryImpliedPivotDirection { get; }

        private VRCLensLocalizationCatalog(
            string localeCode,
            string nativeName,
            Dictionary<string, string> knownNames,
            Dictionary<string, string> directionLabels,
            string cameraPinDrop,
            string next,
            ContextualNameResolver contextualName,
            FunctionalBlankResolver functionalBlankName,
            ImpliedPivotDirectionResolver impliedPivotDirection)
        {
            LocaleCode = localeCode ?? throw new ArgumentNullException(nameof(localeCode));
            NativeName = nativeName ?? throw new ArgumentNullException(nameof(nativeName));
            KnownNames = knownNames ?? throw new ArgumentNullException(nameof(knownNames));
            DirectionLabels = directionLabels ?? throw new ArgumentNullException(nameof(directionLabels));
            CameraPinDrop = cameraPinDrop ?? throw new ArgumentNullException(nameof(cameraPinDrop));
            Next = next ?? throw new ArgumentNullException(nameof(next));
            TryContextualName = contextualName ?? throw new ArgumentNullException(nameof(contextualName));
            TryFunctionalBlankName = functionalBlankName
                ?? throw new ArgumentNullException(nameof(functionalBlankName));
            TryImpliedPivotDirection = impliedPivotDirection
                ?? throw new ArgumentNullException(nameof(impliedPivotDirection));
        }

        private static readonly Dictionary<string, VRCLensLocalizationCatalog> Catalogs =
            CreateCatalogs();

        internal static IEnumerable<VRCLensLocalizationCatalog> All => Catalogs.Values;

        internal static VRCLensLocalizationCatalog ForLocale(string localeCode)
        {
            if (string.IsNullOrEmpty(localeCode)
                || !Catalogs.TryGetValue(localeCode, out var catalog))
                throw new InvalidOperationException(
                    $"No VRCLens localization catalog is registered for '{localeCode ?? "<null>"}'.");
            return catalog;
        }

        private static Dictionary<string, VRCLensLocalizationCatalog> CreateCatalogs()
        {
            var result = new Dictionary<string, VRCLensLocalizationCatalog>(StringComparer.Ordinal)
            {
                {
                    "ja-JP",
                    new VRCLensLocalizationCatalog(
                        "ja-JP", "日本語",
                        VRCLensMenuLocalizer.CreateJapaneseNames(),
                        VRCLensMenuLocalizer.CreateJapaneseDirectionLabels(),
                        "ピン設置", "次へ",
                        VRCLensMenuLocalizer.TryGetJapaneseContextualName,
                        VRCLensMenuLocalizer.TryGetJapaneseFunctionalBlankName,
                        VRCLensMenuLocalizer.TryGetJapaneseImpliedPivotDirection)
                },
                {
                    VRCLensKoreanLocalizationCatalog.LocaleCode,
                    new VRCLensLocalizationCatalog(
                        VRCLensKoreanLocalizationCatalog.LocaleCode,
                        VRCLensKoreanLocalizationCatalog.NativeLanguageName,
                        VRCLensKoreanLocalizationCatalog.CreateNames(),
                        VRCLensKoreanLocalizationCatalog.CreateDirectionLabels(),
                        "핀 설치", "다음",
                        VRCLensKoreanLocalizationCatalog.TryGetContextualName,
                        VRCLensKoreanLocalizationCatalog.TryGetFunctionalBlankName,
                        VRCLensKoreanLocalizationCatalog.TryGetImpliedPivotDirection)
                },
            };

            AddChineseCatalogs(result);
            return result;
        }

        // Kept in one small adapter so the region-specific catalogs remain data-only files.
        private static void AddChineseCatalogs(
            Dictionary<string, VRCLensLocalizationCatalog> result)
        {
            foreach (var catalog in new[]
            {
                CreateChineseCatalog(false),
                CreateChineseCatalog(true),
            })
            {
                if (result.ContainsKey(catalog.LocaleCode))
                    throw new InvalidOperationException(
                        $"Duplicate VRCLens localization catalog '{catalog.LocaleCode}'.");
                result.Add(catalog.LocaleCode, catalog);
            }
        }

        private static VRCLensLocalizationCatalog CreateChineseCatalog(bool traditional)
        {
            var dynamicNames = traditional
                ? VRCLensChineseLocalizationCatalogs.CreateTraditionalDynamicNames()
                : VRCLensChineseLocalizationCatalogs.CreateSimplifiedDynamicNames();
            var names = traditional
                ? VRCLensChineseLocalizationCatalogs.CreateTraditional()
                : VRCLensChineseLocalizationCatalogs.CreateSimplified();
            var directions = traditional
                ? VRCLensChineseLocalizationCatalogs.CreateTraditionalDirections()
                : VRCLensChineseLocalizationCatalogs.CreateSimplifiedDirections();

            ContextualNameResolver contextual = delegate(
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
                        localized = dynamicNames.CameraPinDrop;
                        return true;
                    }
                    if (parameterName == "VRCLFeatureToggle" && Approximately(value, 251))
                    {
                        localized = dynamicNames.WorldDrop;
                        return true;
                    }
                    return false;
                }
                if (parameterName == "VRCLFeatureToggle")
                {
                    if (sourceName == "Zoom In" && Approximately(value, 107))
                    {
                        localized = dynamicNames.ZoomInSpeed4;
                        return true;
                    }
                    if (sourceName == "Zoom Out" && Approximately(value, 103))
                    {
                        localized = dynamicNames.ZoomOutSpeed4;
                        return true;
                    }
                }
                if (sourceName == "Next" && isGeneratedNextWithinVrclens)
                {
                    localized = dynamicNames.NextPage;
                    return true;
                }

                Match match = Regex.Match(sourceName, @"^Pin\s+(\d+)$",
                    RegexOptions.CultureInvariant);
                if (match.Success && IsCameraPinParameter(parameterName))
                {
                    localized = dynamicNames.FormatCameraPin(match.Groups[1].Value);
                    return true;
                }
                match = Regex.Match(sourceName, @"^(\d+)s$", RegexOptions.CultureInvariant);
                if (match.Success && (parameterName == "VRCL_Custom/DollyDelay"
                                      || parameterName == "VRCL_Custom/DollyDuration"))
                {
                    localized = dynamicNames.FormatSeconds(match.Groups[1].Value);
                    return true;
                }
                match = Regex.Match(sourceName, @"^(\d+)\s+min$", RegexOptions.CultureInvariant);
                if (match.Success && parameterName == "VRCL_Custom/DollyDuration")
                {
                    localized = dynamicNames.FormatMinutes(match.Groups[1].Value);
                    return true;
                }
                if (sourceName == "Softness"
                    && parameterName == "VRCL_Custom/VignetteSoftness")
                {
                    localized = dynamicNames.VignetteSoftness;
                    return true;
                }
                if (sourceName == "X" && parameterName == "VRCL_Custom/FisheyeLensCenterX")
                {
                    localized = dynamicNames.FisheyeCenterX;
                    return true;
                }
                if (sourceName == "Y" && parameterName == "VRCL_Custom/FisheyeLensCenterY")
                {
                    localized = dynamicNames.FisheyeCenterY;
                    return true;
                }
                return false;
            };

            FunctionalBlankResolver blank = delegate(
                string parameterName,
                VRCExpressionsMenu.Control.ControlType controlType,
                float value,
                out string localized)
            {
                localized = null;
                int integralValue = (int)Math.Round(value);
                if (!Approximately(value, integralValue)) return false;
                if (parameterName == "VRCLFeatureToggle"
                    && controlType == VRCExpressionsMenu.Control.ControlType.Button)
                    return dynamicNames.FeatureToggleBlankNames.TryGetValue(
                        integralValue, out localized);
                if (parameterName == "VRCLS_SensorSize"
                    && controlType == VRCExpressionsMenu.Control.ControlType.Toggle)
                    return dynamicNames.SensorBlankNames.TryGetValue(integralValue, out localized);
                if (parameterName == "VRCLS_TonemapFilter"
                    && controlType == VRCExpressionsMenu.Control.ControlType.Toggle)
                    return dynamicNames.TonemapBlankNames.TryGetValue(integralValue, out localized);
                return false;
            };

            ImpliedPivotDirectionResolver pivot = delegate(
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
                localized = dynamicNames.ImpliedMovePivotDirections[labelIndex];
                return true;
            };

            return CreateAdapter(
                traditional
                    ? VRCLensChineseLocalizationCatalogs.TraditionalLocaleCode
                    : VRCLensChineseLocalizationCatalogs.SimplifiedLocaleCode,
                traditional ? "繁體中文" : "简体中文",
                names, directions, dynamicNames.CameraPinDrop, dynamicNames.NextPage,
                contextual, blank, pivot);
        }

        private static bool IsCameraPinParameter(string parameterName)
        {
            return !string.IsNullOrEmpty(parameterName)
                   && (parameterName == "VRCL_Custom/DollyToPin"
                       || parameterName == "VRCL_Custom/TelePin"
                       || parameterName.StartsWith("VRCL_Custom/DropPin",
                                                   StringComparison.Ordinal));
        }

        private static bool Approximately(float left, float right)
        {
            return Math.Abs(left - right) < 0.001f;
        }

        internal static VRCLensLocalizationCatalog CreateAdapter(
            string localeCode,
            string nativeName,
            Dictionary<string, string> knownNames,
            Dictionary<string, string> directionLabels,
            string cameraPinDrop,
            string next,
            ContextualNameResolver contextualName,
            FunctionalBlankResolver functionalBlankName,
            ImpliedPivotDirectionResolver impliedPivotDirection)
        {
            return new VRCLensLocalizationCatalog(
                localeCode, nativeName, knownNames, directionLabels, cameraPinDrop, next,
                contextualName, functionalBlankName, impliedPivotDirection);
        }
    }
}
#endif
