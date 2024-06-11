using Assets;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class InteractableAuthoring : MonoBehaviour
{

    public class Baker : Baker<InteractableAuthoring>
    {
        public override void Bake(InteractableAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace);

            AddComponent(entity, new InteractableObject());
        }
    }

}

public struct InteractableObject : IComponentData
{

}


