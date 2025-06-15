using GameFrame.EventBus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace BlackDawn
{
    public class MonoEnvetBus : MonoBehaviour
    {
        private EventBus _eventBus;

        private void Awake()
        { 
            //获取事件总线管理器
            _eventBus = EventBusManager.GetInstance().eventBus;
        }
        void Start()
        {

            StartCoroutine("TsetEvent");

        }

        // Update is called once per frame
        void Update()
        {

        }

        IEnumerator TsetEvent()
        {

            yield return  new WaitForSeconds(10);

          _eventBus.Publish<PlayerTestEvent>(new PlayerTestEvent(new Vector3(0, 1, 0),"daobakaka"));
        
        }





   
    }

    
}