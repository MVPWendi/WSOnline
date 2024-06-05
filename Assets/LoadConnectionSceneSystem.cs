using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine.SceneManagement;

namespace Assets
{
    public partial class LoadConnectionSceneSystem : SystemBase
    {
        protected override void OnCreate()
        {
            Enabled = false;
            if (SceneManager.GetActiveScene() == SceneManager.GetSceneByBuildIndex(0)) return;
            SceneManager.LoadScene(0);
        }

        protected override void OnUpdate()
        {
            
        }
    }
}