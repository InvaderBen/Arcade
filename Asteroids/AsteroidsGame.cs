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
            NameEntry
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
        private SpriteFont _font;

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
            }

            base.Update(gameTime);
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

                    case 2: // Exit
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
                _vibrationTime += deltaTime;
                if (_vibrationTime >= MaxVibrationTime)
                {
                    // Stop vibration after time limit
                    GamePad.SetVibration(PlayerIndex.One, 0f, 0f);
                    _vibrateController = false;
                    _vibrationTime = 0f;
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

            // Possibly spawn new asteroids
            if (_asteroids.Count < GameConstants.MaxAsteroids && _random.Next(100) < 1)
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
            if (_inputManager.IsRestartButtonPressed())
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

        private void UpdateNameEntry(float deltaTime)
        {
            // Update menu background
            _menuRenderer.Update(deltaTime, _inputManager);

            // Process name entry
            foreach (Keys key in _inputManager.GetPressedKeys())
            {
                // Let the high score manager handle the key press
                if (_highScoreManager.HandleKeypress(key))
                {
                    // If name entry is complete, go to high scores
                    if (!_highScoreManager.IsEnteringName)
                    {
                        _gameState = GameState.HighScore;
                    }
                    break;
                }
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
                    _menuRenderer.DrawNameEntry(_highScoreManager.CurrentName);
                    break;
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawPlaying()
        {
            // Draw game objects
            _renderer.DrawPlayer(_player);

            foreach (var asteroid in _asteroids)
            {
                _renderer.DrawAsteroid(asteroid);
            }

            foreach (var bullet in _bullets)
            {
                _renderer.DrawBullet(bullet);
            }

            // Draw UI elements - only if not in game over state
            if (_gameState != GameState.GameOver)
            {
                _renderer.DrawUI(_score, _lives, false);
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
        }

        private void AddBullet(Vector2 position, Vector2 velocity)
        {
            Bullet bullet;

            // Try to get a bullet from the pool
            if (_bulletPool.Count > 0)
            {
                bullet = _bulletPool[_bulletPool.Count - 1];
                _bulletPool.RemoveAt(_bulletPool.Count - 1);

                // Reset the bullet properties
                bullet.Position = position;
                bullet.Velocity = velocity;
                bullet.ResetLifetime();
            }
            else
            {
                // Create a new bullet if the pool is empty
                bullet = new Bullet(position, velocity, _renderer.BulletTexture);
            }

            _bullets.Add(bullet);
        }

        private void RecycleBullet(int index)
        {
            // Return the bullet to the pool instead of destroying it
            Bullet bullet = _bullets[index];
            _bullets.RemoveAt(index);
            _bulletPool.Add(bullet);
        }

        private void RecycleAsteroid(int index)
        {
            // Return the asteroid to the pool instead of destroying it
            Asteroid asteroid = _asteroids[index];
            _asteroids.RemoveAt(index);
            _asteroidPool.Add(asteroid);
        }

        private void HandleCollisions()
        {
            // Check player-asteroid collisions
            for (int i = _asteroids.Count - 1; i >= 0; i--)
            {
                if (_collisionManager.CheckCollision(_player, _asteroids[i]))
                {
                    _lives--;
                    _player.Reset(new Vector2(GameConstants.ScreenWidth / 2, GameConstants.ScreenHeight / 2));

                    // Vibrate controller when player is hit
                    GamePad.SetVibration(PlayerIndex.One, 0.5f, 0.5f);
                    _vibrateController = true;
                    _vibrationTime = 0f;

                    break;
                }
            }

            // Check bullet-asteroid collisions - optimized
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                bool bulletRemoved = false;

                for (int j = _asteroids.Count - 1; j >= 0; j--)
                {
                    if (_collisionManager.CheckCollision(_bullets[i], _asteroids[j]))
                    {
                        // Split the asteroid
                        SplitAsteroid(_asteroids[j]);

                        // Recycle asteroid and bullet
                        RecycleAsteroid(j);
                        RecycleBullet(i);

                        // Update score
                        _score += GameConstants.AsteroidPoints;

                        // Small vibration feedback when hitting an asteroid
                        GamePad.SetVibration(PlayerIndex.One, 0.2f, 0.2f);
                        _vibrateController = true;
                        _vibrationTime = 0f;

                        bulletRemoved = true;
                        break;
                    }
                }

                if (bulletRemoved) break;
            }
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

                    // Get texture for the new size
                    Texture2D newTexture = _renderer.GetAsteroidTexture(newSize);

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
                        newAsteroid = new Asteroid(asteroid.Position, newVelocity, newTexture, newSize);
                    }

                    _asteroids.Add(newAsteroid);
                }
            }
        }

        private void StartNewGame()
        {
            _score = 0;
            _lives = GameConstants.InitialLives;
            _gameOver = false;
            _gameState = GameState.Playing;

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
            StartNewGame();
        }
    }
}