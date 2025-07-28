using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace JWK.Scripts.FireManager
{
    public class WildfireManager : MonoBehaviour
    {
        #region 변수 선언

        public static WildfireManager Instance { get; private set; }
        public bool isFireActive => _hasFireBeenGenerated;
        public List<GameObject> GetActiveFires() => _activeFires;
        
        [Header("화재 설정")] [SerializeField] private Terrain targetTerrain;
        [SerializeField] private GameObject fireParticlePrefab;
        [SerializeField] private int poolSize = 50; // 풀 크기를 늘려 예외 상황 방지
        [SerializeField] private int numberOfFiresToSpawn = 10;

        [Header("화재 발생 영역 설정")] [SerializeField]
        private Vector3 spawnAreaCenter = new Vector3(500, 0, 500);

        [SerializeField] private Vector2 spawnAreaSize = new Vector2(5, 5);
        [SerializeField] private float fireClusterRadius = 2.0f;

        [Header("화재 발생 제어")] [SerializeField] private bool generateFireNow = false;

        public Func<bool> ExtinguishConditionCheck;

        // --- 내부 변수 ---
        // 최적화: List 대신 Queue를 사용하여 O(1) 시간 복잡도로 오브젝트를 가져옴
        private Queue<GameObject> _fireParticlePool;
        private readonly List<GameObject> _activeFires = new List<GameObject>();
        private bool _hasFireBeenGenerated = false;

        private int _fireNamingCounter = 1;

        #endregion

        #region 초기화

        private void Awake()
        {
            if (Instance && Instance != this)
                Destroy(gameObject);

            else
                Instance = this;
        }

        private void Start()
        {
            InitializeObjectPool();
        }

        #endregion

        #region 오브젝트 풀링

        private void InitializeObjectPool()
        {
            _fireParticlePool = new Queue<GameObject>(poolSize);

            if (!fireParticlePrefab)
            {
                Debug.LogError("Fire Particle Prefab이 할당되지 않았습니다!!!");
                enabled = false;
                return;
            }

            for (int i = 0; i < poolSize; i++)
            {
                GameObject fireInstance = Instantiate(fireParticlePrefab, Vector3.zero, Quaternion.identity, this.transform);
                fireInstance.SetActive(false);
                _fireParticlePool.Enqueue(fireInstance); // 풀에 추가
            }
        }

        #endregion

        private void Update()
        {
            if (generateFireNow && !_hasFireBeenGenerated)
            {
                GenerateFires();
                generateFireNow = false;
            }

            if (_hasFireBeenGenerated)
            {
                bool allFiresExtinguished = true;

                foreach (var fire in _activeFires)
                {
                    if (fire && fire.activeInHierarchy)
                    {
                        allFiresExtinguished = false;
                        break;
                    }
                }

                if (allFiresExtinguished)
                {
                    Debug.Log("<color=cyan>모든 화재 진압 완료! 3초 후 연기 제거를 시작합니다.</color>");
                    TriggerSmokeCleanup();
                    _hasFireBeenGenerated = false; // 임무 완료 상태로 변경하여 중복 실행 방지
                }
            }
            if (ExtinguishConditionCheck != null && ExtinguishConditionCheck())
            {
                ExtinguishAllFires();
                ExtinguishConditionCheck = null;
            }
        }

        private void TriggerSmokeCleanup()
        {
            foreach (var smokeController in SmokeVFXController.ActiveSmokeEffects)
            {
                if(smokeController)
                    smokeController.StartDelayFadeOut(3.0f);
            }

            SmokeVFXController.ActiveSmokeEffects.Clear();
        }

        public void GenerateFires()
        {
            if (_hasFireBeenGenerated)
            {
                Debug.LogWarning("화재가 이미 발생했습니다. 기존 화재를 먼저 진압하세요.");
                return;
            }

            if (!targetTerrain)
            {
                Debug.LogError("Terrain이 할당되지 않았습니다.");
                return;
            }

            if (SmokeVFXController.ActiveSmokeEffects.Count > 0)
            {
                Debug.Log("이전 임무의 연기 파티클이 남아있어 정리합니다.");
                foreach (var smoke in SmokeVFXController.ActiveSmokeEffects)
                {
                    if(smoke)
                        Destroy(smoke.gameObject);
                }
                SmokeVFXController.ActiveSmokeEffects.Clear();
            }
            
            _activeFires.Clear();
            
            Vector3 areaStartCorner = spawnAreaCenter - new Vector3(spawnAreaSize.x / 2, 0, spawnAreaSize.y / 2);
            float randomEpicenterX = Random.Range(0, spawnAreaSize.x);
            float randomEpicenterZ = Random.Range(0, spawnAreaSize.y);
            Vector3 fireEpicenter = areaStartCorner + new Vector3(randomEpicenterX, 0, randomEpicenterZ);

            int spawnCount = Mathf.Min(numberOfFiresToSpawn, _fireParticlePool.Count);

            if (spawnCount < numberOfFiresToSpawn)
                Debug.LogWarning($"풀이 부족하여 {spawnCount}개의 화재만 생성합니다.");

            for (int i = 0; i < spawnCount; i++)
            {
                GameObject fireInstance = GetPooledFireObject();
                if (!fireInstance) break; // 풀이 비었으면 중단

                Vector2 randomCirclePoint = Random.insideUnitCircle * fireClusterRadius;
                Vector3 spawnPos = fireEpicenter + new Vector3(randomCirclePoint.x, 0, randomCirclePoint.y);

                float terrainHeight = targetTerrain.SampleHeight(spawnPos);
                Vector3 finalSpawnPosition = new Vector3(spawnPos.x, terrainHeight, spawnPos.z);

                fireInstance.transform.SetPositionAndRotation(finalSpawnPosition, Quaternion.identity);

                fireInstance.name = $"Fire{_fireNamingCounter++}";
                
                fireInstance.SetActive(true);

                _activeFires.Add(fireInstance);
            }

            _hasFireBeenGenerated = true;
        }

        public void ExtinguishAllFires()
        {
            if (_activeFires.Count == 0) return;

            Debug.Log("모든 화재를 진압합니다...");
            foreach (GameObject fire in _activeFires)
            {
                fire.SetActive(false);
                _fireParticlePool.Enqueue(fire); // 비활성화 후 풀에 다시 넣어줌
            }

            _activeFires.Clear();
            _hasFireBeenGenerated = false;
            
            _fireNamingCounter = 1;
        }

        /// <summary>
        /// 오브젝트 풀에서 비활성화 상태인 화재 오브젝트를 찾아 반환
        /// </summary>
        private GameObject GetPooledFireObject()
        {
            if (_fireParticlePool.Count > 0)
                return _fireParticlePool.Dequeue(); // O(1) 작업

            Debug.LogWarning("오브젝트 풀이 비어있습니다! 모든 파티클이 사용 중입니다.");
            return null;
        }
    }
}