// C:\DroneControlProject\UnityDroneSimulator\Assets\Editor\DroneControllerEditor.cs

using JWK.Scripts;
using UnityEditor;
using UnityEngine;

namespace JWK.Editor
{
    /// <summary>
    /// DroneController 스크립트의 Inspector UI를 커스터마이징합니다.
    /// 이 에디터 스크립트는 Unity 에디터에서만 동작합니다.
    /// </summary>
    [CustomEditor(typeof(DroneController))]
    public class DroneControllerEditor : UnityEditor.Editor
    {
        // --- Private Fields ---
        private DroneController _droneController;
        private SerializedProperty _testDispatchTargetProperty;
    
        // EditorStyles를 반복해서 가져오지 않도록 static으로 캐싱
        private static GUIStyle _boldLabelStyle;

        /// <summary>
        /// 에디터가 활성화될 때 호출됩니다.
        /// </summary>
        private void OnEnable()
        {
            // target을 매번 캐스팅하지 않고, OnEnable에서 한 번만 참조를 가져옴.
            _droneController = (DroneController)target;

            // DroneController에 테스트용 목표물이 다시 필요하므로, SerializedProperty를 사용해 연결함.
            // 이 방식은 Undo/Redo 기능을 지원하며 더 안정적임.
            _testDispatchTargetProperty = serializedObject.FindProperty("testDispatchTarget");
        }

        /// <summary>
        /// Inspector GUI를 그리는 함수입니다. 매 프레임 호출될 수 있습니다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // 기본 Inspector 필드들을 먼저 그립니다.
            DrawDefaultInspector();

            // 스타일이 초기화되지 않았다면 설정
            if (_boldLabelStyle == null)
                _boldLabelStyle = new GUIStyle(EditorStyles.boldLabel);

            // 변경 사항을 감지하기 위해 시작점을 표시
            serializedObject.Update();

            // 여백과 함께 커스텀 UI 섹션을 시작
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("커스텀 에디터 기능", _boldLabelStyle);
        
            // 커스텀 UI를 보기 좋게 그룹화
            EditorGUILayout.BeginVertical("box");

            // testDispatchTarget 필드를 Inspector에 표시
            EditorGUILayout.PropertyField(_testDispatchTargetProperty, new GUIContent("수동 테스트 타겟"));

            // Play 모드가 아니거나, 테스트 타겟이 할당되지 않았다면 버튼을 비활성화합니다.
            EditorGUI.BeginDisabledGroup(!Application.isPlaying || _droneController.testDispatchTarget == null);
        
            if (GUILayout.Button("수동 타겟으로 임무 시작", GUILayout.Height(30)))
            {
                _droneController.DispatchMissionToTestTarget();
            }
        
            EditorGUI.EndDisabledGroup();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("임무 시작은 Play 모드에서만 가능합니다.", MessageType.Info);
            }
            else if (_droneController.testDispatchTarget == null)
            {
                EditorGUILayout.HelpBox("수동 테스트 타겟을 먼저 할당해주세요.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            // --- 랜덤 화재 테스트 섹션 ---
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical("box");

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        
            if (GUILayout.Button("랜덤 화재 지점으로 임무 시작", GUILayout.Height(30)))
            {
                _droneController.DispatchMissionToRandomFire();
            }
        
            EditorGUI.EndDisabledGroup();

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("버튼을 누르면 WildFireManager가 생성한 랜덤 화재 지점으로 출동합니다. (화재가 없으면 새로 생성)", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            // [수정] SerializedObject의 변경 사항을 적용합니다.
            serializedObject.ApplyModifiedProperties();
        }
    }
}