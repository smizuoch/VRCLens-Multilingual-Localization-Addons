#if UNITY_EDITOR
using UnityEngine;

namespace VRCLensCustom
{
    /// <summary>Marks an avatar for build-time Korean VRCLens menu localization.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Scripts/VRCLens Korean Localization (VRCLens Custom)")]
    public sealed class VRCLensKoreanLocalization : VRCLensLocalizationMarker
    {
    }
}
#endif
