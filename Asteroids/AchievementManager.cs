using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

namespace AsteroidsGame
{
    // Manages all achievements in the game
    public class AchievementManager
    {
        private Dictionary<string, Achievement> _achievements;
        private List<Achievement> _activeNotifications;
        private const string SaveFilePath = "achievements.json";
        private SpriteFont _font;
        private Texture2D _achievementBackground;
        private Texture2D _progressBar;
        private Texture2D _lockedIcon;
        private Texture2D _unlockedIcon;

        // Game stats being tracked
        private int _totalScore = 0;
        private int _asteroidsDestroyed = 0;
        private int _goldAsteroidsDestroyed = 0;
        private int _enemyShipsDestroyed = 0;
        private int _currentLives = 0;
        private float _timeSinceLastHit = 0f;

        private float _continuousThrusterTime = 0f;
        private float _timeWithoutShooting = 0f;
        private bool _thrusterActive = false;
        private Vector2 _lastPlayerPosition;
        private int _consecutiveHits = 0;
        private Vector2? _lastDeathPosition = null;
        private AsteroidType? _lastDeathCause = null;
        private float _lastLifeTimer = 0f;
        private int _onScreenAsteroidCount = 0;
        private float _closestAsteroidDistance = float.MaxValue;
        private List<Tuple<GameObject, float>> _nearMissTracking = new List<Tuple<GameObject, float>>();
        private Dictionary<int, float> _asteroidTimeToCollision = new Dictionary<int, float>();

        private int _currentPage = 0;
        private AchievementType? _currentFilter = null;
        private const int _achievementsPerPage = 6;
        private bool _isScrolling = false;
        private float _scrollPosition = 0;
        private float _targetScrollPosition = 0;
        private float _scrollSpeed = 500f;

        public AchievementManager()
        {
            _achievements = new Dictionary<string, Achievement>();
            _activeNotifications = new List<Achievement>();
        }

        // Update all achievements
        public void Update(float deltaTime, Game1.GameState gameState, int currentScore, int lives)
        {
            // Update tracking variables
            _totalScore = currentScore;
            _currentLives = lives;

            // Only update timers if actually playing
            if (gameState == Game1.GameState.Playing)
            {
                // Update no-hit timer
                _timeSinceLastHit += deltaTime;

                // Update no-shoot timer
                _timeWithoutShooting += deltaTime;
                if (_timeWithoutShooting >= 60f)
                {
                    // Pacifist achievement - survived 1 minute without shooting
                    GetAchievement("pacifist")?.UpdateProgress(60);
                }

                // Update thruster timer if active
                if (_thrusterActive)
                {
                    _continuousThrusterTime += deltaTime;

                    // Check thruster achievements
                    UpdateThrusterAchievements();
                }

                // Update last life timer if on last life
                if (lives == 1)
                {
                    _lastLifeTimer += deltaTime;

                    // Check for Last Ship Standing achievement
                    if (_lastLifeTimer >= 300f) // 5 minutes
                    {
                        GetAchievement("last_ship")?.UpdateProgress(300);
                    }
                }
            }

            // Update all achievements
            foreach (var achievement in _achievements.Values)
            {
                achievement.Update(deltaTime);

                // Check for new unlocks
                if (!achievement.IsUnlocked)
                {
                    CheckAchievementProgress(achievement);
                }
            }

            // Update notification list
            _activeNotifications = _achievements.Values
                .Where(a => a.IsDisplaying)
                .ToList();
        }

        // Initialize with default achievements
        public void Initialize()
        {
            // Add default achievements
            CreateDefaultAchievements();

            // Load saved achievements
            LoadAchievements();
        }


        // Load content
        public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            // Load font
            _font = content.Load<SpriteFont>("Font");

            // Create textures
            _achievementBackground = new Texture2D(graphicsDevice, 1, 1);
            _achievementBackground.SetData(new[] { Color.White });

            _progressBar = new Texture2D(graphicsDevice, 1, 1);
            _progressBar.SetData(new[] { Color.White });

            // Load icons or create placeholders
            try
            {
                _lockedIcon = content.Load<Texture2D>("achievement_locked");
                _unlockedIcon = content.Load<Texture2D>("achievement_unlocked");
            }
            catch
            {
                // Create placeholder icons
                _lockedIcon = new Texture2D(graphicsDevice, 32, 32);
                Color[] lockData = new Color[32 * 32];
                for (int i = 0; i < lockData.Length; i++) lockData[i] = Color.Gray;
                _lockedIcon.SetData(lockData);

                _unlockedIcon = new Texture2D(graphicsDevice, 32, 32);
                Color[] unlockData = new Color[32 * 32];
                for (int i = 0; i < unlockData.Length; i++) unlockData[i] = Color.Gold;
                _unlockedIcon.SetData(unlockData);
            }
        }
        public void AddAchievement(Achievement achievement)
        {
            if (!_achievements.ContainsKey(achievement.Id))
            {
                _achievements.Add(achievement.Id, achievement);
            }
        }
        public Achievement GetAchievement(string id)
        {
            if (_achievements.ContainsKey(id))
            {
                return _achievements[id];
            }
            return null;
        }
        public float GetAchievementProgress(string achievementId)
        {
            if (_achievements.TryGetValue(achievementId, out Achievement achievement))
            {
                return achievement.GetProgressPercentage();
            }
            return 0f;
        }

        public List<Achievement> GetAchievementsByType(AchievementType type)
        {
            return _achievements.Values
                .Where(a => a.Type == type)
                .OrderByDescending(a => a.IsUnlocked)
                .ThenBy(a => a.ProgressTarget)
                .ToList();
        }

