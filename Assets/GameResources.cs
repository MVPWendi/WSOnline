using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.NetCode;

namespace Assets.Network
{
    public struct GameResources : IComponentData
    {
        public EntitySceneReference GameResourcesScene;
        public EntitySceneReference GameScene;
        public Entity PlayerGhost;
        public Entity CharacterGhost;
        public ClientServerTickRate GetClientServerTickRate()
        {
            ClientServerTickRate tickRate = new ClientServerTickRate();
            tickRate.ResolveDefaults();
            tickRate.SimulationTickRate = 60;
            tickRate.NetworkTickRate = 30;
            tickRate.MaxSimulationStepsPerFrame = 3;
            tickRate.PredictedFixedStepSimulationTickRatio = 1;
            return tickRate;
        }
    }
}
