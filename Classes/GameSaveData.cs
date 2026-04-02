using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace GalactaJumperMo.Classes
{
    /// <summary>
    /// Represents saved game data including checkpoint progress
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public int CurrentCheckpoint { get; set; }
        public int CollectedStars { get; set; }
        public bool HasWallJump { get; set; }
        public bool HasDash { get; set; }
        public DateTime SaveTime { get; set; }
        public List<int> CollectedObjectiveCheckpoints { get; set; } = new List<int>();
        public List<int> CollectedStarCheckpoints { get; set; } = new List<int>();
        public List<string> CollectedObjectiveKeys { get; set; } = new List<string>();
        public float ElapsedTime { get; set; }

        private static string SaveFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GalactaJumperMo",
            "savegame.json"
        );

        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(SaveFilePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                SaveTime = DateTime.Now;
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SaveFilePath, json);
                System.Diagnostics.Debug.WriteLine($"Game saved to: {SaveFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save game: {ex.Message}");
            }
        }


        public static GameSaveData Load()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    var data = JsonSerializer.Deserialize<GameSaveData>(json);
                    if (data != null && data.SaveTime == default)
                    {
                        data.SaveTime = File.GetLastWriteTime(SaveFilePath);
                    }
                    if (data != null)
                    {
                        data.CollectedObjectiveCheckpoints ??= new List<int>();
                        data.CollectedStarCheckpoints ??= new List<int>();
                        data.CollectedObjectiveKeys ??= new List<string>();
                    }
                    System.Diagnostics.Debug.WriteLine($"Game loaded from: {SaveFilePath}");
                    return data;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load game: {ex.Message}");
            }
            return null;
        }

        public static bool SaveExists()
        {
            return File.Exists(SaveFilePath);
        }


        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                    System.Diagnostics.Debug.WriteLine("Save file deleted");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete save: {ex.Message}");
            }
        }
    }
}