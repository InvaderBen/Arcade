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
        private MenuObject[] _tempAsteroidArray;
        private List<MenuObject> _asteroidsToAdd = new List<MenuObject>();
        private List<int> _objectsToRemove = new List<int>();

        // Initialization status
        private bool _fullyInitialized = false;

        public MenuRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _random = new Random();

            // Create pixel texture for drawing lines
            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            // Create title renderer
            _titleRenderer = new VectorTitleRenderer(graphicsDevice, spriteBatch);

            // Initialize menu options
            _mainMenuOptions = new List<string>
            {
                "PLAY",
                "HIGH SCORES",
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

            // Initialize the temp arrays
            _tempAsteroidArray = new MenuObject[MaxAsteroids];
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
            _tempVector.X = _random.Next(GameConstants.ScreenWidth);
            _tempVector.Y = _random.Next(GameConstants.ScreenHeight);

            // Keep trying until we're far enough from center
            while (Vector2.Distance(_tempVector, new Vector2(GameConstants.ScreenWidth / 2, GameConstants.ScreenHeight / 2)) < 150)
            {
                _tempVector.X = _random.Next(GameConstants.ScreenWidth);
                _tempVector.Y = _random.Next(GameConstants.ScreenHeight);
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

            _tempVector2.X = (float)Math.Cos(angle) * speed;
            _tempVector2.Y = (float)Math.Sin(angle) * speed;

            return new MenuObject
            {
                Position = _tempVector,
                Velocity = _tempVector2,
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
                Velocity = new Vector2(direction.X * 500f, direction.Y * 500f),
                Points = new Vector2[] { Vector2.Zero, new Vector2(direction.X * 8, direction.Y * 8) },
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
                    // Turn toward target - reuse vectors
                    _tempDirection.X = target.Position.X - _menuShip.Position.X;
                    _tempDirection.Y = target.Position.Y - _menuShip.Position.Y;

                    // Normalize
                    float length = _tempDirection.Length();
                    if (length > 0)
                    {
                        _tempDirection.X /= length;
                        _tempDirection.Y /= length;
                    }

                    _menuShip.Rotation = (float)Math.Atan2(_tempDirection.Y, _tempDirection.X) + MathHelper.PiOver2;

                    // Move toward target aggressively
                    _menuShip.Velocity.X = _tempDirection.X * 200f;
                    _menuShip.Velocity.Y = _tempDirection.Y * 200f;

                    // Reset attack timer
                    _attackTimer = AttackDelay * (0.5f + (float)_random.NextDouble());
                }
                else
                {
                    // No targets, move randomly
                    _menuShip.Velocity.X = (float)(_random.NextDouble() - 0.5) * 200f;
                    _menuShip.Velocity.Y = (float)(_random.NextDouble() - 0.5) * 200f;
                    _menuShip.Rotation = (float)Math.Atan2(_menuShip.Velocity.Y, _menuShip.Velocity.X) + MathHelper.PiOver2;
                    _attackTimer = AttackDelay;
                }
            }

            // Occasionally fire bullets at asteroids
            _bulletTimer -= deltaTime;
            if (_bulletTimer <= 0f)
            {
                _tempVector.X = (float)Math.Cos(_menuShip.Rotation - MathHelper.PiOver2);
                _tempVector.Y = (float)Math.Sin(_menuShip.Rotation - MathHelper.PiOver2);

                // Add bullet to the list
                _menuObjects.Add(CreateBullet(_menuShip.Position + new Vector2(_tempVector.X * 15, _tempVector.Y * 15), _tempVector));
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

            if (obj.Position.X < -margin) obj.Position.X = GameConstants.ScreenWidth + margin;
            if (obj.Position.X > GameConstants.ScreenWidth + margin) obj.Position.X = -margin;
            if (obj.Position.Y < -margin) obj.Position.Y = GameConstants.ScreenHeight + margin;
            if (obj.Position.Y > GameConstants.ScreenHeight + margin) obj.Position.Y = -margin;
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
                        newPoints[j] = new Vector2(
                            asteroid.Points[j].X * 0.6f,
                            asteroid.Points[j].Y * 0.6f
                        );
                    }
                    newPoints[vertices] = newPoints[0];

                    // Create new velocity at angle from original
                    float angle = (float)Math.Atan2(asteroid.Velocity.Y, asteroid.Velocity.X);
                    angle += (i == 0 ? 0.7f : -0.7f);

                    _tempVector.X = (float)Math.Cos(angle) * asteroid.Velocity.Length() * 1.3f;
                    _tempVector.Y = (float)Math.Sin(angle) * asteroid.Velocity.Length() * 1.3f;

                    // Add new asteroid
                    _asteroidsToAdd.Add(new MenuObject
                    {
                        Position = asteroid.Position,
                        Velocity = _tempVector,
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

            // Draw vector-based title at the top
            _titleRenderer.DrawTitle("ASTEROIDS",
                new Vector2(GameConstants.ScreenWidth / 2 - 160, GameConstants.ScreenHeight / 4 - 30),
                1.0f,
                Color.White);

            // Draw menu options
            int menuOptionHeight = 40;
            int menuStartY = GameConstants.ScreenHeight / 2;

            for (int i = 0; i < _mainMenuOptions.Count; i++)
            {
                Color optionColor = i == _selectedOption ? Color.Yellow : Color.White;
                string prefix = i == _selectedOption ? "> " : "  ";

                Vector2 textSize = _font.MeasureString(_mainMenuOptions[i]);
                Vector2 position = new Vector2(
                    (GameConstants.ScreenWidth - textSize.X) / 2,
                    menuStartY + i * menuOptionHeight
                );

                _spriteBatch.DrawString(_font, prefix + _mainMenuOptions[i], position, optionColor);
            }
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
                        new Vector2(obj.Position.X - direction.X * 4, obj.Position.Y - direction.Y * 4),
                        new Vector2(obj.Position.X + direction.X * 4, obj.Position.Y + direction.Y * 4),
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

            // Draw title
            _titleRenderer.DrawTitle("HIGH SCORES",
                new Vector2(GameConstants.ScreenWidth / 2 - 150, 80),
                0.8f,
                Color.Yellow);

            // Draw headers
            _spriteBatch.DrawString(_font,