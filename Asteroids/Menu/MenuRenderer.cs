using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace AsteroidsGame
{
    public class MenuRenderer
    {
        private SpriteBatch _spriteBatch;
        private Texture2D _pixelTexture;
        private SpriteFont _font;
        private Random _random;
        private VectorTitleRenderer _titleRenderer;

        private AchievementManager _achievementManager;


        // Menu options
        private List<string> _mainMenuOptions;
        private int _selectedOption;

        // Background gameplay elements
        private List<MenuObject> _menuObjects;
        private MenuObject _menuShip;
        private float _bulletTimer;
        private float _attackTimer;
        private const float BulletDelay = 0.25f;
        private const float AttackDelay = 2.0f;
        private const int NumAsteroids = 8; // Reduced initial count for faster loading
        private const int MaxAsteroids = 12;

        // Title animation
        private float _titlePulse;
        private const float PulseSpeed = 2.0f;

        // Name entry animation
        private float _cursorBlinkTimer;
        private bool _showCursor;

        // Cached vectors to reduce GC
        private Vector2 _tempVector = new Vector2();
        private Vector2 _tempVector2 = new Vector2();
        private Vector2 _tempDirection = new Vector2();

        // Cached object arrays
        private List<MenuObject> _asteroidsToAdd = new List<MenuObject>();
        private List<int> _objectsToRemove = new List<int>();

        // Initialization status
        private bool _fullyInitialized = false;



        public MenuRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font, AchievementManager achievementManager = null)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _random = new Random();
            _achievementManager = achievementManager;

            // Create pixel texture for drawing lines
            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            // Create title renderer
            _titleRenderer = new VectorTitleRenderer(graphicsDevice, spriteBatch);

            // Initialize menu options with the new Achievements option
            _mainMenuOptions = new List<string>
            {
                "PLAY",
                "HIGH SCORES",
                "ACHIEVEMENTS",
                "EXIT"
            };
            _selectedOption = 0;

            // Initialize animations
            _titlePulse = 0f;
            _cursorBlinkTimer = 0f;
            _showCursor = true;

            // Defer the expensive background creation until needed
            // Initialize with empty lists
            _menuObjects = new List<MenuObject>();
            _bulletTimer = 0f;
            _attackTimer = 0f;
        }



        public void EnsureInitialized()
        {
            if (_fullyInitialized)
                return;

            // Create ship
            _menuShip = CreateShip();
            _menuObjects.Add(_menuShip);

            // Create initial asteroids - fewer at first, will add more over time
            for (int i = 0; i < NumAsteroids; i++)
            {
                _menuObjects.Add(CreateRandomAsteroid());
            }

            _fullyInitialized = true;
        }

        private MenuObject CreateShip()
        {
            // Create ship at center position
            return new MenuObject
            {
                Position = new Vector2(GameConstants.ScreenWidth / 2, GameConstants.ScreenHeight / 2),
                Velocity = Vector2.Zero,
                Points = new Vector2[]
                {
                    new Vector2(0, -10),   // Top
                    new Vector2(-7, 10),   // Bottom left
                    new Vector2(0, 5),     // Bottom middle
                    new Vector2(7, 10),    // Bottom right
                    new Vector2(0, -10)    // Back to top to close the shape
                },
                Rotation = 0f,
                RotationSpeed = 0f,
                IsShip = true
            };
        }

        private MenuObject CreateRandomAsteroid()
        {
            // Pick location outside the center area
            Vector2 position = new Vector2(_random.Next(GameConstants.ScreenWidth), _random.Next(GameConstants.ScreenHeight));

            // Keep trying until we're far enough from center
            while (Vector2.Distance(position, new Vector2(GameConstants.ScreenWidth / 2, GameConstants.ScreenHeight / 2)) < 150)
            {
                position = new Vector2(_random.Next(GameConstants.ScreenWidth), _random.Next(GameConstants.ScreenHeight));
            }

            // Random size
            int size = 10 + _random.Next(30);
            int vertices = 5 + _random.Next(4);

            // Create shape points
            Vector2[] points = new Vector2[vertices + 1];
            for (int i = 0; i < vertices; i++)
            {
                float vertexAngle = i * MathHelper.TwoPi / vertices;
                float distance = size * (0.8f + 0.4f * (float)_random.NextDouble());

                points[i] = new Vector2(
                    (float)Math.Cos(vertexAngle) * distance,
                    (float)Math.Sin(vertexAngle) * distance
                );
            }
            points[vertices] = points[0];

            // Random velocity
            float angle = (float)_random.NextDouble() * MathHelper.TwoPi;
            float speed = 30f + (float)_random.NextDouble() * 50f;

            Vector2 velocity = new Vector2(
                (float)Math.Cos(angle) * speed,
                (float)Math.Sin(angle) * speed
            );

            return new MenuObject
            {
                Position = position,
                Velocity = velocity,
                Points = points,
                Rotation = (float)_random.NextDouble() * MathHelper.TwoPi,
                RotationSpeed = (float)(_random.NextDouble() - 0.5) * 2.0f,
                IsAsteroid = true
            };
        }

        private MenuObject CreateBullet(Vector2 position, Vector2 direction)
        {
            return new MenuObject
            {
                Position = position,
                Velocity = direction * 500f,
                Points = new Vector2[] { Vector2.Zero, direction * 8 },
                Rotation = 0f,
                RotationSpeed = 0f,
                Lifetime = 0f,
                IsBullet = true
            };
        }

        public void Update(float deltaTime, InputManager inputManager)
        {
            // Ensure all background elements are initialized
            EnsureInitialized();

            // Update title animation via the dedicated renderer
            _titleRenderer.Update(deltaTime);

            // Update background gameplay elements
            UpdateGameplay(deltaTime);

            // Update cursor blink animation
            _cursorBlinkTimer += deltaTime;
            if (_cursorBlinkTimer >= 0.5f)
            {
                _cursorBlinkTimer = 0f;
                _showCursor = !_showCursor;
            }

            // Handle menu navigation
            if (inputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Up) ||
                inputManager.IsButtonPressed(Microsoft.Xna.Framework.Input.Buttons.DPadUp) ||
                inputManager.IsButtonPressed(Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickUp))
            {
                _selectedOption = (_selectedOption - 1 + _mainMenuOptions.Count) % _mainMenuOptions.Count;
            }
            else if (inputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Down) ||
                     inputManager.IsButtonPressed(Microsoft.Xna.Framework.Input.Buttons.DPadDown) ||
                     inputManager.IsButtonPressed(Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickDown))
            {
                _selectedOption = (_selectedOption + 1) % _mainMenuOptions.Count;
            }
        }

        private void UpdateGameplay(float deltaTime)
        {
            // Update attack timing - aggressive AI will seek targets
            _attackTimer -= deltaTime;
            if (_attackTimer <= 0)
            {
                // Find nearest asteroid
                MenuObject target = FindNearestAsteroid();
                if (target != null)
                {
                    // Turn toward target - create a direction vector
                    Vector2 direction = target.Position - _menuShip.Position;

                    // Normalize
                    if (direction != Vector2.Zero)
                    {
                        direction = Vector2.Normalize(direction);
                    }

                    _menuShip.Rotation = (float)Math.Atan2(direction.Y, direction.X) + MathHelper.PiOver2;

                    // Move toward target aggressively - create a new velocity vector
                    _menuShip.Velocity = direction * 200f;

                    // Reset attack timer
                    _attackTimer = AttackDelay * (0.5f + (float)_random.NextDouble());
                }
                else
                {
                    // No targets, move randomly - create a new velocity vector
                    _menuShip.Velocity = new Vector2(
                        (float)(_random.NextDouble() - 0.5) * 200f,
                        (float)(_random.NextDouble() - 0.5) * 200f
                    );

                    _menuShip.Rotation = (float)Math.Atan2(_menuShip.Velocity.Y, _menuShip.Velocity.X) + MathHelper.PiOver2;
                    _attackTimer = AttackDelay;
                }
            }

            // Occasionally fire bullets at asteroids
            _bulletTimer -= deltaTime;
            if (_bulletTimer <= 0f)
            {
                Vector2 direction = new Vector2(
                    (float)Math.Cos(_menuShip.Rotation - MathHelper.PiOver2),
                    (float)Math.Sin(_menuShip.Rotation - MathHelper.PiOver2)
                );

                // Add bullet to the list
                Vector2 bulletPosition = _menuShip.Position + direction * 15;
                _menuObjects.Add(CreateBullet(bulletPosition, direction));
                _bulletTimer = BulletDelay;
            }

            // Clear the removal list
            _objectsToRemove.Clear();
            _asteroidsToAdd.Clear();

            // Update all objects
            for (int i = 0; i < _menuObjects.Count; i++)
            {
                _menuObjects[i].Update(deltaTime);

                // Handle screen wrapping
                WrapObject(_menuObjects[i]);

                // Remove bullets that exceed lifetime
                if (_menuObjects[i].IsBullet && _menuObjects[i].Lifetime > 2.0f)
                {
                    _objectsToRemove.Add(i);
                }
            }

            // Check for collisions between bullets and asteroids
            CheckCollisions();

            // Remove objects (in reverse order to keep indices valid)
            for (int i = _objectsToRemove.Count - 1; i >= 0; i--)
            {
                int index = _objectsToRemove[i];
                if (index < _menuObjects.Count)
                {
                    _menuObjects.RemoveAt(index);
                }
            }

            // Add new asteroids
            foreach (var asteroid in _asteroidsToAdd)
            {
                _menuObjects.Add(asteroid);
            }

            // Make sure we have enough asteroids
            int asteroidCount = CountAsteroids();
            while (asteroidCount < MaxAsteroids)
            {
                _menuObjects.Add(CreateRandomAsteroid());
                asteroidCount++;
            }
        }

        private MenuObject FindNearestAsteroid()
        {
            MenuObject nearest = null;
            float minDistance = float.MaxValue;

            foreach (var obj in _menuObjects)
            {
                if (obj.IsAsteroid)
                {
                    float distance = Vector2.DistanceSquared(obj.Position, _menuShip.Position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = obj;
                    }
                }
            }

            return nearest;
        }

        private int CountAsteroids()
        {
            int count = 0;
            foreach (var obj in _menuObjects)
            {
                if (obj.IsAsteroid)
                {
                    count++;
                }
            }
            return count;
        }

        private void WrapObject(MenuObject obj)
        {
            // Extra margin to prevent popping
            const int margin = 20;

            // Handle X wrapping
            Vector2 position = obj.Position;
            if (position.X < -margin)
                position = new Vector2(GameConstants.ScreenWidth + margin, position.Y);
            else if (position.X > GameConstants.ScreenWidth + margin)
                position = new Vector2(-margin, position.Y);

            // Handle Y wrapping
            if (position.Y < -margin)
                position = new Vector2(position.X, GameConstants.ScreenHeight + margin);
            else if (position.Y > GameConstants.ScreenHeight + margin)
                position = new Vector2(position.X, -margin);

            // Only assign if changed
            if (position != obj.Position)
                obj.Position = position;
        }

        private void CheckCollisions()
        {
            for (int i = 0; i < _menuObjects.Count; i++)
            {
                if (!_menuObjects[i].IsBullet) continue;

                for (int j = 0; j < _menuObjects.Count; j++)
                {
                    if (!_menuObjects[j].IsAsteroid) continue;

                    // Simple distance check - squared distance for better performance
                    float radius = GetObjectRadius(_menuObjects[j]);
                    if (Vector2.DistanceSquared(_menuObjects[i].Position, _menuObjects[j].Position) < radius * radius)
                    {
                        // Split or remove the asteroid
                        SplitAsteroid(_menuObjects[j]);

                        // Mark for removal
                        if (!_objectsToRemove.Contains(i))
                            _objectsToRemove.Add(i);

                        if (!_objectsToRemove.Contains(j))
                            _objectsToRemove.Add(j);

                        // Stop checking this bullet
                        break;
                    }
                }
            }
        }

        private float GetObjectRadius(MenuObject obj)
        {
            // Rough estimate of radius from points
            float maxDist = 0f;
            foreach (var point in obj.Points)
            {
                float dist = point.Length();
                if (dist > maxDist) maxDist = dist;
            }
            return maxDist;
        }

        private void SplitAsteroid(MenuObject asteroid)
        {
            float radius = GetObjectRadius(asteroid);

            // Only split if big enough
            if (radius > 10)
            {
                // Create two smaller asteroids
                for (int i = 0; i < 2; i++)
                {
                    int vertices = asteroid.Points.Length - 1;
                    Vector2[] newPoints = new Vector2[vertices + 1];

                    for (int j = 0; j < vertices; j++)
                    {
                        // New points are 60% of original size
                        newPoints[j] = asteroid.Points[j] * 0.6f;
                    }
                    newPoints[vertices] = newPoints[0];

                    // Create new velocity at angle from original
                    float angle = (float)Math.Atan2(asteroid.Velocity.Y, asteroid.Velocity.X);
                    angle += (i == 0 ? 0.7f : -0.7f);

                    Vector2 newVelocity = new Vector2(
                        (float)Math.Cos(angle),
                        (float)Math.Sin(angle)
                    ) * asteroid.Velocity.Length() * 1.3f;

                    // Add new asteroid
                    _asteroidsToAdd.Add(new MenuObject
                    {
                        Position = asteroid.Position,
                        Velocity = newVelocity,
                        Points = newPoints,
                        Rotation = asteroid.Rotation,
                        RotationSpeed = asteroid.RotationSpeed * 1.5f,
                        IsAsteroid = true
                    });
                }
            }
        }

        public int GetSelectedOption()
        {
            return _selectedOption;
        }


        public void DrawMainMenu()
        {
            // Draw background gameplay
            DrawGameplayObjects();

            // Draw semi-transparent overlay
            DrawRect(Vector2.Zero, new Vector2(GameConstants.ScreenWidth, GameConstants.ScreenHeight),
                    new Color(0, 0, 0, 150));

            // Calculate title width based on character count and spacing
            // Each letter is approximately 40 units wide at scale 1.0
            float titleScale = 1.0f;
            string title = "ASTEROIDS";
            float estimatedTitleWidth = title.Length * 40 * titleScale;

            // Draw vector-based title centered at the top
            _titleRenderer.DrawTitle(title,
                new Vector2((GameConstants.ScreenWidth - estimatedTitleWidth) / 2, GameConstants.ScreenHeight / 4 - 30),
                titleScale,
                Color.White);

            // Draw menu options
            int menuOptionHeight = 40;
            int menuStartY = GameConstants.ScreenHeight / 2;

            for (int i = 0; i < _mainMenuOptions.Count; i++)
            {
                Color optionColor = i == _selectedOption ? Color.Yellow : Color.White;
                string prefix = i == _selectedOption ? "> " : "  ";

                // Special handling for Achievements option to show progress
                string optionText = _mainMenuOptions[i];
                if (i == 2 && _achievementManager != null) // Achievements option
                {
                    int unlocked = _achievementManager.GetUnlockedCount();
                    int total = _achievementManager.GetTotalCount();
                    optionText = $"{_mainMenuOptions[i]} ({unlocked}/{total})";
                }

                Vector2 textSize = _font.MeasureString(optionText);
                Vector2 position = new Vector2(
                    (GameConstants.ScreenWidth - textSize.X) / 2,
                    menuStartY + i * menuOptionHeight
                );

                _spriteBatch.DrawString(_font, prefix + optionText, position, optionColor);
            }

            // Draw version or build info at the bottom
            string versionText = "v1.0.0";
            Vector2 versionSize = _font.MeasureString(versionText);
            _spriteBatch.DrawString(
                _font,
                versionText,
                new Vector2(
                    GameConstants.ScreenWidth - versionSize.X - 10,
                    GameConstants.ScreenHeight - versionSize.Y - 10
                ),
                new Color(150, 150, 150, 200)
            );
        }

        private void DrawGameplayObjects()
        {
            // Draw all objects
            foreach (var obj in _menuObjects)
            {
                if (obj.IsBullet)
                {
                    // Draw bullet as line
                    Vector2 direction = Vector2.Normalize(obj.Velocity);
                    DrawLine(
                        obj.Position - direction * 4,
                        obj.Position + direction * 4,
                        Color.White
                    );
                }
                else
                {
                    // Draw ship or asteroid
                    DrawObject(obj);
                }
            }
        }

        private void DrawObject(MenuObject obj)
        {
            // Transform all points based on position and rotation
            Vector2[] transformedPoints = new Vector2[obj.Points.Length];

            for (int i = 0; i < obj.Points.Length; i++)
            {
                // Rotate the point
                float cos = (float)Math.Cos(obj.Rotation);
                float sin = (float)Math.Sin(obj.Rotation);

                float rotatedX = obj.Points[i].X * cos - obj.Points[i].Y * sin;
                float rotatedY = obj.Points[i].X * sin + obj.Points[i].Y * cos;

                // Translate to position
                transformedPoints[i] = new Vector2(rotatedX + obj.Position.X, rotatedY + obj.Position.Y);
            }

            // Draw lines
            for (int i = 0; i < transformedPoints.Length - 1; i++)
            {
                DrawLine(transformedPoints[i], transformedPoints[i + 1], Color.White);
            }
        }

        public void DrawHighScores(List<ScoreEntry> highScores)
        {
            // Draw background gameplay
            DrawGameplayObjects();

            // Draw semi-transparent overlay
            DrawRect(Vector2.Zero, new Vector2(GameConstants.ScreenWidth, GameConstants.ScreenHeight),
                    new Color(0, 0, 0, 150));

            // Center the title
            string title = "HIGH SCORES";
            float titleScale = 0.8f;
            float estimatedTitleWidth = title.Length * 40 * titleScale;

            // Draw title
            _titleRenderer.DrawTitle(title,
                new Vector2((GameConstants.ScreenWidth - estimatedTitleWidth) / 2, 80),
                titleScale,
                Color.Yellow);

            // Draw headers
            _spriteBatch.DrawString(_font, "RANK", new Vector2(200, 150), Color.LightGray);
            _spriteBatch.DrawString(_font, "NAME", new Vector2(300, 150), Color.LightGray);
            _spriteBatch.DrawString(_font, "SCORE", new Vector2(500, 150), Color.LightGray);

            // Draw scores
            int scoreY = 190;
            for (int i = 0; i < highScores.Count; i++)
            {
                Color rowColor = Color.White;
                _spriteBatch.DrawString(_font, $"{i + 1}.", new Vector2(210, scoreY), rowColor);
                _spriteBatch.DrawString(_font, highScores[i].PlayerName, new Vector2(300, scoreY), rowColor);
                _spriteBatch.DrawString(_font, highScores[i].Score.ToString(), new Vector2(500, scoreY), rowColor);
                scoreY += 30;
            }

            // Draw return instruction
            _spriteBatch.DrawString(
                _font,
                "PRESS ENTER TO RETURN",
                new Vector2((GameConstants.ScreenWidth - _font.MeasureString("PRESS ENTER TO RETURN").X) / 2, 500),
                Color.Yellow
            );
        }

        public void DrawNameEntry(string currentName, HighScoreManager highScoreManager = null)
        {
            // Draw background gameplay
            DrawGameplayObjects();

            // Draw semi-transparent overlay
            DrawRect(Vector2.Zero, new Vector2(GameConstants.ScreenWidth, GameConstants.ScreenHeight),
                    new Color(0, 0, 0, 150));

            // Draw title
            _spriteBatch.DrawString(
                _font,
                "NEW HIGH SCORE!",
                new Vector2((GameConstants.ScreenWidth - _font.MeasureString("NEW HIGH SCORE!").X) / 2, 70),
                Color.Yellow
            );

            // Draw instruction
            _spriteBatch.DrawString(
                _font,
                "ENTER YOUR NAME:",
                new Vector2((GameConstants.ScreenWidth - _font.MeasureString("ENTER YOUR NAME:").X) / 2, 120),
                Color.White
            );

            // Draw name field
            string nameWithCursor = currentName;
            if (_showCursor)
            {
                nameWithCursor += "_";
            }

            Vector2 nameSize = _font.MeasureString(nameWithCursor);
            Vector2 namePos = new Vector2(
                (GameConstants.ScreenWidth - nameSize.X) / 2,
                170
            );

            // Draw name box
            DrawRect(
                new Vector2(namePos.X - 10, namePos.Y - 5),
                new Vector2(nameSize.X + 20, nameSize.Y + 10),
                Color.DarkBlue
            );

            // Draw name text
            _spriteBatch.DrawString(
                _font,
                nameWithCursor,
                namePos,
                Color.White
            );

            // Draw virtual keyboard if enabled
            bool usingVirtKeyboard = highScoreManager != null &&
                                     highScoreManager.UsingVirtualKeyboard &&
                                     highScoreManager.VirtualKeyboard != null &&
                                     highScoreManager.VirtualKeyboard.IsEnabled;

            if (usingVirtKeyboard)
            {
                // Draw virtual keyboard
                highScoreManager.VirtualKeyboard?.Draw(_spriteBatch);
            }

            // Common instructions - always shown
            float instructionY = usingVirtKeyboard ? 460 : 270;

            // Toggle keyboard instruction
            _spriteBatch.DrawString(
                _font,
                "PRESS F1 OR TAB TO TOGGLE KEYBOARD",
                new Vector2((GameConstants.ScreenWidth - _font.MeasureString("PRESS F1 OR TAB TO TOGGLE KEYBOARD").X) / 2, instructionY),
                Color.White
            );

            instructionY += 30;

            if (usingVirtKeyboard)
            {
                _spriteBatch.DrawString(
                    _font,
                    "PRESS A/ENTER TO SELECT, B/ESC TO CANCEL",
                    new Vector2((GameConstants.ScreenWidth - _font.MeasureString("PRESS A/ENTER TO SELECT, B/ESC TO CANCEL").X) / 2, instructionY),
                    Color.Yellow
                );
            }
            else
            {
                _spriteBatch.DrawString(
                    _font,
                    "PRESS ENTER TO CONFIRM, ESC TO CANCEL",
                    new Vector2((GameConstants.ScreenWidth - _font.MeasureString("PRESS ENTER TO CONFIRM, ESC TO CANCEL").X) / 2, instructionY),
                    Color.Yellow
                );
            }
        }

        public void DrawRect(Vector2 position, Vector2 size, Color color)
        {
            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y),
                color);
        }

        public void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 1)
        {
            Vector2 edge = end - start;
            float length = edge.Length();

            // Skip drawing very short lines
            if (length < 0.5f)
                return;

            float angle = (float)Math.Atan2(edge.Y, edge.X);

            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(
                    (int)start.X,
                    (int)start.Y,
                    (int)length,
                    thickness),
                null,
                color,
                angle,
                Vector2.Zero,
                SpriteEffects.None,
                0);
        }
    }
}