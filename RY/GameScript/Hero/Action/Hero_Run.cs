using System.Collections;
using System.Collections.Generic;
using GameFrame.Fsm;
using GameFrame.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace BlackDawn
{
    /// <summary>
    /// 英雄的Run行为
    /// </summary>
    public class Hero_Run : FsmState<Hero>
    {
        IInputOperate _inputOperate; //按键输入
        protected internal override void OnInit(IFsm<Hero> fsm)
        {
       
            _inputOperate = InputOperateHandle.CreateOperate();
            _inputOperate.onMouse0Cancel = (input) =>
            {

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    fsm.Owner.targetPosition = hit.point;
                    //判断非run状态才进入Run
                    if (!(fsm.CurrentState is Hero_Run))
                        fsm.ChangeState<Hero_Run>();
                    // fsm.Owner.animator.SetTrigger("Run");
                }


            };
            _inputOperate.onMouse1Cancel = (input) =>
            {

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
            DevDebug.Log("进入Run状态");
          //s  fsm.Owner.animator.SetTrigger("Run");
            fsm.Owner.animator.SetBool("BoolRun", true);
            InputOperateHandle.PushOperate(_inputOperate);


        }
        protected internal override void OnUpdate(IFsm<Hero> fsm, float elapseSeconds, float realElapseSeconds)
        {
            //攻击
         //  fsm.Owner.HeroAttack(0.1f,1000,360);
            fsm.Owner.HeroAttackBurst();

            //移动
            var transform = fsm.Owner.transform;
            var dir = (fsm.Owner.targetPosition - transform.position).normalized;
            dir.y = 0;
            transform.Translate(dir * fsm.Owner.attributeCmpt.defenseAttribute.moveSpeed* Time.deltaTime, Space.World);

            if (Vector3.Distance(fsm.Owner.transform.position, fsm.Owner.targetPosition) < 0.2f)
            {
                fsm.ChangeState<Hero_Idle>();
               
            }
            ////面朝目标方向
            quaternion rotation = quaternion.LookRotationSafe(dir, new float3(0, 1, 0));//math.normalize(math.lookRotation(dir, math.up()));
            transform.rotation = math.slerp(transform.rotation, rotation, 20f * Time.deltaTime);// 插值旋转


        }
        protected internal override void OnExit(IFsm<Hero> fsm, bool isShutdown)
        {
            fsm.Owner.animator.SetBool("BoolRun", false);
            InputOperateHandle.PopOperate();
        }
        protected internal override void OnDestroy(IFsm<Hero> fsm)
        {

        }
    }

}
