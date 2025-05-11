using System;
using System.Collections.Generic;
using System.Reflection;
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
    public class MyNeutralShipSpawner : MySessionComponentBase
    {

        public static void CustomSpawn(object senderEvent, MySpawnGroupDefinition spawnGroup, MyPlayer selectedPlayer = null)
        {
            spawnGroup.ReloadPrefabs();

            SpawningOptions spawningOptions = SpawningOptions.None;

            if (!spawnGroup.TryGetOwnerId(out var ownerId))
            {
                return;
            }

            bool visitStationIfPossible = !spawnGroup.IsPirate && spawnGroup.EnableTradingStationVisit;

            if (!MySession.Static.NPCBlockLimits.HasRemainingPCU(spawnGroup.IsGlobalEncounter))
            {
                MySandboxGame.Log.Log(MyLogSeverity.Info, "NPC PCUs exhausted. Cargo ship will not spawn.");
                return;
            }

            double num = 8000.0;
            Vector3D vector3D = Vector3D.Zero;

            bool flag = MySession.IsWorldLimited();
            if (flag)
            {
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
                    vector3D = selectedPlayer.GetPosition();
                    MyLog.Default.WriteLine($"SpawnCargoShip event uses selected player '{selectedPlayer.Identity?.DisplayName}'.");
                }
                else
                {
                    Type m_playersBufferType = typeof(MyNeutralShipSpawner);

                    FieldInfo m_playersBufferfield = m_playersBufferType.GetField("m_playersBuffer", BindingFlags.NonPublic | BindingFlags.Static);
                    if (m_playersBufferfield == null)
                    {
                        MyLog.Default.Error("m_playersBuffer field not found.");
                        return;
                    }

                    List<MyPlayer> m_playersBuffer = m_playersBufferfield.GetValue(null) as List<MyPlayer>;
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

            double num3 = 2000.0;
            BoundingBoxD spawnBox;
            Type spawnerType = typeof(MyNeutralShipSpawner);
            MethodInfo method = spawnerType.GetMethod("GetSafeBoundingBoxForPlayers", BindingFlags.NonPublic | BindingFlags.Static);
            if (flag)
            {
                spawnBox = new BoundingBoxD(vector3D - num, vector3D + num);
            }
            else
            {

                if (method == null)
                {
                    throw new Exception("GetSafeBoundingBoxForPlayers 메서드를 찾을 수 없습니다.");
                }

                object[] parameters = new object[] { vector3D, num, null };

                method.Invoke(null, parameters);
                spawnBox = (BoundingBoxD)parameters[2];
                num3 += spawnBox.HalfExtents.Max() - 2000.0;
            }

            Vector3D? vector3D2 = null;
            for (int i = 0; i < 10; i++)
            {
                vector3D2 = MyEntities.TestPlaceInSpace(new Vector3D?(MyUtils.GetRandomBorderPosition(ref spawnBox)).Value, spawnGroup.SpawnRadius);
                if (vector3D2.HasValue)
                {
                    break;
                }
            }

            if (!vector3D2.HasValue)
            {
                Type retryEventType = typeof(MyNeutralShipSpawner);
                MethodInfo retryEventMethod = retryEventType.GetMethod("RetryEventWithMaxTry", BindingFlags.NonPublic | BindingFlags.Static);
                retryEventMethod.Invoke(null, new object[] { senderEvent as MyGlobalEventBase });
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
            Type valueType = typeof(MyNeutralShipSpawner);
            FieldInfo field = valueType.GetField("m_raycastHits", BindingFlags.NonPublic | BindingFlags.Static);

            (field.GetValue(null) as List<MyPhysics.HitInfo>).Clear();

            foreach (MySpawnGroupDefinition.SpawnGroupPrefab prefab in spawnGroup.Prefabs)
            {
                MyPrefabDefinition prefabDefinition = MyDefinitionManager.Static.GetPrefabDefinition(prefab.SubtypeId);
                Vector3D vector3D4 = Vector3.Transform(prefab.Position, matrix);
                Vector3D vector3D5 = vector3D4 + vector3D3;
                float num6 = prefabDefinition?.BoundingSphere.Radius ?? 10f;
                MyLog.Default.WriteLine("Cargo ship destination: " + MyGps.PositionToPasteAbleGpsFormat(vector3D5, prefab.SubtypeId));
                Type IsShipDestinationValidType = typeof(MyNeutralShipSpawner);
                MethodInfo IsShipDestinationValidmethod = IsShipDestinationValidType.GetMethod("IsShipDestinationValid", BindingFlags.NonPublic | BindingFlags.Static);

				
                if (!(bool)IsShipDestinationValidmethod.Invoke(null, new object[] { vector3D4, vector3D5, (spawnGroup.SpawnRadius > num6) ? spawnGroup.SpawnRadius : num6 }))
                {
                    MyLog.Default.WriteLine("Attempted cargo ship destination is not valid.");
                    Type retryEventType = typeof(MyNeutralShipSpawner);
                    MethodInfo retryEventMethod = retryEventType.GetMethod("RetryEventWithMaxTry", BindingFlags.NonPublic | BindingFlags.Static);
                    retryEventMethod.Invoke(null, new object[] { senderEvent as MyGlobalEventBase });
                    return;
                }
            }

            foreach (MySpawnGroupDefinition.SpawnGroupPrefab shipPrefab in spawnGroup.Prefabs)
            {
                Vector3D vector3D6 = Vector3D.Transform((Vector3D)shipPrefab.Position, matrix);
                Vector3D shipDestination = vector3D6 + vector3D3;
                Vector3 up2 = Vector3.CalculatePerpendicularVector(-direction);
                List<MyCubeGrid> tmpGridList = new List<MyCubeGrid>();
                Stack<Action> stack = new Stack<Action>();
                Type methodType = typeof(MyNeutralShipSpawner);
                MethodInfo InitCargoShipmethod = methodType.GetMethod("InitCargoShip", BindingFlags.NonPublic | BindingFlags.Static);

                stack.Push(delegate
                {
                    if (InitCargoShipmethod != null)
                    {
                        object[] parameters = new object[]
                        {
                            shipDestination,
                            direction,
                            spawnBox,
                            visitStationIfPossible,
                            shipPrefab,
                            tmpGridList
                        };

                        InitCargoShipmethod.Invoke(null, parameters);
                    }
                    else
                    {
                        MyLog.Default.Error("InitCargoShip method not found.");
                    }
                });

                spawningOptions |= SpawningOptions.RotateFirstCockpitTowardsDirection | SpawningOptions.SpawnRandomCargo | SpawningOptions.DisableDampeners | SpawningOptions.SetAuthorship;
                spawningOptions = (spawnGroup.RandomizedPaint ? (spawningOptions | SpawningOptions.RandomizeColor) : (spawningOptions | SpawningOptions.ReplaceColor));
                if (spawnGroup.EnableNpcResources)
                {
                    spawningOptions |= SpawningOptions.SetNpcSpawnedGrid;
                }

                MyLog.Default.WriteLine("SpawnCargoShip attempts spawning at position\n" + MyGps.PositionToPasteAbleGpsFormat(vector3D6, shipPrefab.SubtypeId));
                MyPrefabManager.Static.SpawnPrefab(tmpGridList, shipPrefab.SubtypeId, vector3D6, direction, up2, shipPrefab.Speed * direction, default(Vector3), shipPrefab.BeaconText, null, spawningOptions, ownerId, updateSync: false, stack);
            }
            Type m_eventSpawnTryType = typeof(MyNeutralShipSpawner);

            FieldInfo m_eventSpawnTryfield = m_eventSpawnTryType.GetField("m_playersBuffer", BindingFlags.NonPublic | BindingFlags.Static);
            if (m_eventSpawnTryfield == null)
            {
                MyLog.Default.Error("m_playersBuffer field not found.");
                return;
            }

            int m_eventSpawnTry = (int)m_eventSpawnTryfield.GetValue(null);
            m_eventSpawnTry = 0;
        }
    }
}