        public void ResetAllAchievements()
        {
            foreach (var achievement in _achievements.Values)
            {
                achievement.Reset();
            }

            // Reset tracking variables
            _totalScore = 0;
            _asteroidsDestroyed = 0;
            _goldAsteroidsDestroyed = 0;
            _enemyShipsDestroyed = 0;
            _timeSinceLastHit = 0f;
            _continuousThrusterTime = 0f;
            _timeWithoutShooting = 0f;
            _thrusterActive = false;
            _consecutiveHits = 0;
            _lastDeathPosition = null;
            _lastDeathCause = null;
            _lastLifeTimer = 0f;
            _onScreenAsteroidCount = 0;
            _closestAsteroidDistance = float.MaxValue;
            _nearMissTracking.Clear();
            _asteroidTimeToCollision.Clear();

            // Save the reset achievements
            SaveAchievements();
        }
        public void UnlockAllAchievements()
        {
            foreach (var achievement in _achievements.Values)
            {
                achievement.Unlock();
            }

            // Save the unlocked achievements
            SaveAchievements();
        }

        public List<string> GetAchievementCategories()
        {
            var categories = new List<string>();

            foreach (AchievementType type in Enum.GetValues(typeof(AchievementType)))
            {
                // Skip empty categories
                if (_achievements.Values.Any(a => a.Type == type))
                {
                    // Convert enum to display name
                    string displayName = type switch
                    {
                        AchievementType.AsteroidsDestroyed => "Asteroid Destruction",
                        AchievementType.ShipsDestroyed => "UFO Destruction",
                        AchievementType.GoldAsteroidsFound => "Gold Asteroids",
                        AchievementType.ThrusterTime => "Space Travel",
                        AchievementType.NoShoot => "Pacifist",
                        AchievementType.NoHit => "Survival",
                        AchievementType.LivesLeft => "Lives",
                        AchievementType.ScoreReached => "Score",
                        AchievementType.Special => "Special",
                        _ => type.ToString()
                    };

                    categories.Add(displayName);
                }
            }

            return categories;
        }
        public string GetAchievementStats()
        {
            int total = _achievements.Count;
            int unlocked = _achievements.Values.Count(a => a.IsUnlocked);
            float percentage = (float)unlocked / total * 100;

            return $"Achievements: {unlocked}/{total} ({percentage:F1}%)";
        }
        public List<Achievement> GetNearlyCompleted(float threshold = 0.75f)
        {
            return _achievements.Values
                .Where(a => !a.IsUnlocked && a.GetProgressPercentage() >= threshold)
                .OrderByDescending(a => a.GetProgressPercentage())
                .ToList();
        }
        public string GetNewlyUnlockedSinceLastSession()
        {
            var lastSession = LoadLastSessionAchievements();
            var currentSession = _achievements.Values.Where(a => a.IsUnlocked).Select(a => a.Id).ToList();

            // Find achievements unlocked this session
            var newlyUnlocked = currentSession.Except(lastSession).ToList();

            if (newlyUnlocked.Count == 0)
            {
                return "No new achievements.";
            }

            // Build response
            string result = $"New achievements: {newlyUnlocked.Count}\n";
            foreach (var id in newlyUnlocked)
            {
                if (_achievements.TryGetValue(id, out Achievement achievement))
                {
                    result += $"• {achievement.Title}\n";
                }
            }

            // Save current session
            SaveLastSessionAchievements(currentSession);

            return result;
        }
        public List<Achievement> GetRecentlyUnlocked(int count = 5)
        {
            return _achievements.Values
                .Where(a => a.IsUnlocked)
                .OrderByDescending(a => a.UnlockTime)
                .Take(count)
                .ToList();
        }

