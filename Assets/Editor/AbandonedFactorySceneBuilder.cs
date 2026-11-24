using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LethalFactory.Environment;
using LethalFactory.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LethalFactory.EditorTools
{
    /// <summary>
    /// End-to-end automated Unity 6 URP builder for the Abandoned Factory scene,
    /// PBR materials, Character AnimatorControllers, Prefabs, Lighting, Fog, and Post-Processing.
    /// Automatically builds once on first import if AbandonedFactory.unity does not exist yet,
    /// and can be re-run anytime via Tools > LethalFactory > Build Abandoned Factory Scene.
    /// </summary>
    [InitializeOnLoad]
    public static class AbandonedFactorySceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/AbandonedFactory.unity";
        private const string MaterialsDir = "Assets/Art/Materials";
        private const string TexturesDir = "Assets/Art/Textures";
        private const string AnimDir = "Assets/Art/Animations";
        private const string PropsModelDir = "Assets/Art/Models/Props";
        private const string CharModelDir = "Assets/Art/Models/Characters";
        private const string ArchModelPath = "Assets/Art/Models/Environment/Factory_Architecture.fbx";
        private const string PrefabsCharDir = "Assets/Prefabs/Characters";
        private const string PrefabsPropDir = "Assets/Prefabs/Props";
        private const string VolumeProfilePath = "Assets/Settings/AbandonedFactoryVolumeProfile.asset";

        private static bool _autoBuildAttempted;

        static AbandonedFactorySceneBuilder()
        {
            EditorApplication.delayCall += TryAutoBuildOnce;
        }

        private static void TryAutoBuildOnce()
        {
            if (_autoBuildAttempted || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            _autoBuildAttempted = true;

            if (File.Exists(ScenePath))
                return;

            // Ensure required Blender FBX exports have been imported by Unity before auto-building
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ArchModelPath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>($"{CharModelDir}/Player_Hazmat.fbx") == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>($"{CharModelDir}/Monster_RamStalker.fbx") == null)
            {
                return;
            }

            BuildAll();
        }

        [MenuItem("Tools/LethalFactory/Build Abandoned Factory Scene")]
        public static void BuildAll()
        {
            Debug.Log("[LethalFactory] Starting full Abandoned Factory URP build...");

            EnsureDir(MaterialsDir);
            EnsureDir(AnimDir);
            EnsureDir(PrefabsCharDir);
            EnsureDir(PrefabsPropDir);
            EnsureDir("Assets/Scenes");
            EnsureDir("Assets/Settings");

            // 0. Guarantee textures (Normal/MaskMap) and FBXs (Rig/Animations/UV2) ran through FactoryAssetPostprocessor
            EnsureArtImportSettings();

            // 1. Create / update URP PBR materials
            var matMap = BuildAllMaterials();

            // 2. Create Animator Controllers for Player (Idle/Walk/Run) and Monster (Idle/Patrol/Chase/Howl)
            var playerAnimCtrl = BuildPlayerAnimatorController();
            var monsterAnimCtrl = BuildMonsterAnimatorController();

            // 3. Create Prefabs for all 15 Industrial Props
            var propPrefabs = BuildPropPrefabs(matMap);

            // 4. Create Character Prefabs (Player_Hazmat & Monster_RamStalker)
            var playerPrefab = BuildPlayerPrefab(matMap, playerAnimCtrl);
            var monsterPrefab = BuildMonsterPrefab(matMap, monsterAnimCtrl);

            // 5. Create lugubrious URP Post-Processing Volume Profile
            var volumeProfile = BuildVolumeProfile();

            // 6. Assemble and save Assets/Scenes/AbandonedFactory.unity
            BuildFactoryScene(matMap, propPrefabs, playerPrefab, monsterPrefab, volumeProfile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LethalFactory] Abandoned Factory scene, prefabs, materials, and animations built successfully!");
        }

        private static void EnsureDir(string assetDir)
        {
            if (!Directory.Exists(assetDir))
            {
                Directory.CreateDirectory(assetDir);
            }
        }

        private static void EnsureArtImportSettings()
        {
            // Force re-import of any normal/mask textures or character FBXs that Unity imported
            // before FactoryAssetPostprocessor was compiled on the first domain reload.
            var toReimport = new List<string>();

            if (Directory.Exists(TexturesDir))
            {
                foreach (string file in Directory.GetFiles(TexturesDir, "*.png"))
                {
                    string p = file.Replace('\\', '/');
                    var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                    if (ti == null) continue;
                    if (p.EndsWith("_Normal.png") && ti.textureType != TextureImporterType.NormalMap)
                        toReimport.Add(p);
                    else if (p.EndsWith("_MaskMap.png") && ti.sRGBTexture)
                        toReimport.Add(p);
                }
            }

            if (Directory.Exists(CharModelDir))
            {
                foreach (string file in Directory.GetFiles(CharModelDir, "*.fbx"))
                {
                    string p = file.Replace('\\', '/');
                    var mi = AssetImporter.GetAtPath(p) as ModelImporter;
                    if (mi == null) continue;
                    if (mi.clipAnimations == null || mi.clipAnimations.Length == 0 ||
                        mi.clipAnimations.Any(c => c.name.Contains("|")))
                    {
                        toReimport.Add(p);
                    }
                }
            }

            if (toReimport.Count > 0)
            {
                Debug.Log($"[LethalFactory] Reimporting {toReimport.Count} art assets with FactoryAssetPostprocessor settings...");
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (string p in toReimport)
                    {
                        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        // =====================================================================
        // 1. URP PBR MATERIALS
        // =====================================================================
        private static Dictionary<string, Material> BuildAllMaterials()
        {
            var dict = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

            // Architectural & Prop PBR Materials (Albedo + Normal + MetallicSmoothness MaskMap)
            dict["Mat_ConcreteFloor"] = CreateLitMaterial("Mat_ConcreteFloor", new Color(0.88f, 0.90f, 0.88f), "ConcreteFloor", 0.0f, 0.72f, 1.15f);
            dict["Mat_ConcreteWall"] = CreateLitMaterial("Mat_ConcreteWall", new Color(0.86f, 0.90f, 0.86f), "ConcreteWall", 0.0f, 0.58f, 1.20f);
            dict["Mat_RustedMetal"] = CreateLitMaterial("Mat_RustedMetal", new Color(0.95f, 0.92f, 0.90f), "RustedMetal", 1.0f, 0.78f, 1.15f);
            dict["Mat_CorrugatedMetal"] = CreateLitMaterial("Mat_CorrugatedMetal", new Color(0.88f, 0.90f, 0.92f), "CorrugatedMetal", 1.0f, 0.75f, 1.25f);
            dict["Mat_PaintedSteel"] = CreateLitMaterial("Mat_PaintedSteel", new Color(0.82f, 0.90f, 0.92f), "PaintedSteel", 1.0f, 0.75f, 1.0f);
            dict["Mat_SteelGrating"] = CreateLitMaterial("Mat_SteelGrating", new Color(0.92f, 0.94f, 0.96f), "SteelGrating", 1.0f, 0.80f, 1.3f);
            dict["Mat_HazardStripes"] = CreateLitMaterial("Mat_HazardStripes", Color.white, "HazardStripes", 1.0f, 0.70f, 1.0f);
            dict["Mat_CrateWood"] = CreateLitMaterial("Mat_CrateWood", new Color(0.92f, 0.88f, 0.82f), "CrateWood", 0.0f, 0.45f, 1.1f);
            dict["Mat_DarkIron"] = CreateLitMaterial("Mat_DarkIron", new Color(0.32f, 0.34f, 0.36f), "RustedMetal", 1.0f, 0.65f, 0.9f);
            dict["Mat_RedValve"] = CreateLitMaterial("Mat_RedValve", new Color(0.55f, 0.08f, 0.06f), null, 0.35f, 0.45f, 1.0f);
            dict["Mat_CraneYellow"] = CreateLitMaterial("Mat_CraneYellow", new Color(0.76f, 0.54f, 0.09f), "RustedMetal", 0.4f, 0.55f, 0.9f);

            // Transparent dirty observation glass + moonlit skylights
            dict["Mat_DirtyGlass"] = CreateTransparentGlassMaterial("Mat_DirtyGlass", new Color(0.32f, 0.44f, 0.46f, 0.28f));
            dict["Mat_SkylightGlass"] = CreateEmissiveMaterial("Mat_SkylightGlass", new Color(0.18f, 0.30f, 0.45f), new Color(0.35f, 0.58f, 0.92f) * 1.6f, 0.25f);

            // Emissive industrial fixtures
            dict["Mat_LampBulb"] = CreateEmissiveMaterial("Mat_LampBulb", new Color(1f, 0.9f, 0.7f), new Color(1.0f, 0.76f, 0.42f) * 3.8f, 0.2f);
            dict["Mat_FurnaceEmber"] = CreateEmissiveMaterial("Mat_FurnaceEmber", new Color(0.8f, 0.18f, 0.02f), new Color(1.0f, 0.26f, 0.03f) * 4.5f, 0.85f);
            dict["Mat_CRTGreen"] = CreateEmissiveMaterial("Mat_CRTGreen", new Color(0.08f, 0.65f, 0.25f), new Color(0.16f, 1.0f, 0.38f) * 2.4f, 0.15f);
            dict["Mat_CRTAmber"] = CreateEmissiveMaterial("Mat_CRTAmber", new Color(0.85f, 0.45f, 0.08f), new Color(1.0f, 0.58f, 0.12f) * 2.5f, 0.15f);
            dict["Mat_EmissiveRed"] = CreateEmissiveMaterial("Mat_EmissiveRed", new Color(0.85f, 0.06f, 0.04f), new Color(1.0f, 0.10f, 0.05f) * 4.2f, 0.15f);

            // Player Hazmat Suit materials
            dict["Mat_HazmatYellow"] = CreateLitMaterial("Mat_HazmatYellow", Color.white, "HazmatYellow", 0.05f, 0.65f, 0.85f);
            dict["Mat_HazmatDark"] = CreateLitMaterial("Mat_HazmatDark", new Color(0.09f, 0.095f, 0.10f), null, 0.12f, 0.32f, 1.0f);
            dict["Mat_HazmatMetal"] = CreateLitMaterial("Mat_HazmatMetal", new Color(0.42f, 0.45f, 0.48f), null, 0.82f, 0.62f, 1.0f);
            dict["Mat_HazmatTank"] = CreateLitMaterial("Mat_HazmatTank", new Color(0.64f, 0.25f, 0.08f), "RustedMetal", 0.6f, 0.58f, 0.6f);
            dict["Mat_HazmatVisor"] = CreateLitMaterial("Mat_HazmatVisor", new Color(0.03f, 0.05f, 0.08f), null, 0.88f, 0.92f, 1.0f);
            dict["Mat_StatusGreen"] = CreateEmissiveMaterial("Mat_StatusGreen", new Color(0.15f, 0.9f, 0.3f), new Color(0.15f, 1.0f, 0.35f) * 3.0f, 0.2f);

            // Monster Ram-Skull Stalker materials
            dict["Mat_MonsterHide"] = CreateLitMaterial("Mat_MonsterHide", new Color(0.55f, 0.50f, 0.52f), "MonsterFur", 0.05f, 0.26f, 1.25f);
            dict["Mat_MonsterFur"] = CreateLitMaterial("Mat_MonsterFur", new Color(0.42f, 0.38f, 0.40f), "MonsterFur", 0.0f, 0.14f, 1.45f);
            dict["Mat_RamBone"] = CreateLitMaterial("Mat_RamBone", new Color(1.0f, 0.96f, 0.88f), "RamBone", 0.02f, 0.44f, 1.15f);
            dict["Mat_RamHorn"] = CreateLitMaterial("Mat_RamHorn", new Color(0.38f, 0.31f, 0.24f), "RamBone", 0.06f, 0.48f, 1.35f);
            dict["Mat_MonsterSocket"] = CreateLitMaterial("Mat_MonsterSocket", new Color(0.01f, 0.01f, 0.012f), null, 0.0f, 0.05f, 1.0f);
            dict["Mat_MonsterEye"] = CreateEmissiveMaterial("Mat_MonsterEye", new Color(1.0f, 0.25f, 0.04f), new Color(1.0f, 0.32f, 0.05f) * 5.0f, 0.15f);
            dict["Mat_MonsterClaw"] = CreateLitMaterial("Mat_MonsterClaw", new Color(0.24f, 0.20f, 0.17f), "RamBone", 0.12f, 0.58f, 1.0f);

            return dict;
        }

        private static Material LoadOrCreateMaterial(string name)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = urpLit;
            }
            return mat;
        }

        private static Material CreateLitMaterial(string name, Color tint, string texPrefix, float metallic, float smoothness, float normalScale)
        {
            var mat = LoadOrCreateMaterial(name);
            mat.SetColor("_BaseColor", tint);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Surface", 0f);
            mat.DisableKeyword("_EMISSION");

            if (!string.IsNullOrEmpty(texPrefix))
            {
                var alb = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/{texPrefix}_Albedo.png");
                var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/{texPrefix}_Normal.png");
                var msk = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/{texPrefix}_MaskMap.png");

                if (alb != null) mat.SetTexture("_BaseMap", alb);
                if (nrm != null)
                {
                    mat.SetTexture("_BumpMap", nrm);
                    mat.SetFloat("_BumpScale", normalScale);
                    mat.EnableKeyword("_NORMALMAP");
                }
                if (msk != null)
                {
                    mat.SetTexture("_MetallicGlossMap", msk);
                    mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateEmissiveMaterial(string name, Color baseColor, Color emissionHdr, float smoothness)
        {
            var mat = LoadOrCreateMaterial(name);
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", smoothness);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionHdr);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateTransparentGlassMaterial(string name, Color color)
        {
            var mat = LoadOrCreateMaterial(name);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", 0.1f);
            mat.SetFloat("_Smoothness", 0.82f);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void RemapMaterials(GameObject root, Dictionary<string, Material> matMap)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var shared = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null) continue;
                    string rawName = shared[i].name;
                    // Strip Unity's " (Instance)" or Blender ".001" suffix if present
                    int dot = rawName.IndexOf('.');
                    string baseName = dot > 0 ? rawName.Substring(0, dot) : rawName;
                    baseName = baseName.Replace(" (Instance)", "").Trim();

                    if (matMap.TryGetValue(baseName, out var replacement))
                    {
                        shared[i] = replacement;
                        changed = true;
                    }
                }
                if (changed)
                {
                    r.sharedMaterials = shared;
                }
            }
        }

        // =====================================================================
        // 2. ANIMATOR CONTROLLERS (Player & Monster)
        // =====================================================================
        private static Dictionary<string, AnimationClip> LoadClipsFromFbx(string fbxPath)
        {
            var clips = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    clips[clip.name] = clip;
                }
            }
            return clips;
        }

        private static AnimationClip FindClip(Dictionary<string, AnimationClip> clips, string keyword)
        {
            foreach (var kv in clips)
            {
                if (kv.Key.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return kv.Value;
            }
            return clips.Values.FirstOrDefault();
        }

        private static AnimatorController BuildPlayerAnimatorController()
        {
            string ctrlPath = $"{AnimDir}/Player_AnimatorController.controller";
            if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);

            var clips = LoadClipsFromFbx($"{CharModelDir}/Player_Hazmat.fbx");
            var sm = ctrl.layers[0].stateMachine;

            var sIdle = sm.AddState("Player_Idle", new Vector3(240, 60, 0));
            var sWalk = sm.AddState("Player_Walk", new Vector3(500, 0, 0));
            var sRun = sm.AddState("Player_Run", new Vector3(500, 140, 0));

            sIdle.motion = FindClip(clips, "Idle");
            sWalk.motion = FindClip(clips, "Walk");
            sRun.motion = FindClip(clips, "Run");
            sm.defaultState = sIdle;

            // Idle -> Walk / Run
            var tIdleWalk = sIdle.AddTransition(sWalk);
            tIdleWalk.hasExitTime = false;
            tIdleWalk.duration = 0.12f;
            tIdleWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            tIdleWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");

            var tIdleRun = sIdle.AddTransition(sRun);
            tIdleRun.hasExitTime = false;
            tIdleRun.duration = 0.12f;
            tIdleRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            tIdleRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");

            // Walk <-> Run & Walk -> Idle
            var tWalkIdle = sWalk.AddTransition(sIdle);
            tWalkIdle.hasExitTime = false;
            tWalkIdle.duration = 0.15f;
            tWalkIdle.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed");

            var tWalkRun = sWalk.AddTransition(sRun);
            tWalkRun.hasExitTime = false;
            tWalkRun.duration = 0.12f;
            tWalkRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");

            var tRunWalk = sRun.AddTransition(sWalk);
            tRunWalk.hasExitTime = false;
            tRunWalk.duration = 0.14f;
            tRunWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
            tRunWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var tRunIdle = sRun.AddTransition(sIdle);
            tRunIdle.hasExitTime = false;
            tRunIdle.duration = 0.16f;
            tRunIdle.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed");

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        private static AnimatorController BuildMonsterAnimatorController()
        {
            string ctrlPath = $"{AnimDir}/Monster_AnimatorController.controller";
            if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("IsChasing", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Howl", AnimatorControllerParameterType.Trigger);

            var clips = LoadClipsFromFbx($"{CharModelDir}/Monster_RamStalker.fbx");
            var sm = ctrl.layers[0].stateMachine;

            var sIdle = sm.AddState("Monster_Idle", new Vector3(240, 80, 0));
            var sPatrol = sm.AddState("Monster_Patrol", new Vector3(520, 0, 0));
            var sChase = sm.AddState("Monster_Chase", new Vector3(520, 160, 0));
            var sHowl = sm.AddState("Monster_Howl", new Vector3(240, 220, 0));

            sIdle.motion = FindClip(clips, "Idle");
            sPatrol.motion = FindClip(clips, "Patrol");
            sChase.motion = FindClip(clips, "Chase");
            sHowl.motion = FindClip(clips, "Howl");
            sm.defaultState = sIdle;

            // Idle <-> Patrol <-> Chase (ready for AI team later; idles cleanly by default!)
            var tIP = sIdle.AddTransition(sPatrol);
            tIP.hasExitTime = false;
            tIP.duration = 0.2f;
            tIP.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            tIP.AddCondition(AnimatorConditionMode.IfNot, 0, "IsChasing");

            var tIC = sIdle.AddTransition(sChase);
            tIC.hasExitTime = false;
            tIC.duration = 0.15f;
            tIC.AddCondition(AnimatorConditionMode.If, 0, "IsChasing");

            var tPI = sPatrol.AddTransition(sIdle);
            tPI.hasExitTime = false;
            tPI.duration = 0.2f;
            tPI.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed");

            var tPC = sPatrol.AddTransition(sChase);
            tPC.hasExitTime = false;
            tPC.duration = 0.15f;
            tPC.AddCondition(AnimatorConditionMode.If, 0, "IsChasing");

            var tCP = sChase.AddTransition(sPatrol);
            tCP.hasExitTime = false;
            tCP.duration = 0.2f;
            tCP.AddCondition(AnimatorConditionMode.IfNot, 0, "IsChasing");
            tCP.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var tCI = sChase.AddTransition(sIdle);
            tCI.hasExitTime = false;
            tCI.duration = 0.22f;
            tCI.AddCondition(AnimatorConditionMode.IfNot, 0, "IsChasing");
            tCI.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed");

            // Spontaneous Howl trigger from Any State -> returns to Idle on finish
            var tAnyHowl = sm.AddAnyStateTransition(sHowl);
            tAnyHowl.hasExitTime = false;
            tAnyHowl.duration = 0.18f;
            tAnyHowl.AddCondition(AnimatorConditionMode.If, 0, "Howl");

            var tHowlBack = sHowl.AddTransition(sIdle);
            tHowlBack.hasExitTime = true;
            tHowlBack.exitTime = 0.94f;
            tHowlBack.duration = 0.2f;

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        // =====================================================================
        // 3. PROP PREFABS
        // =====================================================================
        private static Dictionary<string, GameObject> BuildPropPrefabs(Dictionary<string, Material> matMap)
        {
            var result = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(PropsModelDir)) return result;

            var noColliderProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Prop_CeilingLamp",
                "Prop_OverheadCrane",
                "Prop_PipeAssembly"
            };

            foreach (string file in Directory.GetFiles(PropsModelDir, "*.fbx"))
            {
                string assetPath = file.Replace('\\', '/');
                string propName = Path.GetFileNameWithoutExtension(assetPath);
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (fbx == null) continue;

                var temp = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                temp.name = propName;
                RemapMaterials(temp, matMap);

                if (!noColliderProps.Contains(propName))
                {
                    foreach (var mf in temp.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (mf.sharedMesh == null) continue;
                        if (propName == "Prop_PalletRack" || propName == "Prop_BlastFurnace")
                        {
                            var mc = mf.gameObject.AddComponent<MeshCollider>();
                            mc.sharedMesh = mf.sharedMesh;
                        }
                        else
                        {
                            mf.gameObject.AddComponent<BoxCollider>();
                        }
                    }
                }

                string prefabPath = $"{PrefabsPropDir}/{propName}.prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
                UnityEngine.Object.DestroyImmediate(temp);
                result[propName] = prefab;
            }

            return result;
        }

        // =====================================================================
        // 4. CHARACTER PREFABS (Player & Monster)
        // =====================================================================
        private static GameObject BuildPlayerPrefab(Dictionary<string, Material> matMap, AnimatorController animCtrl)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharModelDir}/Player_Hazmat.fbx");
            var root = new GameObject("Player_Hazmat");
            root.tag = "Player";

            // CharacterController sized coherently for 1.78m player
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.78f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.89f, 0f);
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.42f;
            cc.skinWidth = 0.04f;

            // Rigged Hazmat Model
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            visual.name = "HazmatRig";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            RemapMaterials(visual, matMap);

            var animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = animCtrl;
            animator.applyRootMotion = false;

            // First-Person Camera Pivot at eye height (1.62m) slightly forward so looking down reveals yellow suit!
            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.62f, 0.12f);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(pivot.transform, false);
            camGo.transform.localPosition = Vector3.zero;
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.08f;
            cam.farClipPlane = 140f;
            cam.fieldOfView = 72f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.05f);
            camGo.AddComponent<AudioListener>();

            var urpCam = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;

            // Suit Industrial Flashlight (toggle with F key!)
            var flashGo = new GameObject("SuitFlashlight");
            flashGo.transform.SetParent(camGo.transform, false);
            flashGo.transform.localPosition = new Vector3(0.16f, -0.14f, 0.10f);
            var flashLight = flashGo.AddComponent<Light>();
            flashLight.type = LightType.Spot;
            flashLight.color = new Color(1.0f, 0.94f, 0.80f);
            flashLight.intensity = 28f;
            flashLight.range = 28f;
            flashLight.spotAngle = 58f;
            flashLight.innerSpotAngle = 32f;
            flashLight.shadows = LightShadows.Soft;

            // Wire FirstPersonExplorer script references via SerializedObject
            var explorer = root.AddComponent<FirstPersonExplorer>();
            Renderer headRend = null;
            foreach (var r in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (r.name.Contains("Head")) { headRend = r; break; }
            }

            var so = new SerializedObject(explorer);
            so.FindProperty("cameraPivot").objectReferenceValue = pivot.transform;
            so.FindProperty("characterAnimator").objectReferenceValue = animator;
            so.FindProperty("headMeshRenderer").objectReferenceValue = headRend;
            so.FindProperty("suitFlashlight").objectReferenceValue = flashLight;
            so.ApplyModifiedPropertiesWithoutUndo();

            string prefabPath = $"{PrefabsCharDir}/Player_Hazmat.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildMonsterPrefab(Dictionary<string, Material> matMap, AnimatorController animCtrl)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharModelDir}/Monster_RamStalker.fbx");
            var root = new GameObject("Monster_RamStalker");

            // Physical presence collider matching 2.82m tall monster (NO AI or movement script!)
            var cap = root.AddComponent<CapsuleCollider>();
            cap.height = 2.75f;
            cap.radius = 0.52f;
            cap.center = new Vector3(0f, 1.38f, 0f);

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            visual.name = "RamStalkerRig";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            RemapMaterials(visual, matMap);

            var animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = animCtrl;
            animator.applyRootMotion = false;

            string prefabPath = $"{PrefabsCharDir}/Monster_RamStalker.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        // =====================================================================
        // 5. LUGUBRIOUS URP POST-PROCESSING VOLUME PROFILE
        // =====================================================================
        private static VolumeProfile BuildVolumeProfile()
        {
            if (File.Exists(VolumeProfilePath)) AssetDatabase.DeleteAsset(VolumeProfilePath);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.overrideState = true;
            tone.mode.value = TonemappingMode.ACES;

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.85f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.45f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.65f;

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.38f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.42f;
            vignette.color.overrideState = true;
            vignette.color.value = new Color(0.02f, 0.03f, 0.04f);

            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.overrideState = true;
            colorAdj.postExposure.value = 0.25f;
            colorAdj.contrast.overrideState = true;
            colorAdj.contrast.value = 16f;
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = -16f;
            colorAdj.colorFilter.overrideState = true;
            colorAdj.colorFilter.value = new Color(0.90f, 0.96f, 1.0f);

            var grain = profile.Add<FilmGrain>(true);
            grain.type.overrideState = true;
            grain.type.value = FilmGrainLookup.Thin1;
            grain.intensity.overrideState = true;
            grain.intensity.value = 0.22f;

            EditorUtility.SetDirty(profile);
            return profile;
        }

        // =====================================================================
        // 6. FULL ABANDONED FACTORY SCENE ASSEMBLY
        // =====================================================================
        private static void BuildFactoryScene(
            Dictionary<string, Material> matMap,
            Dictionary<string, GameObject> propPrefabs,
            GameObject playerPrefab,
            GameObject monsterPrefab,
            VolumeProfile volumeProfile)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lugubrious RenderSettings (Dark enclosed industrial fog + cold shadow ambient!)
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.07f, 0.09f, 0.11f);
            RenderSettings.ambientEquatorColor = new Color(0.045f, 0.055f, 0.06f);
            RenderSettings.ambientGroundColor = new Color(0.025f, 0.025f, 0.028f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.04f, 0.055f, 0.065f);
            RenderSettings.fogDensity = 0.026f;

            // Global Post-Processing Volume
            var volGo = new GameObject("GlobalPostProcessVolume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.profile = volumeProfile;

            // Subtle Moonlight Directional Light (casts faint eerie cold shafts through Turbine Hall skylights!)
            var moonGo = new GameObject("Moonlight_Directional");
            moonGo.transform.rotation = Quaternion.Euler(68f, -25f, 0f);
            var moonLight = moonGo.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = new Color(0.45f, 0.62f, 0.88f);
            moonLight.intensity = 0.16f;
            moonLight.shadows = LightShadows.Soft;

            // Instantiate Factory Architecture FBX
            var envRoot = new GameObject("Environment");
            var archFbx = AssetDatabase.LoadAssetAtPath<GameObject>(ArchModelPath);
            var archInst = (GameObject)PrefabUtility.InstantiatePrefab(archFbx);
            archInst.name = "Factory_Architecture";
            archInst.transform.SetParent(envRoot.transform, false);
            RemapMaterials(archInst, matMap);

            // Configure Architectural Colliders & Static flags cleanly!
            foreach (var mf in archInst.GetComponentsInChildren<MeshFilter>(true))
            {
                string n = mf.gameObject.name;
                mf.gameObject.isStatic = true;

                if (n.Contains("Arch_Stairs_RampCollider"))
                {
                    // Smooth walkable ramp collider — hide renderer so visual grated treads show cleanly!
                    var mr = mf.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = false;
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
                else if (n.Contains("Arch_Stairs_Visual") || n.Contains("Arch_Glass_Skylights"))
                {
                    // Visual only (stair treads walked via smooth ramp collider; glass skylights up high)
                }
                else if (mf.sharedMesh != null)
                {
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }

            // Containers for Props, Lighting & Characters
            var propsParent = new GameObject("Props");
            propsParent.transform.SetParent(envRoot.transform, false);

            var lightsParent = new GameObject("Lighting");
            var charsParent = new GameObject("Characters");

            // Read exact Anchor Empties embedded inside Factory_Architecture.fbx (0.000mm coordinate drift!)
            Transform anchorsRoot = FindChildRecursive(archInst.transform, "Anchors");
            Vector3 playerSpawnPos = new Vector3(0f, 0.05f, 23f);
            Quaternion playerSpawnRot = Quaternion.identity;
            Vector3 monsterSpawnPos = new Vector3(0f, 0f, 1.5f);
            Quaternion monsterSpawnRot = Quaternion.Euler(0f, 180f, 0f);

            if (anchorsRoot != null)
            {
                foreach (Transform anchor in anchorsRoot)
                {
                    string aname = anchor.name;
                    Quaternion anchorRot = GetAnchorHorizontalRotation(anchor);

                    if (aname == "Anchor_PlayerSpawn")
                    {
                        playerSpawnPos = anchor.position;
                        playerSpawnRot = anchorRot;
                    }
                    else if (aname == "Anchor_MonsterSpawn")
                    {
                        monsterSpawnPos = anchor.position;
                        monsterSpawnRot = anchorRot;
                    }
                    else if (aname.StartsWith("Anchor_Prop__"))
                    {
                        // Format: Anchor_Prop__Prop_CrateWooden__001
                        string[] parts = aname.Split(new[] { "__" }, StringSplitOptions.None);
                        if (parts.Length >= 2 && propPrefabs.TryGetValue(parts[1], out var prefab))
                        {
                            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                            inst.name = $"{parts[1]}_{(parts.Length > 2 ? parts[2] : "0")}";
                            inst.transform.SetParent(propsParent.transform, false);
                            inst.transform.position = anchor.position;
                            inst.transform.rotation = anchorRot;
                            inst.isStatic = true;
                        }
                    }
                    else if (aname.StartsWith("Anchor_Light__"))
                    {
                        // Format: Anchor_Light__WarmSodium__00
                        string[] parts = aname.Split(new[] { "__" }, StringSplitOptions.None);
                        if (parts.Length >= 2)
                        {
                            SpawnFactoryLight(parts[1], aname, anchor.position, lightsParent.transform);
                        }
                    }
                }

                // Disable empty anchor helpers in scene (keeps Prefab instance intact cleanly)
                anchorsRoot.gameObject.SetActive(false);
            }

            // Spawn Player Hazmat (First-Person Explorer) facing North into the Turbine Hall
            if (playerPrefab != null)
            {
                var pInst = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                pInst.name = "Player_Hazmat";
                pInst.transform.SetParent(charsParent.transform, false);
                pInst.transform.position = playerSpawnPos;
                pInst.transform.rotation = playerSpawnRot;
            }

            // Spawn Ram-Skull Monster (Rigged + Animated Idle/Patrol/Chase/Howl, NO AI!)
            if (monsterPrefab != null)
            {
                var mInst = (GameObject)PrefabUtility.InstantiatePrefab(monsterPrefab);
                mInst.name = "Monster_RamStalker";
                mInst.transform.SetParent(charsParent.transform, false);
                mInst.transform.position = monsterSpawnPos;
                mInst.transform.rotation = monsterSpawnRot;
            }

            // Add subtle floating dust motes in Central Turbine Hall
            CreateDustMotes(envRoot.transform, new Vector3(0f, 4.5f, 2.0f));

            // Save scene & set as Build Settings index 0
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings(ScenePath);
        }

        private static void SpawnFactoryLight(string lightType, string name, Vector3 pos, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.shadows = LightShadows.None;

            switch (lightType)
            {
                case "WarmSodium":
                    l.color = new Color(1.0f, 0.72f, 0.38f);
                    l.intensity = 14f;
                    l.range = 17f;
                    break;
                case "FlickerLamp":
                    l.color = new Color(0.84f, 0.94f, 0.86f);
                    l.intensity = 12f;
                    l.range = 15f;
                    go.AddComponent<FlickeringLight>();
                    break;
                case "ColdMoon":
                    l.type = LightType.Spot;
                    go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    l.color = new Color(0.42f, 0.64f, 0.95f);
                    l.intensity = 42f;
                    l.range = 16f;
                    l.spotAngle = 82f;
                    l.innerSpotAngle = 42f;
                    l.shadows = LightShadows.Soft;
                    break;
                case "RedEmergency":
                    l.color = new Color(1.0f, 0.14f, 0.08f);
                    l.intensity = 10f;
                    l.range = 13f;
                    break;
                case "FurnaceEmber":
                    l.color = new Color(1.0f, 0.34f, 0.06f);
                    l.intensity = 22f;
                    l.range = 16f;
                    l.shadows = LightShadows.Soft;
                    go.AddComponent<FlickeringLight>();
                    break;
                case "GreenCRT":
                    l.color = new Color(0.18f, 0.95f, 0.40f);
                    l.intensity = 5.5f;
                    l.range = 8f;
                    break;
                case "AmberControl":
                    l.color = new Color(1.0f, 0.62f, 0.22f);
                    l.intensity = 11f;
                    l.range = 13f;
                    break;
                default:
                    l.color = new Color(0.85f, 0.85f, 0.80f);
                    l.intensity = 10f;
                    l.range = 14f;
                    break;
            }
        }

        private static void CreateDustMotes(Transform parent, Vector3 pos)
        {
            var go = new GameObject("AmbientDustMotes");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 12f;
            main.startSpeed = 0.08f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.065f);
            main.startColor = new Color(0.75f, 0.80f, 0.78f, 0.22f);
            main.maxParticles = 240;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;

            var emission = ps.emission;
            emission.rateOverTime = 20f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 7f, 24f);

            var psr = go.GetComponent<ParticleSystemRenderer>();
            Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (pShader != null)
            {
                var pMat = new Material(pShader);
                pMat.SetColor("_BaseColor", new Color(0.78f, 0.82f, 0.80f, 0.25f));
                pMat.SetFloat("_Surface", 1f);
                pMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                pMat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                pMat.SetFloat("_ZWrite", 0f);
                pMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                pMat.renderQueue = (int)RenderQueue.Transparent;
                AssetDatabase.CreateAsset(pMat, $"{MaterialsDir}/Mat_DustMotes.mat");
                psr.sharedMaterial = pMat;
            }
        }

        private static Quaternion GetAnchorHorizontalRotation(Transform anchor)
        {
            // Blender assets face local -Y with +Z up. On Unity FBX import:
            // - Unbaked Blender empties have local +Z pointing world +Y (so local -Y is -anchor.up)
            // - Baked empties have local +Y pointing world +Y (so forward is anchor.forward)
            Vector3 fwd = Mathf.Abs(anchor.up.y) > 0.7f ? anchor.forward : -anchor.up;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f)
                return Quaternion.identity;
            return Quaternion.LookRotation(fwd.normalized, Vector3.up);
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var list = EditorBuildSettings.scenes.ToList();
            list.RemoveAll(s => s.path == scenePath);
            list.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
