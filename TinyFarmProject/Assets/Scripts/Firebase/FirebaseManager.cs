using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TinyFarm.Firebase
{
    /// <summary>
    /// Manages all Firebase Realtime Database operations for TinyFarm
    /// Handles: Players, Farms, Crops, Inventory, Leaderboard
    /// </summary>
    public class FirebaseManager : MonoBehaviour
    {
        private static FirebaseManager _instance;
        private DatabaseReference _databaseRef;
        private string _playerId = "player_001"; // TODO: Replace with actual player ID from auth

        public static FirebaseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<FirebaseManager>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("FirebaseManager");
                        _instance = obj.AddComponent<FirebaseManager>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            try
            {
                _databaseRef = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("✅ Firebase Manager Initialized");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Firebase Manager Init Error: {e.Message}");
            }
        }

        #region ===== PLAYER DATA =====

        /// <summary>
        /// Create or update player profile
        /// </summary>
        public Task CreatePlayerAsync(PlayerData playerData)
        {
            _playerId = playerData.playerId;
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { $"players/{_playerId}/money", playerData.money },
                { $"players/{_playerId}/level", playerData.level },
                { $"players/{_playerId}/lastLogin", UnixTimeStamp() }
            };
            return _databaseRef.UpdateChildrenAsync(updates);
        }

        /// <summary>
        /// Get player data
        /// </summary>
        public async Task<PlayerData> GetPlayerAsync()
        {
            try
            {
                DataSnapshot snapshot = await _databaseRef.Child($"players/{_playerId}").GetValueAsync();
                if (snapshot.Exists)
                {
                    return new PlayerData
                    {
                        playerId = _playerId,
                        money = Convert.ToInt64(snapshot.Child("money").Value ?? 0),
                        level = Convert.ToInt32(snapshot.Child("level").Value ?? 1)
                    };
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ GetPlayer Error: {e.Message}");
            }
            return null;
        }

        /// <summary>
        /// Update player money
        /// </summary>
        public Task UpdateMoneyAsync(long amount)
        {
            return _databaseRef.Child($"players/{_playerId}/money").SetValueAsync(amount);
        }

        /// <summary>
        /// Update player level
        /// </summary>
        public Task UpdateLevelAsync(int level)
        {
            return _databaseRef.Child($"players/{_playerId}/level").SetValueAsync(level);
        }

        #endregion

        #region ===== FARM DATA =====

        /// <summary>
        /// Save plot information
        /// </summary>
        public Task SavePlotAsync(string plotId, int x, int y, string type)
        {
            Dictionary<string, object> plotData = new Dictionary<string, object>
            {
                { "x", x },
                { "y", y },
                { "type", type }
            };
            return _databaseRef.Child($"farms/{_playerId}/plots/{plotId}").SetValueAsync(plotData);
        }

        /// <summary>
        /// Get all plots
        /// </summary>
        public async Task<Dictionary<string, PlotData>> GetPlotsAsync()
        {
            try
            {
                DataSnapshot snapshot = await _databaseRef.Child($"farms/{_playerId}/plots").GetValueAsync();
                Dictionary<string, PlotData> plots = new Dictionary<string, PlotData>();

                if (snapshot.Exists)
                {
                    foreach (DataSnapshot child in snapshot.Children)
                    {
                        plots[child.Key] = new PlotData
                        {
                            plotId = child.Key,
                            x = Convert.ToInt32(child.Child("x").Value ?? 0),
                            y = Convert.ToInt32(child.Child("y").Value ?? 0),
                            type = child.Child("type").Value?.ToString() ?? ""
                        };
                    }
                }
                return plots;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ GetPlots Error: {e.Message}");
                return new Dictionary<string, PlotData>();
            }
        }

        #endregion

        #region ===== CROP DATA =====

        /// <summary>
        /// Plant a new crop
        /// </summary>
        public Task PlantCropAsync(string cropId, string cropType, string plotId)
        {
            Dictionary<string, object> cropData = new Dictionary<string, object>
            {
                { "type", cropType },
                { "plantedTime", UnixTimeStamp() },
                { "growthStage", 0 },
                { "plotId", plotId }
            };
            return _databaseRef.Child($"farms/{_playerId}/crops/{cropId}").SetValueAsync(cropData);
        }

        /// <summary>
        /// Update crop growth stage
        /// </summary>
        public Task UpdateCropGrowthAsync(string cropId, int growthStage)
        {
            return _databaseRef.Child($"farms/{_playerId}/crops/{cropId}/growthStage").SetValueAsync(growthStage);
        }

        /// <summary>
        /// Get all crops
        /// </summary>
        public async Task<Dictionary<string, CropData>> GetCropsAsync()
        {
            try
            {
                DataSnapshot snapshot = await _databaseRef.Child($"farms/{_playerId}/crops").GetValueAsync();
                Dictionary<string, CropData> crops = new Dictionary<string, CropData>();

                if (snapshot.Exists)
                {
                    foreach (DataSnapshot child in snapshot.Children)
                    {
                        crops[child.Key] = new CropData
                        {
                            cropId = child.Key,
                            type = child.Child("type").Value?.ToString() ?? "",
                            plantedTime = Convert.ToInt64(child.Child("plantedTime").Value ?? 0),
                            growthStage = Convert.ToInt32(child.Child("growthStage").Value ?? 0),
                            plotId = child.Child("plotId").Value?.ToString() ?? ""
                        };
                    }
                }
                return crops;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ GetCrops Error: {e.Message}");
                return new Dictionary<string, CropData>();
            }
        }

        /// <summary>
        /// Harvest/Remove crop
        /// </summary>
        public Task RemoveCropAsync(string cropId)
        {
            return _databaseRef.Child($"farms/{_playerId}/crops/{cropId}").RemoveValueAsync();
        }

        #endregion

        #region ===== INVENTORY DATA =====

        /// <summary>
        /// Update seed count
        /// </summary>
        public Task UpdateSeedAsync(string seedType, int count)
        {
            return _databaseRef.Child($"inventory/{_playerId}/seeds/{seedType}").SetValueAsync(count);
        }

        /// <summary>
        /// Get seed inventory
        /// </summary>
        public async Task<Dictionary<string, int>> GetSeedsAsync()
        {
            try
            {
                DataSnapshot snapshot = await _databaseRef.Child($"inventory/{_playerId}/seeds").GetValueAsync();
                Dictionary<string, int> seeds = new Dictionary<string, int>();

                if (snapshot.Exists)
                {
                    foreach (DataSnapshot child in snapshot.Children)
                    {
                        seeds[child.Key] = Convert.ToInt32(child.Value ?? 0);
                    }
                }
                return seeds;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ GetSeeds Error: {e.Message}");
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Update tool count
        /// </summary>
        public Task UpdateToolAsync(string toolType, int count)
        {
            return _databaseRef.Child($"inventory/{_playerId}/tools/{toolType}").SetValueAsync(count);
        }

        /// <summary>
        /// Get tool inventory
        /// </summary>
        public async Task<Dictionary<string, int>> GetToolsAsync()
        {
            try
            {
                DataSnapshot snapshot = await _databaseRef.Child($"inventory/{_playerId}/tools").GetValueAsync();
                Dictionary<string, int> tools = new Dictionary<string, int>();

                if (snapshot.Exists)
                {
                    foreach (DataSnapshot child in snapshot.Children)
                    {
                        tools[child.Key] = Convert.ToInt32(child.Value ?? 0);
                    }
                }
                return tools;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ GetTools Error: {e.Message}");
                return new Dictionary<string, int>();
            }
        }

        #endregion

        #region ===== LEADERBOARD =====

        /// <summary>
        /// Update leaderboard with player stats
        /// </summary>
        public Task UpdateLeaderboardAsync(long money, int crops)
        {
            Dictionary<string, object> leaderboardData = new Dictionary<string, object>
            {
                { $"leaderboard/{_playerId}/money", money },
                { $"leaderboard/{_playerId}/crops", crops }
            };
            return _databaseRef.UpdateChildrenAsync(leaderboardData);
        }

        /// <summary>
        /// Get top players from leaderboard
        /// </summary>
        public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(int limit = 10)
        {
            try
            {
                DataSnapshot snapshot = await _databaseRef.Child("leaderboard")
                    .OrderByChild("money")
                    .LimitToLast(limit)
                    .GetValueAsync();

                List<LeaderboardEntry> leaderboard = new List<LeaderboardEntry>();

                if (snapshot.Exists)
                {
                    foreach (DataSnapshot child in snapshot.Children)
                    {
                        leaderboard.Add(new LeaderboardEntry
                        {
                            playerId = child.Key,
                            money = Convert.ToInt64(child.Child("money").Value ?? 0),
                            crops = Convert.ToInt32(child.Child("crops").Value ?? 0)
                        });
                    }
                }

                // Sort descending
                leaderboard.Reverse();
                return leaderboard;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ GetLeaderboard Error: {e.Message}");
                return new List<LeaderboardEntry>();
            }
        }

        #endregion

        #region ===== UTILITIES =====

        private long UnixTimeStamp()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        #endregion
    }

    #region ===== DATA MODELS =====

    [System.Serializable]
    public class PlayerData
    {
        public string playerId;
        public long money;
        public int level;
    }

    [System.Serializable]
    public class PlotData
    {
        public string plotId;
        public int x;
        public int y;
        public string type;
    }

    [System.Serializable]
    public class CropData
    {
        public string cropId;
        public string type;
        public long plantedTime;
        public int growthStage;
        public string plotId;
    }

    [System.Serializable]
    public class LeaderboardEntry
    {
        public string playerId;
        public long money;
        public int crops;
    }

    #endregion
}