        private List<string> LoadLastSessionAchievements()
        {
            const string fileName = "last_session.json";
            try
            {
                if (File.Exists(fileName))
                {
                    string json = File.ReadAllText(fileName);
                    return JsonSerializer.Deserialize<List<string>>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading last session: {ex.Message}");
            }

            return new List<string>();
        }

        // Helper method to save current session achievements
        private void SaveLastSessionAchievements(List<string> unlockedIds)
        {
            const string fileName = "last_session.json";
            try
            {
                string json = JsonSerializer.Serialize(unlockedIds);
                File.WriteAllText(fileName, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving last session: {ex.Message}");
            }
        }

        public void ShowAchievementNotification(string achievementId)
        {
            if (_achievements.TryGetValue(achievementId, out Achievement achievement))
            {
                achievement.DisplayNotification();
            }
        }

        // Add method to test various achievement triggers
        public void TestAchievementTrigger(string achievementId, int progress = 1)
        {
            if (_achievements.TryGetValue(achievementId, out Achievement achievement))
            {
                achievement.IncrementProgress(progress);
            }
        }



        // Check if specific achievements should be updated
        private void CheckAchievementProgress(Achievement achievement)
        {
            switch (achievement.Type)
            {
                case AchievementType.ScoreReached:
                    achievement.UpdateProgress(_totalScore);
                    break;

                case AchievementType.AsteroidsDestroyed:
                    achievement.UpdateProgress(_asteroidsDestroyed);
                    break;

                case AchievementType.GoldAsteroidsFound:
                    achievement.UpdateProgress(_goldAsteroidsDestroyed);
                    break;

                case AchievementType.ShipsDestroyed:
                    achievement.UpdateProgress(_enemyShipsDestroyed);
                    break;

                case AchievementType.NoHit:
                    achievement.UpdateProgress((int)_timeSinceLastHit);
                    break;

                case AchievementType.LivesLeft:
                    // For "reach X points without losing a life"
                    if (_totalScore >= 5000 && _currentLives == GameConstants.InitialLives)
                    {
                        achievement.UpdateProgress(3);
                    }
                    break;

                case AchievementType.Special:
                    // Special "From the Brink" achievement
                    if (achievement.Id == "special_comeback" && _currentLives == 1 && _totalScore >= 10000)
                    {
                        achievement.UpdateProgress(1);
                    }
                    break;
            }
        }



        // Add this method to handle input for the achievements menu
        public void HandleAchievementsInput(GameTime gameTime, InputManager inputManager)
        {
            // Calculate total pages
            int totalItems = _currentFilter.HasValue
                ? _achievements.Values.Count(a => a.Type == _currentFilter.Value)
                : _achievements.Count;

            int totalPages = (int)Math.Ceiling((double)totalItems / _achievementsPerPage);

            // Handle keyboard/gamepad navigation
            if (inputManager.IsKeyPressed(Keys.Right) || inputManager.IsButtonPressed(Buttons.DPadRight) ||
                inputManager.IsButtonPressed(Buttons.LeftThumbstickRight) || inputManager.IsThumbstickRight())
            {
                // Next page
                if (_currentPage < totalPages - 1)
                {
                    _currentPage++;
                    _isScrolling = true;
                    _targetScrollPosition = _currentPage * (_achievementsPerPage * 100); // Approximate height of page
                }
            }
            else if (inputManager.IsKeyPressed(Keys.Left) || inputManager.IsButtonPressed(Buttons.DPadLeft) ||
                     inputManager.IsButtonPressed(Buttons.LeftThumbstickLeft) || inputManager.IsThumbstickLeft())
            {
                // Previous page
                if (_currentPage > 0)
                {
                    _currentPage--;
                    _isScrolling = true;
                    _targetScrollPosition = _currentPage * (_achievementsPerPage * 100); // Approximate height of page
                }
            }

            // Handle filter cycling with Up/Down keys
            if (inputManager.IsKeyPressed(Keys.Up) || inputManager.IsButtonPressed(Buttons.DPadUp) ||
                inputManager.IsButtonPressed(Buttons.LeftThumbstickUp) || inputManager.IsThumbstickUp())
            {
                // Cycle to previous filter
                CycleToPreviousFilter();
            }
            else if (inputManager.IsKeyPressed(Keys.Down) || inputManager.IsButtonPressed(Buttons.DPadDown) ||
                     inputManager.IsButtonPressed(Buttons.LeftThumbstickDown) || inputManager.IsThumbstickDown())
            {
                // Cycle to next filter
                CycleToNextFilter();
            }

            // Update smooth scrolling animation
            if (_isScrolling)
            {
                float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (Math.Abs(_scrollPosition - _targetScrollPosition) < 1f)
                {
                    _scrollPosition = _targetScrollPosition;
                    _isScrolling = false;
                }
                else
                {
                    _scrollPosition = MathHelper.Lerp(_scrollPosition, _targetScrollPosition, delta * 5f);
                }
            }
        }


        // Method to cycle to the next filter type
        private void CycleToNextFilter()
        {
            if (!_currentFilter.HasValue)
            {
                // If no filter, start with first type
                _currentFilter = AchievementType.AsteroidsDestroyed;
            }
            else
            {
                // Get all available types that have achievements
                var availableTypes = Enum.GetValues(typeof(AchievementType))
                    .Cast<AchievementType>()
                    .Where(t => _achievements.Values.Any(a => a.Type == t))
                    .ToList();

                int currentIndex = availableTypes.IndexOf(_currentFilter.Value);
                int nextIndex = (currentIndex + 1) % availableTypes.Count;

                // Set to next type or null if completed full cycle
                if (nextIndex == 0)
                {
                    _currentFilter = null; // No filter
                }
                else
                {
                    _currentFilter = availableTypes[nextIndex];
                }
            }

            // Reset to first page when changing filters
            _currentPage = 0;
            _targetScrollPosition = 0;
            _scrollPosition = 0;
            _isScrolling = false;
        }

        // Method to cycle to the previous filter type
        private void CycleToPreviousFilter()
        {
            if (!_currentFilter.HasValue)
            {
                // If no filter, start with last type
                var availableTypes = Enum.GetValues(typeof(AchievementType))
                    .Cast<AchievementType>()
                    .Where(t => _achievements.Values.Any(a => a.Type == t))
                    .ToList();

                _currentFilter = availableTypes.LastOrDefault();
            }
            else
            {
                // Get all available types that have achievements
                var availableTypes = Enum.GetValues(typeof(AchievementType))
                    .Cast<AchievementType>()
                    .Where(t => _achievements.Values.Any(a => a.Type == t))
                    .ToList();

                int currentIndex = availableTypes.IndexOf(_currentFilter.Value);
                int prevIndex = (currentIndex - 1 + availableTypes.Count) % availableTypes.Count;

                // Set to previous type or null if at first type
                if (currentIndex == 0)
                {
                    _currentFilter = null; // No filter
                }
                else
                {
                    _currentFilter = availableTypes[prevIndex];
                }
            }

            // Reset to first page when changing filters
            _currentPage = 0;
            _targetScrollPosition = 0;
            _scrollPosition = 0;
            _isScrolling = false;
        }

        // Updated DrawAchievementsScreen method with scrolling support
        public void DrawAchievementsScreen(SpriteBatch spriteBatch)
        {
            // Draw semi-transparent overlay background
            spriteBatch.Draw(
                _achievementBackground,
                new Rectangle(0, 0, GameConstants.ScreenWidth, GameConstants.ScreenHeight),
                new Color(0, 0, 0, 200)
            );

            // Draw title
            string title = "ACHIEVEMENTS";
            Vector2 titleSize = _font.MeasureString(title);
            spriteBatch.DrawString(
                _font,
                title,
                new Vector2((GameConstants.ScreenWidth - titleSize.X) / 2, 40),
                Color.Yellow
            );

            // Draw achievement stats
            string stats = GetAchievementStats();
            Vector2 statsSize = _font.MeasureString(stats);
            spriteBatch.DrawString(
                _font,
                stats,
                new Vector2((GameConstants.ScreenWidth - statsSize.X) / 2, 80),
                Color.LightBlue
            );

            // Draw filter indicator if filter is active
            if (_currentFilter.HasValue)
            {
                string filterText = $"Filtered by: {GetFilterDisplayName(_currentFilter.Value)}";
                Vector2 filterSize = _font.MeasureString(filterText);
                spriteBatch.DrawString(
                    _font,
                    filterText,
                    new Vector2((GameConstants.ScreenWidth - filterSize.X) / 2, 110),
                    Color.Orange
                );
            }

            // Get achievements for current page and filter
            List<Achievement> pageAchievements;

            if (_currentFilter.HasValue)
            {
                // Get filtered achievements
                pageAchievements = GetAchievementsByType(_currentFilter.Value)
                    .Skip(_currentPage * _achievementsPerPage)
                    .Take(_achievementsPerPage)
                    .ToList();
            }
            else
            {
                // Get all achievements for this page
                pageAchievements = _achievements.Values
                    .OrderBy(a => a.Type)
                    .ThenByDescending(a => a.IsUnlocked)
                    .Skip(_currentPage * _achievementsPerPage)
                    .Take(_achievementsPerPage)
                    .ToList();
            }

            // Set layout variables
            int achievementWidth = 600;
            int achievementHeight = 80;
            int padding = 20;
            int startY = 150;

            // Draw page navigation
            DrawPaginationControls(spriteBatch);

            // Draw each achievement item
            for (int i = 0; i < pageAchievements.Count; i++)
            {
                var achievement = pageAchievements[i];
                int y = startY + i * (achievementHeight + padding);

                // Draw achievement item background
                Color bgColor = achievement.IsUnlocked
                    ? new Color(30, 30, 50, 230)
                    : new Color(20, 20, 30, 200);

                spriteBatch.Draw(
                    _achievementBackground,
                    new Rectangle(
                        (GameConstants.ScreenWidth - achievementWidth) / 2,
                        y,
                        achievementWidth,
                        achievementHeight
                    ),
                    bgColor
                );

                // Draw icon
                Texture2D icon = achievement.IsUnlocked ? _unlockedIcon : _lockedIcon;
                spriteBatch.Draw(
                    icon,
                    new Rectangle(
                        (GameConstants.ScreenWidth - achievementWidth) / 2 + 20,
                        y + 20,
                        40,
                        40
                    ),
                    Color.White
                );

                // Draw achievement title and description - SANITIZE TEXT HERE
                string achievementText = SanitizeText(achievement.Title);
                string descriptionText = achievement.IsSecret && !achievement.IsUnlocked
                    ? "???"
                    : SanitizeText(achievement.Description);

                spriteBatch.DrawString(
                    _font,
                    achievementText,
                    new Vector2(
                        (GameConstants.ScreenWidth - achievementWidth) / 2 + 80,
                        y + 15
                    ),
                    achievement.IsUnlocked ? Color.Yellow : Color.Gray
                );

                spriteBatch.DrawString(
                    _font,
                    descriptionText,
                    new Vector2(
                        (GameConstants.ScreenWidth - achievementWidth) / 2 + 80,
                        y + 40
                    ),
                    achievement.IsUnlocked ? Color.White : Color.Gray * 0.8f
                );

                // Draw progress bar for incomplete, non-secret achievements
                if (!achievement.IsUnlocked && !achievement.IsSecret)
                {
                    int progressWidth = 200;
                    int progressHeight = 10;
                    int progressX = (GameConstants.ScreenWidth + achievementWidth) / 2 - progressWidth - 20;
                    int progressY = y + achievementHeight - progressHeight - 15;

                    // Background bar
                    spriteBatch.Draw(
                        _progressBar,
                        new Rectangle(progressX, progressY, progressWidth, progressHeight),
                        new Color(50, 50, 50, 150)
                    );

                    // Progress bar
                    float progress = achievement.GetProgressPercentage();
                    spriteBatch.Draw(
                        _progressBar,
                        new Rectangle(progressX, progressY, (int)(progressWidth * progress), progressHeight),
                        new Color(0, 200, 100, 200)
                    );

                    // Progress text
                    string progressText = $"{achievement.ProgressCurrent}/{achievement.ProgressTarget}";
                    Vector2 progressTextSize = _font.MeasureString(progressText);
                    spriteBatch.DrawString(
                        _font,
                        progressText,
                        new Vector2(
                            progressX + (progressWidth - progressTextSize.X) / 2,
                            progressY - progressTextSize.Y - 2
                        ),
                        Color.White
                    );
                }
            }

            // Draw instructions for returning and navigation
            string returnText = "PRESS ESC TO RETURN";
            Vector2 returnSize = _font.MeasureString(returnText);
            spriteBatch.DrawString(
                _font,
                returnText,
                new Vector2(
                    (GameConstants.ScreenWidth - returnSize.X) / 2,
                    GameConstants.ScreenHeight - 40
                ),
                Color.White
            );

//            string controlsText = "← → PAGE NAVIGATION   ↑ ↓ FILTER BY TYPE";
//            Vector2 controlsSize = _font.MeasureString(controlsText);
//            spriteBatch.DrawString(
//                _font,
//                controlsText,
//                new Vector2(
//                    (GameConstants.ScreenWidth - controlsSize.X) / 2,
//                    GameConstants.ScreenHeight - 70
//                ),
//                Color.LightGray
//            );
        }

        // Updated pagination controls to match current page/filter state
        private void DrawPaginationControls(SpriteBatch spriteBatch)
        {
            // Calculate total pages
            int totalItems = _currentFilter.HasValue
                ? _achievements.Values.Count(a => a.Type == _currentFilter.Value)
                : _achievements.Count;

            int totalPages = (int)Math.Ceiling((double)totalItems / _achievementsPerPage);

            if (totalPages <= 1)
                return; // No pagination needed

            string pageText = $"Page {_currentPage + 1}/{totalPages}";
            Vector2 textSize = _font.MeasureString(pageText);
            float x = (GameConstants.ScreenWidth - textSize.X) / 2;
            float y = GameConstants.ScreenHeight - 100;

            // Draw page indicator
            spriteBatch.DrawString(
                _font,
                pageText,
                new Vector2(x, y),
                Color.White
            );

            // Draw previous/next indicators - Use plain ASCII text
            if (_currentPage > 0)
            {
                spriteBatch.DrawString(
                    _font,
                    "< Prev",
                    new Vector2(x - 100, y),
                    Color.Yellow
                );
            }

            if (_currentPage < totalPages - 1)
            {
                spriteBatch.DrawString(
                    _font,
                    "Next >",
                    new Vector2(x + textSize.X + 20, y),
                    Color.Yellow
                );
            }
        }

        // Helper method to get a display name for achievement types
        private string GetFilterDisplayName(AchievementType type)
        {
            return type switch
            {
                AchievementType.AsteroidsDestroyed => "Asteroid Destruction",
                AchievementType.ShipsDestroyed => "UFO Destruction",
                AchievementType.GoldAsteroidsFound => "Gold Asteroids",
                AchievementType.ThrusterTime => "Space Travel",
                AchievementType.NoShoot => "Pacifist",
                AchievementType.NoHit => "Survival",
                AchievementType.LivesLeft => "Lives",
                AchievementType.ScoreReached => "Score",
                AchievementType.Special => "Special",
                _ => type.ToString()
            };
        }



        public void DrawNotifications(SpriteBatch spriteBatch)
        {
            const int notificationWidth = 300;
            const int notificationHeight = 60;
            const int padding = 10;
            const int ySpacing = notificationHeight + 5;
            const int startY = 20;

            // Draw each active notification
            for (int i = 0; i < _activeNotifications.Count; i++)
            {
                var achievement = _activeNotifications[i];
                if (!achievement.IsDisplaying) continue;

                float alpha = achievement.DisplayAlpha;
                if (alpha <= 0) continue;

                int y = startY + i * ySpacing;

                // Draw background
                spriteBatch.Draw(
                    _achievementBackground,
                    new Rectangle(
                        GameConstants.ScreenWidth - notificationWidth - padding,
                        y,
                        notificationWidth,
                        notificationHeight
                    ),
                    new Color(0, 0, 0, 150 * alpha)
                );

                // Draw icon
                spriteBatch.Draw(
                    _unlockedIcon,
                    new Rectangle(
                        GameConstants.ScreenWidth - notificationWidth - padding + 10,
                        y + 10,
                        40,
                        40
                    ),
                    new Color(255, 255, 255, 255 * alpha)
                );

                // Draw achievement text - SANITIZE TEXT HERE 
                string unlockMessage = "Achievement Unlocked!";
                string achievementTitle = SanitizeText(achievement.Title); // Sanitize title

                spriteBatch.DrawString(
                    _font,
                    unlockMessage,
                    new Vector2(
                        GameConstants.ScreenWidth - notificationWidth - padding + 60,
                        y + 10
                    ),
                    new Color(255, 215, 0, 255 * alpha)
                );

                spriteBatch.DrawString(
                    _font,
                    achievementTitle,
                    new Vector2(
                        GameConstants.ScreenWidth - notificationWidth - padding + 60,
                        y + 30
                    ),
                    new Color(255, 255, 255, 255 * alpha)
                );
            }
        }
        private void DrawPaginationControls(SpriteBatch spriteBatch, int currentPage, int itemsPerPage, AchievementType? filter)
        {
            // Calculate total pages
            int totalItems = filter.HasValue
                ? _achievements.Values.Count(a => a.Type == filter.Value)
                : _achievements.Count;

            int totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);

            if (totalPages <= 1)
                return; // No pagination needed

            string pageText = $"Page {currentPage + 1}/{totalPages}";
            Vector2 textSize = _font.MeasureString(pageText);
            float x = (GameConstants.ScreenWidth - textSize.X) / 2;
            float y = GameConstants.ScreenHeight - 80;

            // Draw page indicator
            spriteBatch.DrawString(
                _font,
                pageText,
                new Vector2(x, y),
                Color.White
            );

            // Draw previous/next indicators - Use plain ASCII text
            if (currentPage > 0)
            {
                spriteBatch.DrawString(
                    _font,
                    "< Prev",
                    new Vector2(x - 100, y),
                    Color.Yellow
                );
            }

            if (currentPage < totalPages - 1)
            {
                spriteBatch.DrawString(
                    _font,
                    "Next >",
                    new Vector2(x + textSize.X + 20, y),
                    Color.Yellow
                );
            }
        }
        private string SanitizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Use string.Replace instead of char.Replace for all replacements
            // This avoids the "too many characters in character literal" errors
            text = text.Replace("\u2026", "...");     // Replace ellipsis with three periods
            text = text.Replace("...", "...");        // Ensure standard ellipsis is kept
            text = text.Replace("\u2014", "-");       // Replace em dash with hyphen
            text = text.Replace("\u2013", "-");       // Replace en dash with hyphen
            text = text.Replace("\u2018", "'");       // Replace left single quote with straight apostrophe
            text = text.Replace("\u2019", "'");       // Replace right single quote with straight apostrophe
            text = text.Replace("\u201C", "\"");      // Replace left double quote with straight quote
            text = text.Replace("\u201D", "\"");      // Replace right double quote with straight quote
            text = text.Replace("\u00AB", "<");       // Replace left guillemet with less than
            text = text.Replace("\u00BB", ">");       // Replace right guillemet with greater than
            text = text.Replace("\u2248", "~");       // Replace approximately equal with tilde
            text = text.Replace("\u00A9", "(c)");     // Replace copyright with (c)
            text = text.Replace("\u00AE", "(R)");     // Replace registered trademark with (R)
            text = text.Replace("\u2122", "(TM)");    // Replace trademark with (TM)

            // Additional replacements for other potential problematic characters
            text = text.Replace("\u00A0", " ");       // Replace non-breaking space with regular space
            text = text.Replace("\u2022", "*");       // Replace bullet with asterisk
            text = text.Replace("\u2212", "-");       // Replace minus sign with hyphen
            text = text.Replace("\u00B7", "*");       // Replace middle dot with asterisk

            return text;
        }
        private void CreateDefaultAchievements()
        {
            // Asteroid Destruction Achievements
            AddAchievement(new Achievement(
                "asteroid_1",
                "Rookie",
                "Destroy 1 asteroid",
                AchievementType.AsteroidsDestroyed,
                1));

            AddAchievement(new Achievement(
                "asteroid_10",
                "Getting Started",
                "Destroy 10 asteroids",
                AchievementType.AsteroidsDestroyed,
                10));

            AddAchievement(new Achievement(
                "asteroid_50",
                "Space Sweeper",
                "Destroy 50 asteroids",
                AchievementType.AsteroidsDestroyed,
                50));

            AddAchievement(new Achievement(
                "asteroid_100",
                "Asteroid Hunter",
                "Destroy 100 asteroids",
                AchievementType.AsteroidsDestroyed,
                100));

            AddAchievement(new Achievement(
                "asteroid_500",
                "Debris Clearer",
                "Destroy 500 asteroids",
                AchievementType.AsteroidsDestroyed,
                500));

            AddAchievement(new Achievement(
                "asteroid_1000",
                "Asteroid Terminator",
                "Destroy 1,000 asteroids",
                AchievementType.AsteroidsDestroyed,
                1000));

            AddAchievement(new Achievement(
                "asteroid_5000",
                "Master Blaster",
                "Destroy 5,000 asteroids",
                AchievementType.AsteroidsDestroyed,
                5000));

            AddAchievement(new Achievement(
                "asteroid_10000",
                "Rock Crusher Supreme",
                "Destroy 10,000 asteroids",
                AchievementType.AsteroidsDestroyed,
                10000));

            // Fixed problematic ellipsis
            AddAchievement(new Achievement(
                "asteroid_1000000",
                "I Have Become Miner, Destroyer of Rocks...",  // Using proper ASCII ellipsis
                "Destroy 1,000,000 asteroids",
                AchievementType.AsteroidsDestroyed,
                1000000));

            // Gold Asteroid Achievements
            AddAchievement(new Achievement(
                "gold_1",
                "First Gold!",
                "Find and destroy a gold asteroid",
                AchievementType.GoldAsteroidsFound,
                1));

            AddAchievement(new Achievement(
                "gold_5",
                "Gold Digger",
                "Find 5 gold asteroids",
                AchievementType.GoldAsteroidsFound,
                5));

            AddAchievement(new Achievement(
                "gold_10",
                "Gold Rush",
                "Find 10 gold asteroids",
                AchievementType.GoldAsteroidsFound,
                10));

            AddAchievement(new Achievement(
                "gold_25",
                "Gold Miner",
                "Find 25 gold asteroids",
                AchievementType.GoldAsteroidsFound,
                25));

            AddAchievement(new Achievement(
                "gold_50",
                "Gold Prospector",
                "Find 50 gold asteroids",
                AchievementType.GoldAsteroidsFound,
                50));

            // UFO Achievements
            AddAchievement(new Achievement(
                "ships_1",
                "First Contact",
                "Destroy a UFO",
                AchievementType.ShipsDestroyed,
                1));

            AddAchievement(new Achievement(
                "ships_5",
                "Alien Hunter",
                "Destroy 5 UFOs",
                AchievementType.ShipsDestroyed,
                5));

            AddAchievement(new Achievement(
                "ships_25",
                "Alien Exterminator",
                "Destroy 25 UFOs",
                AchievementType.ShipsDestroyed,
                25));

            AddAchievement(new Achievement(
                "ships_100",
                "UFO Terminator",
                "Destroy 100 UFOs",
                AchievementType.ShipsDestroyed,
                100));

            // Score Achievements
            AddAchievement(new Achievement(
                "score_1000",
                "Beginner",
                "Score 1,000 points",
                AchievementType.ScoreReached,
                1000));

            AddAchievement(new Achievement(
                "score_5000",
                "Amateur",
                "Score 5,000 points",
                AchievementType.ScoreReached,
                5000));

            AddAchievement(new Achievement(
                "score_10000",
                "Professional",
                "Score 10,000 points",
                AchievementType.ScoreReached,
                10000));

            AddAchievement(new Achievement(
                "score_50000",
                "Expert",
                "Score 50,000 points",
                AchievementType.ScoreReached,
                50000));

            AddAchievement(new Achievement(
                "score_100000",
                "Master",
                "Score 100,000 points",
                AchievementType.ScoreReached,
                100000));

            AddAchievement(new Achievement(
                "score_500000",
                "Space Legend",
                "Score 500,000 points",
                AchievementType.ScoreReached,
                500000));

            AddAchievement(new Achievement(
                "score_1000000",
                "Galactic Champion",
                "Score 1,000,000 points",
                AchievementType.ScoreReached,
                1000000));

            // Survival Achievements
            AddAchievement(new Achievement(
                "no_hit_30",
                "Dodger",
                "Avoid being hit for 30 seconds",
                AchievementType.NoHit,
                30));

            AddAchievement(new Achievement(
                "no_hit_60",
                "Evasive Pilot",
                "Avoid being hit for 60 seconds",
                AchievementType.NoHit,
                60));

            AddAchievement(new Achievement(
                "no_hit_120",
                "Untouchable",
                "Avoid being hit for 2 minutes",
                AchievementType.NoHit,
                120));

            AddAchievement(new Achievement(
                "no_hit_300",
                "Ghost Ship",
                "Avoid being hit for 5 minutes",
                AchievementType.NoHit,
                300));

            // Lives Achievements
            AddAchievement(new Achievement(
                "flawless",
                "Flawless Victory",
                "Score 5,000 points without losing a life",
                AchievementType.LivesLeft,
                3));

            // Special Achievements
            AddAchievement(new Achievement(
                "first_death",
                "Space Debris",
                "Die for the first time",
                AchievementType.Special,
                1));

            AddAchievement(new Achievement(
                "special_comeback",
                "From the Brink",
                "Score 10,000 points with only 1 life left",
                AchievementType.Special,
                1));

            AddAchievement(new Achievement(
                "last_ship",
                "Last Ship Standing",
                "Survive for 5 minutes with only 1 life left",
                AchievementType.Special,
                300));

            // Thruster Achievements
            AddAchievement(new Achievement(
                "thruster_30",
                "Rocket Man",
                "Use thrusters continuously for 30 seconds",
                AchievementType.ThrusterTime,
                30));

            AddAchievement(new Achievement(
                "thruster_60",
                "Full Throttle",
                "Use thrusters continuously for 60 seconds",
                AchievementType.ThrusterTime,
                60));

            AddAchievement(new Achievement(
                "thruster_300",
                "Infinite Propulsion",
                "Use thrusters continuously for 5 minutes",
                AchievementType.ThrusterTime,
                300));

            // Pacifist Achievement
            AddAchievement(new Achievement(
                "pacifist",
                "Pacifist",
                "Survive for 60 seconds without shooting",
                AchievementType.NoShoot,
                60));

            // Secret achievements with safer text
            AddAchievement(new Achievement(
                "deja_vu",
                "Deja Vu",
                "Die in the same place twice",
                AchievementType.Special,
                1,
                true));

            AddAchievement(new Achievement(
                "whisperer",
                "Asteroid Whisperer",
                "Have an asteroid destroy a UFO",
                AchievementType.Special,
                1,
                true));

            AddAchievement(new Achievement(
                "sacrifice",
                "Glorious Sacrifice",
                "Die by ramming into a UFO",
                AchievementType.Special,
                1,
                true));

            AddAchievement(new Achievement(
                "aimbot",
                "Aimbot Activated",
                "Hit 10 targets in a row without missing",
                AchievementType.Special,
                10,
                true));

            AddAchievement(new Achievement(
                "lucky_shot",
                "Lucky Shot",
                "Destroy a UFO while it's off-screen",
                AchievementType.Special,
                1,
                true));

            AddAchievement(new Achievement(
                "speedster",
                "Speedster",
                "Reach maximum ship velocity",
                AchievementType.Special,
                1,
                true));

            AddAchievement(new Achievement(
                "black_hole",
                "Black Hole Theory",
                "Have more than 30 asteroids on screen at once",
                AchievementType.Special,
                30,
                true));

            AddAchievement(new Achievement(
                "close_call",
                "Close Call",
                "Narrowly avoid an asteroid (within 5 pixels)",
                AchievementType.Special,
                1,
                true));

            AddAchievement(new Achievement(
                "clutch_save",
                "Clutch Save",
                "Destroy an asteroid less than 0.5 seconds from impact",
                AchievementType.Special,
                1,
                true));
        }



