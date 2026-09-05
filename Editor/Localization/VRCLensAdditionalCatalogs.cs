#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRCLensCustom
{
    // Catalog JSON is editor data, never a component or an uploaded asset. Provenance travels
    // with each string so the source table and the actual menu cannot diverge.
    internal static class VRCLensAdditionalCatalogs
    {
        [Serializable]
        internal sealed class Entry
        {
            public string key;
            public string value;
            public string source;
            public string section;
            public string basis;
            public string note;
            public string sourceTerm;
        }

        [Serializable]
        internal sealed class Data
        {
            public string localeCode;
            public string nativeName;
            public Entry[] names;
            public Entry[] directions;
            public Entry[] dynamic;
            public Entry[] featureBlanks;
            public Entry[] sensorBlanks;
            public Entry[] tonemapBlanks;

            internal IEnumerable<Entry> AllEntries => names.Concat(directions).Concat(dynamic)
                .Concat(featureBlanks).Concat(sensorBlanks).Concat(tonemapBlanks);
        }

        internal const string Directory = "Assets/VRCLens_Custom/Editor/Catalogs";
        internal static readonly string[] RequiredLocales =
        {
            "fr-FR", "de-DE", "cs-CZ", "es-ES", "es-419", "ru-RU", "it-IT",
            "da-DK", "nl-NL", "fi-FI", "nb-NO", "nn-NO", "pl-PL", "pt-PT",
            "sv-SE", "bg-BG", "el-GR", "hu-HU", "ro-RO", "th-TH", "tr-TR", "uk-UA",
        };

        internal static Data Load(string locale)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(Directory + "/" + locale + ".json");
            if (asset == null) throw new InvalidOperationException("Missing catalog: " + locale);
            var data = JsonUtility.FromJson<Data>(asset.text);
            if (data == null || data.localeCode != locale || string.IsNullOrWhiteSpace(data.nativeName))
                throw new InvalidOperationException("Invalid catalog identity: " + locale);
            return data;
        }

        internal static bool ValidateData(out string error)
        {
            var issues = new List<string>();
            var sourceRows = new List<string> { "locale,group,key,value,basis,source,section,sourceTerm,note" };
            foreach (string locale in RequiredLocales)
            {
                try
                {
                    var data = Load(locale);
                    string[] groupNames = { "names", "directions", "dynamic", "featureBlanks", "sensorBlanks", "tonemapBlanks" };
                    Entry[][] groups = { data.names, data.directions, data.dynamic, data.featureBlanks, data.sensorBlanks, data.tonemapBlanks };
                    for (int index = 0; index < groups.Length; index++)
                        foreach (var entry in groups[index])
                            sourceRows.Add(string.Join(",", new[] { locale, groupNames[index], entry.key,
                                entry.value, entry.basis, entry.source, entry.section, entry.sourceTerm, entry.note }
                                .Select(value => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"")));
                    foreach (var group in new[] { data.names, data.directions, data.dynamic,
                                                 data.featureBlanks, data.sensorBlanks, data.tonemapBlanks })
                        Map(group, locale);
                    if (data.names.Length != 206 || data.directions.Length != 8 || data.dynamic.Length != 11
                        || data.featureBlanks.Length != 17 || data.sensorBlanks.Length != 6
                        || data.tonemapBlanks.Length != 5)
                        issues.Add(locale + " incorrect catalog group sizes");
                    foreach (var entry in data.AllEntries)
                    {
                        if (string.IsNullOrWhiteSpace(entry.basis) || string.IsNullOrWhiteSpace(entry.note))
                            issues.Add(locale + "/" + entry.key + " missing provenance status/rationale");
                        if (entry.basis != "review-required" && !Uri.TryCreate(entry.source, UriKind.Absolute, out _))
                            issues.Add(locale + "/" + entry.key + " missing source URL");
                        if ((entry.basis == "canon-exact" || entry.basis == "adobe-exact")
                            && entry.value != entry.sourceTerm)
                            issues.Add(locale + "/" + entry.key + " official spelling changed");
                        if (entry.value.IndexOf('\ufffd') >= 0 || entry.value.Contains("<")
                            || entry.value.Contains(">") || System.Text.RegularExpressions.Regex.IsMatch(
                                entry.value, @"&(?:[a-zA-Z]+|#\d+);"))
                            issues.Add(locale + "/" + entry.key + " invalid text/HTML entity");
                    }
                    var catalog = VRCLensLocalizationCatalog.ForLocale(locale);
                    foreach (string number in new[] { "1", "2", "12", "60" })
                    {
                        foreach (var pair in new[] {
                            new[] { "Pin " + number, "VRCL_Custom/DropPin" + number, "CameraPin" },
                            new[] { number + "s", "VRCL_Custom/DollyDelay", "Seconds" },
                            new[] { number + " min", "VRCL_Custom/DollyDuration", "Minutes" } })
                        {
                            var expected = data.dynamic.Single(e => e.key == pair[2]).value.Replace("{0}", number);
                            if (!catalog.TryContextualName(pair[0], pair[1],
                                    VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Button,
                                    0, false, false, out var actual) || expected != actual)
                                issues.Add(locale + " template resolution failed: " + pair[0]);
                        }
                    }
                    if (catalog.TryContextualName("Pin 12", "Unrelated",
                            VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Button,
                            0, false, false, out _))
                        issues.Add(locale + " translated an unrelated numbered control");
                }
                catch (Exception exception) { issues.Add(locale + ": " + exception.Message); }
            }
            const string sourceTable = "Assets/VRCLens_Custom/Documentation/TranslationSources.csv";
            if (!System.IO.File.Exists(sourceTable)
                || System.IO.File.ReadAllText(sourceTable).Replace("\r\n", "\n")
                   != string.Join("\n", sourceRows) + "\n")
                issues.Add("TranslationSources.csv differs from the shipped catalog values/provenance");
            error = string.Join("\n", issues);
            return issues.Count == 0;
        }

        internal static void AddTo(Dictionary<string, VRCLensLocalizationCatalog> result)
        {
            foreach (string locale in RequiredLocales)
            {
                var data = Load(locale);
                var names = Map(data.names, locale + " names");
                var directions = Map(data.directions, locale + " directions", StringComparer.OrdinalIgnoreCase);
                var dynamic = Map(data.dynamic, locale + " dynamic");
                foreach (string key in new[] { "CameraPin", "Seconds", "Minutes" })
                    if (dynamic[key].Split(new[] { "{0}" }, StringSplitOptions.None).Length != 2)
                        throw new InvalidOperationException(locale + " invalid number template: " + key);
                var dynamicNames = new VRCLensLocalizationDynamicNames(
                    dynamic["Next"], dynamic["CameraPin"], dynamic["Seconds"], dynamic["Minutes"],
                    dynamic["WorldDrop"], dynamic["CameraPinDrop"], dynamic["VignetteSoftness"],
                    dynamic["FisheyeCenterX"], dynamic["FisheyeCenterY"], dynamic["ZoomInSpeed4"],
                    dynamic["ZoomOutSpeed4"],
                    new[] { directions["FORWARD"], directions["RIGHT"], directions["BACK"], directions["LEFT"] },
                    IntMap(data.featureBlanks, locale), IntMap(data.sensorBlanks, locale),
                    IntMap(data.tonemapBlanks, locale));
                result.Add(locale, VRCLensLocalizationCatalog.CreateDataCatalog(
                    locale, data.nativeName, names, directions, dynamicNames));
            }
        }

        private static Dictionary<int, string> IntMap(Entry[] entries, string subject)
        {
            return Map(entries, subject).ToDictionary(
                p => int.Parse(p.Key, System.Globalization.CultureInfo.InvariantCulture), p => p.Value);
        }

        private static Dictionary<string, string> Map(
            Entry[] entries, string subject, StringComparer comparer = null)
        {
            if (entries == null) throw new InvalidOperationException("Missing entries: " + subject);
            var result = new Dictionary<string, string>(comparer ?? StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key)
                    || string.IsNullOrWhiteSpace(entry.value))
                    throw new InvalidOperationException("Empty translation: " + subject);
                if (result.ContainsKey(entry.key))
                    throw new InvalidOperationException("Duplicate translation: " + subject + "/" + entry.key);
                result.Add(entry.key, entry.value);
            }
            return result;
        }
    }
}
#endif
