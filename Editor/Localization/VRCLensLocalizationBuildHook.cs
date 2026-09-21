#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase.Editor.BuildPipeline;

namespace VRCLensCustom
{
    /// <summary>
    /// Rejects conflicting localization installers before VRCFury reads either prefab. This class is
    /// deliberately independent of Free Camera Add-ons so the translation unitypackage can be used
    /// with VRCLens and VRCFury alone.
    /// </summary>
    public sealed class VRCLensLocalizationPreflightHook : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => -20003;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            // Avatar build callbacks are sequential. Clear the whole transient hand-off table so
            // an aborted earlier build (whose cloned avatar had a different instance id) cannot be
            // mistaken for this build after Unity eventually reuses that id.
            VRCLensLocalizationDispatch.ClearAllFavoriteBridges();
            return VRCLensLocalizationBuildUtility.Prepare(avatarGameObject);
        }
    }

    /// <summary>
    /// Localizes after Free Camera (-1025) and avatar optimization. Preflight retains the locale and
    /// user-authored Favorites settings even if a processor has already stripped their components.
    /// VRCFury's final IEditorOnly cleanup runs at Int32.MaxValue.
    /// </summary>
    public sealed class VRCLensLocalizationBuildHook : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => int.MaxValue - 100;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            try
            {
                VRCLensLocalizationProfile profile;
                if (!VRCLensLocalizationBuildUtility.TryGetPreparedProfile(
                        avatarGameObject, out profile))
                    return false;
                if (profile == null) return true;

                // Compatibility with a project that still has the original localization-aware
                // Free Add-ons hook: never clone/localize the same build menu twice.
                if (VRCLensLocalizationBuildUtility.IsAlreadyLocalized(
                        avatarGameObject, profile))
                {
                    Debug.Log($"{VRCLensLocalizationBuildUtility.LogPrefix} The final menu is " +
                              $"already localized to {profile.NativeName} " +
                              $"({profile.LocaleCode}); skipped a duplicate pass.");
                    return true;
                }

                var before = VRCLensLocalizationBuildUtility.Snapshot(avatarGameObject);
                Debug.Log($"{VRCLensLocalizationBuildUtility.LogPrefix} Localizing the final " +
                          $"VRCLens menu for '{avatarGameObject.name}' in " +
                          $"{profile.NativeName} ({profile.LocaleCode})...");
                string error;
                if (!VRCLensLocalizationDispatch.Apply(
                        avatarGameObject,
                        VRCLensLocalizationBuildUtility.TempDir,
                        profile,
                        out error))
                {
                    if (!string.IsNullOrWhiteSpace(error))
                        return VRCLensLocalizationBuildUtility.Stop(error);
                    return false; // Apply already logged its specific diagnostic.
                }
                return VRCLensLocalizationBuildUtility.Audit(
                    avatarGameObject, before);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{VRCLensLocalizationBuildUtility.LogPrefix} Upload stopped: " +
                               "VRCLens menu localization failed. No source menu was edited.");
                Debug.LogError(exception);
                return false;
            }
            finally
            {
                // Menu Favorites may hand exact aliases across VRCFury. This is the final consumer;
                // no success, early return or exception may leak that avatar/locale state.
                VRCLensLocalizationDispatch.CancelFavoriteBridge(avatarGameObject);
                VRCLensLocalizationBuildUtility.ClearPreparedBuild();
            }
        }
    }

    internal static class VRCLensLocalizationBuildUtility
    {
        internal const string LogPrefix = "[VRCLens Localization]";
        // Outside Free Camera's destructive Temp cleanup and its source-menu validator.
        internal const string TempDir = "Assets/VRCLensLocalizationGenerated";
        private static GameObject preparedAvatar;
        private static VRCLensLocalizationProfile preparedProfile;

        internal static bool Prepare(GameObject avatar)
        {
            ClearPreparedBuild();
            if (!TrySelect(avatar, out var selection)) return false;
            try
            {
                if (selection != null)
                {
                    VRCLensMenuLocalizer.CaptureFavoritePages(avatar);
                    VRCLensCompositionLocalization.PrepareBuild(avatar);
                }
                preparedAvatar = avatar;
                preparedProfile = selection?.Profile;
                return true;
            }
            catch (Exception exception)
            {
                ClearPreparedBuild();
                return Stop(exception.GetBaseException().Message);
            }
        }

        internal static bool TryGetPreparedProfile(GameObject avatar, out VRCLensLocalizationProfile profile)
        {
            if (ReferenceEquals(avatar, preparedAvatar) && avatar != null)
            {
                profile = preparedProfile;
                return true;
            }
            // Direct callback invocation outside the SDK must never borrow another avatar's state.
            ClearPreparedBuild();
            if (!TrySelect(avatar, out var selection)) { profile = null; return false; }
            profile = selection?.Profile;
            return true;
        }

        internal static void ClearPreparedBuild()
        {
            preparedAvatar = null;
            preparedProfile = null;
            VRCLensMenuLocalizer.ClearFavoritePageSnapshot();
        }

        internal static bool SelfTest(out string error)
        {
            GameObject avatar = null;
            GameObject other = null;
            try
            {
                if (new VRCLensLocalizationBuildHook().callbackOrder <= -1024)
                    throw new InvalidOperationException("Localization must follow Free Camera and editor-only stripping.");
                if (TempDir.StartsWith("Assets/VRCLens_Custom/", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Generated menus must be outside Free Camera's asset tree.");
                if (typeof(VRCLensLocalizationMarker).Assembly.GetName().Name != "VRCLens.Localization.Runtime"
                    || typeof(VRCLensLocalizationBuildHook).Assembly.GetName().Name != "VRCLens.Localization.Editor")
                    throw new InvalidOperationException("Localization assembly isolation is missing.");
                avatar = new GameObject("LocalizationBuildStateTest");
                other = new GameObject("LocalizationBuildStateOther");
                foreach (var profile in VRCLensLocalizationRegistry.Profiles)
                {
                    var marker = avatar.AddComponent(profile.MarkerType);
                    if (!Prepare(avatar)) throw new InvalidOperationException("Preflight failed: " + profile.LocaleCode);
                    UnityEngine.Object.DestroyImmediate(marker);
                    if (!TryGetPreparedProfile(avatar, out var selected) || selected != profile)
                        throw new InvalidOperationException("Locale was lost when the marker was stripped: " + profile.LocaleCode);
                    if (!TryGetPreparedProfile(other, out selected) || selected != null)
                        throw new InvalidOperationException("Build state leaked to another avatar");
                    if (!Prepare(avatar) || !TryGetPreparedProfile(avatar, out selected) || selected != null)
                        throw new InvalidOperationException("Removing the installer did not restore the unlocalized build");
                }
                error = null;
                return true;
            }
            catch (Exception exception) { error = exception.GetBaseException().Message; return false; }
            finally
            {
                ClearPreparedBuild();
                if (avatar != null) UnityEngine.Object.DestroyImmediate(avatar);
                if (other != null) UnityEngine.Object.DestroyImmediate(other);
            }
        }

        internal sealed class InvariantSnapshot
        {
            internal VRCAvatarDescriptor Descriptor;
            internal VRCExpressionParameters Parameters;
            internal int ParameterCost;
            internal string ParameterJson;
            internal RuntimeAnimatorController[] BaseControllers;
            internal RuntimeAnimatorController[] SpecialControllers;
        }

        internal static bool TrySelect(
            GameObject avatarGameObject,
            out VRCLensLocalizationSelection selection)
        {
            string error;
            if (!VRCLensLocalizationSelector.TrySelect(
                    avatarGameObject, out selection, out error))
                return Stop(error);
            if (selection == null) return true;
            try
            {
                VRCLensLocalizationCatalog.ForLocale(selection.Profile.LocaleCode);
                return true;
            }
            catch (Exception exception)
            {
                return Stop(exception.Message);
            }
        }

        internal static bool Stop(string error)
        {
            error = string.IsNullOrWhiteSpace(error)
                ? "VRCLens localization stopped without a diagnostic."
                : error;
            Debug.LogError($"{LogPrefix} Upload failed. {error}");
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("VRCLens Localization", error, "OK");
            return false;
        }

        internal static bool IsAlreadyLocalized(
            GameObject avatarGameObject,
            VRCLensLocalizationProfile profile)
        {
            if (avatarGameObject == null || profile == null) return false;
            var descriptor = avatarGameObject.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null || descriptor.expressionsMenu == null) return false;
            string path = AssetDatabase.GetAssetPath(descriptor.expressionsMenu)
                .Replace('\\', '/');
            string root = TempDir + "/LocalizedMenus/" + profile.LocaleCode + "/";
            return path.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        internal static InvariantSnapshot Snapshot(GameObject avatarGameObject)
        {
            if (avatarGameObject == null)
                throw new InvalidOperationException(
                    "VRCLens localization received no avatar GameObject.");
            var descriptor = avatarGameObject.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null)
                throw new InvalidOperationException(
                    $"VRCLens localization could not find a VRCAvatarDescriptor on " +
                    $"'{avatarGameObject.name}'.");
            var parameters = descriptor.expressionParameters;
            return new InvariantSnapshot
            {
                Descriptor = descriptor,
                Parameters = parameters,
                ParameterCost = parameters == null ? -1 : parameters.CalcTotalCost(),
                ParameterJson = parameters == null
                    ? "<null>"
                    : EditorJsonUtility.ToJson(parameters, true),
                BaseControllers = Controllers(descriptor.baseAnimationLayers),
                SpecialControllers = Controllers(descriptor.specialAnimationLayers),
            };
        }

        internal static bool Audit(
            GameObject avatarGameObject,
            InvariantSnapshot before)
        {
            var after = Snapshot(avatarGameObject);
            var changes = new System.Collections.Generic.List<string>();
            if (!ReferenceEquals(before.Descriptor, after.Descriptor))
                changes.Add("VRCAvatarDescriptor reference");
            if (!ReferenceEquals(before.Parameters, after.Parameters))
                changes.Add("Expression Parameters reference");
            if (before.ParameterCost != after.ParameterCost)
                changes.Add($"Expression Parameters cost ({before.ParameterCost} -> " +
                            $"{after.ParameterCost})");
            if (!string.Equals(before.ParameterJson, after.ParameterJson,
                               StringComparison.Ordinal))
                changes.Add("Expression Parameters serialized data");
            if (!SameReferences(before.BaseControllers, after.BaseControllers))
                changes.Add("base animation-layer controllers/order");
            if (!SameReferences(before.SpecialControllers, after.SpecialControllers))
                changes.Add("special animation-layer controllers/order");

            if (changes.Count > 0)
                return Stop("Localization changed data outside display names: " +
                            string.Join(", ", changes) + ". Remove the localization prefab " +
                            "and report this as a VRCLens Localization bug.");

            Debug.Log($"{LogPrefix} Display-only invariant audit passed: Expression Parameters " +
                      "and Animator controller references are unchanged; the menu clone audit " +
                      "verified every serialized field except Control.name and Label.name.");
            return true;
        }

        private static RuntimeAnimatorController[] Controllers(
            VRCAvatarDescriptor.CustomAnimLayer[] layers)
        {
            return (layers ?? new VRCAvatarDescriptor.CustomAnimLayer[0])
                .Select(layer => layer.animatorController).ToArray();
        }

        private static bool SameReferences<T>(T[] left, T[] right)
            where T : UnityEngine.Object
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int index = 0; index < left.Length; index++)
                if (!ReferenceEquals(left[index], right[index])) return false;
            return true;
        }
    }
}
#endif
