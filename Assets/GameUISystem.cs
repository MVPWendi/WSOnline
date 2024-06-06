using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;
using static ClientGameSystem;

namespace Assets.UIs
{
    public partial class GameUISystem : SystemBase
    {

        [SerializeField]
        private Button HostButton;
        [SerializeField]
        private Button JoinButton;
        [SerializeField]
        private Button MainButton;



        [SerializeField]
        private Button HostConfirm;

        [SerializeField]
        private Button JoinConfirm;

        private Button ExitButton;
        [SerializeField]
        private TMP_InputField JoinIP;
        public void SetUIReferences(MainMenu menu)
        {
            HostConfirm = menu.HostConfirm;
            JoinConfirm = menu.JoinConfirm;
            JoinIP = menu.JoinIP;
            ExitButton = menu.ExitButton;
            ExitButton.onClick.AddListener(() => OnExitClick());
            JoinConfirm.onClick.AddListener(() => OnJoinConfirmClick());
            HostConfirm.onClick.AddListener(() => OnHostConfirmClick());
        }
        private void OnExitClick()
        {
            GameManagementSystem.DisconnectRequest disconnectRequest = new GameManagementSystem.DisconnectRequest
            {
               
            };
            Entity disconnectRequestEntity = World.EntityManager.CreateEntity();
            World.EntityManager.AddComponentData(disconnectRequestEntity, disconnectRequest);
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        }

        private void OnJoinConfirmClick()
        {
            if (NetworkEndpoint.TryParse(JoinIP.text, 6666, out NetworkEndpoint newEndPoint))
            {
                GameManagementSystem.JoinRequest joinRequest = new GameManagementSystem.JoinRequest
                {
                    EndPoint = newEndPoint,
                };
                Entity joinRequestEntity = World.EntityManager.CreateEntity();
                World.EntityManager.AddComponentData(joinRequestEntity, joinRequest);
            }
            else
            {
                Debug.LogError("Unable to parse Join IP or Port fields");
            }
        }
        private void OnHostConfirmClick()
        {
            
            if (NetworkEndpoint.TryParse(GameManagementSystem.LocalHost, 6666, out NetworkEndpoint newLocalClientEndPoint))
            {
                NetworkEndpoint newServerEndPoint = NetworkEndpoint.AnyIpv4;
                newServerEndPoint.Port = 6666;
                GameManagementSystem.HostRequest hostRequest = new GameManagementSystem.HostRequest
                {
                    EndPoint = newServerEndPoint,
                };
                Entity hostRequestEntity = World.EntityManager.CreateEntity();
                World.EntityManager.AddComponentData(hostRequestEntity, hostRequest);

                // Only create local client if not in server mode
#if !UNITY_SERVER
                GameManagementSystem.JoinRequest joinRequest = new GameManagementSystem.JoinRequest
                {
                    EndPoint = newLocalClientEndPoint,
                };
                Entity joinRequestEntity = World.EntityManager.CreateEntity();
                World.EntityManager.AddComponentData(joinRequestEntity, joinRequest);
#endif
            }
            else
            {
                Debug.LogError("Unable to parse Host Port field");
            }
        }
        protected override void OnUpdate()
        {
            
        }
    }
}
