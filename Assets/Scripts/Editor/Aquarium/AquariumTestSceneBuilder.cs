using System.Collections.Generic;
using System.IO;
using Blue.Aquarium;
using Blue.Entity;
using Blue.Game;
using Blue.Input;
using Blue.Object;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blue.Editor
{
    /// <summary>
    /// 水族館の動作確認用シーンと、そこで使う設置物アセットを生成するエディタユーティリティ
    /// </summary>
    // 編集UIができるまで、置いたものが正しく見えるかを確かめる手段がないため用意している。
    // 既存の水槽プレハブを流用し、遊泳範囲は Water の大きさから割り出す
    public static class AquariumTestSceneBuilder
    {
        private const string SOURCE_SCENE_PATH = "Assets/Scenes/Aquarium.unity";
        private const string TEST_SCENE_PATH = "Assets/Scenes/AquariumTest.unity";
        private const string TANK_PREFAB_FOLDER = "Assets/Prefab/Aquarium";
        private const string ASSET_FOLDER = "Assets/ScriptableObjects/Aquarium";
        private const string FLOOR_ASSET_PATH = ASSET_FOLDER + "/TestFloor.asset";
        private const string ROOM_ID = "TestRoom";
        private const int ROOM_MARGIN = 2; // 部屋の縁と水槽の間に空けるセル数

        // 遊泳範囲をガラスの内側へ引っ込める割合。1.0 のままだと魚が壁に張り付いて見える
        private const float SWIM_AREA_MARGIN = 0.8f;

        // 内寸1単位あたり、どれだけの DisplaySize までを許すか。
        // 既存の EntityData は 0〜40 と幅があり、実測に基づく値ではないので暫定
        private const float DISPLAY_SIZE_PER_UNIT = 4f;

        [MenuItem("Blue/Aquarium/Setup Test Scene")]
        public static void SetupTestScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            List<string> tank_paths = CreateTankAssets();
            if (tank_paths.Count == 0)
            {
                Debug.LogError($"水槽プレハブが見つかりませんでした: {TANK_PREFAB_FOLDER}");
                return;
            }

            // 水槽の大きさはプレハブ次第なので、部屋は実際に必要なセル数から決める
            Vector2Int room_size = CalculateRoomSize(LoadTanks(tank_paths));
            CreateFloorAsset(room_size);

            if (!CreateSceneCopy()) return;

            Scene scene = EditorSceneManager.OpenScene(TEST_SCENE_PATH, OpenSceneMode.Single);

            // アセットは必ずここで取り直す。CreateSceneCopy の Refresh で以前の参照が
            // 無効になっており、そのまま代入すると参照が空のまま保存される
            List<TankPieceData> tanks = LoadTanks(tank_paths);
            AquariumFloorData floor = AssetDatabase.LoadAssetAtPath<AquariumFloorData>(FLOOR_ASSET_PATH);

            RemoveLegacyObjects();
            CreateGround(room_size);
            EnsurePlayerInput();
            AquariumSceneBootstrap bootstrap = BuildAquariumObjects(floor, tanks, room_size);
            MovePlayerToViewpoint(room_size);
            BuildEditMode(bootstrap, tanks, room_size);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"水族館のテストシーンを作成しました: {TEST_SCENE_PATH}");
        }

        // ---------------- アセット生成 ----------------

        // アセットの実体ではなくパスを返す。この後の Refresh で参照が無効になるため
        private static List<string> CreateTankAssets()
        {
            EnsureFolder(ASSET_FOLDER);

            List<string> created = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { TANK_PREFAB_FOLDER });

            foreach (string guid in guids)
            {
                string prefab_path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefab_path);
                if (prefab == null) continue;

                AddTankView(prefab_path);

                // AddTankView がプレハブを保存し直すので、参照を取り直す
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefab_path);

                string asset_path = CreateTankAsset(prefab);
                if (!string.IsNullOrEmpty(asset_path)) created.Add(asset_path);
            }

            AssetDatabase.SaveAssets();
            return created;
        }

        private static List<TankPieceData> LoadTanks(List<string> asset_paths)
        {
            List<TankPieceData> tanks = new List<TankPieceData>();

            foreach (string asset_path in asset_paths)
            {
                TankPieceData tank = AssetDatabase.LoadAssetAtPath<TankPieceData>(asset_path);

                if (tank == null)
                {
                    Debug.LogWarning($"水槽アセットを読み込めませんでした: {asset_path}");
                    continue;
                }

                tanks.Add(tank);
            }

            return tanks;
        }

        private static void AddTankView(string prefab_path)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefab_path);

            try
            {
                if (contents.GetComponent<TankView>() == null)
                {
                    contents.AddComponent<TankView>();
                    PrefabUtility.SaveAsPrefabAsset(contents, prefab_path);
                    Debug.Log($"TankView を追加しました: {prefab_path}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static string CreateTankAsset(GameObject prefab)
        {
            string asset_path = $"{ASSET_FOLDER}/{prefab.name}_Tank.asset";

            TankPieceData tank = AssetDatabase.LoadAssetAtPath<TankPieceData>(asset_path);
            if (tank == null)
            {
                tank = ScriptableObject.CreateInstance<TankPieceData>();
                AssetDatabase.CreateAsset(tank, asset_path);
            }

            Bounds bounds = FindSwimBounds(prefab);

            // セルからはみ出す水槽は隣に置けなくなるので、占有セル数は外形から切り上げる
            Bounds outer = FindOuterBounds(prefab);
            int footprint_x = Mathf.Max(1, Mathf.CeilToInt(outer.size.x / AquariumGrid.CELL_SIZE));
            int footprint_z = Mathf.Max(1, Mathf.CeilToInt(outer.size.z / AquariumGrid.CELL_SIZE));

            SerializedObject serialized_object = new SerializedObject(tank);

            serialized_object.FindProperty("name").stringValue = prefab.name;
            serialized_object.FindProperty("description").stringValue = "動作確認用に自動生成した水槽";
            serialized_object.FindProperty("prefab").objectReferenceValue = prefab;
            serialized_object.FindProperty("footprint").vector2IntValue = new Vector2Int(footprint_x, footprint_z);
            serialized_object.FindProperty("walkable").boolValue = false;

            SerializedProperty habitations = serialized_object.FindProperty("supportedHabitations");
            habitations.arraySize = 2;
            habitations.GetArrayElementAtIndex(0).enumValueIndex = (int)HabitationArea.Shallow;
            habitations.GetArrayElementAtIndex(1).enumValueIndex = (int)HabitationArea.Depth;

            // 上限を緩めると内寸2.5の水槽に displaySize 40 の生物が入り、壊れて見える。
            // DisplaySize はワールド単位ではないので、内寸の一番狭い辺を基準に係数で見積もる
            Vector3 swim_size = bounds.size * SWIM_AREA_MARGIN;
            float narrowest = Mathf.Min(swim_size.x, Mathf.Min(swim_size.y, swim_size.z));
            serialized_object.FindProperty("maxDisplaySize").floatValue = narrowest * DISPLAY_SIZE_PER_UNIT;

            // 容量は緩めておく。まずは泳ぐところを見たいので、匹数では弾かない
            serialized_object.FindProperty("capacity").floatValue = 999f;
            serialized_object.FindProperty("allowsSchool").boolValue = true;
            serialized_object.FindProperty("schoolDisplayCount").intValue = 20;

            serialized_object.FindProperty("swimAreaCenter").vector3Value = bounds.center;
            serialized_object.FindProperty("swimAreaSize").vector3Value = bounds.size * SWIM_AREA_MARGIN;

            serialized_object.ApplyModifiedProperties();
            EditorUtility.SetDirty(tank);

            return asset_path;
        }

        /// <summary>
        /// 遊泳範囲の元になる大きさ。Water があればそれを、無ければ外形を使う
        /// </summary>
        private static Bounds FindSwimBounds(GameObject prefab)
        {
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (!child.name.ToLowerInvariant().Contains("water")) continue;

                Renderer water_renderer = child.GetComponent<Renderer>();
                if (water_renderer != null) return ToLocalBounds(prefab, water_renderer.bounds);
            }

            Debug.LogWarning($"Water が見つからないため外形から遊泳範囲を決めます: {prefab.name}");
            return FindOuterBounds(prefab);
        }

        private static Bounds FindOuterBounds(GameObject prefab)
        {
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return ToLocalBounds(prefab, bounds);
        }

        private static Bounds ToLocalBounds(GameObject prefab, Bounds world)
        {
            // プレハブアセットはルートが原点にある想定だが、ずれていても中心だけは合わせておく
            return new Bounds(world.center - prefab.transform.position, world.size);
        }

        /// <summary>
        /// 水槽を1列に並べるのに必要な部屋の広さ
        /// </summary>
        private static Vector2Int CalculateRoomSize(List<TankPieceData> tanks)
        {
            int width = 0;
            int depth = 0;

            foreach (TankPieceData tank in tanks)
            {
                // 並べ方は BuildAquariumObjects と揃える。ここがずれると入りきらない
                width += Mathf.Max(1, tank.Footprint.x) + 1;
                depth = Mathf.Max(depth, Mathf.Max(1, tank.Footprint.y));
            }

            return new Vector2Int(width + ROOM_MARGIN, depth + ROOM_MARGIN);
        }

        private static void CreateFloorAsset(Vector2Int room_size)
        {
            EnsureFolder(ASSET_FOLDER);

            AquariumFloorData floor = AssetDatabase.LoadAssetAtPath<AquariumFloorData>(FLOOR_ASSET_PATH);
            if (floor == null)
            {
                floor = ScriptableObject.CreateInstance<AquariumFloorData>();
                AssetDatabase.CreateAsset(floor, FLOOR_ASSET_PATH);
            }

            SerializedObject serialized_object = new SerializedObject(floor);
            SerializedProperty rooms = serialized_object.FindProperty("rooms");

            rooms.ClearArray();
            rooms.InsertArrayElementAtIndex(0);

            SerializedProperty room = rooms.GetArrayElementAtIndex(0);
            room.FindPropertyRelative("roomID").stringValue = ROOM_ID;
            room.FindPropertyRelative("name").stringValue = "テスト部屋";
            room.FindPropertyRelative("origin").vector2IntValue = Vector2Int.zero;
            room.FindPropertyRelative("size").vector2IntValue = room_size;
            room.FindPropertyRelative("unlockedFromStart").boolValue = true;

            serialized_object.ApplyModifiedProperties();
            EditorUtility.SetDirty(floor);
            AssetDatabase.SaveAssets();
        }

        // ---------------- シーン生成 ----------------

        private static bool CreateSceneCopy()
        {
            if (!File.Exists(SOURCE_SCENE_PATH))
            {
                Debug.LogError($"元になるシーンが見つかりません: {SOURCE_SCENE_PATH}");
                return false;
            }

            if (File.Exists(TEST_SCENE_PATH))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "テストシーンの作り直し",
                    $"{TEST_SCENE_PATH} は既にあります。作り直しますか？",
                    "作り直す",
                    "やめる"
                );

                if (!overwrite) return false;

                AssetDatabase.DeleteAsset(TEST_SCENE_PATH);
            }

            // 元のシーンは開かずに複製する。開いてから保存すると Aquarium.unity を壊しかねない
            if (!AssetDatabase.CopyAsset(SOURCE_SCENE_PATH, TEST_SCENE_PATH))
            {
                Debug.LogError("シーンの複製に失敗しました");
                return false;
            }

            AssetDatabase.Refresh();
            return true;
        }

        private static void RemoveLegacyObjects()
        {
            int removed = 0;

            removed += DestroyOwners(UnityEngine.Object.FindObjectsByType<AquariumSceneController>(FindObjectsSortMode.None));
            removed += DestroyOwners(UnityEngine.Object.FindObjectsByType<AquariumManager>(FindObjectsSortMode.None));
            removed += DestroyOwners(UnityEngine.Object.FindObjectsByType<AquariumController>(FindObjectsSortMode.None));

            Debug.Log($"旧システムのオブジェクトを {removed} 個取り除きました");
        }

        private static int DestroyOwners<T>(T[] components) where T : Component
        {
            int count = 0;

            foreach (T component in components)
            {
                if (component == null) continue;

                UnityEngine.Object.DestroyImmediate(component.gameObject);
                count++;
            }

            return count;
        }

        private static AquariumSceneBootstrap BuildAquariumObjects(AquariumFloorData floor, List<TankPieceData> tanks, Vector2Int room_size)
        {
            GameObject owner = new GameObject("Aquarium");
            GameObject pieces_root = new GameObject("Pieces");
            pieces_root.transform.SetParent(owner.transform);

            AquariumBuilder builder = owner.AddComponent<AquariumBuilder>();
            AquariumSceneBootstrap bootstrap = owner.AddComponent<AquariumSceneBootstrap>();
            AquariumDebugPlacer placer = owner.AddComponent<AquariumDebugPlacer>();

            SerializedObject builder_object = new SerializedObject(builder);
            builder_object.FindProperty("root").objectReferenceValue = pieces_root.transform;
            builder_object.ApplyModifiedProperties();

            SerializedObject bootstrap_object = new SerializedObject(bootstrap);
            bootstrap_object.FindProperty("floor").objectReferenceValue = floor;
            bootstrap_object.FindProperty("builder").objectReferenceValue = builder;
            bootstrap_object.ApplyModifiedProperties();

            SerializedObject placer_object = new SerializedObject(placer);
            placer_object.FindProperty("bootstrap").objectReferenceValue = bootstrap;
            placer_object.FindProperty("exhibitCapturedEntities").boolValue = true;

            SerializedProperty rooms = placer_object.FindProperty("roomsToUnlock");
            rooms.arraySize = 1;
            rooms.GetArrayElementAtIndex(0).stringValue = ROOM_ID;

            SerializedProperty placements = placer_object.FindProperty("placements");
            placements.arraySize = tanks.Count;

            int cell_x = 0;
            for (int i = 0; i < tanks.Count; i++)
            {
                SerializedProperty placement = placements.GetArrayElementAtIndex(i);
                placement.FindPropertyRelative("piece").objectReferenceValue = tanks[i];
                placement.FindPropertyRelative("cell").vector2IntValue = new Vector2Int(cell_x, 0);
                placement.FindPropertyRelative("rotationStep").intValue = 0;

                // 隣り合わせにすると占有セルが重なって弾かれるので、幅ぶんだけ空けて並べる
                cell_x += Mathf.Max(1, tanks[i].Footprint.x) + 1;
            }

            placer_object.ApplyModifiedProperties();

            Debug.Log($"水槽を {tanks.Count} 台ぶん配置指定しました（セル 0〜{cell_x} / 部屋は {room_size.x}x{room_size.y}）");

            return bootstrap;
        }

        /// <summary>
        /// 俯瞰の編集モード一式を組み、見学との切り替えを繋ぐ
        /// </summary>
        private static void BuildEditMode(AquariumSceneBootstrap bootstrap, List<TankPieceData> tanks, Vector2Int room_size)
        {
            if (bootstrap == null) return;

            GameObject edit_rig = new GameObject("EditRig");

            GameObject camera_owner = new GameObject("EditCamera");
            camera_owner.transform.SetParent(edit_rig.transform);
            camera_owner.AddComponent<Camera>();

            // 見学側のプレイヤーごと無効になるので、こちらにも用意しないと音が止まる
            camera_owner.AddComponent<AudioListener>();

            AquariumEditCamera edit_camera = camera_owner.AddComponent<AquariumEditCamera>();

            // 下見はカメラの子にしない。カメラと一緒に動いたように見えて紛らわしい
            GameObject ghost_owner = new GameObject("PlacementGhost");
            ghost_owner.transform.SetParent(edit_rig.transform);
            PlacementGhost ghost = ghost_owner.AddComponent<PlacementGhost>();

            AquariumEditController controller = edit_rig.AddComponent<AquariumEditController>();

            Vector3 room_center = new Vector3(
                room_size.x * AquariumGrid.CELL_SIZE * 0.5f, 0f, room_size.y * AquariumGrid.CELL_SIZE * 0.5f);

            SerializedObject camera_object = new SerializedObject(edit_camera);
            camera_object.FindProperty("initialFocus").vector3Value = room_center;
            camera_object.FindProperty("initialHeight").floatValue = Mathf.Max(room_size.x, room_size.y) * 0.6f;
            camera_object.ApplyModifiedProperties();

            SerializedObject controller_object = new SerializedObject(controller);
            controller_object.FindProperty("bootstrap").objectReferenceValue = bootstrap;
            controller_object.FindProperty("editCamera").objectReferenceValue = edit_camera;
            controller_object.FindProperty("view").objectReferenceValue = camera_owner.GetComponent<Camera>();
            controller_object.FindProperty("ghost").objectReferenceValue = ghost;

            SerializedProperty palette = controller_object.FindProperty("palette");
            palette.arraySize = tanks.Count;
            for (int i = 0; i < tanks.Count; i++)
            {
                palette.GetArrayElementAtIndex(i).objectReferenceValue = tanks[i];
            }

            controller_object.ApplyModifiedProperties();

            AquariumModeController mode = bootstrap.gameObject.AddComponent<AquariumModeController>();
            CharacterMovementController player = UnityEngine.Object.FindFirstObjectByType<CharacterMovementController>();

            SerializedObject mode_object = new SerializedObject(mode);
            mode_object.FindProperty("bootstrap").objectReferenceValue = bootstrap;
            mode_object.FindProperty("viewRig").objectReferenceValue = player != null ? player.gameObject : null;
            mode_object.FindProperty("editRig").objectReferenceValue = edit_rig;
            mode_object.FindProperty("editController").objectReferenceValue = controller;
            mode_object.ApplyModifiedProperties();

            // 開始時は見学。Start で切り替わるが、シーン上でも合わせておく
            edit_rig.SetActive(false);

            Debug.Log("編集モードを組みました（Tab で切り替え）");
        }

        /// <summary>
        /// 部屋のぶんだけ地面を敷く
        /// </summary>
        // 元シーンの床がどこまで広がっているか当てにできず、無いとプレイヤーが落ちる
        private static void CreateGround(Vector2Int room_size)
        {
            float width = room_size.x * AquariumGrid.CELL_SIZE;
            float depth = room_size.y * AquariumGrid.CELL_SIZE;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "TestGround";

            // 天面をちょうど y=0 に合わせる。水槽はセル座標から y=0 に置かれる
            ground.transform.position = new Vector3(width * 0.5f, -0.5f, depth * 0.5f);
            ground.transform.localScale = new Vector3(width + 8f, 1f, depth + 8f);
        }

        /// <summary>
        /// 入力を用意するオブジェクトを置く
        /// </summary>
        // 元の Aquarium シーンは Title / Garage から入る前提で、単体では入力が生成されない
        private static void EnsurePlayerInput()
        {
            if (UnityEngine.Object.FindFirstObjectByType<PlayerInputBootstrap>() != null) return;

            GameObject owner = new GameObject("PlayerInput");
            owner.AddComponent<PlayerInputBootstrap>();
        }

        private static void MovePlayerToViewpoint(Vector2Int room_size)
        {
            CharacterMovementController player = UnityEngine.Object.FindFirstObjectByType<CharacterMovementController>();
            if (player == null)
            {
                Debug.LogWarning("プレイヤーが見つかりませんでした。手動で水槽の前へ動かしてください");
                return;
            }

            // 部屋は原点から +X +Z 方向へ広がるので、その手前に立たせる
            player.transform.position = new Vector3(room_size.x * 0.5f, 1f, -4f);
            player.transform.rotation = Quaternion.identity;

            Debug.Log($"プレイヤーを {player.transform.position} へ移動しました");
        }

        private static void EnsureFolder(string folder_path)
        {
            if (AssetDatabase.IsValidFolder(folder_path)) return;

            string parent = Path.GetDirectoryName(folder_path).Replace('\\', '/');
            string leaf = Path.GetFileName(folder_path);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
