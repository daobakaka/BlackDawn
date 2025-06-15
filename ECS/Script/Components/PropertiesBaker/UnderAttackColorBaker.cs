using BlackDawn.DOTS;
using Unity.Entities;
using UnityEngine;

public class UnderAttackColorBaker : MonoBehaviour
{

    // 2) �ڲ� Baker ��
    public class UnderAttackColorBakerAuthoring : Baker<UnderAttackColorBaker>
    {
        public override void Bake(UnderAttackColorBaker authoring)
        {
            // a) ��ȡʵ�壬ȷ������ Renderable ���
            var entity = GetEntity(
                authoring.gameObject,
                TransformUsageFlags.Dynamic
              | TransformUsageFlags.Renderable);

            AddComponent(entity, new UnderAttackColor());

        }
    }


}