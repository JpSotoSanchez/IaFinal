using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// PROVISIONAL: genera la escena Assets/Scenes/TestMap.unity para probar a los enemigos.
// Menú: IA Final > Construir mapa de prueba
//
// Mapa de 60x60: la mitad sur es "interior" (pasillos y un cuarto central), la mitad norte es
// "exterior" (abierto, con rocas) donde viven los 3 Eyeless Dogs. Una puerta une las dos zonas.
public static class TestMapBuilder
{
    const string ScenePath = "Assets/Scenes/TestMap.unity";
    const string DataFolder = "Assets/TestMap";
    const string MaterialsFolder = DataFolder + "/Materials";
    const string NavMeshPath = DataFolder + "/NavMesh-TestMap.asset";

    [MenuItem("IA Final/Construir mapa de prueba")]
    public static void Build()
    {
        if (!Application.isBatchMode)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (System.IO.File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Mapa de prueba",
                    "Ya existe TestMap.unity. ¿Reconstruirlo? (se pierden los cambios hechos a mano)",
                    "Reconstruir", "Cancelar"))
                return;
        }

        EnsureFolder("Assets", "TestMap");
        EnsureFolder(DataFolder, "Materials");
        EnsureFolder("Assets", "Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateLight();

        GameObject level = new GameObject("Level");
        CreateGeometry(level.transform);

        Transform player = CreatePlayer(new Vector3(0f, 1f, -26f));
        CreateEnemies(player);

        BakeNavMesh(level);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Mapa de prueba creado en " + ScenePath);
    }

    // ------------------------------------------------------------------
    // Geometría
    // ------------------------------------------------------------------

    static void CreateGeometry(Transform level)
    {
        Material floorMat = GetMaterial("Floor", new Color(0.35f, 0.35f, 0.38f));
        Material outsideMat = GetMaterial("FloorOutside", new Color(0.30f, 0.38f, 0.28f));
        Material wallMat = GetMaterial("Wall", new Color(0.75f, 0.72f, 0.65f));
        Material rockMat = GetMaterial("Rock", new Color(0.45f, 0.42f, 0.40f));

        // Pisos (la parte de arriba queda en y = 0)
        Box("Floor_Interior", level, new Vector3(0, -0.5f, -10f), new Vector3(60, 1, 40), floorMat);
        Box("Floor_Exterior", level, new Vector3(0, -0.5f, 20f), new Vector3(60, 1, 20), outsideMat);

        const float h = 3f;
        const float y = h / 2f;

        // Borde
        Box("Wall_N", level, new Vector3(0, y, 30.5f), new Vector3(62, h, 1), wallMat);
        Box("Wall_S", level, new Vector3(0, y, -30.5f), new Vector3(62, h, 1), wallMat);
        Box("Wall_E", level, new Vector3(30.5f, y, 0), new Vector3(1, h, 60), wallMat);
        Box("Wall_W", level, new Vector3(-30.5f, y, 0), new Vector3(1, h, 60), wallMat);

        // Divisor interior / exterior con puerta de 8 m en el centro
        Box("Divider_W", level, new Vector3(-17f, y, 10f), new Vector3(26, h, 1), wallMat);
        Box("Divider_E", level, new Vector3(17f, y, 10f), new Vector3(26, h, 1), wallMat);

        // Pasillos laterales
        Box("Corridor_W", level, new Vector3(-15f, y, -12f), new Vector3(1, h, 20), wallMat);
        Box("Corridor_E", level, new Vector3(15f, y, -12f), new Vector3(1, h, 20), wallMat);

        // Cuarto central (con aberturas en las esquinas)
        Box("Room_N", level, new Vector3(0, y, -4f), new Vector3(12, h, 1), wallMat);
        Box("Room_S", level, new Vector3(0, y, -18f), new Vector3(12, h, 1), wallMat);
        Box("Room_W", level, new Vector3(-7.5f, y, -11f), new Vector3(1, h, 8), wallMat);
        Box("Room_E", level, new Vector3(7.5f, y, -11f), new Vector3(1, h, 8), wallMat);

        // Pilares para romper línea de vista
        Box("Pillar_1", level, new Vector3(-23f, y, -6f), new Vector3(2, h, 2), wallMat);
        Box("Pillar_2", level, new Vector3(23f, y, -16f), new Vector3(2, h, 2), wallMat);
        Box("Pillar_3", level, new Vector3(-8f, y, 4f), new Vector3(2, h, 2), wallMat);
        Box("Pillar_4", level, new Vector3(8f, y, 4f), new Vector3(2, h, 2), wallMat);

        // Rocas del exterior
        Box("Rock_1", level, new Vector3(-12f, 1f, 20f), new Vector3(3, 2, 3), rockMat);
        Box("Rock_2", level, new Vector3(10f, 1f, 17f), new Vector3(3, 2, 3), rockMat);
        Box("Rock_3", level, new Vector3(21f, 1f, 25f), new Vector3(3, 2, 3), rockMat);
        Box("Rock_4", level, new Vector3(-23f, 1f, 26f), new Vector3(3, 2, 3), rockMat);
    }

    static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.isStatic = true;
        return go;
    }

    static void CreateLight()
    {
        GameObject go = new GameObject("Directional Light");
        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    // ------------------------------------------------------------------
    // Jugador provisional
    // ------------------------------------------------------------------

    static Transform CreatePlayer(Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Player (provisional)";
        go.tag = "Player";
        go.transform.position = position;
        Object.DestroyImmediate(go.GetComponent<Collider>()); // el collider es el CharacterController
        go.GetComponent<Renderer>().sharedMaterial = GetMaterial("Player", new Color(1f, 0.85f, 0.2f));

        CharacterController cc = go.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = Vector3.zero;

        GameObject camGo = new GameObject("PlayerCamera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(go.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.fieldOfView = 70f;
        camGo.AddComponent<AudioListener>();

        PlayerStub stub = go.AddComponent<PlayerStub>();
        stub.cameraPivot = camGo.transform;
        return go.transform;
    }

    // ------------------------------------------------------------------
    // Enemigos (cápsulas de colores con una "nariz" que indica hacia dónde miran)
    // ------------------------------------------------------------------

    static void CreateEnemies(Transform player)
    {
        Transform waypointsRoot = new GameObject("Waypoints").transform;
        Transform enemiesRoot = new GameObject("Enemies").transform;

        // Bracken: pasillo oeste
        var bracken = CreateEnemy<BrackenController>("Bracken", enemiesRoot, new Vector3(-22f, 1f, -22f),
            new Color(0.55f, 0.08f, 0.12f), player);
        bracken.waypoints = Waypoints(waypointsRoot, "Bracken",
            new Vector3(-22, 0, -22), new Vector3(-22, 0, 2), new Vector3(-10, 0, 2), new Vector3(-10, 0, -25));
        GameObject spot = new GameObject("Bracken_FavoriteSpot");
        spot.transform.position = new Vector3(-27f, 0f, -27f);
        bracken.favoriteSpot = spot.transform;

        // Coil-Head: pasillo este
        var coil = CreateEnemy<CoilHeadController>("Coil-Head", enemiesRoot, new Vector3(22f, 1f, -24f),
            new Color(0.80f, 0.70f, 0.60f), player);
        coil.waypoints = Waypoints(waypointsRoot, "CoilHead",
            new Vector3(22, 0, -24), new Vector3(25, 0, 5), new Vector3(10, 0, 2), new Vector3(10, 0, -25));

        // Nutcracker: cuarto central y alrededores
        var nut = CreateEnemy<NutcrackerController>("Nutcracker", enemiesRoot, new Vector3(0f, 1f, -11f),
            new Color(0.15f, 0.25f, 0.65f), player);
        nut.waypoints = Waypoints(waypointsRoot, "Nutcracker",
            new Vector3(-3, 0, -11), new Vector3(3, 0, -11), new Vector3(0, 0, -1), new Vector3(0, 0, -22));

        // Thumper: vuelta grande por todo el interior (rectas largas)
        var thumper = CreateEnemy<ThumperController>("Thumper", enemiesRoot, new Vector3(25f, 1f, -2f),
            new Color(0.30f, 0.45f, 0.35f), player);
        thumper.waypoints = Waypoints(waypointsRoot, "Thumper",
            new Vector3(25, 0, -26), new Vector3(25, 0, 6), new Vector3(-25, 0, 6), new Vector3(-25, 0, -26));

        // 3 Eyeless Dogs en el exterior
        Vector3[] dogPositions = { new Vector3(-15f, 1f, 20f), new Vector3(0f, 1f, 24f), new Vector3(16f, 1f, 21f) };
        for (int i = 0; i < dogPositions.Length; i++)
        {
            var dog = CreateEnemy<EyelessDogController>($"EyelessDog_{i + 1}", enemiesRoot, dogPositions[i],
                new Color(0.85f, 0.45f, 0.45f), player);
            Vector3 p = dogPositions[i];
            p.y = 0f;
            dog.waypoints = Waypoints(waypointsRoot, $"EyelessDog_{i + 1}",
                p + new Vector3(-4, 0, -3), p + new Vector3(4, 0, -3), p + new Vector3(0, 0, 4));
        }
    }

    static T CreateEnemy<T>(string name, Transform parent, Vector3 position, Color color, Transform player)
        where T : EnemyController
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.GetComponent<Renderer>().sharedMaterial = GetMaterial("Enemy_" + typeof(T).Name, color);

        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Nose";
        Object.DestroyImmediate(nose.GetComponent<Collider>());
        nose.transform.SetParent(go.transform, false);
        nose.transform.localPosition = new Vector3(0f, 0.55f, 0.45f);
        nose.transform.localScale = new Vector3(0.35f, 0.15f, 0.4f);
        nose.GetComponent<Renderer>().sharedMaterial = GetMaterial("EnemyNose", new Color(0.1f, 0.1f, 0.1f));

        NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
        agent.baseOffset = 1f;     // la cápsula tiene el pivote al centro
        agent.height = 2f;
        agent.radius = 0.5f;

        T controller = go.AddComponent<T>();
        controller.player = player;
        return controller;
    }

    static Transform[] Waypoints(Transform root, string owner, params Vector3[] points)
    {
        Transform parent = new GameObject(owner).transform;
        parent.SetParent(root);
        var list = new List<Transform>();
        for (int i = 0; i < points.Length; i++)
        {
            Transform wp = new GameObject($"WP_{i}").transform;
            wp.SetParent(parent);
            wp.position = points[i];
            list.Add(wp);
        }
        return list.ToArray();
    }

    // ------------------------------------------------------------------
    // NavMesh
    // ------------------------------------------------------------------

    static void BakeNavMesh(GameObject level)
    {
        NavMeshSurface surface = level.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();

        AssetDatabase.DeleteAsset(NavMeshPath);
        AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
        EditorUtility.SetDirty(surface);
    }

    // ------------------------------------------------------------------
    // Utilidades
    // ------------------------------------------------------------------

    static Material GetMaterial(string name, Color color)
    {
        string path = $"{MaterialsFolder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void EnsureFolder(string parent, string folder)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{folder}"))
            AssetDatabase.CreateFolder(parent, folder);
    }
}
