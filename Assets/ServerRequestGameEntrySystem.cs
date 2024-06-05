using Assets.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Assets
{
    public struct ClientOwnedEntities : ICleanupBufferElementData
    {
        public Entity Entity;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct ServerRequestGameEntrySystem : ISystem
    {
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<JoinRequest, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
            
        }

        private void HandleJoinRequest(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            var gameResources = SystemAPI.GetSingleton<GameResources>();

            foreach (var (joinrequest, requestSource, requestEntity) in SystemAPI.Query<JoinRequest, ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                Debug.Log("HandleJoinRequest");
                var playerPrefab = gameResources.PlayerGhost;
                var networkID = SystemAPI.GetComponent<NetworkId>(requestSource.SourceConnection).Value;
                ecb.AddBuffer<ClientOwnedEntities>(requestSource.SourceConnection);
                Entity playerEntity = ecb.Instantiate(playerPrefab);
                ecb.SetComponent(playerEntity, new GhostOwner { NetworkId = networkID });

                // Set player data
                FirstPersonPlayer player = SystemAPI.GetComponent<FirstPersonPlayer>(gameResources.PlayerGhost);
                ecb.SetComponent(playerEntity, player);

                // Request to spawn character
                Entity spawnCharacterRequestEntity = ecb.CreateEntity();
                ecb.AddComponent(spawnCharacterRequestEntity, new CharacterSpawnRequest { ForConnection = requestSource.SourceConnection, Delay = -1f, PlayerEntity = playerEntity });
                ecb.AddComponent<NetworkStreamInGame>(requestSource.SourceConnection);
                Debug.Log("AddedCharacterSpawnRequest");
                ecb.DestroyEntity(requestEntity);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void HandleSpawnCharacterRequest(ref SystemState state)
        {
            Debug.Log("HandleSpawnCharacterRequest");
            var gameResources = SystemAPI.GetSingleton<GameResources>();
            EntityCommandBuffer ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (spawnRequest, entity) in SystemAPI.Query<RefRW<CharacterSpawnRequest>>().WithEntityAccess())
            {
                Debug.Log("HandleSpawnCharacterRequest2");
                if (SystemAPI.HasComponent<NetworkId>(spawnRequest.ValueRW.ForConnection))
                {
                    Debug.Log("HandleSpawnCharacterRequest3");
                    int connectionId = SystemAPI.GetComponent<NetworkId>(spawnRequest.ValueRW.ForConnection).Value;
                    float3 randomSpawnPosition = new float3(0, 0, 0);

                    // Spawn character
                    Entity characterEntity = ecb.Instantiate(gameResources.CharacterGhost);
                    ecb.SetComponent(characterEntity, new GhostOwner { NetworkId = connectionId });
                    ecb.SetComponent(characterEntity, LocalTransform.FromPosition(randomSpawnPosition));
                    ecb.SetComponent(characterEntity, new OwningPlayer { Entity = spawnRequest.ValueRO.PlayerEntity });
                    ecb.AppendToBuffer(spawnRequest.ValueRW.ForConnection, new ClientOwnedEntities { Entity = characterEntity });
                    // Assign character to player
                    FirstPersonPlayer player = SystemAPI.GetComponent<FirstPersonPlayer>(spawnRequest.ValueRO.PlayerEntity);
                    player.ControlledCharacter = characterEntity;
                    ecb.SetComponent(spawnRequest.ValueRO.PlayerEntity, player);
                }

                //ecb.DestroyEntity(entity);
            }
        }

        void OnUpdate(ref SystemState state)
        {
            HandleJoinRequest(ref state);
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct ServerSpawnSystem : ISystem
    {
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();
        }

        private void HandleSpawnCharacterRequest(ref SystemState state)
        {
            Debug.Log("HandleSpawnCharacterRequest");
            var gameResources = SystemAPI.GetSingleton<GameResources>();
            EntityCommandBuffer ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (spawnRequest, entity) in SystemAPI.Query<RefRW<CharacterSpawnRequest>>().WithNone<CharacterInitialized>().WithEntityAccess())
            {
                Debug.Log("HandleSpawnCharacterRequest2");
                if (SystemAPI.HasComponent<NetworkId>(spawnRequest.ValueRW.ForConnection))
                {
                    Debug.Log("HandleSpawnCharacterRequest3");
                    int connectionId = SystemAPI.GetComponent<NetworkId>(spawnRequest.ValueRW.ForConnection).Value;
                    float3 randomSpawnPosition = new float3(0, 0, 0);

                    // Spawn character
                    Entity characterEntity = ecb.Instantiate(gameResources.CharacterGhost);
                    ecb.SetComponent(characterEntity, new GhostOwner { NetworkId = connectionId });
                    ecb.SetComponent(characterEntity, LocalTransform.FromPosition(randomSpawnPosition));
                    ecb.SetComponent(characterEntity, new OwningPlayer { Entity = spawnRequest.ValueRO.PlayerEntity });
                    ecb.AppendToBuffer(spawnRequest.ValueRW.ForConnection, new ClientOwnedEntities { Entity = characterEntity });
                    // Assign character to player
                    FirstPersonPlayer player = SystemAPI.GetComponent<FirstPersonPlayer>(spawnRequest.ValueRO.PlayerEntity);
                    player.ControlledCharacter = characterEntity;
                    ecb.SetComponent(spawnRequest.ValueRO.PlayerEntity, player);
                }

                ecb.DestroyEntity(entity);
            }
        }

        void OnUpdate(ref SystemState state)
        {
            HandleSpawnCharacterRequest(ref state);
        }
    }
}
