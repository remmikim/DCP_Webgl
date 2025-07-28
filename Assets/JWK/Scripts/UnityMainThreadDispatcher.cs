using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

// --- 메인 스레드 디스패처 클래스 ---
// WebSocket과 같은 별도의 스레드에서 Unity API를 직접 호출하면 오류가 발생함.
// 이 클래스를 통해 실행해야 할 작업들을 메인 스레드의 Update 루프에서 처리함
namespace JWK.Scripts
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        // 스레드 안전성을 위해 ConcurrentQueue 사용
        private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();
        private static UnityMainThreadDispatcher _instance = null;
        private static bool _isInitialized;

        // 현대적인 싱글턴 프로퍼티
        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (!_isInitialized)
                {
                    // 씬에서 인스턴스를 찾거나, 없으면 새로 생성
                    _instance = FindObjectOfType<UnityMainThreadDispatcher>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UnityMainThreadDispatcher");
                        _instance = go.AddComponent<UnityMainThreadDispatcher>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _isInitialized = true;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않도록 설정
        }

        private void OnDestroy()
        {
            _isInitialized = false;
        }

        private void Update()
        {
            // 큐에 쌓인 작업들을 메인 스레드에서 순차적으로 실행
            while (_executionQueue.TryDequeue(out var action))
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// 다른 스레드에서 실행할 작업을 큐에 추가하는 함수
        /// </summary>
        public void Enqueue(Action action)
        {
            _executionQueue.Enqueue(action);
        }
    }
}