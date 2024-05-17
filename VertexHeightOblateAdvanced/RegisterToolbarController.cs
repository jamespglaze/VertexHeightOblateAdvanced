using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolbarControl_NS;
using UnityEngine;

namespace VertexHeightOblateAdvanced
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbarController : MonoBehaviour
    {
        public void Start()
        {
            try
            {
                if (ToolbarControl.RegisterMod(VertexHeightOblateAdvancedGUI.modID, VertexHeightOblateAdvancedGUI.modNAME))
                {
                    LogUtils.LogInfo("Registered VertexHeightOblateAdvanced with the toolbar controller.");
                }
                else
                {
                    LogUtils.LogWarning("Error registering VertexHeightOblateAdvanced with the toolbar controller.");
                }
            }
            catch (Exception e)
            {
                LogUtils.LogError("An Exception occurred while registering VertexHeightOblateAdvanced with the toolbar controller. Exception thrown: " + e.ToString());
            }
            Destroy(this);
        }
    }
}
