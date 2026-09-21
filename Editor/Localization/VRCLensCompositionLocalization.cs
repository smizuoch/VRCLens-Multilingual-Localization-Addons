#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace VRCLensCustom
{
    // Optional standalone add-on support. No reference to its assembly or asset GUIDs is required.
    internal static class VRCLensCompositionLocalization
    {
        internal const string MenuFolder = "Assets/VRCLens_CompositionGuides/Resources/Menus";
        private const string DataFolder = "Assets/VRCLens_Custom/Editor/CompositionCatalogs";
        private const string ParameterPrefix = "VRCL_Custom/Composition";
        [Serializable] private sealed class Entry { public string key; public string value; }
        [Serializable] private sealed class Data
        {
            public string localeCode;
            public string basis;
            public Entry[] entries;
        }
        private static readonly Dictionary<string, Dictionary<string, string>> Cache =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        internal static IEnumerable<string> Keys => Names("ja-JP").Keys;

        private static Dictionary<string, string> Names(string locale)
        {
            if (Cache.TryGetValue(locale, out var cached)) return cached;
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(DataFolder + "/" + locale + ".json");
            if (asset == null) throw new InvalidOperationException("Missing composition catalog: " + locale);
            var data = JsonUtility.FromJson<Data>(asset.text);
            if (data == null || data.localeCode != locale || data.entries == null
                || string.IsNullOrWhiteSpace(data.basis))
                throw new InvalidOperationException("Invalid composition catalog: " + locale);
            var names = data.entries.ToDictionary(e => e.key, e => e.value, StringComparer.Ordinal);
            Cache.Add(locale, names);
            return names;
        }

        private static bool HasParameter(VRCExpressionsMenu.Control c) => c != null &&
            (IsParameter(c.parameter?.name)
             || (c.subParameters != null && c.subParameters.Any(p => IsParameter(p?.name))));
        private static bool IsParameter(string name) => name != null
            && name.StartsWith(ParameterPrefix, StringComparison.Ordinal);

        private static bool ContainsComposition(VRCExpressionsMenu menu, HashSet<VRCExpressionsMenu> seen)
        {
            if (menu == null || !seen.Add(menu)) return false;
            return (menu.controls ?? new List<VRCExpressionsMenu.Control>()).Any(c =>
                HasParameter(c) || (c != null && c.type == VRCExpressionsMenu.Control.ControlType.SubMenu
                    && ContainsComposition(c.subMenu, seen)));
        }

        internal static bool TryName(VRCExpressionsMenu.Control control, string locale, out string name)
            => TryQualifier(control, control?.name, locale, out name);

        internal static bool TryQualifier(VRCExpressionsMenu.Control control, string sourceName,
                                          string locale, out string name)
        {
            name = null;
            if (control == null || string.IsNullOrEmpty(sourceName)) return false;
            if (!HasParameter(control) && !(control.type == VRCExpressionsMenu.Control.ControlType.SubMenu
                && ContainsComposition(control.subMenu, new HashSet<VRCExpressionsMenu>()))) return false;
            return Names(locale).TryGetValue(sourceName, out name);
        }

        // Must run before the standalone -20002 preflight captures its language. It sees English
        // source labels; the selected localization catalog then owns the final display names.
        // Called only for an avatar with one valid localization marker, on the SDK build copy.
        internal static void PrepareBuild(GameObject avatar)
        {
            foreach (var component in avatar.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component.GetType().FullName !=
                    "VRCLensCompositionGuides.CompositionGuidesInstaller") continue;
                var field = component.GetType().GetField("language", BindingFlags.Public | BindingFlags.Instance);
                if (field == null || !field.FieldType.IsEnum || !Enum.IsDefined(field.FieldType, "English"))
                    throw new InvalidOperationException("Unsupported Composition Guides language setting.");
                field.SetValue(component, Enum.Parse(field.FieldType, "English"));
            }
        }

        internal static bool Validate(out string error)
        {
            Cache.Clear();
            var issues = new List<string>();
            var menus = new List<VRCExpressionsMenu>();
            try
            {
                var expected = new HashSet<string>(Keys, StringComparer.Ordinal);
                if (expected.Count != 76) issues.Add("Expected 76 composition keys.");
                foreach (var catalog in VRCLensLocalizationCatalog.All)
                {
                    var names = Names(catalog.LocaleCode);
                    if (!expected.SetEquals(names.Keys) || names.Values.Any(string.IsNullOrWhiteSpace)
                        || names.Values.Any(v => v.Contains("\ufffd")))
                        issues.Add(catalog.LocaleCode + ": incomplete composition translations.");
                    foreach (var pair in names)
                    {
                        var control = new VRCExpressionsMenu.Control {
                            name = pair.Key, type = VRCExpressionsMenu.Control.ControlType.Button,
                            parameter = new VRCExpressionsMenu.Control.Parameter { name = ParameterPrefix + "ASelectTrigger" }
                        };
                        if (!VRCLensMenuLocalizer.TryLocalizedName(control, catalog, out var actual)
                            || actual != pair.Value) issues.Add(catalog.LocaleCode + ": unresolved " + pair.Key);
                        control.parameter.name = "Unrelated";
                        if (VRCLensMenuLocalizer.TryLocalizedName(control, catalog, out _))
                            issues.Add(catalog.LocaleCode + ": translated unrelated " + pair.Key);
                    }
                    // Radial controls use subParameters; submenus may be shared and cyclic.
                    var root = ScriptableObject.CreateInstance<VRCExpressionsMenu>(); menus.Add(root);
                    var child = ScriptableObject.CreateInstance<VRCExpressionsMenu>(); menus.Add(child);
                    var radial = new VRCExpressionsMenu.Control { name = "Guide Scale",
                        type = VRCExpressionsMenu.Control.ControlType.RadialPuppet,
                        subParameters = new[] { new VRCExpressionsMenu.Control.Parameter { name = ParameterPrefix + "AScale" } }
                    };
                    var submenu = new VRCExpressionsMenu.Control { name = "Composition Guides",
                        type = VRCExpressionsMenu.Control.ControlType.SubMenu, subMenu = child };
                    root.controls.Add(submenu);
                    child.controls.Add(new VRCExpressionsMenu.Control { name = "Loop",
                        type = VRCExpressionsMenu.Control.ControlType.SubMenu, subMenu = root });
                    child.controls.Add(radial);
                    if (!VRCLensMenuLocalizer.TryLocalizedName(radial, catalog, out var radialName)
                        || radialName != names["Guide Scale"]
                        || !VRCLensMenuLocalizer.TryLocalizedName(submenu, catalog, out var submenuName)
                        || submenuName != names["Composition Guides"])
                        issues.Add(catalog.LocaleCode + ": radial/cyclic submenu scope failed.");
                }
                if (new VRCLensLocalizationPreflightHook().callbackOrder >= -20002)
                    issues.Add("Localization preflight must precede Composition Guides (-20002).");

                // If installed, Japanese must remain identical to the standalone author's labels.
                const string labelPath = "Assets/VRCLens_CompositionGuides/Editor/CompositionGuides.labels.json";
                var labelAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(labelPath);
                if (labelAsset != null)
                {
                    var labels = JsonUtility.FromJson<AuthorLabels>(labelAsset.text);
                    if (labels.entries.Length != expected.Count) issues.Add("Standalone composition label count changed.");
                    foreach (var entry in labels.entries)
                        if (!Names("ja-JP").TryGetValue(entry.english, out var japanese) || japanese != entry.japanese)
                            issues.Add("Japanese composition label differs: " + entry.english);
                }
            }
            catch (Exception e) { issues.Add(e.GetBaseException().Message); }
            finally { foreach (var menu in menus) UnityEngine.Object.DestroyImmediate(menu); }
            error = string.Join("\n", issues);
            return issues.Count == 0;
        }
        [Serializable] private sealed class AuthorLabel { public string english; public string japanese; }
        [Serializable] private sealed class AuthorLabels { public AuthorLabel[] entries; }
    }
}
#endif
