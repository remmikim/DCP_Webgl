using UnityEngine;

namespace JWK.Scripts
{
    /// <summary>
    /// 소화탄 충돌 순간을 클로즈업하는 서브 카메라입니다.
    /// 평소에는 비활성화 상태이며 DirectorSystem에 의해 제어됩니다.
    /// </summary>
    public class ImpactCameraController : MonoBehaviour
    {
        // 이 스크립트는 현재 특별한 로직이 필요 없지만,
        // 향후 흔들림 효과(Camera Shake) 등을 추가할 수 있도록 구조를 마련해 둡니다.
        void Start()
        {
            // 메인 카메라보다 위에 렌더링되도록 Depth를 설정합니다.
            GetComponent<Camera>().depth = 1;
        }
    }
}