using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent]
public struct FirstPersonPlayer : IComponentData
{
    [GhostField]
    public Entity ControlledCharacter;
}
[Serializable]
public struct FirstPersonPlayerInputs : IInputComponentData
{
    public float2 MoveInput;
    public float2 LookInput;
    public InputEvent JumpPressed;
    public InputEvent InteractPressed;
}