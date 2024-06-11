using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.CharacterController;
using Unity.NetCode;
using Assets;

namespace test
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class FirstPersonPlayerInputsSystem : SystemBase
    {
        private WSInputs inputs;
        protected override void OnCreate()
        {
            inputs = new WSInputs();
            inputs.Enable();
            inputs.CharacterControl.Enable();
            RequireForUpdate<NetworkTime>();
            RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FirstPersonPlayer, FirstPersonPlayerInputs>().Build());
        }

        protected override void OnUpdate()
        {
            foreach (var (playerInputs, player) in SystemAPI.Query<RefRW<FirstPersonPlayerInputs>, FirstPersonPlayer>().WithAll<GhostOwnerIsLocal>())
            {
                Debug.Log("Input: " + player.ControlledCharacter.Index);
                playerInputs.ValueRW.MoveInput = new float2
                {
                    x = (Input.GetKey(KeyCode.D) ? 1f : 0f) + (Input.GetKey(KeyCode.A) ? -1f : 0f),
                    y = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f),
                };

                NetworkInputUtilities.AddInputDelta(ref playerInputs.ValueRW.LookInput.x, Input.GetAxis("Mouse X"));
                NetworkInputUtilities.AddInputDelta(ref playerInputs.ValueRW.LookInput.y, Input.GetAxis("Mouse Y"));

                playerInputs.ValueRW.JumpPressed = default;
                if (Input.GetKeyDown(KeyCode.Space))
                {                
                    playerInputs.ValueRW.JumpPressed.Set();
                }
                playerInputs.ValueRW.InteractPressed = default;
                if (inputs.CharacterControl.Interact.WasPressedThisFrame())
                {
                    Debug.Log("Interact pressed");
                    playerInputs.ValueRW.InteractPressed.Set();
                }
            }
        }
    }

    /// <summary>
    /// Apply inputs that need to be read at a variable rate
    /// </summary>
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedFixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct FirstPersonPlayerVariableStepControlSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FirstPersonPlayer, FirstPersonPlayerInputs>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NetworkInputUtilities.GetCurrentAndPreviousTick(SystemAPI.GetSingleton<NetworkTime>(), out NetworkTick currentTick, out NetworkTick previousTick);

            foreach (var (playerInputsBuffer, player) in SystemAPI.Query<DynamicBuffer<InputBufferData<FirstPersonPlayerInputs>>, FirstPersonPlayer>().WithAll<Simulate>())
            {
                NetworkInputUtilities.GetCurrentAndPreviousTickInputs(playerInputsBuffer, currentTick, previousTick, out FirstPersonPlayerInputs currentTickInputs, out FirstPersonPlayerInputs previousTickInputs);

                if (SystemAPI.HasComponent<FirstPersonCharacterControl>(player.ControlledCharacter))
                {
                    FirstPersonCharacterControl characterControl = SystemAPI.GetComponent<FirstPersonCharacterControl>(player.ControlledCharacter);

                    characterControl.LookDegreesDelta.x = NetworkInputUtilities.GetInputDelta(currentTickInputs.LookInput.x, previousTickInputs.LookInput.x);
                    characterControl.LookDegreesDelta.y = NetworkInputUtilities.GetInputDelta(currentTickInputs.LookInput.y, previousTickInputs.LookInput.y);

                    SystemAPI.SetComponent(player.ControlledCharacter, characterControl);
                }
            }
        }
    }

    /// <summary>
    /// Apply inputs that need to be read at a fixed rate.
    /// It is necessary to handle this as part of the fixed step group, in case your framerate is lower than the fixed step rate.
    /// </summary>
    [UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct FirstPersonPlayerFixedStepControlSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FirstPersonPlayer, FirstPersonPlayerInputs>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (playerInputs, player) in SystemAPI.Query<FirstPersonPlayerInputs, FirstPersonPlayer>().WithAll<Simulate>())
            {
                if (SystemAPI.HasComponent<FirstPersonCharacterControl>(player.ControlledCharacter))
                {
                    FirstPersonCharacterControl characterControl = SystemAPI.GetComponent<FirstPersonCharacterControl>(player.ControlledCharacter);

                    quaternion characterRotation = SystemAPI.GetComponent<LocalTransform>(player.ControlledCharacter).Rotation;

                    // Move
                    float3 characterForward = MathUtilities.GetForwardFromRotation(characterRotation);
                    float3 characterRight = MathUtilities.GetRightFromRotation(characterRotation);
                    characterControl.MoveVector = (playerInputs.MoveInput.y * characterForward) + (playerInputs.MoveInput.x * characterRight);
                    characterControl.MoveVector = MathUtilities.ClampToMaxLength(characterControl.MoveVector, 1f);

                    characterControl.Jump = playerInputs.JumpPressed.IsSet;


                    SystemAPI.SetComponent(player.ControlledCharacter, characterControl);
                }
            }
        }
    }
}