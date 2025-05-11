using System;
using System.Collections.Generic;
using System.Text;
using Sandbox;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.AI.Autopilots;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders;
using VRage.Network;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace TorchPlugin
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, 500, typeof(MyObjectBuilder_NeutralShipSpawner), null, true)]
    [StaticEventOwner]
    // MyNeutralShipSpawner 클래스는 중립 선박의 스폰 및 관리 로직을 담당합니다.
    public class MyNeutralShipSpawner : MySessionComponentBase
    {
        // CustomSpawn 메서드는 특정 스폰 그룹을 기반으로 스폰 이벤트를 처리합니다.
        // 플레이어를 인수로 받아 스폰 위치를 선택하도록 개선합니다.
        public static void CustomSpawn(object senderEvent, MySpawnGroupDefinition spawnGroup, MyPlayer selectedPlayer = null)
        {
            // 스폰 그룹의 프리팹 데이터를 다시 로드합니다.
            spawnGroup.ReloadPrefabs();

            // 스폰 옵션 초기화
            SpawningOptions spawningOptions = SpawningOptions.None;

            // 소유자 ID를 가져오지 못하면 메서드를 종료합니다.
            if (!spawnGroup.TryGetOwnerId(out var ownerId))
            {
                return;
            }

            // 스테이션 방문 가능 여부를 설정합니다.
            bool visitStationIfPossible = !spawnGroup.IsPirate && spawnGroup.EnableTradingStationVisit;

            // NPC 블록 제한을 확인하고 PCU가 부족하면 스폰을 중단합니다.
            if (!MySession.Static.NPCBlockLimits.HasRemainingPCU(spawnGroup.IsGlobalEncounter))
            {
                MySandboxGame.Log.Log(MyLogSeverity.Info, "NPC PCUs exhausted. Cargo ship will not spawn.");
                return;
            }

            // 스폰 반경 및 초기 위치 설정
            double num = 8000.0;
            Vector3D vector3D = Vector3D.Zero;

            // 월드 제한 여부 확인
            bool flag = MySession.IsWorldLimited();
            if (flag)
            {
                // 월드의 안전 반경을 계산하여 스폰 반경을 조정합니다.
                num = Math.Min(num, MySession.WorldSafeHalfExtent() - spawnGroup.SpawnRadius);
                if (num < 0.0)
                {
                    MySandboxGame.Log.WriteLine("Not enough space in the world to spawn such a huge spawn group!");
                    return;
                }
            }
            else
            {
                if (selectedPlayer != null && selectedPlayer.Character != null)
                {
                    // 선택된 플레이어의 위치를 사용합니다.
                    vector3D = selectedPlayer.GetPosition();
                    MyLog.Default.WriteLine($"SpawnCargoShip event uses selected player '{selectedPlayer.Identity?.DisplayName}'.");
                }
                else
                {
                    // 온라인 플레이어 중 랜덤으로 위치를 선택합니다.
                    using (MyUtils.ReuseCollection(ref m_playersBuffer))
                    {
                        MySession.Static.Players.GetOnlineHumanPlayers(m_playersBuffer);
                        int randomInt = MyUtils.GetRandomInt(0, m_playersBuffer.Count);
                        int num2 = 0;
                        foreach (MyPlayer item in m_playersBuffer)
                        {
                            if (num2 == randomInt)
                            {
                                if (item.Character != null)
                                {
                                    vector3D = item.GetPosition();
                                    MyLog.Default.WriteLine($"SpawnCargoShip event picks player '{item.Identity?.DisplayName}' out of {m_playersBuffer.Count} online.");
                                }
                                break;
                            }
                            num2++;
                        }
                    }
                }
            }

            // 스폰 박스를 계산하고 안전한 위치를 찾습니다.
            double num3 = 2000.0;
            BoundingBoxD spawnBox;
            if (flag)
            {
                spawnBox = new BoundingBoxD(vector3D - num, vector3D + num);
            }
            else
            {
                GetSafeBoundingBoxForPlayers(vector3D, num, out spawnBox);
                num3 += spawnBox.HalfExtents.Max() - 2000.0;
            }

            // 스폰 위치를 테스트합니다.
            Vector3D? vector3D2 = null;
            for (int i = 0; i < 10; i++)
            {
                vector3D2 = MyEntities.TestPlaceInSpace(new Vector3D?(MyUtils.GetRandomBorderPosition(ref spawnBox)).Value, spawnGroup.SpawnRadius);
                if (vector3D2.HasValue)
                {
                    break;
                }
            }

            // 유효한 위치를 찾지 못하면 이벤트를 재시도합니다.
            if (!vector3D2.HasValue)
            {
                RetryEventWithMaxTry(senderEvent as MyGlobalEventBase);
                return;
            }

            float num4 = (float)Math.Atan(num3 / (vector3D2.Value - spawnBox.Center).Length());
            Vector3 direction = -Vector3.Normalize(vector3D2.Value);
            float randomFloat = MyUtils.GetRandomFloat(num4, num4 + 0.5f);
            float randomRadian = MyUtils.GetRandomRadian();
            Vector3 vector = Vector3.CalculatePerpendicularVector(direction);
            Vector3 vector2 = Vector3.Cross(direction, vector);
            vector *= (float)(Math.Sin(randomFloat) * Math.Cos(randomRadian));
            vector2 *= (float)(Math.Sin(randomFloat) * Math.Sin(randomRadian));
            direction = direction * (float)Math.Cos(randomFloat) + vector + vector2;
            double? num5 = new RayD(vector3D2.Value, direction).Intersects(spawnBox);
            Vector3D vector3D3 = ((num5.HasValue && !(num5.Value < 10000.0)) ? ((Vector3D)(direction * (float)num5.Value)) : ((Vector3D)(direction * 10000f)));
            Vector3 up = Vector3.CalculatePerpendicularVector(direction);
            MatrixD matrix = MatrixD.CreateWorld(vector3D2.Value, direction, up);
            m_raycastHits.Clear();

            // 스폰 그룹의 각 프리팹에 대해 유효성을 검사하고 스폰을 시도합니다.
            foreach (MySpawnGroupDefinition.SpawnGroupPrefab prefab in spawnGroup.Prefabs)
            {
                MyPrefabDefinition prefabDefinition = MyDefinitionManager.Static.GetPrefabDefinition(prefab.SubtypeId);
                Vector3D vector3D4 = Vector3.Transform(prefab.Position, matrix);
                Vector3D vector3D5 = vector3D4 + vector3D3;
                float num6 = prefabDefinition?.BoundingSphere.Radius ?? 10f;
                MyLog.Default.WriteLine("Cargo ship destination: " + MyGps.PositionToPasteAbleGpsFormat(vector3D5, prefab.SubtypeId));
                if (!IsShipDestinationValid(vector3D4, vector3D5, (spawnGroup.SpawnRadius > num6) ? spawnGroup.SpawnRadius : num6))
                {
                    MyLog.Default.WriteLine("Attempted cargo ship destination is not valid.");
                    RetryEventWithMaxTry(senderEvent as MyGlobalEventBase);
                    return;
                }
            }

            // 최종적으로 프리팹을 스폰합니다.
            foreach (MySpawnGroupDefinition.SpawnGroupPrefab shipPrefab in spawnGroup.Prefabs)
            {
                Vector3D vector3D6 = Vector3D.Transform((Vector3D)shipPrefab.Position, matrix);
                Vector3D shipDestination = vector3D6 + vector3D3;
                Vector3 up2 = Vector3.CalculatePerpendicularVector(-direction);
                List<MyCubeGrid> tmpGridList = new List<MyCubeGrid>();
                Stack<Action> stack = new Stack<Action>();
                stack.Push(delegate
                {
                    InitCargoShip(shipDestination, direction, spawnBox, visitStationIfPossible, shipPrefab, tmpGridList);
                });

                // 스폰 옵션 설정
                spawningOptions |= SpawningOptions.RotateFirstCockpitTowardsDirection | SpawningOptions.SpawnRandomCargo | SpawningOptions.DisableDampeners | SpawningOptions.SetAuthorship;
                spawningOptions = (spawnGroup.RandomizedPaint ? (spawningOptions | SpawningOptions.RandomizeColor) : (spawningOptions | SpawningOptions.ReplaceColor));
                if (spawnGroup.EnableNpcResources)
                {
                    spawningOptions |= SpawningOptions.SetNpcSpawnedGrid;
                }

                MyLog.Default.WriteLine("SpawnCargoShip attempts spawning at position\n" + MyGps.PositionToPasteAbleGpsFormat(vector3D6, shipPrefab.SubtypeId));
                MyPrefabManager.Static.SpawnPrefab(tmpGridList, shipPrefab.SubtypeId, vector3D6, direction, up2, shipPrefab.Speed * direction, default(Vector3), shipPrefab.BeaconText, null, spawningOptions, ownerId, updateSync: false, stack);
            }

            // 스폰 시도 횟수를 초기화합니다.
            m_eventSpawnTry = 0;
        }
    }
}