using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.NetCode;

namespace Assets.Samples
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class PlayerInputSystem : SystemBase
    {
        private WSInputs inputs;


        protected override void OnCreate()
        {
            RequireForUpdate<NetworkTime>();
            inputs = new WSInputs();
            inputs.Enable();
            inputs.CharacterControl.Enable();
        }

        protected override void OnUpdate()
        {

        }
    }
}
