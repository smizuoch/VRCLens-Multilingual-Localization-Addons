using UnityEngine;
using VRC.SDKBase;

namespace VRCLensCustom
{
    /// <summary>
    /// Common base for the settings-free localization installer markers.
    /// Removing the installer prefab remains the only on/off switch; build hooks intentionally find
    /// markers on inactive objects as well.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class VRCLensLocalizationMarker : MonoBehaviour, IEditorOnly
    {
    }
}
