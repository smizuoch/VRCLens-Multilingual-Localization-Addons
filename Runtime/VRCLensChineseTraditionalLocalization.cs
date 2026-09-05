#if UNITY_EDITOR
using UnityEngine;

namespace VRCLensCustom
{
    /// <summary>Marks an avatar for build-time Traditional Chinese VRCLens menu localization.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Scripts/VRCLens Traditional Chinese Localization (VRCLens Custom)")]
    public sealed class VRCLensChineseTraditionalLocalization : VRCLensLocalizationMarker
    {
    }
}
#endif
