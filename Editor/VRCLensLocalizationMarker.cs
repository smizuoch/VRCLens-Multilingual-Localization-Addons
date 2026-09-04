#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;

namespace VRCLensCustom
{
    /// <summary>
    /// Common base for the settings-free localization installer markers.
    /// Removing the installer prefab remains the only on/off switch; build hooks intentionally find
    /// markers on inactive objects as well.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class VRCLensLocalizationMarker : MonoBehaviour, IEditorOnly
    {
    }

    /// <summary>Immutable identity and installer metadata for one shipped localization.</summary>
    internal sealed class VRCLensLocalizationProfile
    {
        internal string LocaleCode { get; }
        internal string NativeName { get; }
        internal Type MarkerType { get; }
        internal string PrefabPath { get; }

        internal VRCLensLocalizationProfile(
            string localeCode,
            string nativeName,
            Type markerType,
            string prefabPath)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
                throw new ArgumentException("A localization profile needs a locale code.", nameof(localeCode));
            if (string.IsNullOrWhiteSpace(nativeName))
                throw new ArgumentException("A localization profile needs a native name.", nameof(nativeName));
            if (markerType == null || !typeof(VRCLensLocalizationMarker).IsAssignableFrom(markerType))
                throw new ArgumentException(
                    "A localization profile marker must derive from VRCLensLocalizationMarker.",
                    nameof(markerType));
            if (string.IsNullOrWhiteSpace(prefabPath))
                throw new ArgumentException("A localization profile needs a prefab path.", nameof(prefabPath));

