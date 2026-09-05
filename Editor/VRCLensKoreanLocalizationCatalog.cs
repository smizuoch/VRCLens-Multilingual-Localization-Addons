#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace VRCLensCustom
{
    /// <summary>
    /// ko-KR display strings for the VRCLens 1.10.0 / Free Camera Add-ons v2.3.0
    /// localization pipeline.
    ///
    /// This class intentionally contains data and context helpers only. The shared menu localizer
    /// remains responsible for semantic VRCLens scoping, graph cloning, and source-asset safety.
    /// Camera terminology references Canon's Korean manuals; reviewed post-processing terms
    /// reference Adobe. Per-entry evidence and unresolved terms are recorded in
    /// Documentation/LEGACY_TERMINOLOGY.md; the whole catalog is not source-certified.
    ///
    /// Canon references:
    /// https://cam.start.canon/ko/C004/manual/html/UG-04_AF-Drive_0090.html
    /// https://cam.start.canon/ko/C004/manual/html/UG-02_ShootingMode_0070.html
    /// https://cam.start.canon/ko/C002/manual/html/UG-06_Shooting-1_0150.html
    /// https://cam.start.canon/ko/C002/manual/html/UG-11_Reference_0070.html
    /// https://cam.start.canon/ko/C001/manual/html/UG-07_Set-up_0140.html
    /// https://cam.start.canon/ko/C004/manual/html/UG-04_AF-Drive_0110.html
    /// https://cam.start.canon/ko/C002/manual/html/UG-06_Shooting-1_0090.html
    /// </summary>
    internal static class VRCLensKoreanLocalizationCatalog
    {
        internal const string LocaleCode = "ko-KR";
        internal const string NativeLanguageName = "한국어";

        private const string FeatureToggle = "VRCLFeatureToggle";

        private static readonly Regex PinName = new Regex(
            @"^Pin\s+(\d+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex SecondsName = new Regex(
            @"^(\d+)s$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex MinutesName = new Regex(
            @"^(\d+)\s+min$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Creates a mutable catalog keyed by the exact English labels shipped by the supported
        /// VRCLens and Free Camera Add-ons versions. Callers may safely retain or extend the result.
        /// </summary>
        internal static Dictionary<string, string> CreateNames()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // VRCLens 1.10.0
                { "Enable", "활성화" },
                { "Advanced", "상세 설정" },
                { "Zoom", "줌" },
                { "Drone", "드론" },
                { "Settings", "설정" },
                { "Focus", "초점" },
                { "Reset", "초기화" },
                { "Exposure", "노출" },
                { "Filters", "필터" },
                { "Stabilize", "손떨림 보정" },
                { "Aperture", "조리개" },
                { "Portrait", "세로 방향" },
                { "Display", "표시" },
                { "Normal", "표준" },
                { "Depth Cool", "심도·쿨" },
                { "Depth Cute", "심도·큐트" },
                { "Spotlight", "스포트라이트" },
                { "Green Back", "그린백" },
                { "Blue Back", "블루백" },
                { "RIGHT", "오른쪽" },
                { "UP", "위" },
                { "DOWN", "아래" },
                { "LEFT", "왼쪽" },
                { "Show/Hide", "표시 전환" },
                { "Color", "색상" },
                { "Hexagon", "육각형" },
                { "Octagon", "팔각형" },
                { "Square", "사각형" },
                { "Look At Face", "얼굴로 향하기" },
                { "Look Up", "위로 향하기" },
                { "Look Down", "아래로 향하기" },
                { "Look At Feet", "발 쪽으로 향하기" },
                { "Track Self", "자신 추적" },
                { "Track Self...", "자신 추적..." },
                { "Infinity", "무한대" },
                { "Auto Focus", "AF" },
                { "Manual Focus", "MF" },
                { "Macro", "매크로" },
                { "Picture Style", "픽쳐스타일" },
                { "Quick Selfie", "셀카" },
                { "Extra...", "기타..." },
                { "Movie Mode", "동영상 모드" },
                { "Avatar-Drop", "아바타 고정" },
                { "3D Mode", "3D 모드" },
                { "Mount Swap", "마운트 전환" },
                { "Hide Lens", "렌즈 숨기기" },
                { "Increase", "조리개 닫기" },
                { "Decrease", "조리개 열기" },
                { "Sensor Type", "센서 크기" },
                { "DoF Mode", "피사계 심도" },
                { "Av Dial", "조리개 값" },
                { "Bokeh Shape", "보케 모양" },
                { "V. Horizon", "전자 수평계" },
                { "Grid", "격자" },
                { "Monitor", "모니터" },
                { "Move HUD", "HUD 위치" },
                { "Focus Peaking", "MF 피킹" },
                { "Hand-Rotate", "손으로 회전" },
                { "Change Angle", "방향 변경" },
                { "Tracking", "추적" },
                { "Move Camera", "카메라 이동" },
                { "Drone Speed", "드론 속도" },
                { "Move Pivot", "피벗 이동" },
                { "Track Pivot", "피벗 추적" },
                { "Smoothing", "부드러운 추적" },
                { "Drop Pivot", "피벗 설치" },
                { "Vertical Lock", "수직 고정" },
                { "EV Dial", "노출 보정" },
                { "Auto Exposure", "자동 노출" },
                { "White Bal.", "화이트 밸런스" },
                { "Avatar AF", "아바타 AF" },
                { "Move Focus", "초점 위치" },

                // Pro Autofocus. Canon retains AF/MF and One-Shot in its Korean UI.
                { "Pro Autofocus", "고급 AF" },
                { "Operation", "AF 동작" },
                { "AF Area", "AF 영역" },
                { "Response", "추적 감도" },
                { "Drive Speed", "AF 속도" },
                { "AF-ON", "AF 작동" },
                { "AF Lock", "AF 잠금" },
                { "Focus Guide", "초점 가이드" },
                { "One-Shot AF", "One-Shot AF" },
                { "Servo AF", "서보 AF" },
                { "Spot", "스팟" },
                { "Zone", "존" },
                { "Avatar Priority", "아바타 우선" },
                { "Locked-on", "피사체에 고정" },
                { "Standard", "표준" },
                { "Responsive", "즉시 반응" },

                // Free Camera Add-ons v2.3.0
                { "Custom", "추가 기능" },
                { "Far Clip Plane", "원거리 클리핑 평면" },
                { "Smooth DoF Edges", "피사계 심도 경계 완화" },
                { "Softness", "부드러움" },
                { "Smooth Rotate", "부드러운 회전" },
                { "Anamorphic Bokeh", "아나모픽 보케" },
                { "Chromatic Aberration", "색 수차" },
                { "Enabled", "활성화" },
                { "Transverse CA", "배율 색수차" },
                { "Axial CA", "축상 색수차" },
                { "Axial Focus-Aware", "초점 연동" },
                { "Magenta-Green", "마젠타/녹색" },
                { "Color Grading", "컬러 그레이딩" },
                { "Saturation", "채도" },
                { "Vibrance", "활기" },
                { "Contrast", "콘트라스트" },
                { "Shadows", "그림자" },
                { "Midtones", "중간톤" },
                { "Highlights", "하이라이트" },
                { "Brightness", "밝기" },
                { "Temperature", "색온도" },
                { "Red", "빨강" },
                { "Green", "초록" },
                { "Blue", "파랑" },
                { "Depth Fog", "대기 안개" },
                { "Density", "밀도" },
                { "Start Distance", "시작 거리" },
                { "Color R", "빨강" },
                { "Color G", "초록" },
                { "Color B", "파랑" },
                { "Film Grain", "필름 그레인" },
                { "Intensity", "강도" },
                { "Size", "크기" },
                { "Speed", "속도" },
                { "Fisheye Lens", "어안 렌즈" },
                { "Strength", "강도" },
                { "Roundness", "원형률" },
                { "Lens Size", "렌즈 크기" },
                { "Edge Softness", "가장자리 부드러움" },
                { "Reverse Fisheye", "역어안" },
                { "Center", "중심" },
                { "X", "가로 위치" },
                { "Y", "세로 위치" },
                { "Letterbox", "레터박스" },
                { "2.39:1 Cinemascope", "2.39:1 시네마스코프" },
                { "2.35:1 Anamorphic", "2.35:1 아나모픽" },
                { "2.00:1 Univisium", "2.00:1 유니비지엄" },
                { "1.85:1 Theatrical", "1.85:1 극장용" },
                { "16:9 Widescreen", "16:9 와이드스크린" },
                { "3:2 DSLR", "3:2 DSLR" },
                { "4:3 Classic", "4:3 클래식" },
                { "1:1 Square", "1:1 정사각형" },
                { "4:5 Portrait", "4:5 세로" },
                { "9:16 Vertical", "9:16 세로형" },
                { "Pixelation", "픽셀화" },
                { "Block Size", "블록 크기" },
                { "Dither", "디더링" },
                { "Posterize", "포스터화" },
                { "Aspect Ratio", "종횡비" },
                { "Tilt-Shift", "틸트-시프트" },
                { "Depth Mode", "거리 모드" },
                { "Blur", "흐림" },
                { "Position", "위치" },
                { "Width", "폭" },
                { "Angle", "각도" },
                { "Vignette", "비네팅" },
                { "Radius", "반경" },
                { "Zoom Blur", "줌 블러" },
                { "Center X", "중심·가로" },
                { "Center Y", "중심·세로" },
                { "Focus Zone", "초점 영역" },
                { "Spread", "퍼짐" },
                { "Focus Distance", "촬영 거리" },
                { "Manual Focus Assist", "MF 보조" },
                { "Zone Size", "영역 크기" },
                { "Zone Softness", "영역 부드러움" },
                { "Edge Feather", "가장자리 페더" },
                { "Peaking", "피킹" },
                { "Max Blur Size", "최대 흐림 크기" },
                { "Player Visibility", "플레이어 표시" },
                { "Hide Remote Players", "다른 플레이어 숨기기" },
                { "Hide Self", "자신 숨기기" },
                { "Avatar Offset", "아바타 오프셋" },
                { "Rotate With Avatar", "아바타 회전 연동" },
                { "Drop (Reset to Hand)", "손 위치로 복귀" },
                { "Move Drone Vertical", "드론 수직 이동" },
                { "Camera Pins & Dolly", "카메라 핀 및 돌리" },
                { "Teleport", "핀 이동" },
                { "Dolly", "돌리" },
                { "Reset to Hand", "손 위치로 복귀" },
                { "Show Pins", "핀 표시" },
                { "Show Path", "경로 표시" },
                { "Show Numbers", "번호 표시" },
                { "Play", "재생" },
                { "Stop", "정지" },
                { "Playback", "재생 위치" },
                { "Move", "수동 이동" },
                { "Go To Pin", "핀으로 이동" },
                { "Duration", "소요 시간" },
                { "Loop", "반복" },
                { "Delay", "시작 대기" },
                { "Path", "경로" },
                { "Hand Rotate (Pins)", "손으로 회전(핀)" },
                { "Hand Offset", "손 위치 연동" },
                { "Drop Next", "다음 핀 설치" },
                { "Clear All (Hold)", "모두 해제(길게 누르기)" },
                { "Full", "모든 방향" },
                { "Vertical", "수직만" },
                { "Teleport Next", "다음 핀으로" },
                { "Reverse", "왕복" },
                { "Linear", "직선" },
                { "Smooth", "전체 지점 보간" },
                { "Fitted", "근사 곡선" },
                { "Preset Saver", "프리셋 저장" },
                { "Save", "저장" },
                { "Load", "불러오기" },
                { "Load Defaults", "기본값 불러오기" },
                { "Favorites", "즐겨찾기" },
            };
        }

        internal static Dictionary<string, string> CreateDirectionLabels()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "TOP", "위" },
                { "UP", "위" },
                { "RIGHT", "오른쪽" },
                { "BOTTOM", "아래" },
                { "DOWN", "아래" },
                { "LEFT", "왼쪽" },
                { "FORWARD", "앞" },
                { "BACK", "뒤" },
            };
        }

        /// <summary>
        /// Resolves labels whose meaning depends on parameter/type/value rather than English text
        /// alone. The booleans must be supplied only after the shared localizer has semantically
        /// established ownership of the submenu/generated page.
        /// </summary>
        internal static bool TryGetContextualName(
            string sourceName,
            string parameterName,
            VRCExpressionsMenu.Control.ControlType controlType,
            float value,
            bool submenuContainsVrclensParameters,
            bool isGeneratedNextWithinVrclens,
            out string korean)
        {
            korean = null;
            sourceName = sourceName ?? "";
            parameterName = parameterName ?? "";

            if (sourceName == "Drop")
            {
                if (controlType == VRCExpressionsMenu.Control.ControlType.SubMenu
                    && submenuContainsVrclensParameters)
                {
                    korean = "핀 설치";
                    return true;
                }
                if (parameterName == FeatureToggle && Approximately(value, 251))
                {
                    korean = "월드 고정";
                    return true;
                }
                return false;
            }

            if (parameterName == FeatureToggle)
            {
                if (sourceName == "Zoom In" && Approximately(value, 107))
                {
                    korean = "망원 측 속도 4";
                    return true;
                }
                if (sourceName == "Zoom Out" && Approximately(value, 103))
                {
                    korean = "광각 측 속도 4";
                    return true;
                }
            }

            if (sourceName == "Next" && isGeneratedNextWithinVrclens)
            {
                korean = "다음";
                return true;
            }

            Match match = PinName.Match(sourceName);
            if (match.Success && IsCameraPinParameter(parameterName))
            {
                korean = "핀 " + match.Groups[1].Value;
                return true;
            }

            match = SecondsName.Match(sourceName);
            if (match.Success
                && (parameterName == "VRCL_Custom/DollyDelay"
                    || parameterName == "VRCL_Custom/DollyDuration"))
            {
                korean = match.Groups[1].Value + "초";
                return true;
            }

            match = MinutesName.Match(sourceName);
            if (match.Success && parameterName == "VRCL_Custom/DollyDuration")
            {
                korean = match.Groups[1].Value + "분";
                return true;
            }

            if (sourceName == "Softness"
                && parameterName == "VRCL_Custom/VignetteSoftness")
            {
                korean = "가장자리 부드러움";
                return true;
            }
            if (sourceName == "X" && parameterName == "VRCL_Custom/FisheyeLensCenterX")
            {
                korean = "가로 위치";
                return true;
            }
            if (sourceName == "Y" && parameterName == "VRCL_Custom/FisheyeLensCenterY")
            {
                korean = "세로 위치";
                return true;
            }
            return false;
        }

        /// <summary>
        /// Names the 28 icon-only functional controls shipped by VRCLens 1.10.0. Empty controls
        /// not recognized here are the five intentional layout spacers and must stay empty.
        /// </summary>
        internal static bool TryGetFunctionalBlankName(
            string parameterName,
            VRCExpressionsMenu.Control.ControlType controlType,
            float value,
            out string korean)
        {
            korean = null;
            int integralValue = (int)Math.Round(value);
            if (!Approximately(value, integralValue)) return false;

            if (parameterName == FeatureToggle
                && controlType == VRCExpressionsMenu.Control.ControlType.Button)
            {
                switch (integralValue)
                {
                    case 129: korean = "조금 위 보기"; return true;
                    case 130: korean = "조금 아래 보기"; return true;
                    case 121: korean = "무한대 쪽 중간"; return true;
                    case 120: korean = "무한대 쪽 작게"; return true;
                    case 118: korean = "가까운 범위 쪽 작게"; return true;
                    case 117: korean = "가까운 범위 쪽 중간"; return true;
                    case 106: korean = "망원 측 속도 3"; return true;
                    case 105: korean = "망원 측 속도 2"; return true;
                    case 104: korean = "망원 측 속도 1"; return true;
                    case 100: korean = "광각 측 속도 1"; return true;
                    case 101: korean = "광각 측 속도 2"; return true;
                    case 102: korean = "광각 측 속도 3"; return true;
                    case 110: korean = "노출 보정 +1/3스톱"; return true;
                    case 108: korean = "노출 보정 -1/3스톱"; return true;
                    case 115: korean = "WB 보정: 앰버 방향"; return true;
                    case 114: korean = "WB 보정: 초기화"; return true;
                    case 113: korean = "WB 보정: 청색 방향"; return true;
                }
            }

            if (parameterName == "VRCLS_SensorSize"
                && controlType == VRCExpressionsMenu.Control.ControlType.Toggle)
            {
                switch (integralValue)
                {
                    case 0: korean = "35mm 풀프레임"; return true;
                    case 1: korean = "APS-H"; return true;
                    case 2: korean = "APS-C(1.5배)"; return true;
                    case 3: korean = "APS-C(1.6배)"; return true;
                    case 4: korean = "마이크로 포서드"; return true;
                    case 5: korean = "1.0형"; return true;
                }
            }

            if (parameterName == "VRCLS_TonemapFilter"
                && controlType == VRCExpressionsMenu.Control.ControlType.Toggle)
            {
                switch (integralValue)
                {
                    case 0: korean = "해제"; return true;
                    case 1: korean = "필믹 1"; return true;
                    case 2: korean = "필믹 2"; return true;
                    case 3: korean = "HLG"; return true;
                    case 4: korean = "뉴트럴"; return true;
                }
            }

            return false;
        }

        internal static bool TryGetImpliedPivotDirection(
            float featureValue,
            bool hasFaceBlendHorizontal,
            bool hasFaceBlendVertical,
            int labelCount,
            int labelIndex,
            out string korean)
        {
            korean = null;
            if (!Approximately(featureValue, 214)
                || !hasFaceBlendHorizontal
                || !hasFaceBlendVertical
                || labelCount != 4
                || labelIndex < 0
                || labelIndex >= 4)
                return false;

            korean = new[] { "앞", "오른쪽", "뒤", "왼쪽" }[labelIndex];
            return true;
        }

        internal static bool IsIntentionalBlankDroneDirection(
            float featureValue,
            bool hasDroneVerticalSubParameter,
            int labelCount,
            int labelIndex)
        {
            return Approximately(featureValue, 212)
                   && hasDroneVerticalSubParameter
                   && labelCount == 4
                   && (labelIndex == 1 || labelIndex == 3);
        }

        private static bool IsCameraPinParameter(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName)) return false;
            return parameterName == "VRCL_Custom/DollyToPin"
                   || parameterName == "VRCL_Custom/TelePin"
                   || parameterName.StartsWith("VRCL_Custom/DropPin", StringComparison.Ordinal);
        }

        private static bool Approximately(float left, float right)
        {
            return Math.Abs(left - right) < 0.001f;
        }
    }
}
#endif
