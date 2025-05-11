// Warning: Some assembly references could not be resolved automatically. This might lead to incorrect decompilation of some parts,
// for ex. property getter/setter access. To get optimal decompilation results, please manually add the missing references to the list of loaded assemblies.
// Sandbox.Game, Version=0.1.1.0, Culture=neutral, PublicKeyToken=null
// Sandbox.Game.AI.Autopilots.MySimpleAutopilot
using System;
using System.Linq;
using Sandbox;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.AI.Autopilots;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using VRage.Groups;
using VRage.ObjectBuilders.Private;
using VRageMath;
using VRageRender;
namespace TorchPlugin
{
    [MyAutopilotType(typeof(MyObjectBuilder_SimpleAutopilot))]
    public class MySimpleAutopilot : MyAutopilotBase
    {
        private const int SHIP_LIFESPAN_MILLISECONDS = 1800000;

        private int m_spawnTime;

        private long[] m_gridIds;

        private Vector3 m_direction;

        private Vector3D m_destination;

        private int m_subgridLookupCounter = -1;

        public MySimpleAutopilot()
        {
        }

        public MySimpleAutopilot(Vector3D destination, Vector3 direction, long[] gridsIds)
        {
            m_gridIds = gridsIds;
            m_direction = direction;
            m_destination = destination;
            m_spawnTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        public override MyObjectBuilder_AutopilotBase GetObjectBuilder()
        {
            MyObjectBuilder_SimpleAutopilot myObjectBuilder_SimpleAutopilot = MyObjectBuilderSerializerKeen.CreateNewObject<MyObjectBuilder_SimpleAutopilot>();
            myObjectBuilder_SimpleAutopilot.Destination = m_destination;
            myObjectBuilder_SimpleAutopilot.Direction = m_direction;
            myObjectBuilder_SimpleAutopilot.SpawnTime = m_spawnTime;
            myObjectBuilder_SimpleAutopilot.GridIds = m_gridIds;
            return myObjectBuilder_SimpleAutopilot;
        }

        public override void Init(MyObjectBuilder_AutopilotBase objectBuilder)
        {
            MyObjectBuilder_SimpleAutopilot myObjectBuilder_SimpleAutopilot = (MyObjectBuilder_SimpleAutopilot)objectBuilder;
            m_gridIds = myObjectBuilder_SimpleAutopilot.GridIds;
            m_direction = myObjectBuilder_SimpleAutopilot.Direction;
            m_destination = myObjectBuilder_SimpleAutopilot.Destination;
            m_spawnTime = myObjectBuilder_SimpleAutopilot.SpawnTime ?? MySandboxGame.TotalGamePlayTimeInMilliseconds;
            if (m_gridIds == null)
            {
                m_subgridLookupCounter = 100;
            }
        }

        public override void OnAttachedToShipController(MyCockpit newShipController)
        {
            base.OnAttachedToShipController(newShipController);
            if (m_subgridLookupCounter <= 0)
            {
                RegisterGridCallbacks();
            }
        }

        private void RegisterGridCallbacks()
        {
            if (Sync.IsServer)
            {
                ForEachGrid(delegate (MyCubeGrid grid)
                {
                    grid.OnGridChanged += OnGridChanged;
                    grid.OnBlockAdded += OnBlockAddedRemovedOrChanged;
                    grid.OnBlockRemoved += OnBlockAddedRemovedOrChanged;
                    grid.OnBlockIntegrityChanged += OnBlockAddedRemovedOrChanged;
                });
            }
        }

        private void OnBlockAddedRemovedOrChanged(MySlimBlock obj)
        {
            PersistShip();
        }

        private void OnGridChanged(MyCubeGrid grid)
        {
            PersistShip();
        }

        private void PersistShip()
        {
            base.ShipController.RemoveAutopilot();
        }

        public override void OnRemovedFromCockpit()
        {
            if (Sync.IsServer)
            {
                ForEachGrid(delegate (MyCubeGrid grid)
                {
                    grid.OnGridChanged -= OnGridChanged;
                    grid.OnBlockAdded -= OnBlockAddedRemovedOrChanged;
                    grid.OnBlockRemoved -= OnBlockAddedRemovedOrChanged;
                    grid.OnBlockIntegrityChanged -= OnBlockAddedRemovedOrChanged;
                });
            }
            base.OnRemovedFromCockpit();
        }

        public override void Update()
        {
            if (!Sync.IsServer)
            {
                return;
            }
            if (m_subgridLookupCounter > 0 && --m_subgridLookupCounter == 0)
            {
                MyCubeGrid cubeGrid = base.ShipController.CubeGrid;
                MyGroups<MyCubeGrid, MyGridLogicalGroupData>.Group group = MyCubeGridGroups.Static.Logical.GetGroup(cubeGrid);
                m_gridIds = group.Nodes.Select((MyGroups<MyCubeGrid, MyGridLogicalGroupData>.Node x) => x.NodeData.EntityId).ToArray();
                RegisterGridCallbacks();
            }
            MyCockpit shipController = base.ShipController;
            if (shipController != null && (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_spawnTime > 1800000 || (shipController.PositionComp.GetPosition() - m_destination).Dot(m_direction) > 0.0) && !IsPlayerNearby())
            {
                base.ShipController.RemoveAutopilot();
                ForEachGrid(delegate (MyCubeGrid grid)
                {
                    grid.Close();
                });
            }
        }

        private bool IsPlayerNearby()
        {
            BoundingSphereD boundingSphereD = new BoundingSphereD(base.ShipController.PositionComp.GetPosition(), 2000.0);
            foreach (MyPlayer onlinePlayer in Sync.Players.GetOnlinePlayers())
            {
                if (boundingSphereD.Contains(onlinePlayer.GetPosition()) == ContainmentType.Contains)
                {
                    return true;
                }
            }
            return false;
        }

        private void ForEachGrid(Action<MyCubeGrid> action)
        {
            if (m_gridIds == null || m_gridIds.Length == 0)
            {
                return;
            }
            long[] gridIds = m_gridIds;
            for (int i = 0; i < gridIds.Length; i++)
            {
                MyCubeGrid myCubeGrid = (MyCubeGrid)MyEntities.GetEntityById(gridIds[i]);
                if (myCubeGrid != null)
                {
                    action(myCubeGrid);
                }
            }
        }

        public override void DebugDraw()
        {
            if (MyDebugDrawSettings.DEBUG_DRAW_NEUTRAL_SHIPS && base.ShipController != null)
            {
                Vector3D position = MySector.MainCamera.Position;
                Vector3D vector3D = Vector3D.Normalize(base.ShipController.PositionComp.GetPosition() - position);
                Vector3D vector3D2 = Vector3D.Normalize(m_destination - position);
                Vector3D vector3D3 = Vector3D.Normalize((vector3D + vector3D2) * 0.5) + position;
                Vector3D vector3D4 = vector3D + position;
                vector3D2 += position;
                Vector3D vector3D5 = Vector3D.Normalize(base.ShipController.WorldMatrix.Translation - position) + position;
                MyRenderProxy.DebugDrawLine3D(vector3D4, vector3D3, Color.Red, Color.Red, depthRead: false);
                MyRenderProxy.DebugDrawLine3D(vector3D3, vector3D2, Color.Red, Color.Red, depthRead: false);
                MyRenderProxy.DebugDrawSphere(vector3D5, 0.01f, Color.Orange.ToVector3(), 1f, depthRead: false);
                MyRenderProxy.DebugDrawSphere(vector3D5 + m_direction * 0.015f, 0.005f, Color.Yellow.ToVector3(), 1f, depthRead: false);
                MyRenderProxy.DebugDrawText3D(vector3D4, "Remaining time: " + (1800000 - MySandboxGame.TotalGamePlayTimeInMilliseconds + m_spawnTime), Color.Red, 1f, depthRead: false);
            }
        }
    }

}
