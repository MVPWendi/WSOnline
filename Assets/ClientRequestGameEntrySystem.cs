using Assets.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using static FixedTickSystem;

namespace Assets
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ClientRequestGameEntrySystem : ISystem
    {
        private EntityQuery _pendingNetworkIdQuery;

        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<NetworkId>().WithNone<NetworkStreamInGame>();
            _pendingNetworkIdQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate(_pendingNetworkIdQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var pendingNetworkIds = _pendingNetworkIdQuery.ToEntityArray(Allocator.Temp);
            foreach (var pendingNetworkId in pendingNetworkIds)
            {
                ecb.AddComponent<NetworkStreamInGame>(pendingNetworkId);
                var request = ecb.CreateEntity();
                ecb.AddComponent(request, new JoinRequest());
                ecb.AddComponent(request, new SendRpcCommandRequest { TargetConnection = pendingNetworkId });
            }
            ecb.Playback(state.EntityManager);
        }
    }
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ClientGameSystem : ISystem
    {
        private EntityQuery _pendingNetworkIdQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();
        }
        private void HandleCharacterSetupAndDestruction(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<NetworkId>())
            {
                EntityCommandBuffer ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

                // Initialize local-owned characters
                foreach (var (character, owningPlayer, ghostOwner, entity) in SystemAPI.Query<FirstPersonCharacterComponent, OwningPlayer, GhostOwner>().WithAll<GhostOwnerIsLocal>().WithNone<CharacterInitialized>().WithEntityAccess())
                {
                    // Make camera follow character's view
                    ecb.AddComponent(character.ViewEntity, new MainEntityCamera { });

                    // Make local character meshes rendering be shadow-only
                    BufferLookup<Child> childBufferLookup = SystemAPI.GetBufferLookup<Child>();
                    MiscUtilities.SetShadowModeInHierarchy(state.EntityManager, ecb, entity, ref childBufferLookup, UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly);



                    // Mark initialized
                    ecb.AddComponent<CharacterInitialized>(entity);
                }

                // Initialize remote characters
                foreach (var (character, owningPlayer, ghostOwner, entity) in SystemAPI.Query<FirstPersonCharacterComponent, OwningPlayer, GhostOwner>().WithNone<GhostOwnerIsLocal>().WithNone<CharacterInitialized>().WithEntityAccess())
                {

                    // Mark initialized
                    ecb.AddComponent<CharacterInitialized>(entity);
                }
            }
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //HandleCharacterSetupAndDestruction(ref state);
            GameResources gameResources = SystemAPI.GetSingleton<GameResources>();

            if (SystemAPI.HasSingleton<DisableCharacterDynamicContacts>())
            {
                state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<DisableCharacterDynamicContacts>());
            }

            if (SystemAPI.HasSingleton<NetworkId>())
            {
                Debug.Log("T1");
                EntityCommandBuffer ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

                // Initialize local-owned characters
                foreach (var (character, owningPlayer, ghostOwner, entity) in SystemAPI.Query<FirstPersonCharacterComponent, OwningPlayer, GhostOwner>().WithAll<GhostOwnerIsLocal>().WithNone<CharacterInitialized>().WithEntityAccess())
                {
                    Debug.Log("T3");
                    // Make camera follow character's view
                    ecb.AddComponent(character.ViewEntity, new MainEntityCamera { });

                    // Make local character meshes rendering be shadow-only
                    BufferLookup<Child> childBufferLookup = SystemAPI.GetBufferLookup<Child>();
                    MiscUtilities.SetShadowModeInHierarchy(state.EntityManager, ecb, entity, ref childBufferLookup, UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly);

                    

                    // Mark initialized
                    ecb.AddComponent<CharacterInitialized>(entity);
                }

                // Initialize remote characters
                foreach (var (character, owningPlayer, ghostOwner, entity) in SystemAPI.Query<FirstPersonCharacterComponent, OwningPlayer, GhostOwner>().WithNone<GhostOwnerIsLocal>().WithNone<CharacterInitialized>().WithEntityAccess())
                {
                    Debug.Log("T2");
                    // Mark initialized
                    ecb.AddComponent<CharacterInitialized>(entity);
                }
            }
        }
    }

    public struct CharacterSpawnRequest : IComponentData
    {
        public Entity ForConnection;
        public Entity PlayerEntity;
        public float Delay;
    }
}
