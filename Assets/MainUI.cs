using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainUI : MonoBehaviour
{
    [SerializeField] private Button HostButton;
    [SerializeField] private Button ConnectButton;
    // Start is called before the first frame update
    void Start()
    {
        HostButton.onClick.AddListener(OnHost);
        ConnectButton.onClick.AddListener(OnConnect);
    }

    private void OnConnect()
    {
        DestroyLocalSimulationWorld();
        SceneManager.LoadScene(1);
        StartClient();
    }

    private void DestroyLocalSimulationWorld()
    {
        foreach (var world in World.All)
        {
            if(world.Flags==WorldFlags.Game)
            {
                world.Dispose();
                break;
            }
        }
    }

    private void OnHost()
    {
        DestroyLocalSimulationWorld();
        SceneManager.LoadScene(1);
        StartServer();
        StartClient();
    }

    private void StartClient()
    {
        var clientWorld = ClientServerBootstrap.CreateClientWorld("Client world");
        var conenctionEndpoint = NetworkEndpoint.Parse("127.0.0.1", 6666);
        {
            using var networkDriverQuery = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(clientWorld.EntityManager, conenctionEndpoint);
        }

        World.DefaultGameObjectInjectionWorld = clientWorld;
    }
    private void StartServer()
    {
        var serverWorld = ClientServerBootstrap.CreateServerWorld("Server world");
        var serverEndpoint = NetworkEndpoint.AnyIpv4.WithPort(6666);
        {
            using var networkDriverQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndpoint);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
