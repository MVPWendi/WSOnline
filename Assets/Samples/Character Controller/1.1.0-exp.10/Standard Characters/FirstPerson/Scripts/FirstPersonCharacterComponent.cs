using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.CharacterController;
using Unity.NetCode;

[GhostComponent]
public struct FirstPersonCharacterComponent : IComponentData
{
    public float GroundMaxSpeed;
    public float GroundedMovementSharpness;
    public float AirAcceleration;
    public float AirMaxSpeed;
    public float AirDrag;
    public float JumpSpeed;
    public float3 Gravity;
    public bool PreventAirAccelerationAgainstUngroundedHits;
    public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling;

    public float MinViewAngle;
    public float MaxViewAngle;
    
    public Entity ViewEntity;
    public Entity ViewDestination;
    [GhostField]
    public float ViewPitchDegrees;
    public quaternion ViewLocalRotation;
}

[Serializable]
public struct FirstPersonCharacterControl : IComponentData
{
    public float3 MoveVector;
    public float2 LookDegreesDelta;
    public bool Jump;
}

[Serializable]
public struct FirstPersonCharacterView : IComponentData
{
    public Entity CharacterEntity;
}
[Serializable]
[GhostComponent()]
public struct OwningPlayer : IComponentData
{
    [GhostField()]
    public Entity Entity;
}
public struct CharacterInitialized : IComponentData
{ }
