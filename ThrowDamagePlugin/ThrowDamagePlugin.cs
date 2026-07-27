// Disable version conflict warnings for cleaner builds
#pragma warning disable CS0436 
#pragma warning disable CS1701
#pragma warning disable CS1702

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CustomPlayerEffects;
using LabApi.Loader.Features.Plugins;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Events.Handlers;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using PlayerRoles;

namespace ThrowDamagePlugin
{
    // 1. Settings structure for specific items
    public class ItemDamageSettings
    {
        [Description("Damage to the BODY")]
        public float BodyDamage { get; set; } = 2.0f;

        [Description("Damage to the HEAD")]
        public float HeadDamage { get; set; } = 5.0f;

        [Description("Name of the sound on BODY hit (leave empty to disable)")]
        public string BodySoundPath { get; set; } = "";

        [Description("Name of the sound on HEAD hit (leave empty to disable)")]
        public string HeadSoundPath { get; set; } = "";

        [Description("Duration of the flash effect upon a headshot (0 to disable)")]
        public float FlashDuration { get; set; } = 0f;

        [Description("Apply concussion and deafness effect on headshot? (true/false)")]
        public bool ApplyConcussion { get; set; } = false;

        [Description("Message shown to the player on headshot (leave empty to disable)")]
        public string HeadshotMessage { get; set; } = "";
    }

    // 2. Main plugin configuration
    public class PluginConfig
    {
        [Description("Is the plugin enabled?")]
        public bool IsEnabled { get; set; } = true;

        [Description("Deal damage to allies? (true/false)")]
        public bool DamageAllies { get; set; } = false;

        [Description("How many seconds does the item deal damage after being thrown?")]
        public float DangerTime { get; set; } = 1.5f;

        [Description("Command template for 3D sound. Variables: {path}, {player_id}, {x}, {y}, {z}")]
        public string AudioCommandTemplate { get; set; } = "audio atplace {x} {y} {z} 1 {path}";

        [Description("Default damage settings for items not listed in CustomItems below")]
        public ItemDamageSettings DefaultItem { get; set; } = new ItemDamageSettings();

        [Description("Custom settings for specific items. Overrides default settings. Use exact ItemType names.")]
        public Dictionary<ItemType, ItemDamageSettings> CustomItems { get; set; } = new Dictionary<ItemType, ItemDamageSettings>
        {
            // Example of a custom item setup (MicroHID)
            {
                ItemType.MicroHID, new ItemDamageSettings
                {
                    BodyDamage = 10.0f,
                    HeadDamage = 100.0f,
                    BodySoundPath = "",
                    HeadSoundPath = "metalpipe", // Assuming the user has metalpipe.ogg
                    FlashDuration = 3.0f,
                    ApplyConcussion = true,
                    HeadshotMessage = "HEADSHOT!"
                }
            }
        };
    }

    // 3. Main Plugin Class
    public sealed class ThrowDamagePlugin : Plugin<PluginConfig>
    {
        public override string Name => "Throw Damage";
        public override string Description => "Custom throwing damage with headshots and 3D sounds.";
        public override string Author => "Annorda";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredApiVersion => new Version(1, 1, 6);

        public static ThrowDamagePlugin Instance { get; private set; }

        private static Type _senderType;
        private static MethodInfo _processQueryMethod;
        private static bool _reflectionInitialized = false;

        public override void Enable()
        {
            Instance = this;
            PlayerEvents.DroppedItem += OnItemDropped;
            ServerEvents.WaitingForPlayers += OnWaitingForPlayers;

            InitializeReflection();
        }

        public override void Disable()
        {
            PlayerEvents.DroppedItem -= OnItemDropped;
            ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
            Instance = null;
        }

        private void OnWaitingForPlayers()
        {
            // Display welcome message in the console
            LabApi.Features.Console.Logger.Info("<color=blue>[INFO] [Throw Damage] Thank you for using plugin Throw Damage v1.0.0 by Annorda. Discord: https://discord.gg/Y9hSPXAcdG</color>");
        }

        private void OnItemDropped(PlayerDroppedItemEventArgs ev)
        {
            if (!Config.IsEnabled || ev.Player == null || ev.Pickup == null) return;

            GameObject pickupObj = ev.Pickup.GameObject;

            if (pickupObj != null)
            {
                var collisionScript = pickupObj.AddComponent<ThrownItemCollision>();

                collisionScript.Attacker = ev.Player;
                collisionScript.LifeTime = Config.DangerTime;

                // Set default item settings initially
                collisionScript.Settings = Config.DefaultItem;

                // Override if the item is listed in CustomItems
                string objName = pickupObj.name.ToLower();
                foreach (var customItem in Config.CustomItems)
                {
                    if (objName.Contains(customItem.Key.ToString().ToLower()))
                    {
                        collisionScript.Settings = customItem.Value;
                        break;
                    }
                }
            }
        }