        // Save achievements
        public void SaveAchievements()
        {
            try
            {
                // Create serializable data structure
                var saveData = _achievements.Values.Select(a => new AchievementSaveData
                {
                    Id = a.Id,
                    IsUnlocked = a.IsUnlocked,
                    UnlockTime = a.UnlockTime,
                    ProgressCurrent = a.ProgressCurrent
                }).ToList();

                // Serialize to JSON
                string json = JsonSerializer.Serialize(saveData);

                // Write to file
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving achievements: {ex.Message}");
            }
        }

        // Load achievements
        public void LoadAchievements()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    // Read from file
                    string json = File.ReadAllText(SaveFilePath);

                    // Deserialize from JSON
                    var saveData = JsonSerializer.Deserialize<List<AchievementSaveData>>(json);

                    // Update achievement data
                    foreach (var data in saveData)
                    {
                        if (_achievements.ContainsKey(data.Id))
                        {
                            var achievement = _achievements[data.Id];

                            // If previously unlocked, set unlocked again
                            if (data.IsUnlocked)
                            {
                                achievement.Unlock();
                            }
                            // Otherwise just update progress
                            else
                            {
                                achievement.UpdateProgress(data.ProgressCurrent);
                            }
                        }
                    }

                    // Extract stats from saved achievements
                    var destroyAchievement = GetAchievement("destroy_500");
                    if (destroyAchievement != null)
                    {
                        _asteroidsDestroyed = destroyAchievement.ProgressCurrent;
                    }

