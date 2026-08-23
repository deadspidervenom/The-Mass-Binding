using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace TheMassMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class TheMassPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.vilcan.themassbinding";
        public const string PluginName = "The Mass Binding";
        public const string PluginVersion = "1.3";

        public static ConfigEntry<float> HungerDecayMultiplier;
        public static ConfigEntry<float> NerfedFoodFraction;
        public static ConfigEntry<float> DenizenEatRestoreMultiplier;
        public static ConfigEntry<float> DenizenMeatEatMultiplier;
        public static ConfigEntry<int> StartingThreshold;
        public static ConfigEntry<int> ThresholdStep;
        public static ConfigEntry<int> StartingMeatCount;
        public static ConfigEntry<bool> SyncForeignHungerModules;
        public static ConfigEntry<KeyCode> EatKey;
        public static ConfigEntry<KeyCode> FoodMakerKey;
        public static ConfigEntry<float> EatRange;
        public static ConfigEntry<bool> VerboseLogging;
        public static ConfigEntry<string> BlacklistedDenizenTypes;

        internal static new BepInEx.Logging.ManualLogSource Logger;

        private Harmony _harmony;

        private void Awake()
        {
            Logger = base.Logger;

            HungerDecayMultiplier = Config.Bind("Hunger", "HungerDecayMultiplier", 1.0f,
                "Multiplier on the hunger decay rate. 1.0 = same pace as vanilla Survival Mode.");
            NerfedFoodFraction = Config.Bind("Hunger", "NerfedFoodFraction", 0.1f,
                "Fraction of normal food's hunger restore that still applies while The Mass is active.");
            DenizenEatRestoreMultiplier = Config.Bind("Hunger", "DenizenEatRestoreMultiplier", 1.0f,
                "Hunger restored by eating a denizen directly, as a multiple of a normal meal.");
            DenizenMeatEatMultiplier = Config.Bind("Hunger", "DenizenMeatEatMultiplier", 1.0f,
                "Hunger restored by eating Denizen Meat, as a multiple of a normal meal.");
            StartingThreshold = Config.Bind("Perks", "StartingThreshold", 5,
                "Denizens eaten needed for the first random perk.");
            ThresholdStep = Config.Bind("Perks", "ThresholdStep", 5,
                "How much the required count increases after each perk reward.");
            StartingMeatCount = Config.Bind("Hunger", "StartingMeatCount", 3,
                "Denizen Meat granted on taking the binding, in case you can't find a denizen right away.");
            SyncForeignHungerModules = Config.Bind("Perks", "SyncForeignHungerModules", true,
                "When a granted perk brings its own hunger module (e.g. Conditioned Polyphagia), tune that " +
                "module's stats to match The Mass's own and feed it from the denizen/Denizen Meat mechanic, " +
                "instead of leaving it as a separate, unrelated hunger system. The perk keeps its own module " +
                "and UI — only its numbers and restores are kept in sync. Disable to leave foreign hunger " +
                "modules completely untouched.");
            EatKey = Config.Bind("Controls", "EatKey", KeyCode.G,
                "Hold both grab buttons on a denizen and press this to eat it.");
            FoodMakerKey = Config.Bind("Controls", "FoodMakerKey", KeyCode.X,
                "Hold both grab buttons on a denizen and press this to turn it into Denizen Meat instead.");
            EatRange = Config.Bind("Controls", "EatRange", 3.0f,
                "Max distance to a denizen for the eat prompt to register.");
            VerboseLogging = Config.Bind("Debug", "VerboseLogging", false,
                "Per-frame eat-detection logging. Only useful for troubleshooting.");
            BlacklistedDenizenTypes = Config.Bind("Eating", "BlacklistedDenizenTypes",
                "DEN_Mother,DEN_Hunter,DEN_Hunter_Arm,DEN_Teeth,DEN_Face,DEN_Turret," +
                "DEN_Apparition,DEN_DeathFloor,DEN_LadderNightmare,DEN_EngravedDoor,DEN_Roach",
                "Comma-separated component type names that can never be eaten, checked by exact " +
                "class name anywhere in the found denizen's full hierarchy (not just the exact " +
                "hit object) — several of these (e.g. DEN_Hunter_Arm) aren't Denizen subclasses " +
                "themselves and live on a different part of a composite creature's hierarchy. " +
                "Edit this list freely; no rebuild needed.");

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            Logger.LogInfo("The Mass Binding loaded.");
        }
    }

    [HarmonyPatch(typeof(UI_TrinketPicker), "PopulateTrinkets")]
    public static class UI_TrinketPicker_PopulateTrinkets_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(List<Trinket> trinketsToPopulate, Transform root, bool allowInIronKnuckle)
        {
            if (trinketsToPopulate == null || trinketsToPopulate.Count == 0) return;
            if (!trinketsToPopulate.Any(t => t != null && t.isBinding)) return;

            TheMassBinding.EnsureRegistered(trinketsToPopulate);
        }
    }

    [HarmonyPatch(typeof(HandItem_Food), "Eat")]
    public static class HandItem_Food_Eat_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(HandItem_Food __instance)
        {
            if (__instance?.item == null) return;
            if (__instance.item.itemName != TheMassBinding.DenizenMeatItemName) return;

            PerkModule_TheMassEater.ActiveInstance?.OnDenizenMeatEaten();
        }
    }

    [HarmonyPatch(typeof(HandItem_Buff), "StartBuff")]
    public static class HandItem_Buff_StartBuff_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(HandItem_Buff __instance)
        {
            if (__instance?.item == null) return;
            if (__instance.item.itemName != TheMassBinding.DenizenMeatItemName) return;

            PerkModule_TheMassEater.ActiveInstance?.OnDenizenMeatEaten();
        }
    }

    [HarmonyPatch(typeof(ENT_Player), "AddPerk")]
    public static class ENT_Player_AddPerk_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ENT_Player __instance, Perk perk, int stackAmount, bool firstTime, Perk __result)
        {
            if (__instance == null || __result?.modules == null) return;
            if (__result.id == TheMassBinding.BindingId) return;
            if (!__instance.HasPerk(TheMassBinding.BindingId)) return;
            if (!TheMassPlugin.SyncForeignHungerModules.Value) return;

            PerkModule_TheMassEater ourEater = PerkModule_TheMassEater.ActiveInstance;
            if (ourEater == null) return;

            foreach (PerkModule_HungerMeter foreign in __result.modules.OfType<PerkModule_HungerMeter>())
                ourEater.SyncForeignHungerModule(foreign);
        }
    }

    public static class TheMassBinding
    {
        public const string BindingId = "Binding_TheMass";
        private const string TrinketTitle = "The Mass";

        public static void EnsureRegistered(List<Trinket> bindings)
        {
            if (bindings == null) return;
            if (bindings.Any(t => t != null && t.title == TrinketTitle)) return;

            Trinket hungerTemplate = bindings.FirstOrDefault(t => t?.perksToGrant != null
                && t.perksToGrant.OfType<Perk>().Any(p => p.modules != null && p.modules.OfType<PerkModule_HungerMeter>().Any()));

            if (hungerTemplate == null)
            {
                TheMassPlugin.Logger.LogError("[TheMassBinding] No hunger-based binding found to use as a template.");
                return;
            }

            Trinket cosmeticsTemplate = bindings.FirstOrDefault(t => t != null
                    && !string.IsNullOrEmpty(t.title)
                    && t.title.IndexOf("Hunted", System.StringComparison.OrdinalIgnoreCase) >= 0)
                ?? bindings.FirstOrDefault(t => t != null && t != hungerTemplate)
                ?? hungerTemplate;

            PerkModule_HungerMeter templateHunger = hungerTemplate.perksToGrant
                .First(p => p.modules.OfType<PerkModule_HungerMeter>().Any())
                .modules.OfType<PerkModule_HungerMeter>().First();

            int order = (bindings.Count == 0 ? 0 : bindings.Max(t => t?.activateOrder ?? 0)) + 1;

            Perk perk = BuildPerk(templateHunger, cosmeticsTemplate);
            Trinket trinket = BuildTrinket(cosmeticsTemplate, perk, order);

            bindings.Add(trinket);
            RegisterInAssetDatabases(perk, trinket);
        }

        private static void RegisterInAssetDatabases(Perk perk, Trinket trinket)
        {
            if (CL_AssetManager.instance == null) return;

            var databases = AccessTools.Field(typeof(CL_AssetManager), "databases")
                .GetValue(CL_AssetManager.instance) as List<CL_AssetManager.WKDatabaseHolder>;
            if (databases == null) return;

            foreach (var holder in databases)
            {
                if (holder?.database == null) continue;

                if (holder.database.trinketAssets != null && !holder.database.trinketAssets.Any(t => t != null && t.name == trinket.name))
                    holder.database.trinketAssets.Add(trinket);
                if (holder.database.perkAssets != null && !holder.database.perkAssets.Any(p => p != null && p.name == perk.name))
                    holder.database.perkAssets.Add(perk);
            }
        }

        public const string DenizenMeatItemName = "Denizen Meat";
        private const string FunctionalItemPrefabName = "item_food_meat";
        private const string CosmeticItemPrefabName = "item_food_meat";

        private static readonly Color MeatTintOverride = new Color(0.35f, 1f, 0.35f, -1f);

        private static Color ApplyMeatTint(Color original)
        {
            float a = MeatTintOverride.a >= 0f ? MeatTintOverride.a : original.a;
            return new Color(MeatTintOverride.r, MeatTintOverride.g, MeatTintOverride.b, a);
        }

        private static readonly Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

        private static GameObject FindItemPrefabTemplate(string prefabName)
        {
            if (_prefabCache.TryGetValue(prefabName, out GameObject cached) && cached != null) return cached;

            WKAssetDatabase db = CL_AssetManager.GetFullCombinedAssetDatabase();
            GameObject match = db?.itemPrefabs?.FirstOrDefault(p => p != null
                && p.name.Equals(prefabName, System.StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                TheMassPlugin.Logger.LogWarning($"[TheMassBinding] '{prefabName}' not found in item prefab database.");
                return null;
            }

            _prefabCache[prefabName] = match;
            return match;
        }

        public static void SpawnDenizenMeat(Vector3 position, ENT_Player player)
        {
            GameObject functionalTemplate = FindItemPrefabTemplate(FunctionalItemPrefabName);
            Item_Object functionalItemObject = functionalTemplate?.GetComponent<Item_Object>();
            if (functionalItemObject?.itemData == null)
            {
                TheMassPlugin.Logger.LogError("[TheMassBinding] Can't create Denizen Meat — template item not found.");
                return;
            }

            Item clone = functionalItemObject.itemData.GetClone(null, false);
            if (clone == null) return;

            clone.itemName = DenizenMeatItemName;
            ApplyMeatCosmetics(clone);

            if (player == null) return;

            var inventory = AccessTools.Field(typeof(ENT_Player), "inventory").GetValue(player) as Inventory;
            if (inventory == null)
            {
                TheMassPlugin.Logger.LogError("[TheMassBinding] Couldn't reach the player's inventory.");
                return;
            }

            inventory.AddItemToInventoryScreen(position, clone, true, false, false);
        }

        private static GameObject _noteTemplateCache;

        public static void SpawnNote(Vector3 position, ENT_Player player, string noteText)
        {
            if (_noteTemplateCache == null)
            {
                WKAssetDatabase db = CL_AssetManager.GetFullCombinedAssetDatabase();
                _noteTemplateCache = db?.itemPrefabs?.FirstOrDefault(p => p != null
                    && p.GetComponent<Item_Object>()?.itemData?.handItemAsset is HandItem_Note);

                if (_noteTemplateCache == null)
                {
                    TheMassPlugin.Logger.LogWarning("[TheMassBinding] No note-type item found in the item prefab database — can't create the intro note.");
                    return;
                }
            }

            Item_Object templateItemObject = _noteTemplateCache.GetComponent<Item_Object>();
            Item clone = templateItemObject?.itemData?.GetClone(null, false);
            if (clone == null) return;

            // HandItem_Note.Initialize() derives its displayed text from
            // item.GetFirstDataStringByType("text") every time the note is
            // equipped/held — it overwrites the TMP_Text component itself, so
            // setting that component's text directly gets stomped. The actual
            // per-item content lives here, as a "text:<content>" entry in the
            // Item's own data list. Assigning a new list (rather than editing
            // the cloned one in place) keeps this from touching the shared
            // template if GetClone happened to shallow-copy the reference.
            clone.data = new List<string> { $"text:{noteText}" };

            if (player == null) return;

            var inventory = AccessTools.Field(typeof(ENT_Player), "inventory").GetValue(player) as Inventory;
            if (inventory == null)
            {
                TheMassPlugin.Logger.LogError("[TheMassBinding] Couldn't reach the player's inventory.");
                return;
            }

            inventory.AddItemToInventoryScreen(position, clone, true, false, false);
        }

        private const string GrubSacrificeStat = "grubs-used";
        private const string GrubSacrificeFlag = "session_sacrificed_grub";

        public static void ApplyGrubSacrificeEquivalent()
        {
            try
            {
                var statManagerType = AccessTools.TypeByName("StatManager");
                object sessionStats = AccessTools.Field(statManagerType, "sessionStats")?.GetValue(null);
                var gameStatsType = AccessTools.Inner(statManagerType, "GameStats");

                if (sessionStats != null && gameStatsType != null)
                {
                    AccessTools.Method(gameStatsType, "UpdateStatistic",
                        new[] { typeof(string), typeof(object), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })
                        ?.Invoke(sessionStats, new object[] { GrubSacrificeStat, 1, false, true, false, true });
                }

                AccessTools.Method(AccessTools.TypeByName("CL_GameManager"), "SetGameFlag",
                    new[] { typeof(string), typeof(bool), typeof(string), typeof(bool), typeof(bool) })
                    ?.Invoke(null, new object[] { GrubSacrificeFlag, true, "", false, false });
            }
            catch (System.Exception e)
            {
                TheMassPlugin.Logger.LogError($"[TheMassBinding] ApplyGrubSacrificeEquivalent failed: {e}");
            }
        }

        public static void DisableLeaderboardsForThisRun()
        {
            try
            {
                var gameManagerType = AccessTools.TypeByName("CL_GameManager");
                object gMan = AccessTools.Field(gameManagerType, "gMan")?.GetValue(null);
                if (gMan == null)
                {
                    TheMassPlugin.Logger.LogWarning("[TheMassBinding] CL_GameManager.gMan was null — couldn't disable leaderboards for this run.");
                    return;
                }

                AccessTools.Field(gameManagerType, "allowScores")?.SetValue(gMan, false);
            }
            catch (System.Exception e)
            {
                TheMassPlugin.Logger.LogError($"[TheMassBinding] DisableLeaderboardsForThisRun failed: {e}");
            }
        }

        private static void ApplyMeatCosmetics(Item clone)
        {
            GameObject cosmeticTemplate = FindItemPrefabTemplate(CosmeticItemPrefabName);
            if (cosmeticTemplate == null) return;

            Item cosmeticItemData = cosmeticTemplate.GetComponent<Item_Object>()?.itemData;
            if (cosmeticItemData != null)
            {
                if (cosmeticItemData.normalSprite != null) clone.normalSprite = cosmeticItemData.normalSprite;
                if (cosmeticItemData.pickupSounds?.Count > 0) clone.pickupSounds = new List<AudioClip>(cosmeticItemData.pickupSounds);
            }

            Transform meatMeshChild = FindChildByName(cosmeticTemplate.transform, "Item_Food_Fruit");
            Renderer meatRenderer = meatMeshChild?.GetComponent<Renderer>();
            MeshFilter meatMeshFilter = meatMeshChild?.GetComponent<MeshFilter>();

            SpriteRenderer meatSpriteRenderer = null;
            HandItem_Buff meatHandBuff = null;
            if (cosmeticItemData?.handItemAsset != null)
            {
                meatHandBuff = cosmeticItemData.handItemAsset as HandItem_Buff;
                Transform spriteChild = FindChildByName(cosmeticItemData.handItemAsset.transform, "ItemHands Item");
                meatSpriteRenderer = spriteChild?.GetComponent<SpriteRenderer>();
            }

            if (clone.itemAsset != null && (meatRenderer != null || meatMeshFilter != null))
            {
                GameObject worldClone = UnityEngine.Object.Instantiate(clone.itemAsset.gameObject);
                worldClone.name = "DenizenMeat_ItemAsset(Clone)";
                worldClone.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(worldClone);

                Renderer targetRenderer = worldClone.GetComponentInChildren<Renderer>(true);
                MeshFilter targetMeshFilter = worldClone.GetComponentInChildren<MeshFilter>(true);
                if (targetRenderer != null && meatRenderer != null)
                {
                    targetRenderer.sharedMaterials = meatRenderer.sharedMaterials;
                    Material tinted = targetRenderer.material;
                    tinted.color = ApplyMeatTint(tinted.color);
                }
                if (targetMeshFilter != null && meatMeshFilter != null)
                    targetMeshFilter.sharedMesh = meatMeshFilter.sharedMesh;

                Item_Object newItemObject = worldClone.GetComponentInChildren<Item_Object>(true);
                if (newItemObject != null) clone.itemAsset = newItemObject;
            }

            if (clone.handItemAsset != null)
            {
                GameObject handClone = UnityEngine.Object.Instantiate(clone.handItemAsset.gameObject);
                handClone.name = "DenizenMeat_HandItemAsset(Clone)";
                handClone.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(handClone);

                if (meatSpriteRenderer != null)
                {
                    SpriteRenderer targetSprite = FindChildByName(handClone.transform, "ItemHands Item")?.GetComponent<SpriteRenderer>();
                    if (targetSprite != null)
                    {
                        targetSprite.sprite = meatSpriteRenderer.sprite;
                        targetSprite.color = ApplyMeatTint(meatSpriteRenderer.color);
                    }
                }

                HandItem_Buff targetBuff = handClone.GetComponent<HandItem_Buff>();
                if (targetBuff != null)
                {
                    if (meatHandBuff != null)
                    {
                        targetBuff.audioClip = meatHandBuff.audioClip;
                        targetBuff.audioVolume = meatHandBuff.audioVolume;
                    }
                    targetBuff.buff = null;
                    targetBuff.useSecondaryBuffs = false;
                    targetBuff.secondaryBuffs = null;
                }

                HandItem newHandItem = handClone.GetComponentInChildren<HandItem>(true);
                if (newHandItem != null) clone.handItemAsset = newHandItem;
            }
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = FindChildByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Perk BuildPerk(PerkModule_HungerMeter templateHunger, Trinket cosmeticsTemplate)
        {
            Perk perk = ScriptableObject.CreateInstance<Perk>();
            perk.name = TrinketTitle;
            perk.id = BindingId;
            perk.title = TrinketTitle;
            perk.flavorText = "It bonds flesh. So do you, now.";
            perk.description = $"Food barely sustains you. Grab & eat denizens ({TheMassPlugin.EatKey.Value}) instead.";
            perk.perkType = Perk.PerkType.binding;
            perk.spawnPool = Perk.PerkPool.never;
            perk.canStack = false;

            Perk cosmeticsPerk = cosmeticsTemplate?.perksToGrant?.FirstOrDefault();
            if (cosmeticsPerk != null)
            {
                perk.icon = cosmeticsPerk.icon;
                perk.iconMat = cosmeticsPerk.iconMat;
                perk.perkCard = cosmeticsPerk.perkCard;
                perk.perkFrame = cosmeticsPerk.perkFrame;
            }

            float templateEatRecovery = templateHunger.eatRecovery;
            var hunger = new PerkModule_HungerMeter
            {
                hungerMax = templateHunger.hungerMax,
                hungerMeter = templateHunger.hungerMax,
                hungerDecayRate = templateHunger.hungerDecayRate * TheMassPlugin.HungerDecayMultiplier.Value,
                eatRecovery = templateEatRecovery * TheMassPlugin.NerfedFoodFraction.Value,
                consumeBuffIDs = templateHunger.consumeBuffIDs,
                hungerMeterAsset = templateHunger.hungerMeterAsset,
                buff = CloneBuffContainer(templateHunger.buff),
                debuff = CloneBuffContainer(templateHunger.debuff),
                buffCurve = templateHunger.buffCurve,
                debuffCurve = templateHunger.debuffCurve,
                fullColor = templateHunger.fullColor,
                emptyColor = templateHunger.emptyColor,
                hungerTickAudio = templateHunger.hungerTickAudio,
            };

            perk.modules = new List<PerkModule>
            {
                hunger,
                new PerkModule_TheMassEater { NormalEatRecoveryAmount = templateEatRecovery },
            };
            return perk;
        }

        private static BuffContainer CloneBuffContainer(BuffContainer source)
        {
            if (source == null) return null;
            return new BuffContainer
            {
                id = source.id + "_TheMassBinding",
                desc = source.desc,
                buffs = source.buffs != null ? new List<BuffContainer.Buff>(source.buffs) : null,
                loseRate = source.loseRate,
                loseRateEffectedByPerks = source.loseRateEffectedByPerks,
                buffTime = source.buffTime,
                loseOverTime = source.loseOverTime,
                multiplier = source.multiplier,
            };
        }

        private static Trinket BuildTrinket(Trinket template, Perk perk, int activateOrder)
        {
            Trinket trinket = ScriptableObject.CreateInstance<Trinket>();
            trinket.name = TrinketTitle;
            trinket.title = TrinketTitle;
            trinket.description = $"Food barely sustains you. Grab & eat denizens ({TheMassPlugin.EatKey.Value}) instead.";
            trinket.flavorText = "It bonds flesh. So do you, now.";
            trinket.isBinding = true;
            trinket.icon = template.icon;
            trinket.lockIcon = template.lockIcon;
            trinket.cost = template.cost;
            trinket.itemsToGrant = new List<Item_Object>();
            trinket.perksToGrant = new List<Perk> { perk };
            trinket.pouchesToGrant = 0;
            trinket.scoreMultiplierBonus = template.scoreMultiplierBonus;
            trinket.scoreBonus = template.scoreBonus;
            trinket.comingSoon = false;
            trinket.settingBlacklist = template.settingBlacklist != null
                ? new List<string>(template.settingBlacklist)
                : new List<string>();
            trinket.activateOrder = activateOrder;
            return trinket;
        }
    }

    [System.Serializable]
    public class PerkModule_TheMassEater : PerkModule
    {
        public float NormalEatRecoveryAmount = 25f;

        internal static PerkModule_TheMassEater ActiveInstance;

        private Perk _perkRef;
        private ENT_Player _player;
        private PerkModule_HungerMeter _hunger;
        private readonly List<PerkModule_HungerMeter> _syncedForeignHungerModules = new List<PerkModule_HungerMeter>();
        private int _denizensEaten;
        private int _nextThreshold;
        private int _currentIncrement;
        private float _logTimer;

        public override void Initialize(Perk perk, bool isNew)
        {
            base.Initialize(perk, isNew);
            _perkRef = perk;
            _hunger = perk.modules?.OfType<PerkModule_HungerMeter>().FirstOrDefault();
            _player = UnityEngine.Object.FindObjectOfType<ENT_Player>();
            ActiveInstance = this;

            if (isNew)
            {
                TheMassBinding.DisableLeaderboardsForThisRun();

                _currentIncrement = TheMassPlugin.StartingThreshold.Value;
                _nextThreshold = _currentIncrement;

                if (_player != null)
                {
                    for (int i = 0; i < TheMassPlugin.StartingMeatCount.Value; i++)
                        TheMassBinding.SpawnDenizenMeat(_player.transform.position, _player);

                    string noteText = $"While holding both grab keys\nand looking at a denizen\nPress {TheMassPlugin.EatKey.Value} to eat\nPress {TheMassPlugin.FoodMakerKey.Value} to make meat";
                    TheMassBinding.SpawnNote(_player.transform.position, _player, noteText);
                }
            }

            if (TheMassPlugin.SyncForeignHungerModules.Value && _player?.perks != null)
            {
                foreach (Perk otherPerk in _player.perks)
                {
                    if (otherPerk == null || otherPerk == perk || otherPerk.modules == null) continue;
                    foreach (PerkModule_HungerMeter foreign in otherPerk.modules.OfType<PerkModule_HungerMeter>())
                        SyncForeignHungerModule(foreign);
                }
            }
        }

        public override string GetCounterString()
        {
            return Mathf.Max(0, _nextThreshold - _denizensEaten).ToString();
        }

        public void SyncForeignHungerModule(PerkModule_HungerMeter foreign)
        {
            if (_hunger == null || foreign == null || foreign == _hunger) return;

            foreign.hungerMax = _hunger.hungerMax;
            foreign.hungerMeter = _hunger.hungerMeter;
            foreign.hungerDecayRate = _hunger.hungerDecayRate;
            foreign.eatRecovery = _hunger.eatRecovery;

            if (!_syncedForeignHungerModules.Contains(foreign))
                _syncedForeignHungerModules.Add(foreign);

            if (TheMassPlugin.VerboseLogging.Value)
                TheMassPlugin.Logger.LogInfo("[TheMassBinding] Synced a foreign hunger module to match The Mass " +
                    $"(hungerMax={foreign.hungerMax:F1}, decayRate={foreign.hungerDecayRate:F3}, " +
                    $"eatRecovery={foreign.eatRecovery:F1}).");
        }

        public override void Update()
        {
            base.Update();

            if (_player == null)
            {
                _player = UnityEngine.Object.FindObjectOfType<ENT_Player>();
                if (_player == null) return;
            }

            bool leftDown = Input.GetMouseButton(0);
            bool rightDown = Input.GetMouseButton(1);
            bool bothHandsGrabbing = leftDown && rightDown;

            Denizen target = null;
            string hitDebug = "not grabbing";
            if (bothHandsGrabbing)
                target = FindDenizenUnderCrosshair(out hitDebug);

            if (TheMassPlugin.VerboseLogging.Value)
            {
                _logTimer += Time.deltaTime;
                if (_logTimer >= 1f)
                {
                    _logTimer = 0f;
                    TheMassPlugin.Logger.LogInfo($"[TheMassBinding] LMB={leftDown} RMB={rightDown} " +
                        $"target={(target != null ? target.name : "none")} hits=[{hitDebug}] " +
                        $"hunger={(_hunger != null ? _hunger.hungerMeter.ToString("F1") : "n/a")}");
                }
            }

            if (!bothHandsGrabbing || target == null) return;

            if (Input.GetKeyDown(TheMassPlugin.EatKey.Value))
            {
                try { EatDenizen(target); }
                catch (System.Exception e) { TheMassPlugin.Logger.LogError($"[TheMassBinding] EatDenizen failed: {e}"); }
            }
            else if (Input.GetKeyDown(TheMassPlugin.FoodMakerKey.Value))
            {
                try { MakeFoodFromDenizen(target); }
                catch (System.Exception e) { TheMassPlugin.Logger.LogError($"[TheMassBinding] MakeFoodFromDenizen failed: {e}"); }
            }
        }

        private Denizen FindDenizenUnderCrosshair(out string hitDebug)
        {
            hitDebug = "";
            if (_player.cam == null)
            {
                hitDebug = "no camera";
                return null;
            }

            Ray ray = new Ray(_player.cam.transform.position, _player.cam.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, TheMassPlugin.EatRange.Value);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            var names = new List<string>();
            Denizen found = null;
            foreach (var hit in hits)
            {
                var denizen = hit.collider.GetComponentInParent<Denizen>()
                    ?? hit.rigidbody?.GetComponent<Denizen>()
                    ?? hit.rigidbody?.GetComponentInParent<Denizen>();

                bool blacklisted = denizen != null && IsBlacklisted(denizen);
                names.Add(denizen != null ? $"{hit.collider.gameObject.name}[Denizen{(blacklisted ? ",blacklisted" : "")}]" : hit.collider.gameObject.name);
                if (denizen != null && !blacklisted && found == null) found = denizen;
            }
            hitDebug = names.Count > 0 ? string.Join(", ", names) : "no hits";
            return found;
        }

        // Searches the FULL hierarchy from the found denizen's topmost root, not just
        // the exact component found — several blacklisted types (e.g. DEN_Hunter_Arm)
        // aren't Denizen subclasses themselves and live on a different part of a
        // composite creature's hierarchy than whatever our raycast actually finds
        // (same pattern confirmed live for DEN_Barnacle_Zombie/DEN_Barnacle).
        private static bool IsBlacklisted(Denizen denizen)
        {
            HashSet<string> blacklist = GetBlacklistSet();
            if (blacklist.Count == 0) return false;

            // NOTE: transform.root walks all the way up to the scene's top-level
            // object — every denizen in the whole loaded level ends up nested under
            // one shared root, so searching from there matched everything blacklisted
            // anywhere in the level. Even searching the immediate parent's FULL
            // subtree is risky if denizens are flatly parented under one shared
            // container (e.g. ".../Entities/Denizens/<creature>") — that would still
            // pick up unrelated sibling denizens. Narrowed to: the denizen's own
            // subtree (self + its own children only), plus components directly ON its
            // immediate parent (not the parent's OTHER children) — the latter still
            // correctly covers the confirmed Barnacle Zombie pattern, since
            // DEN_Barnacle_Zombie sits directly on the shared immediate parent of both
            // the zombie body and the barnacle attack, without scanning siblings.
            foreach (Component c in denizen.GetComponentsInChildren<Component>(true))
            {
                if (c != null && blacklist.Contains(c.GetType().Name)) return true;
            }

            if (denizen.transform.parent != null)
            {
                foreach (Component c in denizen.transform.parent.GetComponents<Component>())
                {
                    if (c != null && blacklist.Contains(c.GetType().Name)) return true;
                }
            }

            return false;
        }

        private static HashSet<string> _blacklistCache;
        private static string _blacklistCacheSource;

        private static HashSet<string> GetBlacklistSet()
        {
            string current = TheMassPlugin.BlacklistedDenizenTypes.Value ?? "";
            if (_blacklistCache == null || _blacklistCacheSource != current)
            {
                _blacklistCacheSource = current;
                _blacklistCache = new HashSet<string>(
                    current.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0),
                    System.StringComparer.OrdinalIgnoreCase);
            }
            return _blacklistCache;
        }

        private static bool IsGrub(Denizen denizen) => denizen != null && denizen.GetComponent<DEN_SlugGrub>() != null;

        private void EatDenizen(Denizen denizen)
        {
            bool wasGrub = IsGrub(denizen);
            if (!KillAndRemoveDenizen(denizen)) return;

            RestoreHunger(NormalEatRecoveryAmount * TheMassPlugin.DenizenEatRestoreMultiplier.Value);
            RegisterCredit();
            if (wasGrub) TheMassBinding.ApplyGrubSacrificeEquivalent();
        }

        private void MakeFoodFromDenizen(Denizen denizen)
        {
            Vector3 spawnPos = denizen.transform.position;
            bool wasGrub = IsGrub(denizen);
            if (!KillAndRemoveDenizen(denizen)) return;

            if (wasGrub) TheMassBinding.ApplyGrubSacrificeEquivalent();
            TheMassBinding.SpawnDenizenMeat(spawnPos, _player);
        }

        public void OnDenizenMeatEaten()
        {
            RestoreHunger(NormalEatRecoveryAmount * TheMassPlugin.DenizenMeatEatMultiplier.Value);
        }

        private bool KillAndRemoveDenizen(Denizen denizen)
        {
            if (denizen == null || denizen.dead) return false;

            var info = Damageable.DamageInfo.CreateDamageInfo(9999f, _player, "Eaten");
            denizen.Kill("Eaten", info);

            UnityEngine.Object.Destroy(denizen.gameObject, 0.15f);

            // DEN_Barnacle_Zombie sits as a component on the SAME root GameObject as
            // both the zombie body and its barnacle attack (confirmed live via Unity
            // Explorer) — the barnacle's own DEN_Barnacle component lives deeper in a
            // child subtree, so it's what our normal raycast finds and eats first,
            // leaving the zombie body behind with no attack. Walking up from the eaten
            // barnacle with GetComponentInParent reliably finds that shared root.
            // Pure cleanup: kills the now-attackless body too, but grants no extra
            // hunger/credit — only the barnacle itself was actually "eaten".
            if (denizen.GetComponent<DEN_Barnacle>() != null)
            {
                DEN_Barnacle_Zombie ownerZombie = denizen.GetComponentInParent<DEN_Barnacle_Zombie>();
                if (ownerZombie != null && !ownerZombie.dead)
                {
                    var zombieInfo = Damageable.DamageInfo.CreateDamageInfo(9999f, _player, "Eaten");
                    ownerZombie.Kill("Eaten", zombieInfo);
                    UnityEngine.Object.Destroy(ownerZombie.gameObject, 0.15f);
                    TheMassPlugin.Logger.LogInfo("[TheMassMod] Eaten barnacle's zombie host cleaned up too.");
                }
            }

            return true;
        }

        private void RegisterCredit()
        {
            _denizensEaten++;
            if (_nextThreshold <= 0)
            {
                _currentIncrement = TheMassPlugin.StartingThreshold.Value;
                _nextThreshold = _currentIncrement;
            }

            if (_denizensEaten >= _nextThreshold)
            {
                GrantRandomPerk();
                _currentIncrement += TheMassPlugin.ThresholdStep.Value;
                _nextThreshold += _currentIncrement;
            }
        }

        private void RestoreHunger(float amount)
        {
            _hunger ??= _perkRef?.modules?.OfType<PerkModule_HungerMeter>().FirstOrDefault();
            if (_hunger == null) return;
            _hunger.hungerMeter = Mathf.Clamp(_hunger.hungerMeter + amount, 0f, _hunger.hungerMax);

            for (int i = _syncedForeignHungerModules.Count - 1; i >= 0; i--)
            {
                PerkModule_HungerMeter foreign = _syncedForeignHungerModules[i];
                if (foreign == null)
                {
                    _syncedForeignHungerModules.RemoveAt(i);
                    continue;
                }
                foreign.hungerMeter = Mathf.Clamp(foreign.hungerMeter + amount, 0f, foreign.hungerMax);
            }
        }

        private static readonly HashSet<Perk.PerkType> AllowedRewardTypes = new HashSet<Perk.PerkType>
        {
            Perk.PerkType.standard,
            Perk.PerkType.orange,
            Perk.PerkType.delta,
            Perk.PerkType.rho,
            Perk.PerkType.trinket,
        };

        private void GrantRandomPerk()
        {
            List<Perk> pool = Resources.FindObjectsOfTypeAll<Perk>()
                .Where(p => p != null
                    && p.id != TheMassBinding.BindingId
                    && AllowedRewardTypes.Contains(p.perkType)
                    && p.spawnPool != Perk.PerkPool.never
                    && !HasModule<PerkModule_HungerMeter>(p))
                .ToList();

            if (pool.Count == 0) return;
            _player.AddPerk(pool[UnityEngine.Random.Range(0, pool.Count)], 1, true);
        }

        private static bool HasModule<T>(Perk p) where T : PerkModule => p.modules != null && p.modules.OfType<T>().Any();
    }
}
