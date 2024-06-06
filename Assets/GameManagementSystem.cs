using Assets.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Scenes;
using UnityEngine;

[Serializable]
public struct LocalGameData : IComponentData
{
    public FixedString128Bytes LocalPlayerName;
}

[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class GameManagementSystem : SystemBase
{
    public World ClientWorld;
    public World ServerWorld;

    public const string LocalHost = "127.0.0.1";

    [Serializable]
    public struct JoinRequest : IComponentData
    {
        public FixedString128Bytes LocalPlayerName;
        public NetworkEndpoint EndPoint;
        public bool Spectator;
    }

    [Serializable]
    public struct HostRequest : IComponentData
    {
        public NetworkEndpoint EndPoint;
    }

    [Serializable]
    public struct DisconnectRequest : IComponentData
    { }

    [Serializable]
    public struct DisposeClientWorldRequest : IComponentData
    { }

    [Serializable]
    public struct DisposeServerWorldRequest : IComponentData
    { }

    [Serializable]
    public struct Singleton : IComponentData
    {
        public Entity MenuVisualsSceneInstance;
    }

    protected override void OnCreate()
    {
        base.OnCreate();

        // Auto-create singleton
        EntityManager.CreateEntity(typeof(Singleton));
        RequireForUpdate<GameResources>();
        RequireForUpdate<Singleton>();
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        // Start a tmp server just once so we can get a firewall prompt when running the game for the first time
        {
            NetworkDriver tmpNetDriver = NetworkDriver.Create();
            NetworkEndpoint tmpEndPoint = NetworkEndpoint.Parse(LocalHost, 7777);
            if (tmpNetDriver.Bind(tmpEndPoint) == 0)
            {
                tmpNetDriver.Listen();
            }
            tmpNetDriver.Dispose();
        }
    }

    protected override void OnUpdate()
    {
        ref Singleton singleton = ref SystemAPI.GetSingletonRW<Singleton>().ValueRW;
        GameResources gameResources = SystemAPI.GetSingleton<GameResources>();
        EntityCommandBuffer ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(World.Unmanaged);

        ProcessHostRequests(ref singleton, ref ecb, gameResources);
        ProcessJoinRequests(ref singleton, ref ecb, gameResources);
        ProcessDisconnectRequests(ref singleton, ref ecb);
        HandleDisposeClientServerWorldsAndReturnToMenu(ref singleton, ref ecb);
    }

    private void ProcessHostRequests(ref Singleton singleton, ref EntityCommandBuffer ecb, GameResources gameResources)
    {
        EntityCommandBuffer serverECB = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (request, entity) in SystemAPI.Query<RefRO<HostRequest>>().WithEntityAccess())
        {
            if (!WorldUtilities.IsValidAndCreated(ServerWorld))
            {
                // Create server world
                ServerWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");

                // Tickrate
                {
                    EntityQuery tickRateSingletonQuery = new EntityQueryBuilder(Allocator.Temp).WithAllRW<ClientServerTickRate>().Build(ServerWorld.EntityManager);
                    if (tickRateSingletonQuery.HasSingleton<ClientServerTickRate>())
                    {
                        serverECB.SetComponent(tickRateSingletonQuery.GetSingletonEntity(), gameResources.GetClientServerTickRate());
                    }
                    else
                    {
                        Entity tickRateEntity = serverECB.CreateEntity();
                        serverECB.AddComponent(tickRateEntity, gameResources.GetClientServerTickRate());
                    }
                }

                // Listen to endpoint
                EntityQuery serverNetworkDriverQuery = new EntityQueryBuilder(Allocator.Temp).WithAllRW<NetworkStreamDriver>().Build(ServerWorld.EntityManager);
                serverNetworkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(request.ValueRO.EndPoint);

                // Load game resources subscene
                SceneSystem.LoadSceneAsync(ServerWorld.Unmanaged, gameResources.GameResourcesScene);

                // Create a request to accept joins once the game scenes have been loaded
                {
                    EntityQuery serverGameSingletonQuery = new EntityQueryBuilder(Allocator.Temp).WithAllRW<ServerGameSystem.Singleton>().Build(ServerWorld.EntityManager);
                    ref ServerGameSystem.Singleton serverGameSingleton = ref serverGameSingletonQuery.GetSingletonRW<ServerGameSystem.Singleton>().ValueRW;
                    serverGameSingleton.AcceptJoins = false;

                    Entity requestAcceptJoinsEntity = serverECB.CreateEntity();
                    serverECB.AddComponent(requestAcceptJoinsEntity, new ServerGameSystem.AcceptJoinsOnceScenesLoadedRequest
                    {
                        PendingSceneLoadRequest = SceneLoadRequestSystem.CreateSceneLoadRequest(serverECB, gameResources.GameScene),
                    });
                }
            }

            ecb.DestroyEntity(entity);
            break;
        }

        if (WorldUtilities.IsValidAndCreated(ServerWorld))
        {
            serverECB.Playback(ServerWorld.EntityManager);
        }
        serverECB.Dispose();
    }

    private void ProcessJoinRequests(ref Singleton singleton, ref EntityCommandBuffer ecb, GameResources gameResources)
    {
        EntityCommandBuffer clientECB = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (request, entity) in SystemAPI.Query<RefRO<JoinRequest>>().WithEntityAccess())
        {
            if (!WorldUtilities.IsValidAndCreated(ClientWorld))
            {
                // Create client world
                ClientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");

                // Connect to endpoint
                EntityQuery clientNetworkDriverQuery = new EntityQueryBuilder(Allocator.Temp).WithAllRW<NetworkStreamDriver>().Build(ClientWorld.EntityManager);
                clientNetworkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(ClientWorld.EntityManager, request.ValueRO.EndPoint);

                // Create local game data singleton in client world
                Entity localGameDataEntity = ClientWorld.EntityManager.CreateEntity();
                ClientWorld.EntityManager.AddComponentData(localGameDataEntity, new LocalGameData
                {
                    LocalPlayerName = request.ValueRO.LocalPlayerName,
                });

                // Load game resources subscene
                SceneSystem.LoadSceneAsync(ClientWorld.Unmanaged, gameResources.GameResourcesScene);

                // Create a request to join once the game scenes have been loaded
                EntityQuery clientGameSingletonQuery = new EntityQueryBuilder(Allocator.Temp).WithAllRW<ClientGameSystem.Singleton>().Build(ClientWorld.EntityManager);
                ref ClientGameSystem.Singleton clientGameSingleton = ref clientGameSingletonQuery.GetSingletonRW<ClientGameSystem.Singleton>().ValueRW;
                clientGameSingleton.Spectator = request.ValueRO.Spectator;

                Entity requestAcceptJoinsEntity = clientECB.CreateEntity();
                clientECB.AddComponent(requestAcceptJoinsEntity, new ClientGameSystem.JoinOnceScenesLoadedRequest
                {
                    PendingSceneLoadRequest = SceneLoadRequestSystem.CreateSceneLoadRequest(clientECB, gameResources.GameScene),
                });

            }

            ecb.DestroyEntity(entity);
            break;
        }

        if (WorldUtilities.IsValidAndCreated(ClientWorld))
        {
            clientECB.Playback(ClientWorld.EntityManager);
        }
        clientECB.Dispose();
    }

    private void ProcessDisconnectRequests(ref Singleton singleton, ref EntityCommandBuffer ecb)
    {
        EntityQuery disconnectRequestQuery = SystemAPI.QueryBuilder().WithAll<DisconnectRequest>().Build();
        if (disconnectRequestQuery.CalculateEntityCount() > 0)
        {
            if (WorldUtilities.IsValidAndCreated(ClientWorld))
            {
                Entity disconnectClientRequestEntity = ecb.CreateEntity();
                ecb.AddComponent(disconnectClientRequestEntity, new ClientGameSystem.DisconnectRequest());
                ecb.AddComponent(disconnectClientRequestEntity, new MoveToClientWorld());
            }

            if (WorldUtilities.IsValidAndCreated(ServerWorld))
            {
                Entity disconnectServerRequestEntity = ecb.CreateEntity();
                ecb.AddComponent(disconnectServerRequestEntity, new ServerGameSystem.DisconnectRequest());
                ecb.AddComponent(disconnectServerRequestEntity, new MoveToServerWorld());
            }
        }
        ecb.DestroyEntity(disconnectRequestQuery, EntityQueryCaptureMode.AtRecord);
    }

    private void HandleDisposeClientServerWorldsAndReturnToMenu(ref Singleton singleton, ref EntityCommandBuffer ecb)
    {
        EntityQuery disposeClientRequestQuery = SystemAPI.QueryBuilder().WithAll<DisposeClientWorldRequest>().Build();
        if (disposeClientRequestQuery.CalculateEntityCount() > 0)
        {
            if (WorldUtilities.IsValidAndCreated(ClientWorld))
            {
                ClientWorld.Dispose();
            }

            EntityManager.DestroyEntity(disposeClientRequestQuery);
        }

        EntityQuery disposeServerRequestQuery = SystemAPI.QueryBuilder().WithAll<DisposeServerWorldRequest>().Build();
        if (disposeServerRequestQuery.CalculateEntityCount() > 0)
        {
            if (WorldUtilities.IsValidAndCreated(ServerWorld))
            {
                ServerWorld.Dispose();
            }

            EntityManager.DestroyEntity(disposeServerRequestQuery);
        }
    }
}

