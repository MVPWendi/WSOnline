using Assets.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEditor;
using UnityEngine;

public class GameResourcesAuthoring : MonoBehaviour
{
    public GameObject PlayerGhost;
    public GameObject CharacterGhost;

    public class Baker : Baker<GameResourcesAuthoring>
    {
        public override void Bake(GameResourcesAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new GameResources
            {
                PlayerGhost = GetEntity(authoring.PlayerGhost, TransformUsageFlags.Dynamic),
                CharacterGhost = GetEntity(authoring.CharacterGhost, TransformUsageFlags.Dynamic),
            });
        }
    }
}