            LocaleCode = localeCode;
            NativeName = nativeName;
            MarkerType = markerType;
            PrefabPath = prefabPath;
        }
    }

    /// <summary>
    /// One source of truth for the marker type, locale and installer that belong together.
    /// Translation strings deliberately do not live here.
    /// </summary>
    internal static class VRCLensLocalizationRegistry
    {
        internal const string JapaneseLocale = "ja-JP";
        internal const string ChineseSimplifiedLocale = "zh-Hans-CN";
        internal const string ChineseTraditionalLocale = "zh-Hant-TW";
        internal const string KoreanLocale = "ko-KR";

        private static readonly VRCLensLocalizationProfile[] RegisteredProfiles =
        {
            new VRCLensLocalizationProfile(
                JapaneseLocale,
                "日本語",
                typeof(VRCLensJapaneseLocalization),
                "Assets/VRCLens_Custom/[Utility] JapaneseLocalization.prefab"),
            new VRCLensLocalizationProfile(
                ChineseSimplifiedLocale,
                "简体中文",
                typeof(VRCLensChineseSimplifiedLocalization),
                "Assets/VRCLens_Custom/[Utility] ChineseSimplifiedLocalization.prefab"),
            new VRCLensLocalizationProfile(
                ChineseTraditionalLocale,
                "繁體中文",
                typeof(VRCLensChineseTraditionalLocalization),
                "Assets/VRCLens_Custom/[Utility] ChineseTraditionalLocalization.prefab"),
            new VRCLensLocalizationProfile(
                KoreanLocale,
                "한국어",
                typeof(VRCLensKoreanLocalization),
                "Assets/VRCLens_Custom/[Utility] KoreanLocalization.prefab"),
        };

        private static readonly IReadOnlyList<VRCLensLocalizationProfile> ReadOnlyProfiles =
            Array.AsReadOnly(RegisteredProfiles);

        private static readonly IReadOnlyDictionary<Type, VRCLensLocalizationProfile> ByMarkerType =
            RegisteredProfiles.ToDictionary(profile => profile.MarkerType);

        private static readonly IReadOnlyDictionary<string, VRCLensLocalizationProfile> ByLocaleCode =
            RegisteredProfiles.ToDictionary(profile => profile.LocaleCode, StringComparer.Ordinal);

        internal static IReadOnlyList<VRCLensLocalizationProfile> Profiles => ReadOnlyProfiles;

        internal static bool TryGet(Type markerType, out VRCLensLocalizationProfile profile)
        {
            profile = null;
            return markerType != null && ByMarkerType.TryGetValue(markerType, out profile);
        }

        internal static bool TryGet(
            string localeCode,
            out VRCLensLocalizationProfile profile)
        {
            profile = null;
            return localeCode != null && ByLocaleCode.TryGetValue(localeCode, out profile);
        }

        internal static VRCLensLocalizationProfile GetRequiredProfile(string localeCode)
        {
            VRCLensLocalizationProfile profile;
            if (!TryGet(localeCode, out profile))
                throw new InvalidOperationException(
                    $"No VRCLens localization profile is registered for " +
                    $"'{localeCode ?? "<null>"}'.");
            return profile;
        }
    }

    /// <summary>The sole localization selected for one avatar build.</summary>
    internal sealed class VRCLensLocalizationSelection
    {
        internal VRCLensLocalizationProfile Profile { get; }
        internal VRCLensLocalizationMarker Marker { get; }

        internal VRCLensLocalizationSelection(
            VRCLensLocalizationProfile profile,
            VRCLensLocalizationMarker marker)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Marker = marker ?? throw new ArgumentNullException(nameof(marker));
        }
    }

    /// <summary>
    /// Resolves zero or one avatar-wide localization. Multiple installers are always an error,
    /// including duplicate copies of the same language; silently choosing one would make the result
    /// depend on hierarchy/component order.
    /// </summary>
    internal static class VRCLensLocalizationSelector
    {
        internal static bool TrySelect(
            GameObject avatar,
            out VRCLensLocalizationSelection selection,
            out string error)
        {
            selection = null;
            error = null;
            if (avatar == null)
            {
                error = "VRCLens localization could not inspect a missing avatar GameObject.";
                return false;
            }

            // Unity can leave a null slot in GetComponentsInChildren<T>() while an editor-only
            // component is being destroyed (notably when Enter Play Mode Options disables domain
            // and scene reloads). Treat those transient slots as absent. Counting the raw array or
            // dereferencing its only element made this global build hook crash on unrelated
            // avatars after a previous preview/build removed a localization marker.
            var markers = LiveMarkers(
                avatar.GetComponentsInChildren<VRCLensLocalizationMarker>(true));
            if (markers.Length == 0) return true;

            if (markers.Length > 1)
            {
                error = $"Found {markers.Length} VRCLens localization add-ons on this avatar. " +
                        "Only one language can be installed at a time. Remove all but one:\n" +
                        string.Join("\n", markers.Select(marker => "- " + Describe(marker)));
                return false;
            }

            VRCLensLocalizationProfile profile;
            var sole = markers[0];
            if (!VRCLensLocalizationRegistry.TryGet(sole.GetType(), out profile))
            {
                error = "Found an unsupported VRCLens localization marker at " +
                        $"'{HierarchyPath(sole.transform)}' ({sole.GetType().FullName}). " +
                        "Remove it or replace it with one of the shipped localization prefabs.";
                return false;
            }

            selection = new VRCLensLocalizationSelection(profile, sole);
            return true;
        }

        private static VRCLensLocalizationMarker[] LiveMarkers(
            IEnumerable<VRCLensLocalizationMarker> markers)
        {
            return markers == null
                ? new VRCLensLocalizationMarker[0]
                : markers.Where(marker => marker != null).ToArray();
        }

        /// <summary>
        /// Asset-free regression coverage for avatar-wide selection. It intentionally uses separate
        /// child objects for duplicates because DisallowMultipleComponent only guards one object.
        /// No build callback or dialog is invoked by this test.
        /// </summary>
        internal static bool SelfTest(out string error)
        {
            try
            {
                var profiles = VRCLensLocalizationRegistry.Profiles;
                if (profiles.Count != 4)
                    throw new InvalidOperationException(
                        $"expected four localization profiles, found {profiles.Count}");

                var nullOnly = LiveMarkers(new VRCLensLocalizationMarker[] { null });
                if (nullOnly.Length != 0)
                    throw new InvalidOperationException(
                        "a transient null marker was not ignored");

                WithTemporaryAvatar("LocalizationSelector_Zero", avatar =>
                {
                    VRCLensLocalizationSelection selected;
                    string selectionError;
                    if (!TrySelect(avatar, out selected, out selectionError)
                        || selected != null || !string.IsNullOrEmpty(selectionError))
                        throw new InvalidOperationException(
                            "an avatar with no localization marker did not select no language");
                });

                foreach (var profile in profiles)
                {
                    AssertSingleProfile(profile, false);
                    AssertSingleProfile(profile, true);
                    AssertDisabledSingleProfile(profile);
                    AssertConflict(profile, profile, false,
                        "same-language duplicate " + profile.LocaleCode);
                }

                for (int left = 0; left < profiles.Count; left++)
                {
                    for (int right = left + 1; right < profiles.Count; right++)
                    {
                        AssertConflict(profiles[left], profiles[right], false,
                            profiles[left].LocaleCode + " + " + profiles[right].LocaleCode);
                    }
                }

                AssertMultipleConflict(profiles.Take(3).ToArray(),
                    "three different localization markers");
                AssertMultipleConflict(profiles.ToArray(),
                    "all four localization markers");

                // A disabled/inactive installer is still installed: prefab removal, not a checkbox,
                // is the supported switch. Prove it also participates in a conflict.
                AssertConflict(profiles[0], profiles[profiles.Count - 1], true,
                    "active + inactive localization markers");
                AssertDisabledConflict(profiles[1], profiles[2]);

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static void AssertDisabledSingleProfile(VRCLensLocalizationProfile profile)
        {
            WithTemporaryAvatar("LocalizationSelector_Disabled_" + profile.LocaleCode, avatar =>
            {
                var marker = AddMarker(avatar, profile, "Disabled", false);
                marker.enabled = false;
                VRCLensLocalizationSelection selected;
                string selectionError;
                if (!TrySelect(avatar, out selected, out selectionError)
                    || selected == null || selected.Marker != marker)
                    throw new InvalidOperationException(
                        profile.LocaleCode + " disabled marker was not selected");
            });
        }

        private static void AssertDisabledConflict(
            VRCLensLocalizationProfile first,
            VRCLensLocalizationProfile second)
        {
            WithTemporaryAvatar("LocalizationSelector_DisabledConflict", avatar =>
            {
                AddMarker(avatar, first, "Active", false);
                var disabled = AddMarker(avatar, second, "Disabled", false);
                disabled.enabled = false;
                VRCLensLocalizationSelection selected;
                string selectionError;
                if (TrySelect(avatar, out selected, out selectionError)
                    || string.IsNullOrWhiteSpace(selectionError)
                    || selectionError.IndexOf(first.LocaleCode, StringComparison.Ordinal) < 0
                    || selectionError.IndexOf(second.LocaleCode, StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException(
                        "selector did not reject an active + disabled localization conflict");
            });
        }

        private static void AssertSingleProfile(
            VRCLensLocalizationProfile profile,
            bool inactive)
        {
            WithTemporaryAvatar(
                "LocalizationSelector_Single_" + profile.LocaleCode +
                (inactive ? "_Inactive" : ""),
                avatar =>
                {
                    var marker = AddMarker(avatar, profile, "Only", inactive);
                    VRCLensLocalizationSelection selected;
                    string selectionError;
                    if (!TrySelect(avatar, out selected, out selectionError)
                        || selected == null
                        || !ReferenceEquals(selected.Profile, profile)
                        || selected.Marker != marker
                        || !string.IsNullOrEmpty(selectionError))
                        throw new InvalidOperationException(
                            $"{profile.LocaleCode} was not selected as the sole " +
                            (inactive ? "inactive " : "") + "localization marker");
                });
        }

        private static void AssertConflict(
            VRCLensLocalizationProfile first,
            VRCLensLocalizationProfile second,
            bool secondInactive,
            string scenario)
        {
            WithTemporaryAvatar("LocalizationSelector_Conflict_" + scenario, avatar =>
            {
                AddMarker(avatar, first, "First", false);
                AddMarker(avatar, second, "Second", secondInactive);
                VRCLensLocalizationSelection selected;
                string selectionError;
                if (TrySelect(avatar, out selected, out selectionError)
                    || selected != null
                    || string.IsNullOrWhiteSpace(selectionError)
                    || selectionError.IndexOf("Found 2", StringComparison.Ordinal) < 0
                    || selectionError.IndexOf(first.LocaleCode, StringComparison.Ordinal) < 0
                    || selectionError.IndexOf(second.LocaleCode, StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException(
                        $"selector did not reject {scenario} with a complete error message");
            });
        }

        private static void AssertMultipleConflict(
            IReadOnlyList<VRCLensLocalizationProfile> profiles,
            string scenario)
        {
            WithTemporaryAvatar("LocalizationSelector_Conflict_" + scenario, avatar =>
            {
                for (int index = 0; index < profiles.Count; index++)
                    AddMarker(avatar, profiles[index], "Marker" + index, false);

                VRCLensLocalizationSelection selected;
                string selectionError;
                if (TrySelect(avatar, out selected, out selectionError)
                    || selected != null
                    || string.IsNullOrWhiteSpace(selectionError)
                    || selectionError.IndexOf("Found " + profiles.Count,
                                              StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException(
                        $"selector did not reject {scenario} with a complete error message");

                foreach (var profile in profiles)
                {
                    if (selectionError.IndexOf(profile.LocaleCode,
                                               StringComparison.Ordinal) < 0
                        || selectionError.IndexOf(profile.NativeName,
                                                  StringComparison.Ordinal) < 0)
                        throw new InvalidOperationException(
                            $"selector omitted {profile.LocaleCode} from the {scenario} error");
                }
            });
        }

        private static VRCLensLocalizationMarker AddMarker(
            GameObject avatar,
            VRCLensLocalizationProfile profile,
            string suffix,
            bool inactive)
        {
            var child = new GameObject($"{profile.LocaleCode}_{suffix}")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            child.transform.SetParent(avatar.transform, false);
            var marker = child.AddComponent(profile.MarkerType) as VRCLensLocalizationMarker;
            if (marker == null)
                throw new InvalidOperationException(
                    $"could not add the {profile.LocaleCode} marker component");
            if (inactive) child.SetActive(false);
            return marker;
        }

        private static void WithTemporaryAvatar(string name, Action<GameObject> test)
        {
            var avatar = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                test(avatar);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(avatar);
            }
        }

        private static string Describe(VRCLensLocalizationMarker marker)
        {
            if (marker == null) return "missing script";
            VRCLensLocalizationProfile profile;
            string language = VRCLensLocalizationRegistry.TryGet(marker.GetType(), out profile)
                ? $"{profile.NativeName} ({profile.LocaleCode})"
                : marker.GetType().FullName;
            return $"{language} at '{HierarchyPath(marker.transform)}'";
        }

        private static string HierarchyPath(Transform transform)
        {
            if (transform == null) return "(missing object)";
            var segments = new List<string>();
            for (var current = transform; current != null; current = current.parent)
                segments.Add(current.name);
            segments.Reverse();
            return string.Join("/", segments);
        }
    }

    /// <summary>
    /// Narrow hook-facing seam between build callbacks and the shared localization engine.
    /// </summary>
    internal static class VRCLensLocalizationDispatch
    {
        internal static bool BeginFavoriteBridge(
            GameObject avatar,
            VRCLensLocalizationProfile profile,
            out string error)
        {
            error = null;
            if (profile == null) return true;
            try
            {
                VRCLensLocalizationCatalog.ForLocale(profile.LocaleCode);
            }
            catch (InvalidOperationException exception)
            {
                error = exception.Message;
                return false;
            }
            VRCLensMenuLocalizer.BeginFavoriteBridge(avatar, profile);
            return true;
        }

        internal static void TagFavoriteControl(
            GameObject avatar,
            VRCLensLocalizationProfile profile,
            VRCExpressionsMenu.Control control,
            string catalogId,
            string sourceName,
            string parentLabel,
            string chosenAlias)
        {
            if (avatar == null) throw new ArgumentNullException(nameof(avatar));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            VRCLensMenuLocalizer.TagFavoriteControl(
                avatar, control, catalogId, sourceName, parentLabel, chosenAlias);
        }

        internal static void CancelFavoriteBridge(GameObject avatar)
        {
            VRCLensMenuLocalizer.CancelFavoriteBridge(avatar);
        }

        internal static void ClearAllFavoriteBridges()
        {
            VRCLensMenuLocalizer.ClearAllFavoriteBridges();
        }

        internal static bool Apply(
            GameObject avatar,
            string tempDir,
            VRCLensLocalizationProfile profile,
            out string error)
        {
            error = null;
            if (profile == null) return true;
            try
            {
                VRCLensLocalizationCatalog.ForLocale(profile.LocaleCode);
            }
            catch (InvalidOperationException exception)
            {
                error = exception.Message;
                return false;
            }
            return VRCLensMenuLocalizer.Apply(avatar, tempDir, profile);
        }
    }
}
#endif
