#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace VRCLensCustom
{
    // Audit metadata only. Display strings continue to come from the existing C# catalogs.
    internal static class VRCLensLegacyTerminology
    {
        internal const string Root = "Assets/VRCLens_Custom/Documentation";
        internal static readonly string[] Locales = { "ja-JP", "zh-Hans-CN", "zh-Hant-TW", "ko-KR" };
        private static readonly string[] Groups = { "names", "directions", "dynamic", "featureBlanks", "sensorBlanks", "tonemapBlanks" };
        private const string Header = "locale,group,key,value,basis,source,section,sourceTerm,note";

        internal static bool Pending(VRCLensAdditionalCatalogs.Entry e) =>
            e.basis == "review-required" || e.basis == "function-reviewed";

        internal static int PendingCount => Locales.Sum(l => Load(l).AllEntries.Count(Pending));

        private static VRCLensAdditionalCatalogs.Data Load(string locale) =>
            JsonUtility.FromJson<VRCLensAdditionalCatalogs.Data>(File.ReadAllText(Root + "/LegacyCatalogs/" + locale + ".json"));

        internal static bool Validate(out string error)
        {
            var issues = new List<string>();
            var rows = new List<string> { Header };
            var pending = new List<string> { Header };
            foreach (string locale in Locales)
            {
                try
                {
                    var d = Load(locale);
                    var c = VRCLensLocalizationCatalog.ForLocale(locale);
                    if (d.localeCode != locale || d.nativeName != c.NativeName) throw new Exception("identity mismatch");
                    var groups = new[] { d.names, d.directions, d.dynamic, d.featureBlanks, d.sensorBlanks, d.tonemapBlanks };
                    var sizes = new[] { 206, 8, 11, 17, 6, 5 };
                    for (int i = 0; i < groups.Length; i++)
                    {
                        if (groups[i].Length != sizes[i] || groups[i].Select(e => e.key).Distinct().Count() != sizes[i])
                            throw new Exception(Groups[i] + " count/duplicate mismatch");
                        foreach (var e in groups[i])
                        {
                            string label = locale + "/" + Groups[i] + "/" + e.key;
                            if (string.IsNullOrWhiteSpace(e.value) || string.IsNullOrWhiteSpace(e.note)) issues.Add(label + " empty value/note");
                            if (!new[] { "review-required", "function-reviewed", "canon-exact", "canon-adapted", "adobe-exact", "author-adapted" }.Contains(e.basis)) issues.Add(label + " invalid status");
                            if (e.basis != "review-required" && !Uri.TryCreate(e.source, UriKind.Absolute, out _)) issues.Add(label + " missing URL");
                            if (e.basis.EndsWith("-exact", StringComparison.Ordinal) && e.sourceTerm != e.value) issues.Add(label + " exact term mismatch");
                            string row = string.Join(",", new[] { locale, Groups[i], e.key, e.value, e.basis, e.source, e.section, e.sourceTerm, e.note }
                                .Select(v => "\"" + (v ?? "").Replace("\"", "\"\"") + "\""));
                            rows.Add(row);
                            if (Pending(e)) pending.Add(row);
                            if (i < 2)
                            {
                                var names = i == 0 ? c.KnownNames : c.DirectionLabels;
                                if (!names.TryGetValue(e.key, out var value) || value != e.value) issues.Add(label + " actual catalog mismatch");
                            }
                            else if (i >= 3)
                            {
                                string parameter = new[] { "VRCLFeatureToggle", "VRCLS_SensorSize", "VRCLS_TonemapFilter" }[i - 3];
                                var type = i == 3 ? VRCExpressionsMenu.Control.ControlType.Button : VRCExpressionsMenu.Control.ControlType.Toggle;
                                if (!c.TryFunctionalBlankName(parameter, type, int.Parse(e.key), out var value) || value != e.value) issues.Add(label + " actual blank mismatch");
                            }
                            else ValidateDynamic(c, e, issues, label);
                        }
                    }
                }
                catch (Exception ex) { issues.Add(locale + ": " + ex.Message); }
            }
            foreach (var pair in new[] { new { File = "LegacyTranslationSources.csv", Rows = rows }, new { File = "LegacyTranslationReviewQueue.csv", Rows = pending } })
                if (File.ReadAllText(Root + "/" + pair.File).Replace("\r\n", "\n") != string.Join("\n", pair.Rows) + "\n") issues.Add(pair.File + " metadata mismatch");
            error = string.Join("\n", issues);
            return issues.Count == 0;
        }

        private static void ValidateDynamic(VRCLensLocalizationCatalog c, VRCLensAdditionalCatalogs.Entry e, List<string> issues, string label)
        {
            foreach (string n in new[] { "1", "2", "12", "60" })
            {
                string name, parameter;
                float value = 0;
                bool owned = false;
                var type = VRCExpressionsMenu.Control.ControlType.Button;
                switch (e.key)
                {
                    case "Next": name = "Next"; parameter = "VRCLFeatureToggle"; owned = true; break;
                    case "CameraPinDrop": name = "Drop"; parameter = "VRCLFeatureToggle"; owned = true; type = VRCExpressionsMenu.Control.ControlType.SubMenu; break;
                    case "WorldDrop": name = "Drop"; parameter = "VRCLFeatureToggle"; value = 251; break;
                    case "ZoomInSpeed4": name = "Zoom In"; parameter = "VRCLFeatureToggle"; value = 107; break;
                    case "ZoomOutSpeed4": name = "Zoom Out"; parameter = "VRCLFeatureToggle"; value = 103; break;
                    case "CameraPin": name = "Pin " + n; parameter = "VRCL_Custom/DropPin" + n; break;
                    case "Seconds": name = n + "s"; parameter = "VRCL_Custom/DollyDuration"; break;
                    case "Minutes": name = n + " min"; parameter = "VRCL_Custom/DollyDuration"; break;
                    case "VignetteSoftness": name = "Softness"; parameter = "VRCL_Custom/VignetteSoftness"; break;
                    case "FisheyeCenterX": name = "X"; parameter = "VRCL_Custom/FisheyeLensCenterX"; break;
                    case "FisheyeCenterY": name = "Y"; parameter = "VRCL_Custom/FisheyeLensCenterY"; break;
                    default: issues.Add(label + " unknown dynamic key"); return;
                }
                if (!c.TryContextualName(name, parameter, type, value, owned, owned, out var actual) || actual != e.value.Replace("{0}", n)) issues.Add(label + " actual dynamic mismatch");
            }
        }
    }
}
#endif
