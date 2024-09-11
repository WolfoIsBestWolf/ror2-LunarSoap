using R2API;
using RoR2;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

namespace LunarSoap
{
    public class VoidSpareDroneParts
    {
        public static ItemDef NewItemDef;
        public static GameObject DisplayItem;
        public static GameObject PickupModel;

        public static void Start()
        {
            ItemDef CloverVoid = LegacyResourcesAPI.Load<ItemDef>("ItemDefs/CloverVoid");
            ItemDef DroneWeapons = LegacyResourcesAPI.Load<ItemDef>("ItemDefs/DroneWeapons");

            PickupModel = PrefabAPI.InstantiateClone(DroneWeapons.pickupModelPrefab, "VoidDronePartsPickup", false);
            DisplayItem = PrefabAPI.InstantiateClone(new GameObject(), "DisplayVoidDroneParts", false);


            LanguageAPI.Add("ITEM_VOIDDRONEPARTS_NAME", "Leftover Crab Parts");
            LanguageAPI.Add("ITEM_VOIDDRONEPARTS_PICKUP", "All your minions copy your equipment uses. <style=cIsVoid>Corrupts all Spare Drone Parts</style>.");
            LanguageAPI.Add("ITEM_VOIDDRONEPARTS_DESC", "" +
                "All minions <style=cIsUtility>fire your equipment</style> when you fire it. " +
                //"This ability has a cooldown equipvelant to <style=cIsUtility>0%</style> <style=cStack>(-0% per stack)</style> of the equipments cooldown. " +
                "<style=cIsUtility>Reduce equipment cooldown</style> by <style=cIsUtility>0%</style> <style=cStack>(+40% per stack)</style>. " +
                "<style=cIsVoid>Corrupts all Spare Drone Parts</style>. ");
            LanguageAPI.Add("ITEM_VOIDDRONEPARTS_LORE", "All becomes crab eventually, mmm crab");

            //Having an independent cooldown would screw over Gesture (Good) but would also mean you couldn't use it multiple times in a row with stuff like Fuel Cells which is a bit more eh.
            
            //
            Texture2D SoapIcon = new Texture2D(128, 128, TextureFormat.DXT5, false);
            SoapIcon.LoadImage(Properties.Resources.IconDrone, true);
            SoapIcon.filterMode = FilterMode.Bilinear;
            SoapIcon.wrapMode = TextureWrapMode.Clamp;
            Sprite SoapIconS = Sprite.Create(SoapIcon, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
            //
            //ItemDef
            NewItemDef = ScriptableObject.CreateInstance<ItemDef>();
            NewItemDef.name = "VoidDroneWeapons";
            NewItemDef.deprecatedTier = ItemTier.VoidTier3;
            NewItemDef.nameToken = "ITEM_VOIDDRONEPARTS_NAME";
            NewItemDef.pickupToken = "ITEM_VOIDDRONEPARTS_PICKUP";
            NewItemDef.descriptionToken = "ITEM_VOIDDRONEPARTS_DESC";
            NewItemDef.loreToken = "ITEM_VOIDDRONEPARTS_LORE";
            NewItemDef.hidden = false;
            NewItemDef.canRemove = true;
            NewItemDef.pickupIconSprite = SoapIconS;
            NewItemDef.pickupModelPrefab = PickupModel;
            NewItemDef.requiredExpansion = DroneWeapons.requiredExpansion;
            NewItemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,
                ItemTag.Utility,
                ItemTag.CannotCopy,
                ItemTag.AIBlacklist,
            };

            ItemDisplayRuleDict DisplayRules = CreateItemDisplays();

            CustomItem customItem = new CustomItem(NewItemDef, DisplayRules);
            ItemAPI.Add(customItem);

            //
            ItemRelationshipProvider voidTransform = LegacyResourcesAPI.Load<ItemRelationshipProvider>("ItemRelationships/ContagiousItemProvider");
            ItemDef.Pair newTransform = new ItemDef.Pair
            {
                itemDef1 = DroneWeapons,
                itemDef2 = NewItemDef
            };
            voidTransform.relationships = voidTransform.relationships.Add(newTransform);
            //




            On.RoR2.CharacterBody.OnInventoryChanged += (orig, self) =>
            {
                orig(self);
                if (NetworkServer.active)
                {
                    self.AddItemBehavior<VoidDronePartsBehavior>(self.inventory.GetItemCount(NewItemDef));
                }
            };

            On.RoR2.Inventory.CalculateEquipmentCooldownScale += Inventory_CalculateEquipmentCooldownScale;

            CreatePickupModel();
            CreateItemDisplays();

            On.RoR2.EquipmentSlot.PerformEquipmentAction += CopyEquipmentActions;
        }

