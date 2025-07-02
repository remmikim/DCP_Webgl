/*
--- 다른 스크립트에서의 사용 예시 (예: DroneController.cs) ---

// 1. DroneController가 특정 조건을 만족했을 때 (예: 임무 완료 후 스테이션 도착)
void OnLandingCompleteAtStation()
{
    // WildfireManager의 소화 조건 델리게이트에 true를 반환하는 함수(또는 람다식)를 할당합니다.
    // 이렇게 하면 다음 Update 프레임에서 WildfireManager가 불을 끄게 됩니다.
    if (WildfireManager.Instance != null)
    {
        Debug.Log("소화 조건 충족! WildfireManager에 진압 신호를 보냅니다.");
        WildfireManager.Instance.ExtinguishConditionCheck = () => true;
    }
}

// 2. 다른 스크립트에서 화재 발생을 직접 트리거하고 싶을 때
void StartSomeEvent()
{
    if (WildfireManager.Instance != null)
    {
        WildfireManager.Instance.GenerateFires();
    }
}
*/

using UnityEngine;
using System.Collections.Generic;
using System; // Action, Func 사용을 위해 추가

public class WildfireManager : MonoBehaviour
{
    // --- 싱글턴 인스턴스 ---
    // 다른 스크립트에서 WildfireManager.Instance 로 쉽게 접근할 수 있도록 합니다.
    public static WildfireManager Instance { get; private set; }


    [Header("화재 설정")]
    [Tooltip("화재 파티클을 생성할 Terrain Object")]
    public Terrain targetTerrain;
    [Tooltip("화재 효과로 Particle System Object")]
    public GameObject fireParticlePrefab;
    [Tooltip("미리 생성해 둘 화재 파티클의 최대 개수입니다 (오브젝트 풀 크기).")]
    public int poolSize = 20;
    [Tooltip("한 번에 생성할 화재의 개수입니다.")]
    public int numberOfFiresToSpawn = 10;
    
    [Header("화재 발생 영역 설정")]
    [Tooltip("화재가 발생할 영역의 중심 좌표(월드 좌표).")]
    public Vector3 spawnCenter = new Vector3(500, 0, 500);
    [Tooltip("화재가 발생할 영역의 가로, 세로 크기")]
    public Vector2 spawnAreaSize = new Vector2(1000, 1000);

    [Header("화재 발생 제어")]
    [Tooltip("에디터에서 이 값을 체크하면 화재가 즉시 발생 (테스트용)")]
    public bool generateFireNow = false;

    // --- 공개 델리게이트 및 이벤트 ---
    [Tooltip("다른 스크립트에서 이 델리게이트에 'true'를 반환하는 함수를 할당하면 화재가 진압됨")]
    public Func<bool> ExtinguishConditionCheck; // 화재 진압 조건을 체크할 델리게이트


    // --- 내부 변수 ---
    private List<GameObject> fireParticlePool; // 미리 생성된 파티클 오브젝트를 담아두는 풀(Pool)
    private List<GameObject> activeFires;      // 현재 활성화된(보여지는) 화재 오브젝트 목록
    private bool hasFireBeenGenerated = false;

    void Awake()
    {
        // 싱글턴 패턴 설정
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        
        else
            Instance = this;
    }

    void Start()
    {
        InitializeObjectPool(); // 게임 시작 시 오브젝트 풀 초기화
    }

    /// <summary>
    /// 지정된 크기(poolSize)만큼 화재 파티클 오브젝트를 미리 생성하여 풀에 넣어둠
    /// </summary>
    void InitializeObjectPool()
    {
        fireParticlePool = new List<GameObject>();
        activeFires = new List<GameObject>();

        if (fireParticlePrefab == null)
        {
            Debug.LogError("Fire Particle Prefab이 할당되지 않았습니다!");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject fireInstance = Instantiate(fireParticlePrefab, Vector3.zero, Quaternion.identity, this.transform);
            fireInstance.SetActive(false); // 비활성화 상태로 생성
            fireParticlePool.Add(fireInstance);
        }
    }


    void Update()
    {
        // --- 1. 화재 발생 조건 ---
        if (generateFireNow && !hasFireBeenGenerated)
        {
            GenerateFires();
            generateFireNow = false;
        }

        // --- 2. 화재 진압 조건 (델리게이트 호출) ---
        // ExtinguishConditionCheck 델리게이트에 함수가 할당되어 있고, 그 함수의 실행 결과가 true이면 화재를 진압합니다.
        if (ExtinguishConditionCheck != null && ExtinguishConditionCheck())
        {
            Debug.Log("소화 조건을 만족하여 모든 불을 끕니다.");
            ExtinguishAllFires();
            ExtinguishConditionCheck = null; // 조건을 한 번 만족하면 델리게이트를 초기화하여 반복 실행 방지
        }
    }