public struct MoveToClientWorld : IComponentData
{ }

public struct MoveToServerWorld : IComponentData
{ }

public struct MoveToLocalWorld : IComponentData
{ }

public static class WorldUtilities
{
    public static bool IsValidAndCreated(World world)
    {
        return world != null && world.IsCreated;
    }

    public static void CopyEntitiesToWorld(EntityManager srcEntityManager, EntityManager dstEntityManager, EntityQuery entityQuery)
    {
        NativeArray<Entity> entitiesToCopy = entityQuery.ToEntityArray(Allocator.Temp);
        dstEntityManager.CopyEntitiesFrom(srcEntityManager, entitiesToCopy);
        entitiesToCopy.Dispose();
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation)]
public partial class MoveLocalEntitiesToClientServerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Move entities to clients
        EntityQuery pendingMoveToClientQuery = SystemAPI.QueryBuilder().WithAll<MoveToClientWorld>().Build();
        if (pendingMoveToClientQuery.CalculateEntityCount() > 0)
        {
            // For each client world...
            World.NoAllocReadOnlyCollection<World> worlds = World.All;
            for (int i = 0; i < worlds.Count; i++)
            {
                World tmpWorld = worlds[i];
                if (WorldUtilities.IsValidAndCreated(tmpWorld) && (tmpWorld.IsClient() || tmpWorld.IsThinClient()))
                {
                    WorldUtilities.CopyEntitiesToWorld(EntityManager, tmpWorld.EntityManager, pendingMoveToClientQuery);
                }
            }

            // Destroy entities in this world after copying them to all target worlds
            EntityManager.DestroyEntity(pendingMoveToClientQuery);
        }

        // Move entities to server
        EntityQuery pendingMoveToServerQuery = SystemAPI.QueryBuilder().WithAll<MoveToServerWorld>().Build();
        if (pendingMoveToServerQuery.CalculateEntityCount() > 0)
        {
            // For each server world...
            World.NoAllocReadOnlyCollection<World> worlds = World.All;
            for (int i = 0; i < worlds.Count; i++)
            {
                World tmpWorld = worlds[i];
                if (WorldUtilities.IsValidAndCreated(tmpWorld) && tmpWorld.IsServer())
                {
                    WorldUtilities.CopyEntitiesToWorld(EntityManager, tmpWorld.EntityManager, pendingMoveToServerQuery);
                }
            }

            // Destroy entities in this world after copying them to all target worlds
            EntityManager.DestroyEntity(pendingMoveToServerQuery);
        }
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
public partial class MoveClientServerEntitiesToLocalSystem : SystemBase
{
    protected override void OnUpdate()
    {
        EntityQuery pendingMoveToLocalQuery = SystemAPI.QueryBuilder().WithAll<MoveToLocalWorld>().Build();
        if (pendingMoveToLocalQuery.CalculateEntityCount() > 0)
        {
            World.NoAllocReadOnlyCollection<World> worlds = World.All;
            for (int i = 0; i < worlds.Count; i++)
            {
                World tmpWorld = worlds[i];
                if (WorldUtilities.IsValidAndCreated(tmpWorld) &&
                    !(tmpWorld.IsClient() || tmpWorld.IsThinClient()) &&
                    !tmpWorld.IsServer() &&
                    tmpWorld.GetExistingSystemManaged<GameManagementSystem>() != null)
                {
                    WorldUtilities.CopyEntitiesToWorld(EntityManager, tmpWorld.EntityManager, pendingMoveToLocalQuery);
                }

                // Destroy entities in this world after copying them to all target worlds
                EntityManager.DestroyEntity(pendingMoveToLocalQuery);
            }
        }
    }
}