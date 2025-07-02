using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackDawn.DOTS
{
    public class Monster : MonoBehaviour
    {
        public class MonsterBaker : Baker<Monster>
        {
            public override void Bake(Monster authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                var monster = new MonsterComponent();
                AddComponent(entity, monster);
            }
        }
    }

    public struct MonsterComponent : IComponentData
    {

    }
        /// <summary>
        ///测试
        /// </summary>
    public struct Monsterkaka: IComponentData{}

}
