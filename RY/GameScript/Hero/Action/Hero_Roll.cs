using GameFrame.Fsm;
using GameFrame.Runtime;
using UnityEngine;

namespace BlackDawn
{
    /// <summary>
    /// 英雄滚动
    /// </summary>
    public class Hero_Roll : FsmState<Hero>
    {
        private IInputOperate _inputOperate; // 按键输入
        private int _rollTagHash;

        protected internal override void OnInit(IFsm<Hero> fsm)
        {
            // 每个状态创建自己的 InputOperate
            _inputOperate = InputOperateHandle.CreateOperate();

            // 动画State标签
            _rollTagHash = Animator.StringToHash("Roll");
        }

        protected internal override void OnEnter(IFsm<Hero> fsm)
        {
            DevDebug.Log("进入Roll状态");
            fsm.Owner.animator.SetTrigger("Roll");
            InputOperateHandle.PushOperate(_inputOperate);
        }

        protected internal override void OnUpdate(IFsm<Hero> fsm, float elapseSeconds, float realElapseSeconds)
        {
            // 1. 移动角色
            var owner = fsm.Owner;
            var trans = owner.transform;
            // 取水平面前向
            Vector3 forward = trans.forward;
            forward.y = 0;
            forward.Normalize();
            // 设定滚动速度（你可以改成从 Hero 里读一个字段）
            float rollSpeed = fsm.Owner.attributeCmpt.defenseAttribute.moveSpeed*3; // 3被移速的滚动速度
                                               // 注意：用 realElapseSeconds 以防缩放时间影响
            trans.position += forward * rollSpeed * realElapseSeconds;

            // 2. 检测动画是否播完一轮
            var anim = owner.animator;
            var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.tagHash == _rollTagHash && stateInfo.normalizedTime >= 1f)
            {
                // 动画完成，切回 Idle
                fsm.ChangeState<Hero_Idle>();
                //fsm.Owner.animator.SetTrigger("Idle");
            }
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
