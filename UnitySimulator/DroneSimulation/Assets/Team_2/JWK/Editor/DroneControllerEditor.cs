// C:\DroneControlProject\UnityDroneSimulator\Assets\Editor\DroneControllerEditor.cs

using UnityEngine;
using UnityEditor;

// DroneController 스크립트의 Inspector를 확장합니다.
[CustomEditor(typeof(DroneController))]
public class DroneControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 Inspector 필드들을 먼저 그립니다 (public 변수들).
        DrawDefaultInspector();

        // 타겟 스크립트(DroneController)의 인스턴스를 가져옵니다.
        DroneController droneController = (DroneController)target;

        // Inspector에 여백을 추가합니다.
        EditorGUILayout.Space(20);

        // 굵은 글씨로 라벨을 추가합니다.
        EditorGUILayout.LabelField("커스텀 에디터 기능", EditorStyles.boldLabel);
        
        // 버튼을 생성합니다. GUILayout.Button이 true를 반환하면 버튼이 클릭된 것입니다.
        if (GUILayout.Button("Inspector에서 테스트 임무 시작"))
        {
            // Application이 Play 모드일 때만 함수를 실행하도록 하여 에디터 오류를 방지합니다.
            if (Application.isPlaying)
            {
                // DroneController 스크립트의 public 함수를 호출합니다.
                droneController.DispatchMissionFromInspector();
            }
            else
            {
                Debug.LogWarning("임무 시작은 Play 모드에서만 가능합니다.");
            }
        }
    }
}