                    var goldAchievement = GetAchievement("gold_10");
                    if (goldAchievement != null)
                    {
                        _goldAsteroidsDestroyed = goldAchievement.ProgressCurrent;
                    }

                    var shipAchievement = GetAchievement("ships_25");
                    if (shipAchievement != null)
                    {
                        _enemyShipsDestroyed = shipAchievement.ProgressCurrent;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading achievements: {ex.Message}");
            }
        }

        // Get count of unlocked achievements
        public int GetUnlockedCount()
        {
            return _achievements.Values.Count(a => a.IsUnlocked);
        }

        // Get total count of achievements
        public int GetTotalCount()
        {
            return _achievements.Count;
        }


        // Track thruster usage
        public void TrackThruster(bool isActive)
        {
            if (isActive)
            {
                if (!_thrusterActive)
                {
                    // Thruster just turned on
                    _thrusterActive = true;
                }
            }
            else
            {
                if (_thrusterActive)
                {
                    // Thruster just turned off - reset timer
                    _thrusterActive = false;
                    _continuousThrusterTime = 0f;
                }
            }
        }

        // Track asteroid destruction
        public void TrackAsteroidDestroyed(AsteroidType type)
        {
            _asteroidsDestroyed++;

            if (type == AsteroidType.Gold)
            {
                _goldAsteroidsDestroyed++;
            }

            SaveAchievements();
        }

        // Track enemy ship destruction
        public void TrackEnemyShipDestroyed()
        {
            _enemyShipsDestroyed++;
            SaveAchievements();
        }

        // Track player hit
        public void TrackPlayerHit()
        {
            _timeSinceLastHit = 0f;
        }

        public void TrackShooting()
        {
            // Reset the time without shooting
            _timeWithoutShooting = 0f;

            // Clear consecutive hits if this shot missed
            // (This will be maintained in UpdateConsecutiveHits)
        }

        public void TrackHit(bool isAsteroid, bool isUFO, bool offScreen = false)
        {
            // Increment consecutive hits
            _consecutiveHits++;

            // Check for Lucky Shot achievement (destroyed UFO while off-screen)
            if (isUFO && offScreen)
            {
                GetAchievement("lucky_shot")?.UpdateProgress(1);
            }

            // Check for Aimbot achievement
            if (_consecutiveHits >= 10)
            {
                GetAchievement("aimbot")?.UpdateProgress(_consecutiveHits);
            }
        }

        public void TrackMiss()
        {
            // Reset consecutive hits
            _consecutiveHits = 0;
        }

        public void TrackDeath(Vector2 position, bool hitByAsteroid, bool hitByUFO, bool rammingUFO)
        {
            // Track first death
            if (hitByAsteroid)
            {
                GetAchievement("first_death")?.UpdateProgress(1);
            }

            // Check for Glorious Sacrifice
            if (rammingUFO)
            {
                GetAchievement("sacrifice")?.UpdateProgress(1);
            }

            // Check for Déjà Vu
            if (_lastDeathPosition.HasValue)
            {
                // If died in approximately the same position
                if (Vector2.Distance(_lastDeathPosition.Value, position) < 20f)
                {
                    GetAchievement("deja_vu")?.UpdateProgress(1);
                }
            }

            // Store this death for future comparison
            _lastDeathPosition = position;

            // Reset last life timer
            _lastLifeTimer = 0f;
        }

        // Track asteroid-UFO collision (for Asteroid Whisperer)
        public void TrackAsteroidHitUFO()
        {
            GetAchievement("whisperer")?.UpdateProgress(1);
        }

        // Track player speed
        public void TrackPlayerSpeed(float speed)
        {
            // Check for maximum possible speed achievement
            if (speed > 900f) // Adjust threshold as needed based on your game mechanics
            {
                GetAchievement("speedster")?.UpdateProgress(1);
            }
        }

        // Track asteroid proximity to player (for Close Call)
        public void TrackAsteroidProximity(float distance)
        {
            if (distance < 5f) // 5 pixels close call
            {
                GetAchievement("close_call")?.UpdateProgress(1);
            }
        }

        // Track asteroid about to hit player but destroyed (for Clutch Save)
        public void TrackClutchSave(float timeToImpact)
        {
            if (timeToImpact < 0.5f) // Less than half a second from impact
            {
                GetAchievement("clutch_save")?.UpdateProgress(1);
            }
        }


        // Track number of asteroids on screen
        public void TrackAsteroidCount(int count)
        {
            _onScreenAsteroidCount = count;

            // Check for Black Hole Theory achievement
            if (count > 30)
            {
                GetAchievement("black_hole")?.UpdateProgress(count);
            }
        }


        // Helper method for thruster achievements
        private void UpdateThrusterAchievements()
        {
            // First threshold - 30 seconds
            if (_continuousThrusterTime >= 30f)
            {
                GetAchievement("thruster_30")?.UpdateProgress(30);
            }

            // Second threshold - 60 seconds
            if (_continuousThrusterTime >= 60f)
            {
                GetAchievement("thruster_60")?.UpdateProgress(60);
            }

            // Third threshold - 300 seconds (5 minutes)
            if (_continuousThrusterTime >= 300f)
            {
                GetAchievement("thruster_300")?.UpdateProgress(300);
            }
        }

        // 5. Add helper method to calculate approximate time to collision:
        public void CalculateTimeToCollision(GameObject player, List<Asteroid> asteroids)
        {
            foreach (var asteroid in asteroids)
            {
                int id = asteroid.GetHashCode();

                // Calculate vector from asteroid to player
                Vector2 toPlayer = player.Position - asteroid.Position;

                // Project asteroid velocity onto this vector
                Vector2 relativeVelocity = asteroid.Velocity - Vector2.Zero; // Assuming player is stationary for simplicity
                float projection = Vector2.Dot(Vector2.Normalize(toPlayer), relativeVelocity);

                // Only consider asteroids moving toward the player
                if (projection > 0)
                {
                    // Estimate time to collision
                    float distance = toPlayer.Length();
                    float timeToCollision = distance / projection;

                    // Store for tracking
                    _asteroidTimeToCollision[id] = timeToCollision;
                }
            }
        }

        // 6. When player destroys an asteroid, check if it was a clutch save:
        public void CheckClutchSave(int asteroidId)
        {
            if (_asteroidTimeToCollision.TryGetValue(asteroidId, out float timeToCollision))
            {
                if (timeToCollision < 0.5f)
                {
                    // This was a clutch save!
                    GetAchievement("clutch_save")?.UpdateProgress(1);
                }

                // Remove from tracking
                _asteroidTimeToCollision.Remove(asteroidId);
            }
        }
    }


        // Serializable class for saving achievement data
        public class AchievementSaveData
    {
        public string Id { get; set; }
        public bool IsUnlocked { get; set; }
        public DateTime? UnlockTime { get; set; }
        public int ProgressCurrent { get; set; }
    }
}
