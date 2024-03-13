using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Mono.Cecil.Cil;
using MonoMod.Cil;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

namespace LunarSoap
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("Wolfo.LunarSoap", "LunarSoap", "1.0.0")]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]

    public class LunarSoap : BaseUnityPlugin
    {
        public static ItemDef ItemDef;
        public static GameObject DisplayItem;
        public static GameObject PickupModel;

        public void Awake()
        {
            ItemDef RandomlyLunar = LegacyResourcesAPI.Load<ItemDef>("ItemDefs/RandomlyLunar");
            ItemTierDef LunarTierDef = LegacyResourcesAPI.Load<ItemTierDef>("ItemTierDefs/LunarTierDef");
            PickupModel = PrefabAPI.InstantiateClone(RandomlyLunar.pickupModelPrefab, "LunarSoapPickup", false);
            DisplayItem = PrefabAPI.InstantiateClone(new GameObject(), "DisplayLunarSoap", false);
            CreatePickupModel();

            LanguageAPI.Add("ITEM_LUNARSOAP_NAME", "Temporal Soap");
            LanguageAPI.Add("ITEM_LUNARSOAP_PICKUP", "Doubled movement speed... <color=#FF7F7F>BUT movement is harder to control.</color>");
            LanguageAPI.Add("ITEM_LUNARSOAP_LORE", "" +
                "Order: Special Soap\r\n" +
                "Tracking Number: 2*****\r\nEstimated Delivery: 09/24/2056\r\n" +
                "Shipping Method:  Expedited\r\n" +
                "Shipping Address: 5757 Main St, Frisco, TX 75034, USA, Earth\r\n" +
                "Shipping Details:\r\n" +
                "\r\n" +
                "If this delivery ever reaches the writing team they better come up with a new slogan for this new soap because we're fresh out of ideas over here. Maybe make a joke about Chiralium we're sure someone would appreciate that.");
            //
            Texture2D SoapIcon = new Texture2D(128, 128, TextureFormat.DXT5, false);
            SoapIcon.LoadImage(Properties.Resources.Icon, true);
            SoapIcon.filterMode = FilterMode.Bilinear;
            SoapIcon.wrapMode = TextureWrapMode.Clamp;
            Sprite SoapIconS = Sprite.Create(SoapIcon, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
            //
            //ItemDef
            ItemDef = ScriptableObject.CreateInstance<ItemDef>();
            ItemDef.name = "LunarSoap";
            ItemDef.deprecatedTier = ItemTier.Lunar;
            ItemDef._itemTierDef = LunarTierDef;
            ItemDef.nameToken = "ITEM_LUNARSOAP_NAME";
            ItemDef.pickupToken = "ITEM_LUNARSOAP_PICKUP";
            ItemDef.descriptionToken = "ITEM_LUNARSOAP_DESC";
            ItemDef.loreToken = "ITEM_LUNARSOAP_LORE";
            ItemDef.hidden = false;
            ItemDef.canRemove = true;
            ItemDef.pickupIconSprite = SoapIconS;
            ItemDef.pickupModelPrefab = PickupModel;
            ItemDef.tags = new ItemTag[]
            {
                ItemTag.Utility,
                ItemTag.CannotCopy,
                ItemTag.AIBlacklist,
            };

            ItemDisplayRuleDict DisplayRules = MakeItemDisplays();

            CustomItem customItem = new CustomItem(ItemDef, DisplayRules);
            ItemAPI.Add(customItem);

            On.RoR2.CharacterBody.RecalculateStats += Stats_VariantWalkSpeed;
            LanguageAPI.Add("ITEM_LUNARSOAP_DESC", "Increases <style=cIsUtility>maxiumum walk speed</style> by <style=cIsUtility>100%</style> <style=cStack>(+40% per stack)</style>. " +
                "<style=cIsUtility>Reduces acceleration and deceleration by 80%</style> <style=cStack>(+20% per stack)</style>. " +
                "Acceleration no longer scales with movement speed.");


            On.RoR2.CharacterBody.OnInventoryChanged += (orig, self) =>
            {
                orig(self);
                self.AddItemBehavior<LunarSoapBehavior>(self.inventory.GetItemCount(ItemDef));
            };
            //
            IL.EntityStates.BaseCharacterMain.UpdateAnimationParameters += ScaleAnimationWithWalkCoeff;
        }

        private void ScaleAnimationWithWalkCoeff(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld("RoR2.AnimationParameters", "walkSpeed"));
            if (c.TryGotoNext(MoveType.After,
            x => x.MatchCallvirt("RoR2.CharacterBody", "get_moveSpeed")
            ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<System.Func<float, EntityStates.BaseCharacterMain, float>>((speed, state) =>
                {
                    if (state.hasCharacterMotor)
                    {
                        return state.characterMotor.walkSpeed;
                    }
                    return speed;
                });
                Debug.Log("IL Found : IL.EntityStates.BaseCharacterMain.UpdateAnimationParameters");
            }
            else
            {
                Debug.LogWarning("IL Failed : IL.EntityStates.BaseCharacterMain.UpdateAnimationParameters");
            }
        }

        private static ItemDisplayRuleDict MakeItemDisplays()
        {
            ItemDisplayRule[] DefaultRules = new ItemDisplayRule[]
            {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = DisplayItem,
                            childName = "FootR",
                            localPos = new Vector3(0f, 0f, 0f),
                            localAngles = new Vector3(0f,180f,0f),
                            localScale = new Vector3(0.1f,0.1f,0.1f),
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = DisplayItem,
                            childName = "FootL",
                            localPos = new Vector3(0f, 0f, 0f),
                            localAngles = new Vector3(0f,0f,0f),
                            localScale = new Vector3(0.1f,0.1f,0.1f),
                        },
            };

            ItemDisplayRule[] CHEFRules = new ItemDisplayRule[]
{
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = DisplayItem,
                            childName = "RightLeg",
                            localPos = new Vector3(0f, 0f, 0f),
                            localAngles = new Vector3(0f,180f,0f),
                            localScale = new Vector3(0.1f,0.1f,0.1f),
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = DisplayItem,
                            childName = "LeftLeg",
                            localPos = new Vector3(0f, 0f, 0f),
                            localAngles = new Vector3(0f,0f,0f),
                            localScale = new Vector3(0.1f,0.1f,0.1f),
                        },
            };

            ItemDisplayRule[] MULTRules = new ItemDisplayRule[]
{
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = DisplayItem,
                            childName = "MainWheelR",
                            localPos = new Vector3(0f, 0f, 0f),
                            localAngles = new Vector3(0f,180f,0f),
                            localScale = new Vector3(0.1f,0.1f,0.1f),
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = DisplayItem,
                            childName = "MainWheelL",
                            localPos = new Vector3(0f, 0f, 0f),
                            localAngles = new Vector3(0f,0f,0f),
                            localScale = new Vector3(0.1f,0.1f,0.1f),
                        },
};

            ItemDisplayRule[] REXRules = new ItemDisplayRule[]
{
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = DisplayItem,
                            childName = "FootBackR",
                            localPos = new Vector3(0f, 1f, 0f),
                            localAngles = new Vector3(0f,180f,0f),
                            localScale = new Vector3(0.1f,0.1f,0.1f),
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = DisplayItem,
                            childName = "FootBackL",
                            localPos = new Vector3(0f, 1f, 0f),
                            localAngles = new Vector3(0f,0f,0f),
                            localScale = new Vector3(0.1f,0.1f,0.1f),
                        },
                                                new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = DisplayItem,
                            childName = "FootFrontR",
                            localPos = new Vector3(0f, 1f, 0f),
                            localAngles = new Vector3(0f,180f,0f),
                            localScale = new Vector3(0.1f,0.1f,0.1f),
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = DisplayItem,
                            childName = "FootFrontL",
                            localPos = new Vector3(0f, 1f, 0f),
                            localAngles = new Vector3(0f,0f,0f),
                            localScale = new Vector3(0.1f,0.1f,0.1f),
                        },
};

            ItemDisplayRuleDict DisplayRules = new ItemDisplayRuleDict(DefaultRules);
            DisplayRules.Add("NemesisEnforcerBody", DefaultRules);
            DisplayRules.Add("MinerBody", DefaultRules);
            DisplayRules.Add("CHEF", CHEFRules);
            DisplayRules.Add("ToolbotBody", MULTRules);
            DisplayRules.Add("TreebotBody", REXRules);

            return DisplayRules;
        }


        private static void Stats_VariantWalkSpeed(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (self.inventory)
            {
                int num = self.inventory.GetItemCount(ItemDef);
                if (num > 0)
                {
                    num--;
                    if (self.characterMotor)
                    {
                        self.characterMotor.walkSpeedPenaltyCoefficient = 2f + 0.4f * (num);
                        //self.characterMotor.walkSpeedPenaltyCoefficient = WConfig.SpeedMult.Value + WConfig.SpeedMultStack.Value * (num);
                    }
                    self.acceleration = self.baseAcceleration * 2 * (0.2f / (1f + num * 0.2f));
                    //self.acceleration = self.baseAcceleration * 2 / (5f + 1f * (num));
                    if (self.isSprinting)
                    {
                        self.acceleration *= 0.875f;
                    }
                }
            }
        }


        public static void CreatePickupModel()
        {
            GameObject BlueprintStation = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/BlueprintStation");
            GameObject PillarFull = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/MoonBatteryBlood");
            GameObject PillarCube = PillarFull.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(1).transform.GetChild(0).gameObject;

            GameObject CrabFoam1 = Addressables.LoadAssetAsync<GameObject>(key: "RoR2/Base/arena/CrabFoam1Prop.prefab").WaitForCompletion();

            GameObject FullArtifactFormulaDisplay = Addressables.LoadAssetAsync<GameObject>(key: "RoR2/Base/artifactworld/ArtifactFormulaDisplay.prefab").WaitForCompletion();
            GameObject ArtifactHolder = FullArtifactFormulaDisplay.transform.FindChild("ArtifactFormulaHolderMesh").gameObject;
            GameObject PickupLunarCoin = LegacyResourcesAPI.Load<GameObject>("Prefabs/PickupModels/PickupLunarCoin");

            Destroy(PickupModel.transform.GetChild(0).gameObject);

            PickupModel.GetComponent<ModelPanelParameters>().minDistance = 5;
            PickupModel.GetComponent<ModelPanelParameters>().maxDistance = 10;
            PickupModel.GetComponent<ModelPanelParameters>().cameraPositionTransform.localPosition = new Vector3(1, 1, -0.3f); //1 1 -0.3
            PickupModel.GetComponent<ModelPanelParameters>().cameraPositionTransform.localEulerAngles = new Vector3(0, 0, 0);
            PickupModel.GetComponent<ModelPanelParameters>().focusPointTransform.localPosition = new Vector3(0, 1, -0.3f);
            PickupModel.GetComponent<ModelPanelParameters>().focusPointTransform.localEulerAngles = new Vector3(0, 0, 0);

            GameObject MainSoap = Instantiate(PillarCube, PickupModel.transform);
            MainSoap.name = "SoapMainBrick";
            MainSoap.GetComponent<MeshFilter>().mesh = ArtifactHolder.GetComponent<MeshFilter>().mesh;
            MainSoap.transform.localPosition = new Vector3(0, -1.5f, 0);
            MainSoap.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            //MainSoap.transform.localEulerAngles = new Vector3(75, 340, 75);
            MainSoap.transform.localEulerAngles = new Vector3(80, 85, 180);//85 85 180

            Material OGMaterial = Addressables.LoadAssetAsync<Material>(key: "RoR2/Base/EliteIce/matEliteIce.mat").WaitForCompletion();
            Material NewMatIce1 = Instantiate(OGMaterial);
            Material NewMatIce2 = Instantiate(OGMaterial);

            NewMatIce1.name = "matLunarSoapMain";
            NewMatIce1.SetFloat("_FresnelBoost", 2.4f);
            NewMatIce1.color = new Color(0.6f, 0.8f, 1);
            NewMatIce1.enableInstancing = false;

            NewMatIce2.name = "matLunarSoapCoin";
            NewMatIce2.SetFloat("_FresnelBoost", 6.4f);
            NewMatIce2.color = new Color(0.6f, 0.8f, 1);
            NewMatIce2.enableInstancing = false;
           

            MainSoap.GetComponent<MeshRenderer>().material = NewMatIce1;
            Destroy(MainSoap.GetComponent<BoxCollider>());
            Destroy(MainSoap.GetComponent<DitherModel>());
            Destroy(MainSoap.GetComponent<NonSolidToCamera>());
            Destroy(MainSoap.GetComponent<EntityLocator>());

            GameObject SoapEmblem = Instantiate(PickupLunarCoin.transform.GetChild(0).gameObject, MainSoap.transform);
            SoapEmblem.name = "CoinEmblem";
            SoapEmblem.transform.localPosition = new Vector3(0, -1.1f, -4);
            SoapEmblem.transform.localScale = new Vector3(1.18f, 1.8f, 1.18f);
            SoapEmblem.transform.localEulerAngles = new Vector3(0, 0, 0);
            SoapEmblem.GetComponent<MeshRenderer>().material = NewMatIce2;
            Destroy(SoapEmblem.transform.GetChild(0).gameObject);

            //Probably do use crab foam
            GameObject BubbleParticles = Instantiate(BlueprintStation.transform.GetChild(0).transform.GetChild(0).gameObject, MainSoap.transform);
            ParticleSystem BubbleSystem = BubbleParticles.GetComponent<ParticleSystem>();
            BubbleParticles.transform.localPosition = new Vector3(0, 0, -4.5f);
            BubbleParticles.transform.localEulerAngles = new Vector3(0, 0, 0);
            //BubbleParticles.transform.localScale = new Vector3(1.6f, 2.6f, 2.6f);
            BubbleParticles.transform.localScale = new Vector3(3f, 3f, 3f);

            var Shape = BubbleSystem.shape;
            Shape.shapeType = ParticleSystemShapeType.Sphere;
            Shape.radius = 1.1f;
            Shape.scale = new Vector3(1, 1f, 1.9f);
            BubbleSystem.gravityModifier = 0f;

            BubbleSystem.playbackSpeed = 0.15f;
            BubbleSystem.emissionRate = 18;
            BubbleSystem.scalingMode = ParticleSystemScalingMode.Hierarchy;


            GameObject Bubble = Instantiate(BlueprintStation.transform.GetChild(0).transform.GetChild(1).gameObject, MainSoap.transform);
            Bubble.transform.localPosition = new Vector3(0, 0, -4f);
            Bubble.transform.localEulerAngles = new Vector3(0, 0, 0);
            Bubble.transform.localScale = new Vector3(4f, 3f, 6f);
            Bubble.SetActive(false);

            GameObject BottomBubbles = Instantiate(CrabFoam1, MainSoap.transform);
            BottomBubbles.name = "BottomBubbles1";
            BottomBubbles.GetComponent<MeshRenderer>().material = Bubble.GetComponent<MeshRenderer>().material;
            Destroy(BottomBubbles.GetComponent<MeshCollider>());
            Destroy(BottomBubbles.GetComponent<SurfaceDefProvider>());

            Material NewMatBubble1 = Instantiate(BottomBubbles.GetComponent<MeshRenderer>().material);
            NewMatBubble1.SetFloat("_InvFade", 8);
            NewMatBubble1.enableInstancing = false;
            NewMatBubble1.name = "matLunarSoapBubbles";
            BottomBubbles.GetComponent<MeshRenderer>().material = NewMatBubble1;

            //BottomBubbles.transform.localPosition = new Vector3(1.41f, 0.6f, -0.58f);
            //BottomBubbles.transform.rotation = new Quaternion(-0.7366f, 0.3013f, -0.5325f, -0.2883f);
            BottomBubbles.transform.localScale = new Vector3(1.55f, 1.55f, 1.55f);
            BottomBubbles.transform.localPosition = new Vector3(1.81f, 0.4f, -0.58f);
            BottomBubbles.transform.localEulerAngles = new Vector3(0, 0, 0);

            BottomBubbles = Instantiate(BottomBubbles, MainSoap.transform);
            BottomBubbles.name = "BottomBubbles2";
            BottomBubbles.GetComponent<MeshRenderer>().material = NewMatBubble1;
            BottomBubbles.transform.localPosition = new Vector3(-1.68f, -0.49f, -0.4f);
            //BottomBubbles.transform.localPosition = new Vector3(-1.78f, 0.11f, -0.4f);
            //BottomBubbles.transform.rotation = new Quaternion(-0.7505f, 0.0443f, -0.0505f, -0.6575f);
            BottomBubbles.transform.rotation = new Quaternion(0.8168f, 0.1933f, 0.4053f, -0.3623f);
            BottomBubbles.transform.localScale = new Vector3(1.55f, 1.55f, 1.55f);
            Destroy(BottomBubbles.GetComponent<MeshCollider>());
            Destroy(BottomBubbles.GetComponent<SurfaceDefProvider>());
            //
            //
            //Item Display
            GameObject DisplayBubbles = Instantiate(BottomBubbles, DisplayItem.transform);
            DisplayBubbles.name = "BottomBubbles";
            Destroy(DisplayBubbles.GetComponent<MeshCollider>());
            Destroy(DisplayBubbles.GetComponent<SurfaceDefProvider>());

            DisplayBubbles.GetComponent<MeshRenderer>().material = NewMatBubble1;
            DisplayBubbles.transform.localScale = new Vector3(1f, 1f, 1f);
            DisplayBubbles.transform.localPosition = new Vector3(0, 0, 0);
            DisplayBubbles.transform.localEulerAngles = new Vector3(0, 0, 0);
            DisplayBubbles.SetActive(false);

            GameObject DisplayParticles = Instantiate(BubbleParticles, DisplayItem.transform);
            DisplayParticles.name = "BubbleParticlesDisplay";
            BubbleSystem = DisplayParticles.GetComponent<ParticleSystem>();
            DisplayParticles.transform.localPosition = new Vector3(0, 0, 0f);
            DisplayParticles.transform.localEulerAngles = new Vector3(90, 0, 0);
            DisplayParticles.transform.localScale = new Vector3(1f, 1f, 1f);

            DisplayParticles.GetComponent<ParticleSystemRenderer>().material = NewMatBubble1;

            var Main = BubbleSystem.main;
            Main.cullingMode = ParticleSystemCullingMode.Automatic;

            var Curve = Main.startLifetime;
            Curve.m_ConstantMin = 0.25f;
            Main.startLifetime = Curve;

            //Debug.Log(Curve.constantMin);
            //Debug.Log(Main.startLifetime.m_ConstantMin);

            Curve = Main.startSize;
            Curve.mode = ParticleSystemCurveMode.TwoConstants;
            Curve.m_ConstantMin = 0.1f;
            Main.startSize = Curve;
            Main.startSizeX = Curve;

            //Debug.Log(Curve.constantMin);
            //Debug.Log(Main.startSize.m_ConstantMin);

            //Shape
            Shape = BubbleSystem.shape;
            Shape.shapeType = ParticleSystemShapeType.Circle;
            Shape.radius = 0.4f;
            Shape.scale = new Vector3(1f, 1f, 1f);
            Shape.rotation = new Vector3(90f, 0f, 0f);
            //
            //Size over time
            var SizeOverTime = BubbleSystem.sizeOverLifetime;
            var SizeSizeSize = SizeOverTime.size;
            var SizeCurve = SizeOverTime.size.curveMax;
            UnityEngine.Keyframe[] SizeKeys = SizeCurve.keys;
            SizeKeys[0].value = 0.15f;
            SizeKeys[1].time = 0.8f;

            SizeCurve.SetKeys(SizeKeys);
            SizeSizeSize.curve = SizeCurve;
            SizeSizeSize.curveMax = SizeCurve;
            SizeOverTime.size = SizeSizeSize;
            //
            BubbleSystem.transform.localPosition = new Vector3(0.05f, 0.05f, -0.15f);
            BubbleSystem.gravityModifier = -0.15f;
            BubbleSystem.playbackSpeed = 0.25f;
            BubbleSystem.startLifetime = 0.5f;
            BubbleSystem.startSize = 0.25f;
            BubbleSystem.emissionRate = 10;
            //BubbleSystem.startSpeed = 0.2f;
            BubbleSystem.scalingMode = ParticleSystemScalingMode.Local;
            BubbleSystem.simulationSpace = ParticleSystemSimulationSpace.World;

            ItemDisplay itemDisplay = DisplayItem.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = new CharacterModel.RendererInfo[]
            {
                new CharacterModel.RendererInfo
                {
                    defaultMaterial = DisplayParticles.GetComponent<ParticleSystemRenderer>().material,
                    renderer = DisplayParticles.GetComponent<ParticleSystemRenderer>(),
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                }
            };

            //matEliteIce
            //matIceAuraSphere
            //matIceRingSplash 
            //matFrostRelic 
        }

    }

    internal class LunarSoapBehavior : CharacterBody.ItemBehavior
    {
        private void Start()
        {
            Debug.Log("Start Soap");
            if (body.baseAcceleration >= 80)
            {
                body.baseAcceleration /= 2;
            }
            else
            {
                body.baseAcceleration = 40;
            }
            if (this.body.characterMotor)
            {
                body.characterMotor.airControl *= 2;
                //body.characterMotor.mass /= 1.5f;
            }
        }

        private void OnDisable()
        {
            if (body)
            {
                body.baseAcceleration = this.body.master.bodyPrefab.GetComponent<CharacterBody>().baseAcceleration;
                CharacterMotor og = this.body.master.bodyPrefab.GetComponent<CharacterMotor>();
                if (og)
                {
                    this.body.characterMotor.walkSpeedPenaltyCoefficient = 1;
                    this.body.characterMotor.mass = og.mass;
                    this.body.characterMotor.airControl = og.airControl;
                }
            }
        }
    }
}