using System;
using System.Collections;
using System.Collections.Generic;
using BlackDawn.DOTS;
using GameFrame.Fsm;
using GameFrame.Runtime;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using GameFrame.BaseClass;

namespace BlackDawn
{

    public class GameManager : MonoBehaviour, IECSSyncedMono, IOneStepFun
    {
        private static GameManager _testManager;
        public static GameManager instance { get { return _testManager; } }

        public bool Enable { get; set; }
        public bool Done { get; set; }

        public SubScene mySubScene; // SubScene 进来
        public int testCount = 100;
        public bool handleLoadScenes;
        //怪物预制体
        [Header("MonoPrefabs")] public GameObject[] gameObjects;
        public Transform parent;
        //集合方法单例,场景加载完毕之后获取
        private SpawnCollection _spawnCollection;


        void Awake()
        {


            // 初始化全局查表，用于构建伤害飘字的查询表
            DamageTextUVLookup.InitializeAtlas();
            _testManager = this;
            // FsmManager.Shutdown();
            //异步加载初始化
            if (handleLoadScenes)
                OnSceneEcsReady();
            Physics.simulationMode = SimulationMode.Script;

        }
        private GameManager() { }
        void Start()
        {
           

        }

        // Update is called once per frame
        void Update()
        {
            if (!Enable) return;
            //初始化英雄
            InsGameObj(0);
            TsetInsMonster();

            //更新状态机
            FsmManager.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }


        void FixedUpdate()
        {
            if (!Enable) return;
            //更新传统物理驱动，这里添加ECS 之后，默认自动关闭
            Physics.Simulate(Time.fixedDeltaTime);

        }

        void OnDisable()
        {
            FsmManager.Shutdown();
        }

        public void OnSceneEcsReady()
        {
            if (mySubScene.SceneGUID == default)
                return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogError("World 不可用！");
                return;
            }

            Debug.Log("开始加载场景");
            var loadParams = new SceneSystem.LoadParameters
            {
                Flags = SceneLoadFlags.BlockOnStreamIn
            };

           var  sceneRef = SceneSystem.LoadSceneAsync(world.Unmanaged, mySubScene.SceneGUID, loadParams);

            //使用协程管理器,启动协程或通过 System 检查加载状态
            var scenLoadCoroutine = CoroutineController.instance.StartRoutine(
                        WaitForSceneLoad(sceneRef),
                        "ECSSenceLoad",
                        () =>
                        {
                            Debug.Log("场景加载完成");
                            Enable = true;
                            //开启GameController ECS System
                            var gameControllerSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<GameControllerSystemBase>();
                            gameControllerSystem.Enabled = true;


                            //获取entity 生成方法集合
                            _spawnCollection = SpawnCollection.GetInstance();
                        });

        }



       void InsGameObj(int order)
        {
            if (!Done)
            {

                Debug.Log("初始化英雄");
                var hero = GameObject.Instantiate(gameObjects[order].gameObject,parent);

                Done = true;           
            }
        }
       
        void TsetInsMonster()
        {
 
            //生成恶犬
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
         

                var entity = _spawnCollection.InstantiateMonster(MonsterName.Albono, _spawnCollection.prefabs.Monster_Albono);               

                //var entity = _spawnCollection.InstantiateMonster(MonsterName.Albono, _spawnCollection.prefabs.entity);
            }
            //生成恶龙升空者
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                var entity = _spawnCollection.InstantiateMonster(MonsterName.AlbonoUpper, _spawnCollection.prefabs.Monster_AlbonoUpper);

            }


        }




        #region 协程模块
        /// <summary>
        /// ECS 和 Mono混合开发场景流式加载 注意事项，需等待场景加载完成之后，再进行相关处理
        /// </summary>
        /// <param name="sceneRef"></param>
        /// <returns></returns>
        private IEnumerator WaitForSceneLoad(Entity sceneRef)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            while (!SceneSystem.IsSceneLoaded(world.Unmanaged, sceneRef))
            {
                yield return null;
            }
        }

        #endregion


    }
}