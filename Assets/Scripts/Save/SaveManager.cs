using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Blue.Save
{
    /// <summary>
    /// セーブデータの保存と読み込みを管理する静的クラス
    /// </summary>
    public static class SaveManager
    {
        private const string SAVE_FILE_NAME = "savedata.dat";
        private const string DEBUG_SAVE_FILE_NAME = "savedata.json";

        private static SaveData currentSaveData;
        private static bool isInitialized = false;

        public static SaveData CurrentSaveData
        {
            get
            {
                if (!isInitialized)
                {
                    Initialize();
                }
                return currentSaveData;
            }
        }

        /// <summary>
        /// 初期化（セーブデータを読み込むか、新規作成する）
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized) return;

            if (HasSaveData())
            {
                currentSaveData = Load();
            }
            else
            {
                currentSaveData = new SaveData();
            }

            isInitialized = true;
        }

        /// <summary>
        /// セーブデータの存在確認
        /// </summary>
        public static bool HasSaveData()
        {
#if UNITY_EDITOR
            string debug_path = GetDebugSavePath();
            if (File.Exists(debug_path)) return true;
#endif
            string save_path = GetSavePath();
            return File.Exists(save_path);
        }

        /// <summary>
        /// セーブデータを保存
        /// </summary>
        public static void Save()
        {
            Save(currentSaveData);
        }

        /// <summary>
        /// セーブデータを保存
        /// </summary>
        public static void Save(SaveData save_data)
        {
            if (save_data == null)
            {
                Debug.LogError("SaveData is null");
                return;
            }

            currentSaveData = save_data;
            currentSaveData.lastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            try
            {
                // JSON文字列に変換
                string json = JsonUtility.ToJson(save_data, true);
                byte[] json_bytes = Encoding.UTF8.GetBytes(json);

#if UNITY_EDITOR
                // エディタではJSONファイルも保存（デバッグ用）
                string debug_path = GetDebugSavePath();
                File.WriteAllText(debug_path, json);
                Debug.Log($"Debug save file created at: {debug_path}");
#endif

                // 暗号化して保存
                byte[] encrypted = SaveEncryption.Encrypt(json_bytes);
                string save_path = GetSavePath();
                File.WriteAllBytes(save_path, encrypted);

                Debug.Log($"Game saved successfully at: {save_path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
            }
        }

        /// <summary>
        /// セーブデータを読み込み
        /// </summary>
        public static SaveData Load()
        {
            try
            {
#if UNITY_EDITOR
                // エディタではJSONファイルを優先的に読み込む
                string debug_path = GetDebugSavePath();
                if (File.Exists(debug_path))
                {
                    string json = File.ReadAllText(debug_path);
                    SaveData data = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log($"Game loaded from debug file: {debug_path}");
                    currentSaveData = data;
                    isInitialized = true;
                    return data;
                }
#endif

                // 暗号化されたファイルを読み込み
                string save_path = GetSavePath();
                if (File.Exists(save_path))
                {
                    byte[] encrypted = File.ReadAllBytes(save_path);
                    byte[] decrypted = SaveEncryption.Decrypt(encrypted);
                    string json = Encoding.UTF8.GetString(decrypted);
                    SaveData data = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log($"Game loaded successfully from: {save_path}");
                    currentSaveData = data;
                    isInitialized = true;
                    return data;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
            }

            // 読み込み失敗時は新しいデータを返す
            Debug.LogWarning("No save file found. Creating new save data.");
            SaveData new_data = new SaveData();
            currentSaveData = new_data;
            isInitialized = true;
            return new_data;
        }

        /// <summary>
        /// セーブデータを削除
        /// </summary>
        public static void DeleteSaveData()
        {
            try
            {
                string save_path = GetSavePath();
                if (File.Exists(save_path))
                {
                    File.Delete(save_path);
                    Debug.Log("Save file deleted.");
                }

#if UNITY_EDITOR
                string debug_path = GetDebugSavePath();
                if (File.Exists(debug_path))
                {
                    File.Delete(debug_path);
                    Debug.Log("Debug save file deleted.");
                }
#endif

                currentSaveData = new SaveData();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save data: {e.Message}");
            }
        }

        /// <summary>
        /// セーブファイルのパスを取得
        /// </summary>
        private static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        }

        /// <summary>
        /// デバッグ用JSONファイルのパスを取得
        /// </summary>
        private static string GetDebugSavePath()
        {
            return Path.Combine(Application.persistentDataPath, DEBUG_SAVE_FILE_NAME);
        }
    }
}
