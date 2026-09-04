#if UNITY_EDITOR
using UnityEngine;

namespace VRCLensCustom
{
    /// <summary>Marks an avatar for build-time Simplified Chinese VRCLens menu localization.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Scripts/VRCLens Simplified Chinese Localization (VRCLens Custom)")]
    public sealed class VRCLensChineseSimplifiedLocalization : VRCLensLocalizationMarker
    {
    }
}
#endif
