using System.Collections.Generic;
using System.IO;
using Blue.Aquarium;
using Unity.Cinemachine;
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
        private const string PATH_ASSET_PATH = ASSET_FOLDER + "/TestPath.asset";
        private const string PATH_PREFAB_PATH = "Assets/Prefab/Aquarium/Path/PathTile.prefab";
        private const string ROOM_ID = "TestRoom";
        private const int ROOM_MARGIN = 2; // 部屋の縁と水槽の間に空けるセル数

        // 遊泳範囲をガラスの内側へ引っ込める割合。1.0 のままだと魚が壁に張り付いて見える
        private const float SWIM_AREA_MARGIN = 0.8f;

        // 内寸1単位あたり、どれだけの DisplaySize までを許すか。
        // 既存の EntityData は 0〜40 と幅があり、実測に基づく値ではないので暫定
        private const float DISPLAY_SIZE_PER_UNIT = 4f;

        // 素の Camera の既定値。仮想カメラでも同じ見え方に揃える
        private const float CAMERA_FIELD_OF_VIEW = 60f;

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
            BuildEditMode(bootstrap, tanks, CreatePathAsset(), room_size);

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

                // FindAssets は再帰的に拾う。通路タイルなど、水槽以外を入れた
                // サブフォルダまで水槽として扱わないよう直下だけに限る
                if (Path.GetDirectoryName(prefab_path).Replace('\\', '/') != TANK_PREFAB_FOLDER) continue;

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

        /// <summary>
        /// 通路を1種類だけ用意する。無いと警告を出しても直す手段が無い
        /// </summary>
        private static PathPieceData CreatePathAsset()
        {
            EnsureFolder(ASSET_FOLDER);

            PathPieceData path = AssetDatabase.LoadAssetAtPath<PathPieceData>(PATH_ASSET_PATH);
            if (path != null) return path;

            GameObject prefab = CreatePathPrefab();

            path = ScriptableObject.CreateInstance<PathPieceData>();
            AssetDatabase.CreateAsset(path, PATH_ASSET_PATH);

            SerializedObject serialized_object = new SerializedObject(path);
            serialized_object.FindProperty("name").stringValue = "通路";
            serialized_object.FindProperty("description").stringValue = "動作確認用に自動生成した通路";
            serialized_object.FindProperty("prefab").objectReferenceValue = prefab;
            serialized_object.FindProperty("footprint").vector2IntValue = Vector2Int.one;
            serialized_object.FindProperty("walkable").boolValue = true;
            serialized_object.ApplyModifiedProperties();

            EditorUtility.SetDirty(path);
            AssetDatabase.SaveAssets();

            return AssetDatabase.LoadAssetAtPath<PathPieceData>(PATH_ASSET_PATH);
        }

        private static GameObject CreatePathPrefab()
        {
            EnsureFolder("Assets/Prefab/Aquarium/Path");

            GameObject root = new GameObject("PathTile");

            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "Tile";
            tile.transform.SetParent(root.transform);
            tile.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            tile.transform.localScale = new Vector3(0.96f, 0.1f, 0.96f);

            // 床は TestGround が持っているので、通路のコライダーは邪魔にしかならない
            UnityEngine.Object.DestroyImmediate(tile.GetComponent<Collider>());

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PATH_PREFAB_PATH);
            UnityEngine.Object.DestroyImmediate(root);

            return prefab;
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

        // 一度作ったアセットは手で直すものとして扱い、二度と上書きしない。
        // 毎回すべての項目を書き直すと、モデルに合わせて調整した遊泳範囲や
        // 見る位置が、次にこのメニューを回した時点で消える
        private static string CreateTankAsset(GameObject prefab)
        {
            string asset_path = $"{ASSET_FOLDER}/{prefab.name}_Tank.asset";

            if (AssetDatabase.LoadAssetAtPath<TankPieceData>(asset_path) != null)
            {
                return asset_path;
            }

            TankPieceData tank = ScriptableObject.CreateInstance<TankPieceData>();
            AssetDatabase.CreateAsset(tank, asset_path);

            Debug.Log($"水槽アセットを新規作成しました（以後は手で調整してください）: {asset_path}");

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

            // 中を見せるカメラの位置。ローカル -Z を正面とし、
            // 半分の奥行きに加えて内寸のぶん引くと、画角60度でおおよそ収まる
            float view_distance = swim_size.z * 0.5f + Mathf.Max(swim_size.x, swim_size.y);
            serialized_object.FindProperty("viewOffset").vector3Value =
                bounds.center + new Vector3(0f, 0f, -view_distance);

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

            // 入口が無いと通路をいくら置いても繋がらない。手前の辺の中央に置く
            SerializedProperty entrances = room.FindPropertyRelative("entrances");
            entrances.arraySize = 1;
            entrances.GetArrayElementAtIndex(0).vector2IntValue = new Vector2Int(room_size.x / 2, 0);

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

                // 開いたままのシーンファイルは差し替えられない。空のシーンへ退避してから消す。
                // これを怠ると削除も複製も失敗し、アセットだけ新しくなってシーンは古いまま残る
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                if (!AssetDatabase.DeleteAsset(TEST_SCENE_PATH))
                {
                    Debug.LogError($"古いテストシーンを削除できませんでした: {TEST_SCENE_PATH}");
                    return false;
                }
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

            // 所持数の差し替えは読み込みより前に効く必要があるので、Bootstrap より先に付ける
            DebugEntityStockProvider stock_provider = owner.AddComponent<DebugEntityStockProvider>();

            AquariumSceneBootstrap bootstrap = owner.AddComponent<AquariumSceneBootstrap>();
            AquariumDebugPlacer placer = owner.AddComponent<AquariumDebugPlacer>();

            SerializedObject builder_object = new SerializedObject(builder);
            builder_object.FindProperty("root").objectReferenceValue = pieces_root.transform;
            builder_object.ApplyModifiedProperties();

            SerializedObject bootstrap_object = new SerializedObject(bootstrap);
            bootstrap_object.FindProperty("floor").objectReferenceValue = floor;
            bootstrap_object.FindProperty("builder").objectReferenceValue = builder;
            bootstrap_object.FindProperty("stockProvider").objectReferenceValue = stock_provider;
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
        private static void BuildEditMode(AquariumSceneBootstrap bootstrap, List<TankPieceData> tanks, PathPieceData path, Vector2Int room_size)
        {
            if (bootstrap == null) return;

            GameObject edit_rig = new GameObject("EditRig");

            // 実カメラは Brain だけを持ち、位置は仮想カメラが決める。
            // ここで実カメラを直接動かすと Brain と奪い合って壊れる
            GameObject camera_owner = new GameObject("EditCamera");
            camera_owner.transform.SetParent(edit_rig.transform);
            Camera camera = camera_owner.AddComponent<Camera>();
            camera_owner.AddComponent<CinemachineBrain>();

            // 見学側のプレイヤーごと無効になるので、こちらにも用意しないと音が止まる
            camera_owner.AddComponent<AudioListener>();

            GameObject overview_owner = new GameObject("OverviewCamera");
            overview_owner.transform.SetParent(edit_rig.transform);
            CinemachineCamera overview = overview_owner.AddComponent<CinemachineCamera>();
            overview.Priority = 20;
            SetFieldOfView(overview);

            GameObject focus_owner = new GameObject("FocusCamera");
            focus_owner.transform.SetParent(edit_rig.transform);
            CinemachineCamera focus = focus_owner.AddComponent<CinemachineCamera>();
            focus.Priority = 0;
            SetFieldOfView(focus);

            AquariumEditCamera edit_camera = overview_owner.AddComponent<AquariumEditCamera>();

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
            // 画面座標からセルを求めるのは実カメラの役目。仮想カメラは描画に関与しない
            controller_object.FindProperty("view").objectReferenceValue = camera;
            controller_object.FindProperty("ghost").objectReferenceValue = ghost;

            // 通路を先頭にする。導線を引いてから水槽を並べる順序が自然で、
            // 通路が無いまま水槽を置いて全部が警告色になるのを避けられる
            List<GridPieceData> pieces = new List<GridPieceData>();
            if (path != null) pieces.Add(path);
            pieces.AddRange(tanks);

            SerializedProperty palette = controller_object.FindProperty("palette");
            palette.arraySize = pieces.Count;
            for (int i = 0; i < pieces.Count; i++)
            {
                palette.GetArrayElementAtIndex(i).objectReferenceValue = pieces[i];
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

            AquariumCameraDirector director = edit_rig.AddComponent<AquariumCameraDirector>();

            SerializedObject director_object = new SerializedObject(director);
            director_object.FindProperty("overviewCamera").objectReferenceValue = overview;
            director_object.FindProperty("focusCamera").objectReferenceValue = focus;
            director_object.ApplyModifiedProperties();

            AquariumExhibitScreenBuilder.Build(bootstrap, controller, director);

            // 開始時は見学。Start で切り替わるが、シーン上でも合わせておく
            edit_rig.SetActive(false);

            Debug.Log("編集モードを組みました（Tab で切り替え／右クリックで水槽の中身）");
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

        // 実カメラの画角は仮想カメラの Lens が上書きする。
        // CinemachineCamera の既定は 40 で、素の Camera の 60 より狭い
        private static void SetFieldOfView(CinemachineCamera virtual_camera)
        {
            LensSettings lens = virtual_camera.Lens;
            lens.FieldOfView = CAMERA_FIELD_OF_VIEW;
            virtual_camera.Lens = lens;
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
