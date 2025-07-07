using UnityEngine;
using System;
using System.Collections.Generic;

// --- 메인 스레드 디스패처 클래스 ---
// WebSocket과 같은 별도의 스레드에서 Unity API를 직접 호출하면 오류가 발생함.
// 이 클래스를 통해 실행해야 할 작업들을 메인 스레드의 Update 루프에서 처리함
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance = null;

    // 싱글턴 인스턴스에 접근하기 위한 프로퍼티
    public static UnityMainThreadDispatcher Instance()
    {
        if (!_instance)
        {
            _instance = FindObjectOfType<UnityMainThreadDispatcher>();
            if (!_instance)
            {
                GameObject go = new GameObject("MainThreadDispatcher_Instance");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go); // 씬이 바뀌어도 파괴되지 않도록 설정
            }
        }
        return _instance;
    }

    void Awake()
    {
        // 중복 생성 방지
        if (!_instance)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // 큐에 쌓인 작업들을 메인 스레드에서 순차적으로 실행
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    // 다른 스레드에서 실행할 작업을 큐에 추가하는 함수
    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}