using AnalogMovementVS;
using Vintagestory.API.Client;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;
using WootingAnalogSDKNET;

namespace WootingAnalogVS
{
    internal class AnalogMovement
    {
        readonly ICoreClientAPI capi;
        readonly Config config;
        internal AnalogMovement(ICoreClientAPI capi, Config config)
        {
            this.capi = capi;
            this.config = config;
        }

        ushort ForwardKey = 0;
        ushort BackwardKey = 0;
        ushort LeftKey = 0;
        ushort RightKey = 0;

        float forward = 0;
        float backward = 0;
        float left = 0;
        float right = 0;


        internal void ConsumeInputs()
        {
            if (capi.World.Player.Entity.Controls is EntityControlsAMfVS am)
            {
                //get key values
                if (ForwardKey != 0)
                {
                    forward = WootingAnalogSDK.ReadAnalog(ForwardKey).Item1;
                }
                if (BackwardKey != 0)
                {
                    backward = WootingAnalogSDK.ReadAnalog(BackwardKey).Item1;
                }
                if (LeftKey != 0)
                {
                    left = WootingAnalogSDK.ReadAnalog(LeftKey).Item1;
                }
                if (RightKey != 0)
                {
                    right = WootingAnalogSDK.ReadAnalog(RightKey).Item1;
                }

                //set movement
                if (config.autosprint && !am.IsMounted) AutoSprint(am);
                else
                {
                    am.amForwardBackward = forward + -backward;
                    am.amLeftRight = left + -right;
                    am.amSprint = capi.Input.IsHotKeyPressed("sprint") || (am.Sprint && am.TriesToMove && ClientSettings.ToggleSprint && am.IsMouseGrabbed);
                }

                //set jump & sneak
                var player = capi.World.Player;
                am.amJump = capi.Input.IsHotKeyPressed("jump") && (player.Entity.PrevFrameCanStandUp || player.WorldData.NoClip) && am.IsMouseGrabbed;
                am.amSneak = capi.Input.IsHotKeyPressed("sneak") && am.IsMouseGrabbed;
            }
        }


        void AutoSprint(EntityControlsAMfVS am)
        {
            //calculate inputs
            float forwardback = forward + -backward;
            float leftright = left + -right;
            bool ShouldSprint = false;
            if (left > 0.5f || right > 0.5f || forward > 0.5f || backward > 0.5f)
            {
                ShouldSprint = true;
            }
            if ((forward <= 0.5f || backward <= 0.5f) && ShouldSprint == false)
            {
                forwardback *= 2f;
            }
            if ((left <= 0.5f || right <= 0.5f) && ShouldSprint == false)
            {
                leftright *= 2f;
            }
            
            if (config.ReverseSprint && capi.Input.IsHotKeyPressed("sprint"))
            {
                ShouldSprint = false;
            }

            //output controls
            am.amForwardBackward = forwardback;
            am.amLeftRight = leftright;
            am.amSprint = ShouldSprint;
        }


        internal void LoadKeyCodes()
        {
            //get game key mapping(glkey), then convert to usbhid
            ForwardKey = UsbHidCodes.GlKeysToUsbHid[ScreenManager.hotkeyManager.HotKeys["walkforward"].CurrentMapping.KeyCode];
            BackwardKey = UsbHidCodes.GlKeysToUsbHid[ScreenManager.hotkeyManager.HotKeys["walkbackward"].CurrentMapping.KeyCode];
            LeftKey = UsbHidCodes.GlKeysToUsbHid[ScreenManager.hotkeyManager.HotKeys["walkleft"].CurrentMapping.KeyCode];
            RightKey = UsbHidCodes.GlKeysToUsbHid[ScreenManager.hotkeyManager.HotKeys["walkright"].CurrentMapping.KeyCode];
        }
    }
}
