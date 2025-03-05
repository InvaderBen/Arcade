using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace AsteroidsGame
{
    public class Game1 : Game
    {
        // Add a new state for name entry
        public enum GameState
        {
            MainMenu,
            Playing,
            GameOver,
            HighScore,
            NameEntry,
            Achievements // New state for viewing achievements
        }

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Game objects
        private Player _player;
        private List<Asteroid> _asteroids;
        private List<Bullet> _bullets;
        private Random _random;

        // Object pools for better performance
        private List<Bullet> _bulletPool;
        private List<Asteroid> _asteroidPool;

        // Maximum objects to pre-allocate
        private const int MaxBullets = 20;
        private const int MaxAsteroids = 20;

        // Game state
        private int _score;
        private int _lives;
        private GameState _gameState;
        private bool _gameOver;

        // Rendering and input
        private Renderer _renderer;
        private MenuRenderer _menuRenderer;
        private InputManager _inputManager;
        private CollisionManager _collisionManager;
        private HighScoreManager _highScoreManager;
        private AchievementManager _achievementManager;
        private SpriteFont _font;

        private VirtualKeyboard _virtualKeyboard;

        // Controller vibration
        private bool _vibrateController;
        private float _vibrationTime;
        private const float MaxVibrationTime = 0.2f;

        // Window resizing
        private bool _isResizing;
        private Point _oldWindowSize;
        private Matrix _scaleMatrix;
        private Rectangle _virtualViewport;

        // Constants for window resizing
        private readonly int _minWindowWidth = 640;
        private readonly int _minWindowHeight = 480;

        private DifficultyManager _difficultyManager;
        private List<EnemyShip> _enemyShips;
        private List<EnemyShip> _enemyShipPool;
        private const int MaxEnemyShips = 3;

        private float _respawnTimer = 0f;


        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Make the window resizable
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += Window_ClientSizeChanged;

            // Set initial window size
            _graphics.PreferredBackBufferWidth = GameConstants.ScreenWidth;
            _graphics.PreferredBackBufferHeight = GameConstants.ScreenHeight;

            // Show mouse cursor
            IsMouseVisible = true;

            // Set target frame rate
            _graphics.SynchronizeWithVerticalRetrace = true;
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d); // 60fps

            // Initialize resizing variables
            _isResizing = false;
            _oldWindowSize = new Point(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            _scaleMatrix = Matrix.Identity;
            _virtualViewport = new Rectangle(0, 0, GameConstants.ScreenWidth, GameConstants.ScreenHeight);
        }


        protected override void Initialize()
        {
            _random = new Random();
            _asteroids = new List<Asteroid>();
            _bullets = new List<Bullet>();

            // Initialize object pools
            _bulletPool = new List<Bullet>();
            _asteroidPool = new List<Asteroid>();

            _score = 0;
            _lives = GameConstants.InitialLives;
            _gameState = GameState.MainMenu;
            _gameOver = false;
            _vibrateController = false;
            _vibrationTime = 0f;

            _inputManager = new InputManager();
            _collisionManager = new CollisionManager();
            _highScoreManager = new HighScoreManager();

            // Initialize achievement manager
            _achievementManager = new AchievementManager();
            _achievementManager.Initialize();

            _difficultyManager = new DifficultyManager();
            _enemyShips = new List<EnemyShip>();
            _enemyShipPool = new List<EnemyShip>();

            // Initialize scaling
            UpdateScaling();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load font
            _font = Content.Load<SpriteFont>("Font");

            // Initialize renderers
            _renderer = new Renderer(GraphicsDevice, Content, _spriteBatch);
            _menuRenderer = new MenuRenderer(GraphicsDevice, _spriteBatch, _font);

            // Initialize virtual keyboard for gamepad input
            _virtualKeyboard = new VirtualKeyboard(GraphicsDevice, _font,
                new Vector2((GameConstants.ScreenWidth - 600) / 2, 220));
            _highScoreManager.SetVirtualKeyboard(_virtualKeyboard);

            // Load achievements content
            _achievementManager.LoadContent(Content, GraphicsDevice);

            // Initialize player
            _player = new Player(
                new Vector2(GameConstants.ScreenWidth / 2, GameConstants.ScreenHeight / 2),
                _renderer.ShipTexture
            );

            // Set bullet texture for player
            _player.SetBulletTexture(_renderer.BulletTexture);

            // Pre-allocate bullet objects for pool
            PreallocateBullets();

            // Pre-allocate asteroid objects for pool
            PreallocateAsteroids();
        }



        private void ForceSpawnEnemyShip()
        {
            // Create a spawn position at the top of the screen
            // Choose a random X position
            Vector2 position = new Vector2(
                _random.Next(50, GameConstants.ScreenWidth - 50),  // Random X position
                40  // Fixed Y position at the top
            );

            // Create a new enemy ship
            EnemyShip enemyShip = new EnemyShip(
                position,
                _renderer.ShipTexture,
                _player,
                _renderer.BulletTexture,
                AddEnemyBullet
            );

            // Add it to the active enemy ships list
            _enemyShips.Add(enemyShip);
        }

        private void ForceSpawnGoldAsteroid(AsteroidSize size = AsteroidSize.Large)
        {
            // Get a random edge position
            Vector2 position = GetRandomEdgePosition();

            // Create velocity for the asteroid
            Vector2 velocity = GetRandomAsteroidVelocity(position);

            // Apply difficulty scaling if using difficulty manager
            if (_difficultyManager != null)
            {
                velocity *= _difficultyManager.GetSpeedMultiplier();
            }

            // Apply speed controller if using it
            if (typeof(GameSpeedController) != null)
            {
                velocity *= GameSpeedController.AsteroidSpeed;
            }

            // Get the appropriate texture
            Texture2D texture = _renderer.GetAsteroidTexture(size);

            // Create a gold asteroid
            Asteroid goldAsteroid = new Asteroid(
                position,
                velocity,
                texture,
                size,
                AsteroidType.Gold  // Gold type
            );

            // Add it to the active asteroids list
            _asteroids.Add(goldAsteroid);
        }

        private void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            // Flag that we need to update scaling
            _isResizing = true;
        }

        private void UpdateScaling()
        {
            if (!_isResizing) return;

            // Get current window size
            int windowWidth = Window.ClientBounds.Width;
            int windowHeight = Window.ClientBounds.Height;

            // Enforce minimum window size
            if (windowWidth < _minWindowWidth || windowHeight < _minWindowHeight)
            {
                windowWidth = Math.Max(windowWidth, _minWindowWidth);
                windowHeight = Math.Max(windowHeight, _minWindowHeight);

                _graphics.PreferredBackBufferWidth = windowWidth;
                _graphics.PreferredBackBufferHeight = windowHeight;
                _graphics.ApplyChanges();
            }

            // Update back buffer size
            _graphics.PreferredBackBufferWidth = windowWidth;
            _graphics.PreferredBackBufferHeight = windowHeight;
            _graphics.ApplyChanges();

            // Calculate scaling to maintain aspect ratio
            float scaleX = (float)windowWidth / GameConstants.ScreenWidth;
            float scaleY = (float)windowHeight / GameConstants.ScreenHeight;
            float scale = Math.Min(scaleX, scaleY);

            // Calculate letterbox/pillarbox positioning
            int viewWidth = (int)(GameConstants.ScreenWidth * scale);
            int viewHeight = (int)(GameConstants.ScreenHeight * scale);
            int viewX = (windowWidth - viewWidth) / 2;
            int viewY = (windowHeight - viewHeight) / 2;

            // Update viewport
            _virtualViewport = new Rectangle(viewX, viewY, viewWidth, viewHeight);

            // Create scale matrix for rendering
            _scaleMatrix = Matrix.CreateScale(scale, scale, 1) *
                          Matrix.CreateTranslation(viewX, viewY, 0);

            // Store window size for next comparison
            _oldWindowSize = new Point(windowWidth, windowHeight);

            // Clear resize flag
            _isResizing = false;
        }
        private void PreallocateBullets()
        {
            // Create bullets for the object pool
            for (int i = 0; i < MaxBullets; i++)
            {
                _bulletPool.Add(new Bullet(Vector2.Zero, Vector2.Zero, _renderer.BulletTexture));
            }
        }

        private void PreallocateAsteroids()
        {
            // Create asteroids for each size in the object pool
            for (int i = 0; i < MaxAsteroids / 3; i++)
            {
                _asteroidPool.Add(new Asteroid(Vector2.Zero, Vector2.Zero, _renderer.GetAsteroidTexture(AsteroidSize.Large), AsteroidSize.Large));
                _asteroidPool.Add(new Asteroid(Vector2.Zero, Vector2.Zero, _renderer.GetAsteroidTexture(AsteroidSize.Medium), AsteroidSize.Medium));
                _asteroidPool.Add(new Asteroid(Vector2.Zero, Vector2.Zero, _renderer.GetAsteroidTexture(AsteroidSize.Small), AsteroidSize.Small));
            }
        }

        // Create a method to spawn enemy ships
        private void SpawnEnemyShip()
        {
            // Pick a random edge for spawning
            Vector2 position = GetRandomEdgePosition();

            // Get enemy ship from pool if available
            EnemyShip enemyShip = null;
            if (_enemyShipPool.Count > 0)
            {
                enemyShip = _enemyShipPool[_enemyShipPool.Count - 1];
                _enemyShipPool.RemoveAt(_enemyShipPool.Count - 1);

                // Reset position
                enemyShip.Position = position;
            }
            else
            {
                // Create a new enemy ship if pool is empty
                enemyShip = new EnemyShip(
                    position,
                    _renderer.ShipTexture, // Using same texture as player for now
                    _player,
                    _renderer.BulletTexture,
                    AddEnemyBullet
                );
            }

            _enemyShips.Add(enemyShip);
        }



        protected override void Update(GameTime gameTime)
        {
            // Check if window has been resized
            if (_isResizing || Window.ClientBounds.Width != _oldWindowSize.X || Window.ClientBounds.Height != _oldWindowSize.Y)
            {
                UpdateScaling();
            }

            // Process input - always call Update on input manager
            _inputManager.Update();

            // Update input manager's state awareness
            _inputManager.SetInMainMenu(_gameState == GameState.MainMenu);

            // Exit game if requested (in any state)
            if (_inputManager.IsExitRequested())
                Exit();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // If the game state changes to menu or game over, stop any vibration
            if (_gameState == GameState.MainMenu || _gameState == GameState.GameOver)
            {
                GamePad.SetVibration(PlayerIndex.One, 0, 0);
                _vibrateController = false;
            }

            // Update based on current game state
            switch (_gameState)
            {
                case GameState.MainMenu:
                    UpdateMainMenu(deltaTime);
                    break;

                case GameState.Playing:
                    UpdatePlaying(deltaTime);
                    break;

                case GameState.GameOver:
                    UpdateGameOver(deltaTime);
                    break;

                case GameState.HighScore:
                    UpdateHighScore(deltaTime);
                    break;

                case GameState.NameEntry:
                    UpdateNameEntry(deltaTime);
                    break;

                case GameState.Achievements:
                    UpdateAchievements(deltaTime);
                    break;
            }

            // Update achievements (always update to handle notifications)
            _achievementManager.Update(deltaTime, _gameState, _score, _lives);

            base.Update(gameTime);
        }



        private void UpdateNameEntry(float deltaTime)
        {
            // Update menu background
            _menuRenderer.Update(deltaTime, _inputManager);

            // Check for F1 or Tab key to toggle virtual keyboard
            if (_inputManager.IsKeyPressed(Keys.F1) || _inputManager.IsKeyPressed(Keys.Tab))
            {
                _highScoreManager.ToggleVirtualKeyboard();
            }

            // For debugging - check current state
            if (!_highScoreManager.IsEnteringName)
            {
                // Name entry is complete, transition to high scores
                _gameState = GameState.HighScore;
                return; // Exit immediately
            }

            // Always process physical keyboard input regardless of virtual keyboard state
            bool inputProcessed = false;
            foreach (Keys key in _inputManager.GetPressedKeys())
            {
                // Process physical keyboard input
                if (_highScoreManager.HandleKeypress(key))
                {
                    inputProcessed = true;
                    // If name entry is complete, go to high scores
                    if (!_highScoreManager.IsEnteringName)
                    {
                        _gameState = GameState.HighScore;
                        return; // Exit immediately 
                    }
                    break;
                }
            }

            // If using virtual keyboard, update it only if no physical keyboard input was processed
            if (!inputProcessed && _highScoreManager.UsingVirtualKeyboard &&
                _highScoreManager.VirtualKeyboard != null &&
                _highScoreManager.VirtualKeyboard.IsEnabled)
            {
                _virtualKeyboard.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(deltaTime)), _inputManager);

                // Check for controller Back button (B) to cancel
                if (_inputManager.IsButtonPressed(Buttons.B))
                {
                    _highScoreManager.CancelNameEntry();
                    _gameState = GameState.HighScore;
                    return;
                }

                // Check again after virtual keyboard update - important!
                if (!_highScoreManager.IsEnteringName)
                {
                    _gameState = GameState.HighScore;
                    return;
                }
            }
        }

        private void UpdateMainMenu(float deltaTime)
        {
            // Update menu
            _menuRenderer.Update(deltaTime, _inputManager);

            // Check for selection
            if (_inputManager.IsMenuConfirmPressed())
            {
                int selectedOption = _menuRenderer.GetSelectedOption();

                switch (selectedOption)
                {
                    case 0: // Play
                        StartNewGame();
                        break;

                    case 1: // High Score
                        _gameState = GameState.HighScore;
                        break;

                    case 2: // Achievements
                        _gameState = GameState.Achievements;
                        break;

                    case 3: // Exit
                        Exit();
                        break;
                }
            }
        }

        private void UpdatePlaying(float deltaTime)
        {
            // Handle controller vibration if active
            if (_vibrateController)
            {
                _vibrationTime -= deltaTime;
                if (_vibrationTime <= 0)
                {
                    // Stop vibration after time limit
                    GamePad.SetVibration(PlayerIndex.One, 0f, 0f);
                    _vibrateController = false;
                }
            }

            // Update respawn timer
            if (_respawnTimer > 0)
            {
                _respawnTimer -= deltaTime;

                // Don't spawn asteroids while in respawn delay
                if (_respawnTimer <= 0)
                {
                    // Respawn timer completed, spawn initial asteroids
                    for (int i = 0; i < 2; i++) // Spawn fewer asteroids initially for safety
                    {
                        SpawnAsteroid(AsteroidSize.Large);
                    }
                }
            }
            else
            {
                // Normal asteroid spawning - only if respawn timer is complete
                if (_asteroids.Count < _difficultyManager.GetMaxAsteroids() && _random.Next(100) < 1)
                {
                    SpawnAsteroid(AsteroidSize.Large);
                }
            }

            // Handle player movement
            _player.HandleInput(_inputManager);
            _player.Update(deltaTime);

            // Fire bullets
            if (_inputManager.IsFirePressed() && _player.CanFire())
            {
                AddBullet(_player.GetBulletPosition(), _player.GetBulletVelocity());
            }

            // Test feature shortcuts
            if (_inputManager.IsKeyPressed(Keys.G))
            {
                // Spawn gold asteroid with G key
                ForceSpawnGoldAsteroid();
            }

            if (_inputManager.IsKeyPressed(Keys.E))
            {
                // Spawn enemy ship with E key
                ForceSpawnEnemyShip();
            }


            // Update difficulty based on score
            _difficultyManager.UpdateDifficulty(_score);

            // Check if we should spawn an enemy ship
            if (_difficultyManager.ShouldSpawnEnemyShip() && _enemyShips.Count < MaxEnemyShips)
            {
                SpawnEnemyShip();
            }

            // Update enemy ships
            for (int i = _enemyShips.Count - 1; i >= 0; i--)
            {
                _enemyShips[i].Update(deltaTime);
            }


            // Handle player movement
            _player.HandleInput(_inputManager);
            _player.Update(deltaTime);

            // Fire bullets
            if (_inputManager.IsFirePressed() && _player.CanFire())
            {
                AddBullet(_player.GetBulletPosition(), _player.GetBulletVelocity());
            }

            // Update bullets
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                _bullets[i].Update(deltaTime);

                if (_bullets[i].ShouldRemove())
                {
                    RecycleBullet(i);
                }
            }

            // Update asteroids
            foreach (var asteroid in _asteroids)
            {
                asteroid.Update(deltaTime);
            }

            // Check for collisions
            HandleCollisions();

            // Possibly spawn new asteroids - updated to use difficulty manager's max asteroids
            if (_asteroids.Count < _difficultyManager.GetMaxAsteroids() && _random.Next(100) < 1)
            {
                SpawnAsteroid(AsteroidSize.Large);
            }

            // Check for game over
            if (_lives <= 0)
            {
                _gameOver = true;
                _gameState = GameState.GameOver;

                // Check if score qualifies for high score list
                if (_highScoreManager.IsHighScore(_score))
                {
                    // Move to name entry state
                    _gameState = GameState.NameEntry;
                    _highScoreManager.StartNameEntry(_score);
                }
            }

            // Pause game
            if (_inputManager.IsKeyPressed(Keys.P) || _inputManager.IsButtonPressed(Buttons.Start))
            {
                _gameState = GameState.MainMenu;
            }

            // Handle ESC key - go to main menu rather than exiting
            if (_inputManager.IsKeyPressed(Keys.Escape))
            {
                _gameState = GameState.MainMenu;
            }
        }

        private void UpdateGameOver(float deltaTime)
        {
            // Wait for restart input
            if (_inputManager.IsRestartButtonPressed() ||
                _inputManager.IsKeyPressed(Keys.Enter) ||
                _inputManager.IsButtonPressed(Buttons.A))
            {
                RestartGame();
            }

            // Return to main menu
            if (_inputManager.IsKeyPressed(Keys.Escape) || _inputManager.IsButtonPressed(Buttons.B))
            {
                _gameState = GameState.MainMenu;
            }
        }

        private void UpdateHighScore(float deltaTime)
        {
            // Update menu background
            _menuRenderer.Update(deltaTime, _inputManager);

            // Return to main menu
            if (_inputManager.IsMenuConfirmPressed() ||
                _inputManager.IsKeyPressed(Keys.Escape) ||
                _inputManager.IsButtonPressed(Buttons.B))
            {
                _gameState = GameState.MainMenu;
            }
        }

        private void UpdateAchievements(float deltaTime)
        {
            // Return to main menu when Escape is pressed
            if (_inputManager.IsKeyPressed(Keys.Escape) || _inputManager.IsButtonPressed(Buttons.B))
            {
                _gameState = GameState.MainMenu;
            }
        }



        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // Begin with scaling matrix applied
            _spriteBatch.Begin(transformMatrix: _scaleMatrix);

            // Draw based on current game state
            switch (_gameState)
            {
                case GameState.MainMenu:
                    _menuRenderer.DrawMainMenu();
                    break;

                case GameState.Playing:
                    DrawPlaying();
                    break;

                case GameState.GameOver:
                    DrawPlaying();
                    DrawGameOver();
                    break;

                case GameState.HighScore:
                    _menuRenderer.DrawHighScores(_highScoreManager.GetHighScores());
                    break;

                case GameState.NameEntry:
                    // Make sure to pass both parameters now
                    _menuRenderer.DrawNameEntry(_highScoreManager.CurrentName, _highScoreManager);
                    break;

                case GameState.Achievements:
                    // Draw background game objects for visual effect
                    DrawGameplayObjects();
                    // Draw the achievements screen
                    _achievementManager.DrawAchievementsScreen(_spriteBatch);
                    break;
            }

            // Always draw achievement notifications (if there are any)
            if (_gameState == GameState.Playing)
            {
                _achievementManager.DrawNotifications(_spriteBatch);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawPlaying()
        {
            // Draw player
            _renderer.DrawPlayer(_player);

            // Draw asteroids
            foreach (var asteroid in _asteroids)
            {
                _renderer.DrawAsteroid(asteroid);
            }

            // Draw enemy ships
            foreach (var enemyShip in _enemyShips)
            {
                _renderer.DrawEnemyShip(enemyShip);
            }

            // Draw bullets
            foreach (var bullet in _bullets)
            {
                _renderer.DrawBullet(bullet);
            }

            // Draw UI elements - use the correct method signature
            if (_gameState != GameState.GameOver)
            {
                // Check if we're using difficulty manager
                if (_difficultyManager != null)
                {
                    // Use the 5-parameter version
                    _renderer.DrawUI(
                        _score,
                        _lives,
                        false,
                        _difficultyManager.GetDifficultyLevel(),
                        _difficultyManager.GetSpeedMultiplier()
                    );
                }
                else
                {
                    // Use the 3-parameter version
                    _renderer.DrawUI(_score, _lives, false);
                }
            }
        }

        private void DrawGameOver()
        {
            // Draw game over message
            string gameOverText = "GAME OVER";
            Vector2 textSize = _font.MeasureString(gameOverText);
            _spriteBatch.DrawString(
                _font,
                gameOverText,
                new Vector2(
                    (GameConstants.ScreenWidth - textSize.X) / 2,
                    GameConstants.ScreenHeight / 2 - 50
                ),
                Color.Red
            );

            // Draw final score
            string scoreText = $"FINAL SCORE: {_score}";
            Vector2 scoreSize = _font.MeasureString(scoreText);
            _spriteBatch.DrawString(
                _font,
                scoreText,
                new Vector2(
                    (GameConstants.ScreenWidth - scoreSize.X) / 2,
                    GameConstants.ScreenHeight / 2
                ),
                Color.White
            );

            // Draw restart instructions
            string restartText = "PRESS ENTER TO RESTART";
            Vector2 restartSize = _font.MeasureString(restartText);
            _spriteBatch.DrawString(
                _font,
                restartText,
                new Vector2(
                    (GameConstants.ScreenWidth - restartSize.X) / 2,
                    GameConstants.ScreenHeight / 2 + 50
                ),
                Color.Yellow
            );

            // Draw menu return instructions
            string menuText = "PRESS ESC FOR MENU";
            Vector2 menuSize = _font.MeasureString(menuText);
            _spriteBatch.DrawString(
                _font,
                menuText,
                new Vector2(
                    (GameConstants.ScreenWidth - menuSize.X) / 2,
                    GameConstants.ScreenHeight / 2 + 80
                ),
                Color.Yellow
            );

            // Draw recently unlocked achievements
            string achievementText = "Recent Achievements:";
            Vector2 achievementPos = new Vector2(
                (GameConstants.ScreenWidth - _font.MeasureString(achievementText).X) / 2,
                GameConstants.ScreenHeight / 2 + 130
            );

            _spriteBatch.DrawString(
                _font,
                achievementText,
                achievementPos,
                Color.LightBlue
            );

            // Get recently unlocked achievements (up to 3)
            var recentAchievements = _achievementManager.GetRecentlyUnlocked(3);
            for (int i = 0; i < recentAchievements.Count; i++)
            {
                _spriteBatch.DrawString(
                    _font,
                    "• " + recentAchievements[i].Title,
                    new Vector2(achievementPos.X, achievementPos.Y + 30 + (i * 25)),
                    Color.White
                );
            }


        }

        private void DrawGameplayObjects()
        {
            // This is a simplified version of DrawPlaying that just shows asteroids and background elements
            // but doesn't show UI, player, or bullets

            // Draw asteroids
            foreach (var asteroid in _asteroids)
            {
                _renderer.DrawAsteroid(asteroid);
            }

            // Optionally draw enemy ships
            foreach (var enemyShip in _enemyShips)
            {
                _renderer.DrawEnemyShip(enemyShip);
            }
        }


        private void AddBullet(Vector2 position, Vector2 velocity)
        {
            Bullet bullet;

            // Try to get a bullet from the pool
            if (_bulletPool.Count > 0)
            {
                bullet = _bulletPool[_bulletPool.Count - 1];
                _bulletPool.RemoveAt(_bulletPool.Count - 1);

                // Reset the bullet properties and explicitly set as player bullet
                bullet.Position = position;
                bullet.Velocity = velocity;
                bullet.ResetLifetime();
                bullet.IsPlayerBullet = true; // Explicitly set as player bullet
            }
            else
            {
                // Create a new bullet if the pool is empty - explicitly set as player bullet
                bullet = new Bullet(position, velocity, _renderer.BulletTexture, true);
            }

            _bullets.Add(bullet);
        }

        private void AddEnemyBullet(Vector2 position, Vector2 velocity)
        {
            Bullet bullet;

            // Try to get a bullet from the pool
            if (_bulletPool.Count > 0)
            {
                bullet = _bulletPool[_bulletPool.Count - 1];
                _bulletPool.RemoveAt(_bulletPool.Count - 1);

                // Reset the bullet properties - mark as enemy bullet (not player bullet)
                bullet.Position = position;
                bullet.Velocity = velocity;
                bullet.ResetLifetime();
                bullet.IsPlayerBullet = false; // Explicitly mark as enemy bullet
            }
            else
            {
                // Create a new bullet if the pool is empty - mark as enemy bullet
                bullet = new Bullet(position, velocity, _renderer.BulletTexture, false);
            }

            _bullets.Add(bullet);
        }


        private void HandleCollisions()
        {
            // PLAYER COLLISIONS - skip if player is invulnerable
            if (!_player.IsInvulnerable())
            {
                // Check player-asteroid collisions
                for (int i = _asteroids.Count - 1; i >= 0; i--)
                {
                    if (_collisionManager.CheckCollision(_player, _asteroids[i]))
                    {
                        // Track player hit for achievements
                        _achievementManager.TrackPlayerHit();

                        // Call player death method
                        PlayerDeath();
                        return; // Exit immediately - field is now clear
                    }
                }

                // Check player-enemy ship collisions
                for (int i = _enemyShips.Count - 1; i >= 0; i--)
                {
                    if (_collisionManager.CheckCollision(_player, _enemyShips[i]))
                    {
                        // Track player hit for achievements
                        _achievementManager.TrackPlayerHit();

                        // Call player death method
                        PlayerDeath();
                        return; // Exit immediately - field is now clear
                    }
                }

                // Check player-enemy bullet collisions
                for (int i = _bullets.Count - 1; i >= 0; i--)
                {
                    // Skip player bullets, only check enemy bullets
                    if (_bullets[i].IsPlayerBullet)
                        continue;

                    if (_collisionManager.CheckCollision(_player, _bullets[i]))
                    {
                        // Track player hit for achievements
                        _achievementManager.TrackPlayerHit();

                        // Recycle the enemy bullet
                        RecycleBullet(i);

                        // Call player death method
                        PlayerDeath();
                        return; // Exit immediately - field is now clear
                    }
                }
            }

            // PLAYER BULLET COLLISIONS
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                // Skip enemy bullets, only process player bullets
                if (!_bullets[i].IsPlayerBullet)
                    continue;

                bool bulletRemoved = false;

                // Check bullet-asteroid collisions
                for (int j = _asteroids.Count - 1; j >= 0; j--)
                {
                    if (_collisionManager.CheckCollision(_bullets[i], _asteroids[j]))
                    {
                        // Get points based on asteroid type and size
                        _score += _asteroids[j].GetPointValue();

                        // Track asteroid destroyed for achievements
                        _achievementManager.TrackAsteroidDestroyed(_asteroids[j].Type);

                        // Split the asteroid
                        SplitAsteroid(_asteroids[j]);

                        // Recycle asteroid and bullet
                        RecycleAsteroid(j);
                        RecycleBullet(i);

                        // Small vibration feedback when hitting an asteroid
                        GamePad.SetVibration(PlayerIndex.One, 0.2f, 0.2f);
                        _vibrateController = true;
                        _vibrationTime = MaxVibrationTime;

                        bulletRemoved = true;
                        break;
                    }
                }

                // Check bullet-enemy ship collisions if bullet wasn't already removed
                if (!bulletRemoved)
                {
                    for (int j = _enemyShips.Count - 1; j >= 0; j--)
                    {
                        if (_collisionManager.CheckCollision(_bullets[i], _enemyShips[j]))
                        {
                            // Damage the enemy ship
                            if (_enemyShips[j].TakeDamage())
                            {
                                // Enemy ship destroyed - add points
                                _score += GameConstants.EnemyShipPoints;

                                // Track enemy ship destroyed for achievements
                                _achievementManager.TrackEnemyShipDestroyed();

                                // Recycle enemy ship
                                _enemyShipPool.Add(_enemyShips[j]);
                                _enemyShips.RemoveAt(j);
                            }

                            // Recycle bullet
                            RecycleBullet(i);

                            // Vibration feedback
                            GamePad.SetVibration(PlayerIndex.One, 0.3f, 0.3f);
                            _vibrateController = true;
                            _vibrationTime = MaxVibrationTime;

                            bulletRemoved = true;
                            break;
                        }
                    }
                }

                if (bulletRemoved) break;
            }
        }


        private void RecycleBullet(int index)
        {
            // Return the bullet to the pool instead of destroying it
            if (index >= 0 && index < _bullets.Count)
            {
                Bullet bullet = _bullets[index];
                _bullets.RemoveAt(index);

                // Make sure we don't add null bullets to the pool
                if (bullet != null)
                {
                    _bulletPool.Add(bullet);
                }
            }
        }

        private void RecycleAsteroid(int index)
        {
            // Return the asteroid to the pool instead of destroying it
            Asteroid asteroid = _asteroids[index];
            _asteroids.RemoveAt(index);
            _asteroidPool.Add(asteroid);
        }

        private void SpawnAsteroid(AsteroidSize size)
        {
            // Determine spawn position (at screen edge)
            Vector2 position = GetRandomEdgePosition();

            // Create velocity aimed toward center with randomness
            Vector2 velocity = GetRandomAsteroidVelocity(position);

            // Get asteroid from pool if available
            Asteroid asteroid = null;
            for (int i = _asteroidPool.Count - 1; i >= 0; i--)
            {
                if (_asteroidPool[i].Size == size)
                {
                    asteroid = _asteroidPool[i];
                    _asteroidPool.RemoveAt(i);

                    // Reset asteroid properties
                    asteroid.Position = position;
                    asteroid.Velocity = velocity;
                    asteroid.ResetRotation(_random);
                    break;
                }
            }

            // Create new asteroid if none available in pool
            if (asteroid == null)
            {
                Texture2D texture = _renderer.GetAsteroidTexture(size);
                asteroid = new Asteroid(position, velocity, texture, size);
            }

            _asteroids.Add(asteroid);
        }

        private Vector2 GetRandomEdgePosition()
        {
            if (_random.Next(2) == 0)
            {
                // Spawn at horizontal edge
                return new Vector2(
                    _random.Next(2) == 0 ? -10 : GameConstants.ScreenWidth + 10,
                    _random.Next(GameConstants.ScreenHeight)
                );
            }
            else
            {
                // Spawn at vertical edge
                return new Vector2(
                    _random.Next(GameConstants.ScreenWidth),
                    _random.Next(2) == 0 ? -10 : GameConstants.ScreenHeight + 10
                );
            }
        }

        private Vector2 GetRandomAsteroidVelocity(Vector2 position)
        {
            // Aim toward center with randomness
            Vector2 center = new Vector2(GameConstants.ScreenWidth / 2, GameConstants.ScreenHeight / 2);
            Vector2 direction = Vector2.Normalize(center - position);

            // Add randomness to direction
            direction = Vector2.Transform(
                direction,
                Matrix.CreateRotationZ((float)(_random.NextDouble() - 0.5) * MathHelper.PiOver2)
            );

            // Random speed within defined range
            float speed = GameConstants.AsteroidMinSpeed +
                         (float)_random.NextDouble() *
                         (GameConstants.AsteroidMaxSpeed - GameConstants.AsteroidMinSpeed);

            return direction * speed;
        }

        private void SplitAsteroid(Asteroid asteroid)
        {
            if (asteroid.Size != AsteroidSize.Small)
            {
                AsteroidSize newSize = asteroid.Size == AsteroidSize.Large ?
                                       AsteroidSize.Medium : AsteroidSize.Small;

                // Create two smaller asteroids
                for (int i = 0; i < 2; i++)
                {
                    Vector2 newVelocity = Vector2.Transform(
                        asteroid.Velocity,
                        Matrix.CreateRotationZ((float)(_random.NextDouble() - 0.5) * MathHelper.Pi)
                    );

                    // Add some speed to smaller asteroids
                    newVelocity *= 1.2f;

                    // Apply difficulty scaling
                    newVelocity *= _difficultyManager.GetSpeedMultiplier();

                    // Get texture for the new size
                    Texture2D newTexture = _renderer.GetAsteroidTexture(newSize);

                    // Determine if this should be a gold asteroid - preserve the gold status
                    AsteroidType asteroidType = asteroid.Type;

                    // Try to get an asteroid from the pool
                    Asteroid newAsteroid = null;
                    for (int j = _asteroidPool.Count - 1; j >= 0; j--)
                    {
                        if (_asteroidPool[j].Size == newSize)
                        {
                            newAsteroid = _asteroidPool[j];
                            _asteroidPool.RemoveAt(j);

                            // Reset asteroid properties
                            newAsteroid.Position = asteroid.Position;
                            newAsteroid.Velocity = newVelocity;
                            newAsteroid.ResetRotation(_random);
                            break;
                        }
                    }

                    // Create a new asteroid if none available in pool
                    if (newAsteroid == null)
                    {
                        newAsteroid = new Asteroid(asteroid.Position, newVelocity, newTexture, newSize, asteroidType);
                    }

                    _asteroids.Add(newAsteroid);
                }
            }
        }



        private void ClearPlayingField()
        {
            // Return all active asteroids to pool
            foreach (var asteroid in _asteroids)
            {
                _asteroidPool.Add(asteroid);
            }
            _asteroids.Clear();

            // Return all enemy ships to pool
            foreach (var ship in _enemyShips)
            {
                _enemyShipPool.Add(ship);
            }
            _enemyShips.Clear();

            // Return all bullets to pool
            foreach (var bullet in _bullets)
            {
                _bulletPool.Add(bullet);
            }
            _bullets.Clear();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            // Save achievements
            _achievementManager.SaveAchievements();

            base.OnExiting(sender, args);
        }


        private void PlayerDeath()
        {
            // Decrease lives
            _lives--;

            // Clear all dangerous objects
            ClearPlayingField();

            // If game isn't over, reset player position
            if (_lives > 0)
            {
                // Reset player to center
                _player.Reset(new Vector2(GameConstants.ScreenWidth / 2, GameConstants.ScreenHeight / 2));

                // Controller vibration - with a reasonable time limit
                GamePad.SetVibration(PlayerIndex.One, 0.5f, 0.5f);
                _vibrateController = true;
                _vibrationTime = 0.5f; // Set to a half second only

                // Delay before spawning new asteroids (give player breathing room)
                _respawnTimer = 2.0f;
            }
            else
            {
                // Game over
                _gameOver = true;
                _gameState = GameState.GameOver;

                // Stop vibration when game is over
                GamePad.SetVibration(PlayerIndex.One, 0, 0);
                _vibrateController = false;

                // Check for high score
                if (_highScoreManager.IsHighScore(_score))
                {
                    _gameState = GameState.NameEntry;
                    _highScoreManager.StartNameEntry(_score);
                }
            }
        }

        private void StartNewGame()
        {
            _score = 0;
            _lives = GameConstants.InitialLives;
            _gameOver = false;
            _gameState = GameState.Playing;
            _difficultyManager.Reset();
            _achievementManager.TrackPlayerHit();

            // Return all active game objects to their pools
            foreach (var bullet in _bullets)
            {
                _bulletPool.Add(bullet);
            }

            foreach (var asteroid in _asteroids)
            {
                _asteroidPool.Add(asteroid);
            }

            _bullets.Clear();
            _asteroids.Clear();

            _player.Reset(new Vector2(GameConstants.ScreenWidth / 2, GameConstants.ScreenHeight / 2));

            // Create initial asteroids
            for (int i = 0; i < GameConstants.InitialAsteroidCount; i++)
            {
                SpawnAsteroid(AsteroidSize.Large);
            }
        }

        private void RestartGame()
        {
            _score = 0;
            _lives = GameConstants.InitialLives;
            _gameOver = false;

            // Reset difficulty
            _difficultyManager.Reset();

            _achievementManager.TrackPlayerHit();


            // Return all active game objects to their pools
            foreach (var bullet in _bullets)
            {
                _bulletPool.Add(bullet);
            }

            foreach (var asteroid in _asteroids)
            {
                _asteroidPool.Add(asteroid);
            }

            foreach (var enemyShip in _enemyShips)
            {
                _enemyShipPool.Add(enemyShip);
            }

            _bullets.Clear();
            _asteroids.Clear();
            _enemyShips.Clear();

            _player.Reset(new Vector2(GameConstants.ScreenWidth / 2, GameConstants.ScreenHeight / 2));

            // Create initial asteroids
            for (int i = 0; i < GameConstants.InitialAsteroidCount; i++)
            {
                SpawnAsteroid(AsteroidSize.Large);
            }
        }
    }
}