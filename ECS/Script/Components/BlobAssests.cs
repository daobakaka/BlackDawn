using System.Collections;
using Unity.Entities;
using UnityEngine;
using System;

namespace BlackDawn.DOTS
{
    
    //原始英雄属性组分
    [Serializable]
    public struct OriginalHeroAttribute
    { 
        public BaseAttribute baseAttribute;
        public AttackAttribute attackAttribute;
        public DefenseAttribute defenseAttribute;
        public GainAttribute gainAttribute;
        public LossPoolAttribute lossPoolAttribute;
        public DebuffAttribute debuffAttribute;
        public ControlAbilityAttribute controlAbilityAttribute;
        public ControlledEffectAttribute controlledEffectAttribute;
        // 新增武器属性
        public WeaponAttribute weaponAttribute;
        //新增控制伤害加成属性
        public ControlDamageAttribute controlDamageAttribute;
        //新增DOT影响伤害成属性
        public DotDamageAttribute dotDamageAttribute;
        //新增技能伤害结构体
        public SkillDamageAttribute skillDamageAttribute;
        //新增动态伤害结构体

    }
    //基于bolb数据的ECS 组件
    public struct HeroAttributeBlobCmpt : IComponentData
{
    public BlobAssetReference<OriginalHeroAttribute> Blob;
}



    public class BlobAssests : MonoBehaviour
    {
        public  OriginalHeroAttribute originalHeroAttribute;
     

     public class HeroAttributeBaker : Baker<BlobAssests>
        {
            public override void Bake(BlobAssests authoring)
            {
                var builder = new BlobBuilder(Unity.Collections.Allocator.Temp);

                // 一步直接整体拷贝 struct
                ref OriginalHeroAttribute root = ref builder.ConstructRoot<OriginalHeroAttribute>();
                root = authoring.originalHeroAttribute;

                // hash 也可以直接用所有属性的 hash 混合，或全 0
                var hash = new Unity.Entities.Hash128(
                    (uint)authoring.originalHeroAttribute.attackAttribute.armorBreak. GetHashCode(),
                    (uint)authoring.originalHeroAttribute.attackAttribute.attackPower.GetHashCode(),
                    0,0);

                if (!TryGetBlobAssetReference(hash, out BlobAssetReference<OriginalHeroAttribute> blobReference))
                {
                    blobReference = builder.CreateBlobAssetReference<OriginalHeroAttribute>(Unity.Collections.Allocator.Persistent);
                    builder.Dispose();
                    AddBlobAssetWithCustomHash(ref blobReference, hash);
                }
                else
                {
                    builder.Dispose();
                }

                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new HeroAttributeBlobCmpt { Blob = blobReference });
            }
        }

    }



   
}