        // Safe type scanner to prevent loading exceptions
        private IEnumerable<Type> GetSafeTypes(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types; }
            catch { return new Type[0]; }
        }

        // Reflection method to access server console processor
        private void InitializeReflection()
        {
            if (_reflectionInitialized) return;
            _reflectionInitialized = true;

            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in GetSafeTypes(assembly))
                    {
                        if (type == null) continue;
                        if (type.Name == "ServerConsoleSender") _senderType = type;
                        if (type.Name == "CommandProcessor")
                        {
                            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                            {
                                if (method.Name == "ProcessQuery")
                                {
                                    var parameters = method.GetParameters();
                                    if (parameters.Length == 2 && parameters[0].ParameterType == typeof(string))
                                    {
                                        _processQueryMethod = method;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LabApi.Features.Console.Logger.Error($"Reflection search error: {ex.Message}");
            }
        }

        // Triggers the 3D Audio API command
        public void PlayAudioCommand(Player target, string soundName)
        {
            if (string.IsNullOrEmpty(Config.AudioCommandTemplate) || string.IsNullOrEmpty(soundName)) return;

            if (_processQueryMethod != null && _senderType != null)
            {
                try
                {
                    object senderInstance = Activator.CreateInstance(_senderType);

                    Vector3 pos = target.Position;

                    string strX = Mathf.RoundToInt(pos.x).ToString();
                    string strY = Mathf.RoundToInt(pos.y).ToString();
                    string strZ = Mathf.RoundToInt(pos.z).ToString();

                    string cmd = Config.AudioCommandTemplate
                        .Replace("{path}", soundName)
                        .Replace("{player_id}", target.PlayerId.ToString())
                        .Replace("{x}", strX)
                        .Replace("{y}", strY)
                        .Replace("{z}", strZ);

                    _processQueryMethod.Invoke(null, new object[] { cmd, senderInstance });
                }
                catch (Exception ex)
                {
                    LabApi.Features.Console.Logger.Error($"Audio command error: {ex.Message}");
                }
            }
        }
    }

    // 4. Unity MonoBehaviour script to handle collision logic
    public class ThrownItemCollision : MonoBehaviour
    {
        public Player Attacker;
        public float LifeTime;
        public ItemDamageSettings Settings;

        private float _timer = 0f;

        void Update()
        {
            _timer += Time.deltaTime;

            if (_timer > LifeTime)
            {
                Destroy(this);
                return;
            }

            // General body hit radius (1.0 meter)
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.0f);

            Player finalTarget = null;

            // Step 1: Detect if any player body part was hit
            foreach (Collider hit in hitColliders)
            {
                Player target = Player.Get(hit.transform.root.gameObject);

                if (target != null && target != Attacker && target.IsAlive)
                {
                    // Friendly fire check
                    if (!ThrowDamagePlugin.Instance.Config.DamageAllies && target.Role.GetFaction() == Attacker.Role.GetFaction())
                    {
                        continue;
                    }

                    finalTarget = target;
                    break;
                }
            }

            // Step 2: Validate Headshot and apply damage
            if (finalTarget != null)
            {
                bool headshotDetected = false;

                // Strict headshot checking sphere (0.3 meters)
                Collider[] headColliders = Physics.OverlapSphere(transform.position, 0.3f);
                foreach (Collider hit in headColliders)
                {
                    if (hit.name.ToLower().Contains("head"))
                    {
                        Player headTarget = Player.Get(hit.transform.root.gameObject);
                        if (headTarget == finalTarget)
                        {
                            headshotDetected = true;
                            break;
                        }
                    }
                }

                // Determine final damage and sound
                float finalDamage = headshotDetected ? Settings.HeadDamage : Settings.BodyDamage;
                string soundToPlay = headshotDetected ? Settings.HeadSoundPath : Settings.BodySoundPath;

                // Play Audio
                if (!string.IsNullOrEmpty(soundToPlay))
                {
                    ThrowDamagePlugin.Instance.PlayAudioCommand(finalTarget, soundToPlay);
                }

                // Apply special effects for headshots
                if (headshotDetected)
                {
                    if (Settings.FlashDuration > 0)
                    {
                        finalTarget.ReferenceHub.playerEffectsController.EnableEffect<Flashed>(Settings.FlashDuration);
                    }

                    if (Settings.ApplyConcussion)
                    {
                        finalTarget.ReferenceHub.playerEffectsController.EnableEffect<Deafened>(3f);
                        finalTarget.ReferenceHub.playerEffectsController.EnableEffect<Concussed>(3f);
                    }

                    if (!string.IsNullOrEmpty(Settings.HeadshotMessage))
                    {
                        finalTarget.SendBroadcast(Settings.HeadshotMessage, 3);
                    }
                }

                // Apply damage
                finalTarget.Damage(finalDamage, "Blunt force trauma from a thrown object");

                // Destroy the scanner object to prevent multi-hits
                Destroy(this);
                return;
            }
        }
    }
}