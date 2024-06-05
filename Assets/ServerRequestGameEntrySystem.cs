using Assets.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
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
    public partial struct ServerRequestGameEntrySystem : ISystem
    {
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<JoinRequest, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }
        void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var gameResources = SystemAPI.GetSingleton<GameResources>();
            
            foreach (var (joinrequest, requestSource, requestEntity ) in SystemAPI.Query<JoinRequest, ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                var characterPrefab = gameResources.CharacterGhost;
                var playerPrefab = gameResources.PlayerGhost;
                ecb.AddBuffer<ClientOwnedEntities>(requestSource.SourceConnection);
                ecb.DestroyEntity(requestEntity);
                
                var networkID = SystemAPI.GetComponent<NetworkId>(requestSource.SourceConnection).Value;
                Debug.Log(networkID);


                // Spawn character
                Entity characterEntity = ecb.Instantiate(characterPrefab);
                Entity playerEntity = ecb.Instantiate(playerPrefab);
                ecb.SetComponent(characterEntity, new GhostOwner { NetworkId = networkID});
                ecb.SetComponent(playerEntity, new GhostOwner { NetworkId = networkID});
                ecb.AppendToBuffer(requestSource.SourceConnection, new ClientOwnedEntities { Entity = playerEntity });
                FirstPersonPlayer player = SystemAPI.GetComponent<FirstPersonPlayer>(gameResources.PlayerGhost);
                player.ControlledCharacter = characterEntity;
                ecb.SetComponent(playerEntity, player);


                ecb.SetComponent(characterEntity, LocalTransform.FromPosition(new Unity.Mathematics.float3(0,0,0)));
                ecb.SetComponent(characterEntity, new OwningPlayer { Entity = playerEntity });
                ecb.AddComponent<NetworkStreamInGame>(requestSource.SourceConnection);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

        }
    }
}
