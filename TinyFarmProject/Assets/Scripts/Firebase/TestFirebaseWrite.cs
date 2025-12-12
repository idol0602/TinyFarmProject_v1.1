using TinyFarm.Firebase;
using UnityEngine;

namespace TinyFarm.Tests
{
    /// <summary>
    /// Test Firebase Emulator integration with TinyFarm
    /// Demonstrates: Save/Load Player, Farms, Crops, Inventory
    /// </summary>
    public class TestFirebaseWrite : MonoBehaviour
    {
        private FirebaseManager _firebaseManager;

        private void Start()
        {
            _firebaseManager = FirebaseManager.Instance;
            
            // Run test after 1 second to ensure Firebase is initialized
            Invoke(nameof(RunTests), 1f);
        }

        private async void RunTests()
        {
            Debug.Log("🧪 Starting Firebase Emulator Tests...\n");

            // Test 1: Create Player
            await TestCreatePlayer();

            // Test 2: Get Player Data
            await TestGetPlayer();

            // Test 3: Create Farm Plots
            await TestCreatePlots();

            // Test 4: Get Farm Plots
            await TestGetPlots();

            // Test 5: Plant Crops
            await TestPlantCrops();

            // Test 6: Get Crops
            await TestGetCrops();

            // Test 7: Update Inventory
            await TestUpdateInventory();

            // Test 8: Update Leaderboard
            await TestUpdateLeaderboard();

            Debug.Log("\n✅ All tests completed!");
        }

        private async System.Threading.Tasks.Task TestCreatePlayer()
        {
            Debug.Log("📝 TEST 1: Creating Player Data...");
            
            PlayerData newPlayer = new PlayerData
            {
                playerId = "player_001",
                money = 5000,
                level = 1
            };

            try
            {
                await _firebaseManager.CreatePlayerAsync(newPlayer);
                Debug.Log("✅ Player created successfully\n");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ CreatePlayer failed: {e.Message}\n");
            }
        }

        private async System.Threading.Tasks.Task TestGetPlayer()
        {
            Debug.Log("📖 TEST 2: Getting Player Data...");

            try
            {
                var player = await _firebaseManager.GetPlayerAsync();
                if (player != null)
                {
                    Debug.Log($"✅ Player Data Retrieved:");
                    Debug.Log($"   ID: {player.playerId}");
                    Debug.Log($"   Money: {player.money}");
                    Debug.Log($"   Level: {player.level}\n");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ GetPlayer failed: {e.Message}\n");
            }
        }

        private async System.Threading.Tasks.Task TestCreatePlots()
        {
            Debug.Log("🌾 TEST 3: Creating Farm Plots...");

            try
            {
                await _firebaseManager.SavePlotAsync("plot_001", 0, 0, "dirt");
                await _firebaseManager.SavePlotAsync("plot_002", 1, 0, "dirt");
                await _firebaseManager.SavePlotAsync("plot_003", 0, 1, "dirt");
                
                Debug.Log("✅ Plots created successfully\n");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ SavePlot failed: {e.Message}\n");
            }
        }

        private async System.Threading.Tasks.Task TestGetPlots()
        {
            Debug.Log("🗺️  TEST 4: Getting Farm Plots...");

            try
            {
                var plots = await _firebaseManager.GetPlotsAsync();
                Debug.Log($"✅ Retrieved {plots.Count} plots:");
                
                foreach (var plot in plots)
                {
                    Debug.Log($"   - {plot.Key}: ({plot.Value.x}, {plot.Value.y}) Type: {plot.Value.type}");
                }
                Debug.Log(string.Empty);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ GetPlots failed: {e.Message}\n");
            }
        }

        private async System.Threading.Tasks.Task TestPlantCrops()
        {
            Debug.Log("🌱 TEST 5: Planting Crops...");

            try
            {
                await _firebaseManager.PlantCropAsync("crop_001", "wheat", "plot_001");
                await _firebaseManager.PlantCropAsync("crop_002", "corn", "plot_002");
                await _firebaseManager.PlantCropAsync("crop_003", "carrot", "plot_003");
                
                Debug.Log("✅ Crops planted successfully\n");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ PlantCrop failed: {e.Message}\n");
            }
        }

        private async System.Threading.Tasks.Task TestGetCrops()
        {
            Debug.Log("🌾 TEST 6: Getting Crops...");

            try
            {
                var crops = await _firebaseManager.GetCropsAsync();
                Debug.Log($"✅ Retrieved {crops.Count} crops:");
                
                foreach (var crop in crops)
                {
                    Debug.Log($"   - {crop.Key}: Type={crop.Value.type}, Growth={crop.Value.growthStage}%, Plot={crop.Value.plotId}");
                }
                Debug.Log(string.Empty);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ GetCrops failed: {e.Message}\n");
            }
        }

        private async System.Threading.Tasks.Task TestUpdateInventory()
        {
            Debug.Log("📦 TEST 7: Updating Inventory...");

            try
            {
                // Update seeds
                await _firebaseManager.UpdateSeedAsync("wheat", 100);
                await _firebaseManager.UpdateSeedAsync("corn", 50);
                await _firebaseManager.UpdateSeedAsync("carrot", 75);

                // Update tools
                await _firebaseManager.UpdateToolAsync("shovel", 1);
                await _firebaseManager.UpdateToolAsync("watering_can", 2);

                // Get and display seeds
                var seeds = await _firebaseManager.GetSeedsAsync();
                Debug.Log($"✅ Inventory Updated - Seeds:");
                foreach (var seed in seeds)
                {
                    Debug.Log($"   - {seed.Key}: {seed.Value}");
                }

                // Get and display tools
                var tools = await _firebaseManager.GetToolsAsync();
                Debug.Log($"   Tools:");
                foreach (var tool in tools)
                {
                    Debug.Log($"   - {tool.Key}: {tool.Value}");
                }
                Debug.Log(string.Empty);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Inventory update failed: {e.Message}\n");
            }
        }

        private async System.Threading.Tasks.Task TestUpdateLeaderboard()
        {
            Debug.Log("🏆 TEST 8: Updating Leaderboard...");

            try
            {
                await _firebaseManager.UpdateLeaderboardAsync(5000, 3);
                
                // Get leaderboard
                var leaderboard = await _firebaseManager.GetLeaderboardAsync(5);
                Debug.Log($"✅ Leaderboard Updated - Top {leaderboard.Count} Players:");
                
                int rank = 1;
                foreach (var entry in leaderboard)
                {
                    Debug.Log($"   #{rank} {entry.playerId}: ${entry.money} | {entry.crops} crops");
                    rank++;
                }
                Debug.Log(string.Empty);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Leaderboard update failed: {e.Message}\n");
            }
        }
    }
}