    /// <summary>
    /// 지정된 영역에 화재를 생성하는 함수입니다. (오브젝트 풀링 사용)
    /// </summary>
    public void GenerateFires()
    {
        if (hasFireBeenGenerated)
        {
            Debug.LogWarning("화재가 이미 발생했습니다. 기존 화재를 먼저 진압하세요.");
            return;
        }
        if (!targetTerrain)
        {
            Debug.LogError("Terrain이 할당되지 않았습니다.");
            return;
        }
        if (numberOfFiresToSpawn > poolSize)
            Debug.LogWarning($"생성하려는 화재 개수({numberOfFiresToSpawn})가 풀 크기({poolSize})보다 큽니다. 풀 크기를 늘리는 것을 권장합니다.");

        Debug.Log($"지정된 영역에 {numberOfFiresToSpawn}개의 화재를 생성합니다...");

        Vector3 areaStartCorner = spawnCenter - new Vector3(spawnAreaSize.x / 2, 0, spawnAreaSize.y / 2);

        for (int i = 0; i < numberOfFiresToSpawn; i++)
        {
            GameObject fireInstance = GetPooledFireObject(); // 풀에서 비활성화된 오브젝트 가져오기
            if (!fireInstance)
            {
                Debug.LogWarning("사용 가능한 화재 파티클이 풀에 없습니다. 화재 생성을 중단합니다.");
                break; // 풀이 가득 찼으면 더 이상 생성하지 않음
            }

            // 랜덤 위치 계산
            float randomX = UnityEngine.Random.Range(0, spawnAreaSize.x);
            float randomZ = UnityEngine.Random.Range(0, spawnAreaSize.y);
            Vector3 spawnPos = areaStartCorner + new Vector3(randomX, 0, randomZ);
            float terrainHeight = targetTerrain.SampleHeight(spawnPos);
            Vector3 finalSpawnPosition = new Vector3(spawnPos.x, terrainHeight, spawnPos.z);

            // 가져온 오브젝트의 위치와 회전을 설정하고 활성화
            fireInstance.transform.position = finalSpawnPosition;
            fireInstance.transform.rotation = Quaternion.identity;
            fireInstance.SetActive(true);

            activeFires.Add(fireInstance); // 활성화된 목록에 추가
        }

        hasFireBeenGenerated = true;
    }

    /// <summary>
    /// 현재 발생한 모든 화재를 비활성화하여 풀로 되돌립니다.
    /// </summary>
    public void ExtinguishAllFires()
    {
        if (activeFires.Count == 0) return;

        Debug.Log("모든 화재를 진압합니다...");
        foreach (GameObject fire in activeFires)
            fire.SetActive(false); // Destroy 대신 비활성화하여 풀로 반납
        
        activeFires.Clear(); // 활성화 목록만 비움 (풀에는 오브젝트가 그대로 남아있음)
        hasFireBeenGenerated = false; // 다시 화재를 발생시킬 수 있도록 플래그 초기화
    }

    /// <summary>
    /// 오브젝트 풀에서 비활성화 상태인 화재 오브젝트를 찾아 반환합니다.
    /// </summary>
    /// <returns>사용 가능한 게임 오브젝트 또는 null</returns>
    private GameObject GetPooledFireObject()
    {
        // 풀에 있는 모든 오브젝트를 확인
        foreach (GameObject fire in fireParticlePool)
        {
            if (!fire.activeInHierarchy) // 비활성화된 오브젝트를 찾으면
                return fire; // 반환
        }
        
        // 만약 모든 오브젝트가 사용 중이라면 null을 반환 (또는 여기서 풀을 동적으로 확장할 수도 있음)
        Debug.LogWarning("오브젝트 풀이 가득 찼습니다! 모든 파티클이 사용 중입니다.");
        return null;
    }

    // 에디터의 Scene 뷰에서 화재 발생 영역을 시각적으로 보여주는 Gizmo
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.4f);
        Vector3 gizmoCenter = spawnCenter;
        
        if(targetTerrain != null)
            gizmoCenter.y = targetTerrain.SampleHeight(spawnCenter) + 1f;
        
        Gizmos.DrawCube(gizmoCenter, new Vector3(spawnAreaSize.x, 2, spawnAreaSize.y));
    }
}
