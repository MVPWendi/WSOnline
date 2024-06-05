using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Assets
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct InteractSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstTimeFullyPredictingTick) return;

            var currentTick = networkTime.ServerTick;

            foreach (var (inp, player) in SystemAPI.Query<RefRW<FirstPersonPlayerInputs>, FirstPersonPlayer>().WithAll<Simulate, GhostOwnerIsLocal>())
            {
                if (inp.ValueRO.InteractPressed.IsSet)
                {
                    Debug.Log("Interact pressed 2");
                }
            }
        }
    }
}
