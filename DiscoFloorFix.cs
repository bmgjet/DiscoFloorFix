using UnityEngine;
using System.Collections.Generic;
using CompanionServer.Handlers;
using ProtoBuf;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("DiscoFloorFix", "bmgjet", "1.0.0")]
    [Description("Fixes clients null reference when joining map with disco floors.")]

    public class DiscoFloorFix : RustPlugin
    {
        const string Colour = "DiscoFloorFix.colour";
        private PropertyInfo _currentGradient = typeof(AudioVisualisationEntity).GetProperty("currentGradient");
        private List<DiscoFloor> ServersDiscoFloors = new List<DiscoFloor>();

        private void Init()
        {
            permission.RegisterPermission(Colour, this);
        }

        void OnServerInitialized()
        {
            //Remove server spawned ones which cause null reference to clients joining
            int invalid = 0;
            foreach (BaseEntity ServerEnt in UnityEngine.Object.FindObjectsOfType<BaseEntity>())
            {
                switch (ServerEnt.PrefabName)
                {
                    case "assets/prefabs/voiceaudio/discofloor/discofloor.deployed.prefab":
                        BaseEntity DiscoFloor = ServerEnt as BaseEntity;
                        DiscoFloor.Kill();
                        invalid++;
                        break;
                    case "assets/prefabs/voiceaudio/discofloor/skins/discofloor.largetiles.deployed.prefab":
                        BaseEntity DiscoFloorLarge = ServerEnt as BaseEntity;
                        DiscoFloorLarge.Kill();
                        invalid++;
                        break;
                }
            }

            //Scan over maps prefab data to get location of normal and large grid disco floors
            //Spawn them in with the correct references, removed ground trigger and powered.
            foreach (PrefabData PD in World.Serialization.world.prefabs)
            {
                Vector3 position = new Vector3(PD.position.x, PD.position.y, PD.position.z);
                switch (PD.id)
                {
                    case 3677777210: //normal
                        DiscoFloor DF = GameManager.server.CreateEntity("assets/prefabs/voiceaudio/discofloor/discofloor.deployed.prefab", PD.position, PD.rotation) as DiscoFloor;
                        DestroyGroundComp(DF);
                        DF.Spawn();
                        DF.SetFlag(DiscoFloor.Flag_HasPower, true);
                        ServersDiscoFloors.Add(DF);
                        DF.SendNetworkUpdateImmediate();
                        break;
                    case 1416531191: //large
                        DiscoFloor DFL = GameManager.server.CreateEntity("assets/prefabs/voiceaudio/discofloor/skins/discofloor.largetiles.deployed.prefab", PD.position, PD.rotation) as DiscoFloor;
                        DestroyGroundComp(DFL);
                        DFL.Spawn();
                        DFL.SetFlag(DiscoFloor.Flag_HasPower, true);
                        ServersDiscoFloors.Add(DFL);
                        DFL.SendNetworkUpdateImmediate();
                        break;
                }
            }
            Puts("Fixed " + invalid.ToString() + " invalid disco floors");
        }

        void DestroyGroundComp(BaseEntity ent)
        {
            UnityEngine.Object.DestroyImmediate(ent.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(ent.GetComponent<GroundWatch>());
        }


        void Unload()
        {
            //clean up
            foreach(BaseEntity be in ServersDiscoFloors)
            {
                be.Kill();
            }
        }

        //Change the colour on chat command
        [ChatCommand("colour")]
        private void cmdcolour(BasePlayer player, string command, string[] args)
        {
            if (player.IPlayer.HasPermission(Colour))
            {
                int colour = 0;
                try
                {
                    colour = int.Parse(args[0]);
                }
                catch
                {
                    player.ChatMessage("Error converting colour to int");
                    return;
                }
                if (colour >= 0 && colour <= 8)
                {
                    foreach (DiscoFloor df in ServersDiscoFloors)
                    {
                        _currentGradient.SetValue(df, int.Parse(args[0]));
                        df.MarkDirty();
                        df.SendNetworkUpdateImmediate();
                    }
                    player.ChatMessage("Changed colour " + colour.ToString());
                }
            }
        }
    }
}