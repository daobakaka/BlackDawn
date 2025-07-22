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
    /// 英雄潜行状态
    /// </summary>
    public class Hero_Stealth : FsmState<Hero>
    {
        private IInputOperate _inputOperate;  // 按键输入

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
                    //判断非hero_Stealth 状态才进行 Stealth
                    if (!(fsm.CurrentState is Hero_Stealth)&&fsm.Owner.skillAttackPar.stealth)
                        fsm.ChangeState<Hero_Stealth>();
                }


            };


            //鼠标右键点击事件，捕获鼠标位置，为技能释放位置
            _inputOperate.onMouse1Cancel = (input) =>
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    fsm.Owner.skillTargetPositon = new Vector3(hit.point.x, 0, hit.point.z);
                    
                    Vector3 direction = fsm.Owner.skillTargetPositon - fsm.Owner.transform.position;
                    direction.y = 0; // 保持水平方向
                     fsm.Owner.transform.rotation = Quaternion.LookRotation(direction);

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
           
   
            DevDebug.Log("---------------------------------潜行状态状态机");

           
        }
        /// <summary>
        /// 这里可以强转技能枚举形成参数传入，锁定技能，可以增加一个结构体，读取之后储存技能枚举和触发时间
        /// </summary>
        /// <param name="fsm"></param>
        protected internal override void OnEnter(IFsm<Hero> fsm)
        {
      
             DevDebug.LogError("进入潜行状态");
            fsm.Owner.animator.SetBool("Stealth",true);
            InputOperateHandle.PushOperate(_inputOperate);
        }

        protected internal override void OnUpdate(IFsm<Hero> fsm, float elapseSeconds, float realElapseSeconds)
        {
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
            DevDebug.LogError("离开潜行状态");
              fsm.Owner.animator.SetBool("Stealth",false);
              InputOperateHandle.PopOperate();
        }

        protected internal override void OnDestroy(IFsm<Hero> fsm)
        {
        }
    }
}
