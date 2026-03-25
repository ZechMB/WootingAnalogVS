using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using WootingAnalogSDKNET;
using AnalogMovementVS;
using Vintagestory.Client.NoObf;

namespace WootingAnalogVS
{
    public class WootingAnalogVSModSystem : ModSystem
    {
        private bool wootActivated = false;
        private ICoreClientAPI? capi;
        private AnalogMovement? am;
        private long tickListenerId = 0;
        private bool initialized = false;
        private static Config? config;
        private static bool resolverSet = false;

        
        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            tickListenerId = api.Event.RegisterGameTickListener(OnTick, 0);

            // On Linux, the P/Invoke name "native/wooting_analog_wrapper" won't resolve automatically.
            // Intercept it and load the .so from the mod's own unpacked directory using its absolute path.
            // The resolverSet guard is needed because SetDllImportResolver can only be called once per assembly per process.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !resolverSet)
            {
                resolverSet = true;
                var modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
                var soPath = Path.Combine(modDir, "native", "wooting_analog_wrapper.so");
                NativeLibrary.SetDllImportResolver(typeof(WootingAnalogSDK).Assembly, (libraryName, asm, searchPath) =>
                {
                    if (libraryName == "native/wooting_analog_wrapper")
                    {
                        try { return NativeLibrary.Load(soPath); }
                        catch (Exception ex) { Mod.Logger.Error($"[WootingLinuxFix] Failed to load wrapper: {ex.Message}"); }
                    }
                    return IntPtr.Zero;
                });
            }

            // Initialise the SDK
            try
            {
                var (numDevices, error) = WootingAnalogSDK.Initialise();
                if (numDevices >= 0)
                {
                    Mod.Logger.Notification($"Analog SDK Successfully initialised with {numDevices} devices!");
                    wootActivated = true;
                    TryToLoadConfig();
                }
                else
                {
                    Mod.Logger.Error($"Analog SDK failed to initialise: {error}");
                }
            }
            catch (Exception ex)
            {
                Mod.Logger.Error("failed wooting init: " + ex.Message);
            }
        }
        

        //can't be called until entitycontrols is set
        internal void Init()
        {
            initialized = true;
            if (capi is not null)
            {
                if (capi.World.Player.Entity.Controls is EntityControlsAMfVS amcontrols)
                {
                    if (config is not null)
                    {
                        am = new AnalogMovement(capi, config);

                        //call LoadKeyCodes() if any key is rebinded
                        ClientSettings.Inst.AddKeyCombinationUpdatedWatcher((keyName, combination) => { am.LoadKeyCodes(); });

                        am.LoadKeyCodes();
                        amcontrols.EnableKeyboardBoolMovement = false;
                        amcontrols.EnableKeyboardJumpSneakSprint = false;
                    }
                }
            }
        }

        
        internal void OnTick(float deltaTime)
        {
            if (wootActivated)
            {
                if (!initialized)
                {
                    Init();
                }
                if (capi is not null && am is not null)
                {
                    am.ConsumeInputs();
                }
            }
        }
        

        private void TryToLoadConfig()
        {
            try
            {
                if (capi is not null)
                {
                    config = capi.LoadModConfig<Config>("WootingAnalogVS.json");
                    if (config == null)
                    {
                        config = new Config();
                    }
                    capi.StoreModConfig<Config>(config, "WootingAnalogVS.json");
                }
            }
            catch (Exception e)
            {
                //Couldn't load the mod config... Create a new one with default settings, but don't save it.
                Mod.Logger.Error("Could not load config! Loading default settings instead.");
                Mod.Logger.Error(e);
                config = new Config();
            }
        }


        public override void Dispose()
        {
            capi?.Event.UnregisterGameTickListener(tickListenerId);
            if (wootActivated) WootingAnalogSDK.UnInitialise();
            base.Dispose();
        }
    }
}
