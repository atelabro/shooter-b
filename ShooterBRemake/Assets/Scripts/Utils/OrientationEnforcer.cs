using UnityEngine;

namespace ShooterB
{
    public class OrientationEnforcer : MonoBehaviour
    {
        [SerializeField] private bool allowLandscapeLeft = true;
        [SerializeField] private bool allowLandscapeRight = true;

        private void Awake()
        {
            ApplyOrientationPolicy();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                ApplyOrientationPolicy();
        }

        private void ApplyOrientationPolicy()
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = allowLandscapeLeft;
            Screen.autorotateToLandscapeRight = allowLandscapeRight;

            if (allowLandscapeLeft)
                Screen.orientation = ScreenOrientation.LandscapeLeft;
            else if (allowLandscapeRight)
                Screen.orientation = ScreenOrientation.LandscapeRight;

            Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }
}
