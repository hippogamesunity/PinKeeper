using UnityEngine;

namespace Assets.Scripts
{
    public class ScreenManager : MonoBehaviour
    {
        public UIRoot UIRoot;
        public float TargetHeight;

        public static ScreenManager Instance;
        
        public void Awake()
        {
            const float designAspect = 1 / 2f;
            var ratio = Mathf.Max(1f, designAspect / Camera.main.aspect);

            UIRoot.manualHeight = (int) (TargetHeight * ratio);
            Instance = this;
        }

        public float ScreenWidth => TargetHeight * Camera.main.aspect;
    }
}