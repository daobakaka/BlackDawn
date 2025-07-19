using System.Collections;
using System.Collections.Generic;
using BlackDawn.DOTS;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
namespace BlackDawn
{   //这里必要可以转换完全entity 类型
    public class HeroBranchDeal : MonoBehaviour
    {
        //这里就可以调节相关的时间参数
        private float _survivalTime = 5;
        public float originalSurvivalTime = 5;
        public float spawnInterval = 1;
        public float spawnChance = 0;
        public int totalSpawn = 5;

        public bool enableSecondA;
        public bool enableSecondC;

        public GameObject heroBranchMono;
        private EntityManager _entityManager;

        private ScenePrefabsSingleton _scenePrefabs;

        private float _spawnTimer;
        void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            heroBranchMono = GameManager.instance.gameObjects[1];
            _scenePrefabs = _entityManager.CreateEntityQuery(typeof(ScenePrefabsSingleton)).GetSingleton<ScenePrefabsSingleton>();
            //这里可以增加时间参数
            _survivalTime = originalSurvivalTime;
        }

        // Update is called once per frame
        void Update()
        {
             _survivalTime -= Time.deltaTime;
            _spawnTimer += Time.deltaTime;

            // 到达生成间隔
            if (_spawnTimer >= spawnInterval)
            {
                _spawnTimer = 0f;
                if (Random.value < spawnChance && heroBranchMono != null&&totalSpawn>=0)
                {
                    totalSpawn -= 1;
                    // 随机半径1以内新位置
                    Vector3 offset = Random.insideUnitSphere * 10.0f;
                    offset.y = 0; // 只在XZ平面
                    var mono=Instantiate(heroBranchMono, transform.position + offset, Hero.instance.transform.rotation);
                    mono.TryGetComponent<HeroBranchDeal>(out var cop);
                    cop.enableSecondA = enableSecondA;
                    cop.enableSecondC = enableSecondC;
                    cop.originalSurvivalTime = originalSurvivalTime;
                    cop.spawnChance = spawnChance;
                    //这里还需添加entity 世界相关标签
                    var cloneEntiy = Entity.Null;
                    if (enableSecondA)
                    {
                        cloneEntiy = _entityManager.Instantiate(_scenePrefabs.HeroBrachWithCollider);
                        

                        var tras = _entityManager.GetComponentData<LocalTransform>(cloneEntiy);
                        tras.Position = transform.position + offset;
                        _entityManager.SetComponentData(cloneEntiy, tras);

                        _entityManager.AddBuffer<HitRecord>(cloneEntiy);
                        _entityManager.AddBuffer<HitElementResonanceRecord>(cloneEntiy);
                        _entityManager.AddComponentData(cloneEntiy, Hero.instance.skillsDamageCalPar);
                        _entityManager.AddComponentData(cloneEntiy, new HeroEntityBranchTag { });
                    }
                    else
                    {
                        cloneEntiy = _entityManager.Instantiate(_scenePrefabs.HeroBrach);
                        _entityManager.AddComponentData(cloneEntiy, Hero.instance.skillsDamageCalPar);
                        _entityManager.AddComponentData(cloneEntiy, new HeroEntityBranchTag { });

                        var tras = _entityManager.GetComponentData<LocalTransform>(cloneEntiy);
                        tras.Position = transform.position + offset;
                        _entityManager.SetComponentData(cloneEntiy, tras);
                    }
                    
                    if (enableSecondC)
                        _entityManager.AddComponentData(cloneEntiy, new SkillPhantomStepTag() { tagSurvivalTime = originalSurvivalTime, enableSecondC = true });
                    else
                        _entityManager.AddComponentData(cloneEntiy, new SkillPhantomStepTag { tagSurvivalTime = originalSurvivalTime });
                    
                }
            }
            //后期制作两种情况下的缓存池
            if (_survivalTime <= 0)
                Destroy(this.gameObject);
        }







    }
}
