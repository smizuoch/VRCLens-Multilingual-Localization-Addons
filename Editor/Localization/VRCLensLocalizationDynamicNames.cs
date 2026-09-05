#if UNITY_EDITOR
using System.Collections.Generic;
namespace VRCLensCustom
{
        internal sealed class VRCLensLocalizationDynamicNames
        {
            internal readonly string NextPage;
            internal readonly string CameraPinTemplate;
            internal readonly string SecondsTemplate;
            internal readonly string MinutesTemplate;
            internal readonly string WorldDrop;
            internal readonly string CameraPinDrop;
            internal readonly string VignetteSoftness;
            internal readonly string FisheyeCenterX;
            internal readonly string FisheyeCenterY;
            internal readonly string ZoomInSpeed4;
            internal readonly string ZoomOutSpeed4;
            internal readonly string[] ImpliedMovePivotDirections;
            internal readonly Dictionary<int, string> FeatureToggleBlankNames;
            internal readonly Dictionary<int, string> SensorBlankNames;
            internal readonly Dictionary<int, string> TonemapBlankNames;

            internal VRCLensLocalizationDynamicNames(
                string nextPage,
                string cameraPinPrefix,
                string secondsSuffix,
                string minutesSuffix,
                string worldDrop,
                string cameraPinDrop,
                string vignetteSoftness,
                string fisheyeCenterX,
                string fisheyeCenterY,
                string zoomInSpeed4,
                string zoomOutSpeed4,
                string[] impliedMovePivotDirections,
                Dictionary<int, string> featureToggleBlankNames,
                Dictionary<int, string> sensorBlankNames,
                Dictionary<int, string> tonemapBlankNames)
            {
                NextPage = nextPage;
                CameraPinTemplate = cameraPinPrefix.Contains("{0}") ? cameraPinPrefix : cameraPinPrefix + "{0}";
                SecondsTemplate = secondsSuffix.Contains("{0}") ? secondsSuffix : "{0}" + secondsSuffix;
                MinutesTemplate = minutesSuffix.Contains("{0}") ? minutesSuffix : "{0}" + minutesSuffix;
                WorldDrop = worldDrop;
                CameraPinDrop = cameraPinDrop;
                VignetteSoftness = vignetteSoftness;
                FisheyeCenterX = fisheyeCenterX;
                FisheyeCenterY = fisheyeCenterY;
                ZoomInSpeed4 = zoomInSpeed4;
                ZoomOutSpeed4 = zoomOutSpeed4;
                ImpliedMovePivotDirections = impliedMovePivotDirections;
                FeatureToggleBlankNames = featureToggleBlankNames;
                SensorBlankNames = sensorBlankNames;
                TonemapBlankNames = tonemapBlankNames;
            }

            internal string FormatCameraPin(string number)
            {
                return CameraPinTemplate.Replace("{0}", number);
            }

            internal string FormatSeconds(string number)
            {
                return SecondsTemplate.Replace("{0}", number);
            }

            internal string FormatMinutes(string number)
            {
                return MinutesTemplate.Replace("{0}", number);
            }
        }

}
#endif
