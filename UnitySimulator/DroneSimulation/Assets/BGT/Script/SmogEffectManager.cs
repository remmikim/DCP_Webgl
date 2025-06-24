using UnityEngine;

public class SmogEffectManager : MonoBehaviour
{
    public GameObject smog; // Unity Inspector에서 Smog 프리팹을 여기에 연결합니다.

    // SmogEffectManager의 인스턴스를 어디서든 쉽게 접근할 수 있도록 싱글톤 패턴 사용
    public static SmogEffectManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 씬이 전환되어도 파괴되지 않게 하려면 주석 해제
        }
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// 지정된 위치에 스모그 이펙트를 생성합니다.
    /// </summary>
    /// <param name="position">스모그가 생성될 월드 좌표</param>
    /// <param name="rotation">스모그가 생성될 때의 회전 값</param>
    public void CreateSmog(Vector3 position, Quaternion rotation)
    {
        if (smog != null)
        {
            Instantiate(smog, position, rotation);
            Debug.Log($"Smog created at: {position}");
        }
       
    }
}