#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRCLensCustom
{
    /// <summary>
    /// Self-contained release/import validator shipped by the translation-only unitypackage. It has
    /// no dependency on ModPrefabValidator or any Free Camera Add-ons class.
    /// </summary>
    public static class VRCLensLocalizationPackageValidator
    {
        private const string Prefix = "[VRCLens Localization Validator]";

        [MenuItem("Tools/VRCLens Localization/Validate Package")]
        public static void ValidateMenu()
        {
            var issues = ValidateAll();
            if (issues.Count == 0)
            {
                Debug.Log(Prefix + " Four catalogs, semantic coverage, selector, menu cloning, " +
                          "and all four installer prefabs passed validation.");
                if (!Application.isBatchMode)
                    EditorUtility.DisplayDialog(
                        "VRCLens Localization", "Validation passed.", "OK");
                return;
            }

            foreach (var issue in issues) Debug.LogError(Prefix + " " + issue);
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog(
                    "VRCLens Localization",
                    $"Validation failed with {issues.Count} issue(s). See Console for details.",
                    "OK");
        }

        public static List<string> ValidateAll()
        {
            var issues = new List<string>();
            TryValidation("catalog/menu coverage", issues,
                VRCLensMenuLocalizer.ValidateShippedCoverage);
            TryValidation("semantic/clone self-test", issues,
                VRCLensMenuLocalizer.SelfTest);
            TryValidation("marker selector self-test", issues,
                VRCLensLocalizationSelector.SelfTest);

            var profiles = VRCLensLocalizationRegistry.Profiles;
            if (profiles.Count != 4)
                issues.Add($"expected four localization profiles, found {profiles.Count}");
            AddDuplicates(issues, profiles.GroupBy(profile => profile.LocaleCode), "locale");
            AddDuplicates(issues, profiles.GroupBy(profile => profile.MarkerType.FullName),
                          "marker type");
            AddDuplicates(issues, profiles.GroupBy(
                profile => profile.PrefabPath, StringComparer.OrdinalIgnoreCase),
                "prefab path");

            Type vrcfuryType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly =>
                {
                    try { return assembly.GetType("VF.Model.VRCFury", false, false); }
                    catch { return null; }
                })
                .FirstOrDefault(type => type != null);
            if (vrcfuryType == null)
                issues.Add("VRCFury is not loaded; installer Full Controllers cannot be verified");

            foreach (var profile in profiles)
                ValidateInstaller(profile, vrcfuryType, issues);
            return issues;
        }

        public static void RunValidationForBatchMode()
        {
            var issues = ValidateAll();
            if (issues.Count != 0)
                throw new InvalidOperationException(
                    "VRCLens Localization validation failed:\n" + string.Join("\n", issues));
            Debug.Log(Prefix + " Batch validation passed.");
        }

        private delegate bool Validation(out string error);

        private static void TryValidation(
            string name,
            List<string> issues,
            Validation validation)
        {
            try
            {
                string error;
                if (!validation(out error))
                    issues.Add(name + " failed: " +
                               (string.IsNullOrWhiteSpace(error)
                                   ? "no diagnostic was returned"
                                   : error));
            }
            catch (Exception exception)
            {
                issues.Add(name + " threw: " + exception.GetBaseException().Message);
            }
        }

        private static void AddDuplicates<T>(
            List<string> issues,
            IEnumerable<IGrouping<string, T>> groups,
            string kind)
        {
            foreach (var group in groups.Where(group => group.Count() > 1))
                issues.Add($"{kind} '{group.Key}' is registered more than once");
        }

        private static void ValidateInstaller(
            VRCLensLocalizationProfile profile,
            Type vrcfuryType,
            List<string> issues)
        {
            string subject = $"{profile.NativeName} ({profile.LocaleCode}) installer " +
                             $"'{profile.PrefabPath}'";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(profile.PrefabPath);
            if (prefab == null)
            {
                issues.Add(subject + " is missing");
                return;
            }

            var markers = prefab.GetComponentsInChildren<VRCLensLocalizationMarker>(true);
            if (markers.Length != 1)
                issues.Add($"{subject} has {markers.Length} markers; expected exactly one");
            else if (markers[0].GetType() != profile.MarkerType
                     || markers[0].gameObject != prefab)
                issues.Add(subject + " does not have its exact marker on the root");

            var transforms = prefab.GetComponentsInChildren<Transform>(true);
            if (transforms.Length != 1)
                issues.Add($"{subject} has {transforms.Length - 1} child object(s)");
            if (vrcfuryType == null) return;

            var vrcfuryComponents = prefab.GetComponentsInChildren(vrcfuryType, true);
            if (vrcfuryComponents.Length != 1)
                issues.Add($"{subject} has {vrcfuryComponents.Length} VRCFury components; " +
                           "expected one empty Full Controller");
            foreach (var component in vrcfuryComponents)
            {
                var serialized = new SerializedObject(component);
                var broken = serialized.FindProperty("somethingIsBroken");
                var legacy = serialized.FindProperty("config.features");
                if (broken == null || broken.propertyType != SerializedPropertyType.Boolean
                    || broken.boolValue)
                    issues.Add(subject + " has a broken/unverifiable VRCFury component");
                if (legacy == null || !legacy.isArray || legacy.arraySize != 0)
                    issues.Add(subject + " has unexpected legacy VRCFury feature data");

                var content = serialized.FindProperty("content");
                string typeName = content == null ? "" : content.managedReferenceFullTypename;
                if (content == null
                    || !typeName.EndsWith(" VF.Model.Feature.FullController",
                                          StringComparison.Ordinal))
                {
                    issues.Add(subject + " does not contain a Full Controller");
                    continue;
                }
                string reason;
                if (!IsEmptyFullController(content, out reason))
                    issues.Add(subject + " Full Controller is not empty/canonical: " + reason);
            }

            var unexpected = prefab.GetComponentsInChildren<Component>(true)
                .Where(component => component == null
                    || (!(component is Transform)
                        && component.GetType() != profile.MarkerType
                        && !vrcfuryType.IsInstanceOfType(component)))
                .Select(component => component == null
                    ? "missing script"
                    : component.GetType().FullName)
                .Distinct().ToArray();
            if (unexpected.Length > 0)
                issues.Add(subject + " contains unexpected payload: " +
                           string.Join(", ", unexpected));
        }

        private static bool IsEmptyFullController(
            SerializedProperty content,
            out string reason)
        {
            var populated = new List<string>();
            foreach (var field in new[]
            {
                "controllers", "menus", "prms", "smoothedPrms", "rewriteBindings",
                "injectParams", "removePrefixes",
            })
            {
                var property = content.FindPropertyRelative(field);
                if (property == null || !property.isArray)
                    populated.Add(field + " (not verifiable)");
                else if (property.arraySize != 0)
                    populated.Add(field);
            }

            var globalParams = content.FindPropertyRelative("globalParams");
            if (globalParams == null || !globalParams.isArray
                || globalParams.arraySize != 1
                || globalParams.GetArrayElementAtIndex(0).stringValue != "*")
                populated.Add("globalParams (expected exactly ['*'])");

            foreach (var field in new[]
            {
                "allNonsyncedAreGlobal", "ignoreSaved", "rootBindingsApplyToAvatar",
                "allowMissingAssets", "useSecurityForToggle",
            })
            {
                var property = content.FindPropertyRelative(field);
                if (property == null
                    || property.propertyType != SerializedPropertyType.Boolean)
                    populated.Add(field + " (not verifiable)");
                else if (property.boolValue)
                    populated.Add(field);
            }

            foreach (var field in new[]
            {
                "toggleParam", "injectSpsDepthParam", "injectSpsVelocityParam", "submenu",
                "addPrefix",
            })
            {
                var property = content.FindPropertyRelative(field);
                if (property == null || property.propertyType != SerializedPropertyType.String)
                    populated.Add(field + " (not verifiable)");
                else if (!string.IsNullOrEmpty(property.stringValue))
                    populated.Add(field);
            }

            var rootOverride = content.FindPropertyRelative("rootObjOverride");
            if (rootOverride == null
                || rootOverride.propertyType != SerializedPropertyType.ObjectReference
                || rootOverride.objectReferenceValue != null)
                populated.Add("rootObjOverride");

            foreach (var field in new[] { "controller", "menu", "parameters" })
            {
                var wrapper = content.FindPropertyRelative(field);
                var objectReference = wrapper?.FindPropertyRelative("objRef");
                var guid = wrapper?.FindPropertyRelative("guid");
                var id = wrapper?.FindPropertyRelative("id");
                var fileId = wrapper?.FindPropertyRelative("fileID");
                if (wrapper == null || objectReference == null || guid == null
                    || id == null || fileId == null)
                {
                    populated.Add(field + " (not verifiable)");
                    continue;
                }
                if (objectReference.objectReferenceValue != null
                    || !string.IsNullOrEmpty(guid.stringValue)
                    || !string.IsNullOrEmpty(id.stringValue)
                    || fileId.longValue != 0)
                    populated.Add(field);
            }

            reason = string.Join(", ", populated.Distinct());
            return populated.Count == 0;
        }
    }
}
#endif
