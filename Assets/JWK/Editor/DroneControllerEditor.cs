// C:\DroneControlProject\UnityDroneSimulator\Assets\Editor\DroneControllerEditor.cs

using JWK.Scripts;
using UnityEngine;
using UnityEditor;

/// <summary>
/// DroneController 스크립트의 Inspector UI를 커스터마이징합니다.
/// 이 에디터 스크립트는 Unity 에디터에서만 동작합니다.
/// </summary>
[CustomEditor(typeof(DroneController))]
public class DroneControllerEditor : Editor
{
    // --- Private Fields ---
    private DroneController _droneController;
    private SerializedProperty _testDispatchTargetProperty;
    
    // 최적화: EditorStyles를 반복해서 가져오지 않도록 static으로 캐싱합니다.
    private static GUIStyle _boldLabelStyle;

    /// <summary>
    /// 에디터가 활성화될 때 호출됩니다.
    /// </summary>
    private void OnEnable()
    {
        // 최적화: target을 매번 캐스팅하지 않고, OnEnable에서 한 번만 참조를 가져옵니다.
        _droneController = (DroneController)target;

        // DroneController에 테스트용 목표물이 다시 필요하므로, SerializedProperty를 사용해 연결합니다.
        // 이 방식은 Undo/Redo 기능을 지원하며 더 안정적입니다.
        // DroneController.cs에 'testDispatchTarget' 필드를 다시 추가해야 합니다.
        // public Transform testDispatchTarget;
        _testDispatchTargetProperty = serializedObject.FindProperty("testDispatchTarget");
    }

    /// <summary>
    /// Inspector GUI를 그리는 함수입니다. 매 프레임 호출될 수 있습니다.
    /// </summary>
    public override void OnInspectorGUI()
    {
        // 기본 Inspector 필드들을 먼저 그립니다.
        DrawDefaultInspector();

        // 스타일이 초기화되지 않았다면 설정합니다.
        if (_boldLabelStyle == null)
        {
            _boldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
        }

        // 변경 사항을 감지하기 위해 시작점을 표시합니다.
        serializedObject.Update();

        // 여백과 함께 커스텀 UI 섹션을 시작합니다.
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("커스텀 에디터 기능", _boldLabelStyle);
        
        // 커스텀 UI를 보기 좋게 그룹화합니다.
        EditorGUILayout.BeginVertical("box");

        // DroneController에 있는 테스트용 목표물 필드를 그립니다.
        // 만약 DroneController.cs에 testDispatchTarget 필드가 없다면, 이 부분은 주석 처리해야 합니다.
        if (_testDispatchTargetProperty != null)
        {
            EditorGUILayout.PropertyField(_testDispatchTargetProperty, new GUIContent("테스트 임무 타겟"));
        }
        else
        {
            EditorGUILayout.HelpBox("DroneController.cs에 'public Transform testDispatchTarget;' 필드가 필요합니다.", MessageType.Warning);
        }

        // 버튼 클릭 비활성화/활성화 로직
        // Play 모드가 아니거나, 테스트 타겟이 할당되지 않았다면 버튼을 비활성화합니다.
        EditorGUI.BeginDisabledGroup(!Application.isPlaying || !_droneController.testDispatchTarget);
        
        if (GUILayout.Button("Inspector에서 테스트 임무 시작", GUILayout.Height(30)))
        {
            // DroneController 스크립트의 public 함수를 호출합니다.
            // 이 함수는 DroneController.cs 내부에 다시 정의되어야 합니다.
            _droneController.DispatchMissionFromInspector();
        }
        
        EditorGUI.EndDisabledGroup();

        // 버튼 비활성화 이유를 설명하는 도움말 박스
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("임무 시작은 Play 모드에서만 가능합니다.", MessageType.Info);
        }
        else if (!_droneController.testDispatchTarget)
        {
            EditorGUILayout.HelpBox("테스트 임무 타겟을 먼저 할당해주세요.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();

        // 변경된 사항이 있다면 적용합니다. (Undo/Redo 지원)
        serializedObject.ApplyModifiedProperties();
    }
}