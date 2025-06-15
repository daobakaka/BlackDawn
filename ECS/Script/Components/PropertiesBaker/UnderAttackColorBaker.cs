using BlackDawn.DOTS;
using Unity.Entities;
using UnityEngine;

public class UnderAttackColorBaker : MonoBehaviour
{

    // 2) 内部 Baker 类
    public class UnderAttackColorBakerAuthoring : Baker<UnderAttackColorBaker>
    {
        public override void Bake(UnderAttackColorBaker authoring)
        {
            // a) 获取实体，确保打上 Renderable 标记
            var entity = GetEntity(
                authoring.gameObject,
                TransformUsageFlags.Dynamic
              | TransformUsageFlags.Renderable);

            AddComponent(entity, new UnderAttackColor());

        }
    }


}