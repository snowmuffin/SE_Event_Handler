#define USE_HARMONY

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using HarmonyLib;
using Sandbox;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Shared.Config;
using Shared.Logging;
using Shared.Patches;
using Shared.Plugin;
using SpaceEngineers.Game.EntityComponents.GameLogic;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using Torch.Utils;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Network;
using VRage.Utils;
using System.Collections.Concurrent;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities.Planet;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using SpaceEngineers.Game.SessionComponents;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Components.Session;
using VRage.Game.ModAPI;
using VRage.Serialization;
using VRage.Voxels;
using VRageMath;

namespace TorchPlugin
{
    // Torch 플러그인 기본 클래스, WPF 플러그인, 공통 플러그인 인터페이스 구현
    public class Plugin : TorchPluginBase, IWpfPlugin, ICommonPlugin
    {
        public const string PluginName = "EventHandler"; // 플러그인 이름 상수
        public static Plugin Instance { get; private set; } // 싱글턴 인스턴스

        public long Tick { get; private set; } // 틱 카운터

        public IPluginLogger Log => Logger; // 로거 프로퍼티
        private static readonly IPluginLogger Logger = new PluginLogger(PluginName); // 정적 로거 인스턴스

        public IPluginConfig Config => config?.Data; // 설정 데이터 접근 프로퍼티
        private PersistentConfig<PluginConfig> config; // 설정 파일 관리 객체
        private static readonly string ConfigFileName = $"{PluginName}.cfg"; // 설정 파일 이름
	    private static readonly short ONE_MINUTE = 60; // 1분(초 단위)

        public UserControl GetControl() => control ?? (control = new ConfigView()); // WPF 설정창 반환
        private ConfigView control; // 설정창 컨트롤

        private TorchSessionManager sessionManager; // 세션 매니저

        private bool initialized; // 초기화 여부
        private bool failed; // 실패 여부

        private readonly Commands commands = new Commands(); // 명령어 객체

        private CustomInstance _customInstance; // 커스텀 인스턴스

        private long _lastResetTick = 0; // 마지막 리셋 틱
        private const long ResetIntervalTicks = 36000; // 리셋 주기(틱 단위)

        // 플러그인 초기화 메서드
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

#if DEBUG
            // 디버깅 시 디버거 연결 대기
            Thread.Sleep(100);
#endif

            Instance = this;

            Log.Info("Init");

            // 설정 파일 로드
            var configPath = Path.Combine(StoragePath, ConfigFileName);
            config = PersistentConfig<PluginConfig>.Load(Log, configPath);

            // 게임 버전 정보 획득 및 공통 설정
            var gameVersionNumber = MyPerGameSettings.BasicGameInfo.GameVersion ?? 0;
            var gameVersion = new StringBuilder(MyBuildNumbers.ConvertBuildNumberFromIntToString(gameVersionNumber)).ToString();
            Common.SetPlugin(this, gameVersion, StoragePath);

#if USE_HARMONY
            // Harmony 패치 적용
            if (!PatchHelpers.HarmonyPatchAll(Log, new Harmony(Name)))
            {
                failed = true;
                return;
            }
#endif

            // 세션 매니저 구독
            sessionManager = torch.Managers.GetManager<TorchSessionManager>();
            sessionManager.SessionStateChanged += SessionStateChanged;

            // 커스텀 인스턴스 싱글턴 초기화 및 시작
            _customInstance = CustomInstance.GetInstance();
            _customInstance.Start();

            // 예시 커뮤니케이션
            _customInstance.Communicate("Plugin initialized.");

            initialized = true;
        }

        // 세션 상태 변경 이벤트 핸들러
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

        // 플러그인 해제 및 리소스 정리
        public override void Dispose()
        {
            if (initialized)
            {
                Log.Debug("Disposing");

                sessionManager.SessionStateChanged -= SessionStateChanged;
                sessionManager = null;

                // 커스텀 인스턴스 정지
                _customInstance?.Stop();
                _customInstance = null;

                Log.Debug("Disposed");
            }

            Instance = null;

            base.Dispose();
        }