        private static float Inventory_CalculateEquipmentCooldownScale(On.RoR2.Inventory.orig_CalculateEquipmentCooldownScale orig, Inventory self)
        {
            int itemCount = self.GetItemCount(NewItemDef);
            float num = orig(self);
            if (itemCount > 1)
            {
                num *= Mathf.Pow(0.6f, (float)itemCount-1);
            }
            return num;
        }

        private static bool CopyEquipmentActions(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, EquipmentSlot self, EquipmentDef equipmentDef)
        {
            bool newBool = orig(self, equipmentDef);
            if (newBool == true)
            {
                var voidParts = self.GetComponent<VoidDronePartsBehavior>();
                if (voidParts)
                {
                    voidParts.FireEquipment(equipmentDef);
                }
            }

            return newBool;
        }

        public static void CreatePickupModel()
        {

        }

        private static ItemDisplayRuleDict CreateItemDisplays()
        {

            return null;
        }

    }

    public class VoidDronePartsBehavior : CharacterBody.ItemBehavior
    {
        public int cooldown;

        private void OnEnable()
        {
            MasterSummon.onServerMasterSummonGlobal += this.OnServerMasterSummonGlobal;
            UpdateAllMinions(stack);
            //Idk what we could use as a drone replacement or if there even needs to be one
        }

        private void OnDisable()
        {
            MasterSummon.onServerMasterSummonGlobal -= this.OnServerMasterSummonGlobal;
            UpdateAllMinions(stack);
        }

        private void UpdateAllMinions(int newStack)
        {
            EquipmentIndex equipmentIndex;

            if (newStack == 0)
            {
                //Remove all displays ig
            }

            if (this.body && this.body.master)
            {
                MinionOwnership.MinionGroup minionGroup = MinionOwnership.MinionGroup.FindGroup(this.body.master.netId);
                if (minionGroup != null)
                {
                    foreach (MinionOwnership minionOwnership in minionGroup.members)
                    {
                        if (minionOwnership)
                        {
                            CharacterMaster component = minionOwnership.GetComponent<CharacterMaster>();
                            if (component && component.inventory)
                            {
                                CharacterBody body2 = component.GetBody();
                                if (body2)
                                {
                                    if (!body2.equipmentSlot)
                                    {
                                        Debug.Log("Adding EquipmentSlot to "+body2);
                                        body2.gameObject.AddComponent<EquipmentSlot>();
                                        body2.equipmentSlot = body2.gameObject.GetComponent<EquipmentSlot>();
                                    }                  
                                }
                            }
                        }
                    }
                }
            }
        }


        private void OnServerMasterSummonGlobal(MasterSummon.MasterSummonReport summonReport)
        {
            if (this.body && this.body.master && this.body.master == summonReport.leaderMasterInstance)
            {
                CharacterMaster summonMasterInstance = summonReport.summonMasterInstance;
                if (summonMasterInstance)
                {
                    CharacterBody body = summonMasterInstance.GetBody();
                    if (body && !body.equipmentSlot)
                    {
                        body.gameObject.AddComponent<EquipmentSlot>();
                        body.equipmentSlot = body.gameObject.GetComponent<EquipmentSlot>();
                    }
                }
            }
        }

        public void FireEquipment(EquipmentDef equipmentDef)
        {
            if (this.body && this.body.master)
            {
                MinionOwnership.MinionGroup minionGroup = MinionOwnership.MinionGroup.FindGroup(this.body.master.netId);
                if (minionGroup != null)
                {
                    foreach (MinionOwnership minionOwnership in minionGroup.members)
                    {
                        if (minionOwnership)
                        {
                            CharacterMaster minionMaster = minionOwnership.GetComponent<CharacterMaster>();
                            if (minionMaster && minionMaster.inventory)
                            {
                                CharacterBody body2 = minionMaster.GetBody();
                                if (body2)
                                {
                                    Debug.Log(body2);
                                    if (!body2.equipmentSlot)
                                    {
                                        Debug.Log(body2 + "Has no equipment slot");
                                        body2.gameObject.AddComponent<EquipmentSlot>();
                                        body2.equipmentSlot = body2.gameObject.GetComponent<EquipmentSlot>();
                                    }
                                    else
                                    {
                                        if (minionMaster.inventory.GetItemCount(DLC1Content.Items.GummyCloneIdentifier) == 0)
                                        {
                                            body2.equipmentSlot.PerformEquipmentAction(equipmentDef);
                                        }
                                    }
      
                                    //Spawn some sort of visual effect probably

                                   /*if (!body2.equipmentSlot.currentTarget.hurtBox)
                                    {
                                        body2.equipmentSlot.currentTarget = body.equipmentSlot.currentTarget;
                                    }*/
  
                                }
                            }
                        }
                    }
                }
            }

        }
    }
    public class PickupDisplayTracker : CharacterBody.ItemBehavior
    {
        public PickupDisplay display;

        private void OnEnable()
        {

        }

        private void OnDisable()
        {

        }
    }
}