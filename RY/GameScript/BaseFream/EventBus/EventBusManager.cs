using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFrame.BaseClass;
using GameFrame.EventBus;

namespace BlackDawn
{/// <summary>
/// �¼����߹��������ں��¼�����
/// </summary>
    public class EventBusManager :Singleton<EventBusManager> 
    {
        private EventBusManager() { eventBus = EventBus.GetInstance(); }

        public EventBus eventBus;
        public void TestEvent(PlayerTestEvent testEvent)
        {


            DevDebug.Log("�����¼�����" + testEvent.Position + testEvent.text);
        
        }





    }

    #region �¼��ṹ��
    public struct PlayerTestEvent
    {
        public Vector3 Position;
        public string text;
        public PlayerTestEvent(Vector3 pos,string name) { Position = pos;text = name; }
    }
    #endregion
}