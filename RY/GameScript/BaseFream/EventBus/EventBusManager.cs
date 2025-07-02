using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFrame.BaseClass;
using GameFrame.EventBus;

namespace BlackDawn
{/// <summary>
/// 事件总线管理器，内含事件方法
/// </summary>
    public class EventBusManager :Singleton<EventBusManager> 
    {
        private EventBusManager() { eventBus = EventBus.GetInstance(); }

        public EventBus eventBus;
        public void TestEvent(PlayerTestEvent testEvent)
        {


            DevDebug.Log("测试事件方法" + testEvent.Position + testEvent.text);
        
        }





    }

    #region 事件结构体
    public struct PlayerTestEvent
    {
        public Vector3 Position;
        public string text;
        public PlayerTestEvent(Vector3 pos,string name) { Position = pos;text = name; }
    }
    #endregion
}