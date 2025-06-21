using System.Collections;
using System.Collections.Generic;
using GameFrame.Fsm;
using GameFrame.Runtime;
using Unity.VisualScripting;
using UnityEngine;

namespace BlackDawn
{
    /// <summary>
    /// 英雄idle状态
    /// </summary>
    public class Hero_Idle : FsmState<Hero>
    {
        IInputOperate _inputOperate; //按键输入
        protected internal override void OnInit(IFsm<Hero> fsm)
        {
            //状态分离
            _inputOperate = InputOperateHandle.CreateOperate();

            _inputOperate.onMouse0Cancel = (input) =>
            {
                
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit))
                    {
                        fsm.Owner.targetPosition = hit.point;
                        fsm.ChangeState<Hero_Run>();
                        
                }
                
            };
            //鼠标右键事件
            _inputOperate.onMouse1Cancel = (input) =>
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    fsm.Owner.skillTargetPositon = new Vector3(hit.point.x, 0, hit.point.z);
                   // fsm.Owner.skillElur = hit.transform.position.normalized;
                   

                }
                fsm.ChangeState<Hero_Skill>();
            };

            _inputOperate.onSpace = () =>
            {
                fsm.ChangeState<Hero_Roll>();
            };

            _inputOperate.onMouseWheel = (input) =>
            {
                fsm.Owner.TotalListenController(input);

            };


        }
        protected internal override void OnEnter(IFsm<Hero> fsm)
        {
       
            DevDebug.Log("进入idel状态");
            fsm.Owner.animator.SetTrigger("Idle");
            InputOperateHandle.PushOperate(_inputOperate);



      


        }
        protected internal override void OnUpdate(IFsm<Hero> fsm, float elapseSeconds, float realElapseSeconds)
        {
            // fsm.Owner.HeroAttack(0.1f,100,360);

            // fsm.Owner.HeroAttackBurst(0.1f, 100, 360);
            // fsm.Owner.HeroAttackBurst();
           // fsm.Owner.HeroAttackBurst();
        }
        protected internal override void OnExit(IFsm<Hero> fsm, bool isShutdown)
        {

            InputOperateHandle.PopOperate();
        }
        protected internal override void OnDestroy(IFsm<Hero> fsm)
        {

        }
        
    }
}

