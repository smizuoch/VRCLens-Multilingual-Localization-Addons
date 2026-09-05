#if UNITY_EDITOR
using UnityEngine;

namespace VRCLensCustom
{
    /// <summary>
    /// Marks an avatar for build-time Japanese localization of its merged VRCLens menu.
    /// The SDK removes this editor-only component from the uploaded avatar.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Scripts/VRCLens Japanese Localization (VRCLens Custom)")]
    public sealed class VRCLensJapaneseLocalization : VRCLensLocalizationMarker
    {
    }
}
#endif
