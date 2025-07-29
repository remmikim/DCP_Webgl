using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// List에서 오브젝트를 쉽게 찾기 위해 사용

namespace JWK.Scripts.Test
{
    public class ExtinguishBomb : MonoBehaviour
    {
        [Header("폭탄 오브젝트 목록")]
        [Tooltip("분리할 모든 폭탄 게임 오브젝트들을 여기에 할당하세요.")]
        public List<GameObject> bombList; // Inspector에서 모든 폭탄 오브젝트를 할당

        #region
        // 지정된 이름의 폭탄을 찾아 분리(Joint 해제)합니다.
        // 이 함수는 DroneController 같은 다른 스크립트에서 호출됩니다.
        public void DetachBombByName(string bombName)
        {
            // bombList에서 이름이 일치하는 첫 번째 GameObject를 찾습니다.
            GameObject bombToDrop = bombList.FirstOrDefault(b => b && b.name == bombName);

            if (bombToDrop)
            {
                Debug.Log($"'{bombName}' 폭탄을 찾았습니다. 분리를 시도합니다.");
            
                // 해당 오브젝트에 붙어있는 FixedJoint 컴포넌트를 찾습니다.
                FixedJoint joint = bombToDrop.GetComponent<FixedJoint>();

                if (joint)
                {
                    // Joint를 파괴하여 드론과의 연결을 끊습니다.
                    // 이제 이 폭탄은 자신의 Rigidbody와 중력의 영향을 받게 됩니다.
                    Destroy(joint);
                    Debug.Log($"'{bombName}' 폭탄의 FixedJoint가 성공적으로 해제되었습니다.");
                }
            
                else
                    Debug.LogWarning($"'{bombName}' 폭탄에 FixedJoint 컴포넌트가 없습니다.");
            }
        
            else
                Debug.LogWarning($"리스트에서 '{bombName}' 이름의 폭탄을 찾을 수 없습니다.");
        }
        #endregion

        // --- 테스트용 예시 ---
        // 실제로는 DroneController의 PerformActionCoroutine에서 호출하게 됩니다.
        void Update()
        {
            /*
        #region 1, 2, 3, 4, 5, 6 을 눌러서 폭탄 드랍
        // '1' 키를 누르면 "Bomb_1-1"을 투하 시도
        if (Input.GetKeyDown(KeyCode.Alpha1))
            DetachBombByName("Bomb_1");
        
        // '2' 키를 누르면 "Bomb_1-2"를 투하 시도
        if (Input.GetKeyDown(KeyCode.Alpha2))
            DetachBombByName("Bomb_2");
        
        // '3' 키를 누르면 "Bomb_2-1"를 투하 시도
        if (Input.GetKeyDown(KeyCode.Alpha3))
            DetachBombByName("Bomb_3");
        
        // '4' 키를 누르면 "Bomb_2-2"를 투하 시도
        if (Input.GetKeyDown(KeyCode.Alpha4))
            DetachBombByName("Bomb_4");
        
        // '5' 키를 누르면 "Bomb_3-1"를 투하 시도
        if (Input.GetKeyDown(KeyCode.Alpha5))
            DetachBombByName("Bomb_5");
        
        // '6' 키를 누르면 "Bomb_3-2"를 투하 시도
        if (Input.GetKeyDown(KeyCode.Alpha6))
            DetachBombByName("Bomb_6");
        #endregion
        */
        }

        public void DestroyFIxedJoint()
        {
            for (var i = 1; i < 6; i++)
            {
                DetachBombByName($"Bomb_{i}");
            
            }
        }
    }
}