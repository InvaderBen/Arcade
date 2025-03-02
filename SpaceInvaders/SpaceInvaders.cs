using AsteroidsGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SpaceInvaders
{
    public class SpaceInvadersGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Game constants
        private const int ScreenWidth = 800;
        private const int ScreenHeight = 600;
        private const int PlayerSpeed = 200;
        private const int InvaderRows = 5;
        private const int InvadersPerRow = 11;
        private const float InvaderHorizontalSpacing = 48;
        private const float InvaderVerticalSpacing = 48;
        private const float InvaderStartY = 100;
        private const float InvaderMoveAmount = 10;
        private const float InvaderDropAmount = 20;

        // Game objects
        private Player _player;
        private List<Invader> _invaders;
        private List<Projectile> _playerProjectiles;
        private List<Projectile> _invaderProjectiles;
        private List<Barrier> _barriers;
        private Random _random;

        // Object pools for better performance
        private List<Projectile> _projectilePool;

        // Game state
        private int _score;
        private int _lives;
        private bool _gameOver;
        private float _invaderMoveTimer;
        private float _invaderShootTimer;
        private float _invaderCurrentSpeed;
        private bool _invaderMovingRight;

        // Rendering and input
        private InputManager _inputManager;

        public SpaceInvadersGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = ScreenWidth;
            _graphics.PreferredBackBufferHeight = ScreenHeight;

            // Set target frame rate
            _graphics.SynchronizeWithVerticalRetrace = true;
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d); // 60fps
        }

        protected override void Initialize()
        {
            _random = new Random();
            _invaders = new List<Invader>();
            _playerProjectiles = new List<Projectile>();
            _invaderProjectiles = new List<Projectile>();
            _barriers = new List<Barrier>();

            // Initialize object pools
            _projectilePool = new List<Projectile>();

            _score = 0;
            _lives = 3;
            _gameOver = false;
            _invaderMoveTimer = 0;
            _invaderShootTimer = 0;
            _invaderCurrentSpeed = 1.0f;
            _invaderMovingRight = true;

            _inputManager = new InputManager();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create textures (in a real game, you'd load these from files)
            Texture2D playerTexture = CreateRectangleTexture(44, 24, Color.LightGreen);
            Texture2D invaderTexture1 = CreateInvaderTexture(32, 32, Color.Red);
            Texture2D invaderTexture2 = CreateInvaderTexture(32, 32, Color.Green);
            Texture2D invaderTexture3 = CreateInvaderTexture(32, 32, Color.Yellow);
            Texture2D projectileTexture = CreateRectangleTexture(4, 12, Color.White);
            Texture2D barrierTexture = CreateBarrierTexture(80, 64, Color.LightBlue);

            // Initialize player
            _player = new Player(
                new Vector2(ScreenWidth / 2, ScreenHeight - 40),
                playerTexture,
                PlayerSpeed
            );

            // Initialize invaders
            InitializeInvaders(invaderTexture1, invaderTexture2, invaderTexture3);

            // Initialize barriers
            InitializeBarriers(barrierTexture);

            // Pre-allocate projectiles
            for (int i = 0; i < 50; i++)
            {
                _projectilePool.Add(new Projectile(Vector2.Zero, Vector2.Zero, projectileTexture));
            }
        }

        private void InitializeInvaders(Texture2D topTexture, Texture2D middleTexture, Texture2D bottomTexture)
        {
            _invaders.Clear();

            for (int row = 0; row < InvaderRows; row++)
            {
                Texture2D texture;
                int pointValue;

                // Select texture and point value based on row
                if (row < 1)
                {
                    texture = topTexture;
                    pointValue = 30;
                }
                else if (row < 3)
                {
                    texture = middleTexture;
                    pointValue = 20;
                }
                else
                {
                    texture = bottomTexture;
                    pointValue = 10;
                }

                for (int col = 0; col < InvadersPerRow; col++)
                {
                    float x = (ScreenWidth - (InvadersPerRow - 1) * InvaderHorizontalSpacing) / 2 + col * InvaderHorizontalSpacing;
                    float y = InvaderStartY + row * InvaderVerticalSpacing;

                    _invaders.Add(new Invader(new Vector2(x, y), texture, pointValue));
                }
            }
        }

        private void InitializeBarriers(Texture2D texture)
        {
            _barriers.Clear();

            int numBarriers = 4;
            float spacing = ScreenWidth / (numBarriers + 1);

            for (int i = 0; i < numBarriers; i++)
            {
                float x = spacing * (i + 1) - texture.Width / 2;
                float y = ScreenHeight - 120;

                _barriers.Add(new Barrier(new Vector2(x, y), texture));
            }
        }

        protected override void Update(GameTime gameTime)
        {
            _inputManager.Update();

            if (_inputManager.IsExitRequested())
                Exit();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (!_gameOver)
            {
                UpdatePlayer(deltaTime);
                UpdateInvaders(deltaTime);
                UpdateProjectiles(deltaTime);
                CheckCollisions();

                // Check for game over conditions
                if (_invaders.Count == 0)
                {
                    // Player has won, go to next level
                    _invaderCurrentSpeed *= 1.2f;
                    InitializeInvaders(
                        CreateInvaderTexture(32, 32, Color.Red),
                        CreateInvaderTexture(32, 32, Color.Green),
                        CreateInvaderTexture(32, 32, Color.Yellow)
                    );
                }

                if (_lives <= 0 || InvadersReachedBottom())
                {
                    _gameOver = true;
                }
            }
            else if (_inputManager.IsRestartButtonPressed())
            {
                RestartGame();
            }

            base.Update(gameTime);
        }

        private void UpdatePlayer(float deltaTime)
        {
            // Handle player movement
            if (_inputManager.IsLeftPressed && _player.Position.X > 10)
            {
                _player.Position = new Vector2(_player.Position.X - _player.Speed * deltaTime, _player.Position.Y);
            }

            if (_inputManager.IsRightPressed && _player.Position.X < ScreenWidth - 10 - _player.Texture.Width)
            {
                _player.Position = new Vector2(_player.Position.X + _player.Speed * deltaTime, _player.Position.Y);
            }

            // Handle player shooting
            if (_inputManager.IsFireButtonPressed && _player.CanShoot())
            {
                FirePlayerProjectile();
                _player.ResetShootTimer();
            }

            _player.Update(deltaTime);
        }

        private void UpdateInvaders(float deltaTime)
        {
            // Update invader movement
            _invaderMoveTimer += deltaTime;

            if (_invaderMoveTimer >= 1.0f / _invaderCurrentSpeed)
            {
                _invaderMoveTimer = 0;

                bool moveDown = false;

                // Check if invaders need to change direction
                if (_invaderMovingRight)
                {
                    foreach (var invader in _invaders)
                    {
                        if (invader.Position.X + invader.Texture.Width / 2 + InvaderMoveAmount > ScreenWidth - 10)
                        {
                            _invaderMovingRight = false;
                            moveDown = true;
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var invader in _invaders)
                    {
                        if (invader.Position.X - invader.Texture.Width / 2 - InvaderMoveAmount < 10)
                        {
                            _invaderMovingRight = true;
                            moveDown = true;
                            break;
                        }
                    }
                }

                // Move invaders
                foreach (var invader in _invaders)
                {
                    if (moveDown)
                    {
                        invader.Position = new Vector2(invader.Position.X, invader.Position.Y + InvaderDropAmount);
                    }
                    else
                    {
                        invader.Position = new Vector2(
                            invader.Position.X + (_invaderMovingRight ? InvaderMoveAmount : -InvaderMoveAmount),
                            invader.Position.Y
                        );
                    }
                }

                // Update animation frames
                foreach (var invader in _invaders)
                {
                    invader.NextFrame();
                }
            }

            // Handle invader shooting
            _invaderShootTimer += deltaTime;

            if (_invaderShootTimer >= 1.0f && _invaders.Count > 0)
            {
                _invaderShootTimer = 0;

                // Random chance to fire based on number of invaders
                if (_random.Next(20) < Math.Max(1, 10 * _invaders.Count / (InvaderRows * InvadersPerRow)))
                {
                    int shooterIndex = _random.Next(_invaders.Count);
                    FireInvaderProjectile(_invaders[shooterIndex]);
                }
            }
        }

        private void UpdateProjectiles(float deltaTime)
        {
            // Update player projectiles
            for (int i = _playerProjectiles.Count - 1; i >= 0; i--)
            {
                _playerProjectiles[i].Update(deltaTime);

                if (_playerProjectiles[i].Position.Y < -20)
                {
                    RecycleProjectile(_playerProjectiles[i]);
                    _playerProjectiles.RemoveAt(i);
                }
            }

            // Update invader projectiles
            for (int i = _invaderProjectiles.Count - 1; i >= 0; i--)
            {
                _invaderProjectiles[i].Update(deltaTime);

                if (_invaderProjectiles[i].Position.Y > ScreenHeight + 20)
                {
                    RecycleProjectile(_invaderProjectiles[i]);
                    _invaderProjectiles.RemoveAt(i);
                }
            }
        }

        private void CheckCollisions()
        {
            // Check player projectiles vs invaders
            for (int i = _playerProjectiles.Count - 1; i >= 0; i--)
            {
                bool hit = false;

                for (int j = _invaders.Count - 1; j >= 0; j--)
                {
                    if (CheckCollision(_playerProjectiles[i], _invaders[j]))
                    {
                        // Add to score
                        _score += _invaders[j].PointValue;

                        // Remove invader
                        _invaders.RemoveAt(j);

                        // Remove projectile
                        RecycleProjectile(_playerProjectiles[i]);
                        _playerProjectiles.RemoveAt(i);

                        hit = true;
                        break;
                    }
                }

                if (hit) continue;

                // Check player projectiles vs barriers
                for (int j = 0; j < _barriers.Count; j++)
                {
                    if (_barriers[j].CheckProjectileCollision(_playerProjectiles[i].Position))
                    {
                        // Remove projectile
                        RecycleProjectile(_playerProjectiles[i]);
                        _playerProjectiles.RemoveAt(i);
                        hit = true;
                        break;
                    }
                }
            }

            // Check invader projectiles vs player
            for (int i = _invaderProjectiles.Count - 1; i >= 0; i--)
            {
                if (CheckCollision(_invaderProjectiles[i], _player))
                {
                    // Player hit
                    _lives--;

                    // Remove projectile
                    RecycleProjectile(_invaderProjectiles[i]);
                    _invaderProjectiles.RemoveAt(i);
                    continue;
                }

                // Check invader projectiles vs barriers
                for (int j = 0; j < _barriers.Count; j++)
                {
                    if (_barriers[j].CheckProjectileCollision(_invaderProjectiles[i].Position))
                    {
                        // Remove projectile
                        RecycleProjectile(_invaderProjectiles[i]);
                        _invaderProjectiles.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private bool CheckCollision(Projectile projectile, GameObject gameObject)
        {
            // Simple rectangular collision check
            Rectangle projectileRect = new Rectangle(
                (int)(projectile.Position.X - projectile.Texture.Width / 2),
                (int)(projectile.Position.Y - projectile.Texture.Height / 2),
                projectile.Texture.Width,
                projectile.Texture.Height
            );

            Rectangle objectRect = new Rectangle(
                (int)(gameObject.Position.X - gameObject.Texture.Width / 2),
                (int)(gameObject.Position.Y - gameObject.Texture.Height / 2),
                gameObject.Texture.Width,
                gameObject.Texture.Height
            );

            return projectileRect.Intersects(objectRect);
        }

        private bool InvadersReachedBottom()
        {
            float bottomThreshold = ScreenHeight - 80;

            foreach (var invader in _invaders)
            {
                if (invader.Position.Y + invader.Texture.Height / 2 > bottomThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        private void FirePlayerProjectile()
        {
            if (_projectilePool.Count > 0)
            {
                Projectile projectile = _projectilePool[0];
                _projectilePool.RemoveAt(0);

                projectile.Position = new Vector2(_player.Position.X, _player.Position.Y - 15);
                projectile.Velocity = new Vector2(0, -400); // Upward

                _playerProjectiles.Add(projectile);
            }
        }

        private void FireInvaderProjectile(Invader invader)
        {
            if (_projectilePool.Count > 0)
            {
                Projectile projectile = _projectilePool[0];
                _projectilePool.RemoveAt(0);

                projectile.Position = new Vector2(invader.Position.X, invader.Position.Y + 15);
                projectile.Velocity = new Vector2(0, 200); // Downward

                _invaderProjectiles.Add(projectile);
            }
        }

        private void RecycleProjectile(Projectile projectile)
        {
            _projectilePool.Add(projectile);
        }

        private void RestartGame()
        {
            _score = 0;
            _lives = 3;
            _gameOver = false;
            _invaderCurrentSpeed = 1.0f;

            // Clear projectiles
            foreach (var projectile in _playerProjectiles)
            {
                _projectilePool.Add(projectile);
            }

            foreach (var projectile in _invaderProjectiles)
            {
                _projectilePool.Add(projectile);
            }

            _playerProjectiles.Clear();
            _invaderProjectiles.Clear();

            // Reset player
            _player.Position = new Vector2(ScreenWidth / 2, ScreenHeight - 40);

            // Reinitialize invaders and barriers
            InitializeInvaders(
                CreateInvaderTexture(32, 32, Color.Red),
                CreateInvaderTexture(32, 32, Color.Green),
                CreateInvaderTexture(32, 32, Color.Yellow)
            );

            InitializeBarriers(CreateBarrierTexture(80, 64, Color.LightBlue));
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();

            // Draw player
            _player.Draw(_spriteBatch);

            // Draw invaders
            foreach (var invader in _invaders)
            {
                invader.Draw(_spriteBatch);
            }

            // Draw barriers
            foreach (var barrier in _barriers)
            {
                barrier.Draw(_spriteBatch);
            }

            // Draw projectiles
            foreach (var projectile in _playerProjectiles)
            {
                projectile.Draw(_spriteBatch);
            }

            foreach (var projectile in _invaderProjectiles)
            {
                projectile.Draw(_spriteBatch);
            }

            // Draw UI
            DrawUI();

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawUI()
        {
            // Create a font texture (in a real game, you'd use SpriteFont)
            Texture2D pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            // Draw score
            DrawText(_spriteBatch, pixelTexture, $"SCORE: {_score}", new Vector2(20, 20), Color.White);

            // Draw lives
            DrawText(_spriteBatch, pixelTexture, $"LIVES: {_lives}", new Vector2(ScreenWidth - 100, 20), Color.White);

            // Draw game over message
            if (_gameOver)
            {
                DrawText(_spriteBatch, pixelTexture, "GAME OVER",
                    new Vector2(ScreenWidth / 2 - 50, ScreenHeight / 2), Color.Red);

                DrawText(_spriteBatch, pixelTexture, "PRESS ENTER TO RESTART",
                    new Vector2(ScreenWidth / 2 - 110, ScreenHeight / 2 + 40), Color.White);
            }
        }

        private void DrawText(SpriteBatch spriteBatch, Texture2D pixel, string text, Vector2 position, Color color)
        {
            // A very simple text renderer using rectangles
            Vector2 pos = position;
            int charSize = 10;
            int spacing = 2;

            foreach (char c in text)
            {
                // Draw a simple rectangle for each character (in a real game, use SpriteFont)
                spriteBatch.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y, charSize, charSize * 2), color);
                pos.X += charSize + spacing;
            }
        }

        private Texture2D CreateRectangleTexture(int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(GraphicsDevice, width, height);
            Color[] data = new Color[width * height];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = color;
            }

            texture.SetData(data);
            return texture;
        }

        private Texture2D CreateInvaderTexture(int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(GraphicsDevice, width, height);
            Color[] data = new Color[width * height];

            // Initialize with transparent pixels
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Color.Transparent;
            }

            // Create a simple invader shape
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Create a shape that looks like a classic space invader
                    if ((x >= width / 4 && x < width * 3 / 4 && y >= height / 4 && y < height * 3 / 4) ||
                        (x >= width / 8 && x < width * 7 / 8 && y >= height * 3 / 8 && y < height * 5 / 8))
                    {
                        data[y * width + x] = color;
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }

        private Texture2D CreateBarrierTexture(int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(GraphicsDevice, width, height);
            Color[] data = new Color[width * height];

            // Initialize with transparent pixels
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Color.Transparent;
            }

            // Create a barrier shape (like an inverted U)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y < height * 3 / 4 &&
                        ((x < width / 4) || (x >= width * 3 / 4) || (y < height / 4)))
                    {
                        data[y * width + x] = color;
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }
    }
}