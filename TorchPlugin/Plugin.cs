#define USE_HARMONY

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using HarmonyLib;
using Sandbox.Game;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Shared.Config;
using Shared.Logging;
using Shared.Patches;
using Shared.Plugin;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using Torch.Utils;
using VRage.ModAPI;
using VRage.Utils;

namespace TorchPlugin
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Plugin : TorchPluginBase, IWpfPlugin, ICommonPlugin
    {
        public const string PluginName = "EventHandler";
        public static Plugin Instance { get; private set; }

        public long Tick { get; private set; }

        public IPluginLogger Log => Logger;
        private static readonly IPluginLogger Logger = new PluginLogger(PluginName);

        public IPluginConfig Config => config?.Data;
        private PersistentConfig<PluginConfig> config;
        private static readonly string ConfigFileName = $"{PluginName}.cfg";

        // ReSharper disable once UnusedMember.Global
        public UserControl GetControl() => control ?? (control = new ConfigView());
        private ConfigView control;

        private TorchSessionManager sessionManager;

        private bool initialized;
        private bool failed;

        // ReSharper disable once UnusedMember.Local
        private readonly Commands commands = new Commands();

        private CustomInstance _customInstance;

        private long _lastResetTick = 0;
        private const long ResetIntervalTicks = 36000;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

#if DEBUG
            // Allow the debugger some time to connect once the plugin assembly is loaded
            Thread.Sleep(100);
#endif

            Instance = this;

            Log.Info("Init");

            var configPath = Path.Combine(StoragePath, ConfigFileName);
            config = PersistentConfig<PluginConfig>.Load(Log, configPath);

            var gameVersionNumber = MyPerGameSettings.BasicGameInfo.GameVersion ?? 0;
            var gameVersion = new StringBuilder(MyBuildNumbers.ConvertBuildNumberFromIntToString(gameVersionNumber)).ToString();
            Common.SetPlugin(this, gameVersion, StoragePath);

#if USE_HARMONY
            if (!PatchHelpers.HarmonyPatchAll(Log, new Harmony(Name)))
            {
                failed = true;
                return;
            }
#endif

            sessionManager = torch.Managers.GetManager<TorchSessionManager>();
            sessionManager.SessionStateChanged += SessionStateChanged;

            // Initialize CustomInstance as a singleton
            _customInstance = CustomInstance.GetInstance();
            _customInstance.Start();

            // Example communication
            _customInstance.Communicate("Plugin initialized.");

            initialized = true;
        }

        private void SessionStateChanged(ITorchSession session, TorchSessionState newstate)
        {
            switch (newstate)
            {
                case TorchSessionState.Loading:
                    Log.Debug("Loading");
                    break;

                case TorchSessionState.Loaded:
                    Log.Debug("Loaded");
                    break;

                case TorchSessionState.Unloading:
                    Log.Debug("Unloading");
                    break;

                case TorchSessionState.Unloaded:
                    Log.Debug("Unloaded");
                    break;
            }
        }

        public override void Dispose()
        {
            if (initialized)
            {
                Log.Debug("Disposing");

                sessionManager.SessionStateChanged -= SessionStateChanged;
                sessionManager = null;

                // Ensure CustomInstance is stopped during plugin disposal
                _customInstance?.Stop();
                _customInstance = null;

                Log.Debug("Disposed");
            }

            Instance = null;

            base.Dispose();
        }
        public void ResetStationEntityIds()
        {
            if (MySession.Static?.Factions == null)
            {
                Log.Info("Stations data is not available. Ensure the game session is running.");
                return;
            }
            var myEntitiesInstance = MyAPIGateway.Entities;
            var factions = MySession.Static.Factions.Factions;
            Log.Info("Resetting StationEntityId to 0 for all stations in the current game session:");
            foreach (var faction in factions)
            {
                var factionData = faction.Value;
                Log.Info($"Processing faction: {factionData.Tag}");
                var stations = _stations((MyFaction)factionData);
                if (stations == null || stations.Count == 0)
                {
                    continue;
                }
                
                foreach (var station in stations)
                {
                    long stationEntitiyId = station.Value.StationEntityId;
                    IMyEntity stationEntity = myEntitiesInstance.GetEntityById(stationEntitiyId);
                    if(stationEntity != null)
                    {
                        Log.Info($"Station entity with ID {stationEntitiyId} exist.");
                        continue;
                    }
                    else
                    {
                        var stationobjectbuilder = station.Value.GetObjectBuilder();
                        var station_safezone_entityid = stationobjectbuilder.SafeZoneEntityId;
                        
                        IMyEntity safezoneEntity = myEntitiesInstance.GetEntityById(station_safezone_entityid);
                        stationEntity?.Close();
                        safezoneEntity?.Close();
                        station.Value.StationEntityId = 0;
                    }
                }
            }

            var economyComponent = MySession.Static.GetComponent<MySessionComponentEconomy>();
            if (economyComponent != null)
            {
                var updateStationsMethod = typeof(MySessionComponentEconomy).GetMethod("UpdateStations", System.Reflection.BindingFlags.NonPublic | BindingFlags.Instance);
                if (updateStationsMethod != null)
                {
                    updateStationsMethod.Invoke(economyComponent, null);
                    Log.Info("UpdateStations method invoked successfully.");
                }
                else
                {
                    Log.Info("Failed to find the UpdateStations method.");
                }
            }
            else
            {
                Log.Info("MySessionComponentEconomy component is not available.");
            }
        }
        [ReflectedGetter(Name = "m_stations", Type = typeof(MyFaction))]
        private static Func<MyFaction, Dictionary<long, MyStation>> _stations;
        public override void Update()
        {
            if (failed)
                return;

            try
            {
                CustomUpdate();
                Tick++;
                if (Tick - _lastResetTick >= ResetIntervalTicks)
                {
                    ResetStationEntityIds();
                    _lastResetTick = Tick;
                }
            }
            catch (Exception e)
            {
                Log.Critical(e, "Update failed");
                failed = true;
            }
        }

        private void CustomUpdate()
        {
            // TODO: Put your update processing here. It is called on every simulation frame!
            PatchHelpers.PatchUpdates();
        }
    }
}