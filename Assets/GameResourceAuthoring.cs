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

    [Header("Scenes")]
    public BakedSubSceneReference MenuVisualsScene;
    public BakedSubSceneReference GameResourcesScene;
    public BakedSubSceneReference GameScene;

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
                GameResourcesScene = authoring.GameResourcesScene.GetEntitySceneReference(),
                GameScene = authoring.GameScene.GetEntitySceneReference(),
            });
        }
    }
}
[Serializable]
public struct BakedSubSceneReference
{
#if UNITY_EDITOR
    public SceneAsset SceneAsset;
#endif

    public EntitySceneReference GetEntitySceneReference()
    {
#if UNITY_EDITOR
        return new EntitySceneReference(SceneAsset);
#else
        return default;
#endif
    }
}