        // 모든 스테이션의 StationEntityId를 0으로 리셋하는 함수
        public void ResetStationEntityIds()
        {
            try
            {
                // 세션 및 팩션 데이터 확인
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
                        if (stationEntity != null)
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
                // 스테이션 정보 갱신
                var economyComponent = MySession.Static.GetComponent<MySessionComponentEconomy>();
                if (economyComponent != null)
                {
                    var updateStationsMethod = typeof(MySessionComponentEconomy).GetMethod("UpdateStations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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
            catch (Exception ex)
            {
                Log.Error($"Exception in ResetStationEntityIds: {ex}");
            }
        }

        // 리플렉션을 통한 스테이션 딕셔너리 접근자
        [ReflectedGetter(Name = "m_stations", Type = typeof(MyFaction))]
        private static Func<MyFaction, Dictionary<long, MyStation>> _stations;

        // 리플렉션을 통한 MyGlobalEncountersGenerator의 필드 접근자들
        [ReflectedGetter(Name = "m_secondTimer", Type = typeof(MyGlobalEncountersGenerator))]
        private static Func<MyGlobalEncountersGenerator, int> _secondTimer;

        [ReflectedGetter(Name = "m_minuteTimer", Type = typeof(MyGlobalEncountersGenerator))]
        private static Func<MyGlobalEncountersGenerator, int> _minuteTimer;

        [ReflectedGetter(Name = "m_currentSpawnTimer", Type = typeof(MyGlobalEncountersGenerator))]
        private static Func<MyGlobalEncountersGenerator, int> _currentSpawnTimer;

        [ReflectedGetter(Name = "m_activeEncounters", Type = typeof(MyGlobalEncountersGenerator))]
        private static Func<MyGlobalEncountersGenerator, int> _activeEncounters;

        [ReflectedGetter(Name = "m_selectedSpawnGroup", Type = typeof(MyGlobalEncountersGenerator))]
        private static Func<MyGlobalEncountersGenerator, string> _selectedSpawnGroup;

        [ReflectedGetter(Name = "m_spawnGroups", Type = typeof(MyGlobalEncountersGenerator))]
        private static Func<MyGlobalEncountersGenerator, MySpawnGroupDefinition[]> _spawnGroups;

        [ReflectedGetter(Name = "m_spawnGroupTotalFrequencies", Type = typeof(MyGlobalEncountersGenerator))]
        private static Func<MyGlobalEncountersGenerator, float> _spawnGroupTotalFrequencies;

        [ReflectedGetter(Name = "m_spawnGroupCumulativeFrequencies", Type = typeof(MyGlobalEncountersGenerator))]
        private static Func<MyGlobalEncountersGenerator, List<float>> _spawnGroupCumulativeFrequencies;

        [ReflectedGetter(Name = "m_encounterComponents", Type = typeof(MyGlobalEncountersGenerator))]
        private static Func<MyGlobalEncountersGenerator, ConcurrentDictionary<long, HashSet<MyGlobalEncounterComponent>>> _encounterComponentsDict;

        [ReflectedGetter(Name = "m_encountersGps", Type = typeof(MyGlobalEncountersGenerator))]
        private static Func<MyGlobalEncountersGenerator, ConcurrentDictionary<long, long>> _encountersGps;

        [ReflectedGetter(Name = "m_encountersTimer", Type = typeof(MyGlobalEncountersGenerator))]
        private static Func<MyGlobalEncountersGenerator, ConcurrentDictionary<long, int>> _encountersTimer;

        [ReflectedGetter(Name = "m_spawnQueue", Type = typeof(MyGlobalEncountersGenerator))]
        private static Func<MyGlobalEncountersGenerator, Queue<MyEncounterSpawnInfo>> _spawnQueue;

        [ReflectedGetter(Name = "m_definition", Type = typeof(MyGlobalEncountersGenerator))]
        private static Func<MyGlobalEncountersGenerator, MyGlobalEncountersGeneratorDefinition> _definition;

        // 리플렉션 Setter 추가
        [ReflectedSetter(Name = "m_secondTimer", Type = typeof(MyGlobalEncountersGenerator))]
        private static Action<MyGlobalEncountersGenerator, int> _setSecondTimer;
        [ReflectedSetter(Name = "m_minuteTimer", Type = typeof(MyGlobalEncountersGenerator))]
        private static Action<MyGlobalEncountersGenerator, int> _setMinuteTimer;
        [ReflectedSetter(Name = "m_currentSpawnTimer", Type = typeof(MyGlobalEncountersGenerator))]
        private static Action<MyGlobalEncountersGenerator, int> _setCurrentSpawnTimer;
        [ReflectedSetter(Name = "m_activeEncounters", Type = typeof(MyGlobalEncountersGenerator))]
        private static Action<MyGlobalEncountersGenerator, int> _setActiveEncounters;
        [ReflectedSetter(Name = "m_selectedSpawnGroup", Type = typeof(MyGlobalEncountersGenerator))]
        private static Action<MyGlobalEncountersGenerator, string> _setSelectedSpawnGroup;

        // 각 필드에 접근하는 프로퍼티들 (get/set)
        private static MyGlobalEncountersGenerator EncGen => MySession.Static?.GetComponent<MyGlobalEncountersGenerator>();
        private static int m_secondTimer { get => _secondTimer(EncGen); set => _setSecondTimer(EncGen, value); }
        private static int m_minuteTimer { get => _minuteTimer(EncGen); set => _setMinuteTimer(EncGen, value); }
        private static int m_currentSpawnTimer { get => _currentSpawnTimer(EncGen); set => _setCurrentSpawnTimer(EncGen, value); }
        private static int m_activeEncounters { get => _activeEncounters(EncGen); set => _setActiveEncounters(EncGen, value); }
        private static string m_selectedSpawnGroup { get => _selectedSpawnGroup(EncGen); set => _setSelectedSpawnGroup(EncGen, value); }
        private static MySpawnGroupDefinition[] m_spawnGroups => _spawnGroups(MySession.Static?.GetComponent<MyGlobalEncountersGenerator>());
        private static float m_spawnGroupTotalFrequencies => _spawnGroupTotalFrequencies(MySession.Static?.GetComponent<MyGlobalEncountersGenerator>());
        private static List<float> m_spawnGroupCumulativeFrequencies => _spawnGroupCumulativeFrequencies(MySession.Static?.GetComponent<MyGlobalEncountersGenerator>());
        private static ConcurrentDictionary<long, HashSet<MyGlobalEncounterComponent>> m_encounterComponents => _encounterComponentsDict(MySession.Static?.GetComponent<MyGlobalEncountersGenerator>());
        private static ConcurrentDictionary<long, long> m_encountersGps => _encountersGps(MySession.Static?.GetComponent<MyGlobalEncountersGenerator>());
        private static ConcurrentDictionary<long, int> m_encountersTimer => _encountersTimer(MySession.Static?.GetComponent<MyGlobalEncountersGenerator>());
        private static Queue<MyEncounterSpawnInfo> m_spawnQueue => _spawnQueue(MySession.Static?.GetComponent<MyGlobalEncountersGenerator>());
        private static MyGlobalEncountersGeneratorDefinition m_definition => _definition(MySession.Static?.GetComponent<MyGlobalEncountersGenerator>());

        // 매 프레임마다 호출되는 업데이트 함수
        public override void Update()
        {
            if (failed)
                return;

            try
            {
                CustomUpdate();
                Tick++;
                // 일정 틱마다 스테이션 엔티티 ID 리셋
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

        private struct MyEncounterSpawnInfo
        {
            public long EncounterId; // 인카운터 고유 ID
            public long OwnerId; // 소유자 ID
            public MySpawnGroupDefinition SpawnGroup; // 스폰 그룹 정의
            public Vector3D Position; // 스폰 위치
        }




        private void GlobalEncounterCleanup()
        {
            try
            {
                // 활성화된 인카운터가 없으면 종료
                if (EncGen == null || m_encounterComponents.Count == 0)
                    return;
                if (MySession.Static.Settings.GlobalEncounterCap <= m_activeEncounters ||!MySession.Static.NPCBlockLimits.HasRemainingPCU(true))
                {
                    // 활성화된 인카운터를 순회하며 GPS 업데이트 및 제거
                    foreach (var kv in m_encountersTimer)
                    {
                        long encounterId = kv.Key;
                        if (kv.Value <= 60)
                        {
                            RemoveGlobalEncounter(encounterId);
                            return;
                        }
                    }
                    
                }
                return;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception in GlobalEncounterCleanup: {ex}");
            }
        }

        // 커스텀 업데이트(패치 업데이트 등)
        private void CustomUpdate()
        {
            // TODO: 여기에 매 프레임마다 실행할 코드를 작성하세요.
            PatchHelpers.PatchUpdates();
        }


        [ReflectedMethod(Name = "RemoveGlobalEncounter", Type = typeof(MyGlobalEncountersGenerator))]
        private static Action<MyGlobalEncountersGenerator, long> _removeGlobalEncounter;
        private static void RemoveGlobalEncounter(long id) => _removeGlobalEncounter(EncGen, id);




    }
}