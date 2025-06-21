using GameFrame.Fsm;
using GameFrame.Runtime;
using UnityEngine;

namespace BlackDawn
{
    /// <summary>
    /// 英雄技能状态（Skill 1）
    /// </summary>
    public class Hero_Skill : FsmState<Hero>
    {
        private IInputOperate _inputOperate;  // 按键输入
        private int _skillTagHash;
        private HeroSkills _heroSkills;
        // 标记中点逻辑是否已经触发过
        private bool _hasFiredMid;
        private int _skillID;
        private int _skillIDType;
        private float _skillRelaseTime;

        protected internal override void OnInit(IFsm<Hero> fsm)
        {
            _inputOperate = InputOperateHandle.CreateOperate();

            _skillID = (int)fsm.Owner.attributeCmpt.skillDamageAttribute.skillName;
            _skillIDType = (int)fsm.Owner.attributeCmpt.skillDamageAttribute.skillPsionicType;
            _skillRelaseTime = (float)fsm.Owner.attributeCmpt.skillDamageAttribute.skillRelaseTime;
            _heroSkills = fsm.Owner.heroSkills;
            _hasFiredMid = false;
            // 在 Animator 中给 Skill1 State 打的 Tag,这里的string 由外部的加载传入， 确定技能状态
            _skillTagHash = Animator.StringToHash("SkillNumber"+_skillID);
           
   


            DevDebug.Log("----------------------------------初始化技能操作手柄");

           
        }
        /// <summary>
        /// 这里可以强转技能枚举形成参数传入，锁定技能，可以增加一个结构体，读取之后储存技能枚举和触发时间
        /// </summary>
        /// <param name="fsm"></param>
        protected internal override void OnEnter(IFsm<Hero> fsm)
        {
      
            DevDebug.Log("进入skill1状态");
            // 播 Skill1 动画
            _hasFiredMid = false;
            var anim = fsm.Owner.animator;
            anim.SetInteger("SkillNumber", _skillID);
            anim.SetTrigger("Skill");
            DevDebug.Log("开始播放动画");

            // 切入输入上下文
            InputOperateHandle.PushOperate(_inputOperate);
        }

        protected internal override void OnUpdate(IFsm<Hero> fsm, float elapseSeconds, float realElapseSeconds)
        {
            var anim = fsm.Owner.animator;
            var stateInfo = anim.GetCurrentAnimatorStateInfo(0);

            if (!_hasFiredMid
        && stateInfo.tagHash == _skillTagHash
        //确保只对第一圈做响应
        && stateInfo.normalizedTime >= _skillRelaseTime && stateInfo.normalizedTime < 1.0f)
            {
                _hasFiredMid = true;
                DevDebug.Log("SKILL 动画到中点，执行特殊逻辑"+stateInfo.normalizedTime);
                _heroSkills.RelasesHeroSkill((HeroSkillID)_skillID, (HeroSkillPsionicType)_skillIDType);


                

            }




            // 如果当前 State 带了 “Skill1” 这个 Tag，且播放完一轮
            if (stateInfo.tagHash == _skillTagHash && stateInfo.normalizedTime >= 1f )
            {
                DevDebug.Log("SKILL动画播放完毕"+stateInfo.normalizedTime);
                // 动画播完，回到 Idle
                fsm.ChangeState<Hero_Idle>();
            }
        }

        protected internal override void OnExit(IFsm<Hero> fsm, bool isShutdown)
        {

            // fsm.Owner.animator.SetTrigger("Idle");
            // 恢复上层输入上下文
            _hasFiredMid = false;
            InputOperateHandle.PopOperate();
        }

        protected internal override void OnDestroy(IFsm<Hero> fsm)
        {
        }
    }
}
