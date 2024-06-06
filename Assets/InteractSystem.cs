using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

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

            foreach (var (player, input,  entity) in SystemAPI.Query<RefRW<FirstPersonPlayer>, FirstPersonPlayerInputs>().WithEntityAccess())
            {
                if (!input.InteractPressed.IsSet)
                {
                    
                    return;
                }
                var interact = SystemAPI.GetComponent<InteractComponent>(player.ValueRO.ControlledCharacter);
                FirstPersonCharacterComponent character = SystemAPI.GetComponent<FirstPersonCharacterComponent>(player.ValueRO.ControlledCharacter);
                Debug.Log("interact on update true");
                // Получаем PhysicsWorldSingleton
                PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

                // Получаем положение и ориентацию камеры (дочернего объекта)
                var cameraTransform = SystemAPI.GetComponent<LocalToWorld>(character.ViewEntity);

                // Получаем позицию камеры
                var cameraPosition = cameraTransform.Position;

                // Получаем направление камеры (переднее направление из матрицы LocalToWorld)
                var cameraForward = cameraTransform.Forward;

                // Вычисляем конечную точку рейкаста
                var rayEnd = cameraPosition + cameraForward * interact.Distance;
                // Визуализация луча для отладки
                Debug.DrawLine(cameraPosition, rayEnd, Color.red);
                // Создаем RaycastInput
                RaycastInput raycastInput = new RaycastInput
                {
                    Start = cameraPosition,
                    End = rayEnd,
                    Filter = new CollisionFilter
                    {
                        CollidesWith = (uint)CollisionLayers.Interactable,
                        BelongsTo = (uint)CollisionLayers.Player
                    }
                };

                // Выполняем рейкаст
                if (physicsWorld.CastRay(raycastInput, out var hit))
                {
                    ecb.DestroyEntity(hit.Entity);
                    Debug.Log("HIT!");
                }
            }
        }
    }
    public enum CollisionLayers
    {
        Interactable = 1 << 6,
        Player = 1 << 7
    }

    public struct InteractComponent : IComponentData
    {
        public Unity.Physics.RaycastHit InteractedEntity;
        public float Distance;
    }
}
