using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace AsteroidsGame
{
    // Simple utility classes just for menu rendering
    internal class MenuObject
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2[] Points;
        public float Rotation;
        public float RotationSpeed;
        public float Lifetime;
        public bool IsAsteroid;
        public bool IsBullet;
        public bool IsShip;

        public void Update(float deltaTime)
        {
            Position += Velocity * deltaTime;
            Rotation += RotationSpeed * deltaTime;
            if (IsBullet) Lifetime += deltaTime;
        }
    }

    public class MenuRenderer
    {
        private SpriteBatch _spriteBatch;
        private Texture2D _pixelTexture;
        private SpriteFont _font;
        private Random _random;

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
        private const int NumAsteroids = 12;

        // Title animation
        private float _titlePulse;
        private const float PulseSpeed = 2.0f;

        // Name entry animation
        private float _cursorBlinkTimer;
        private bool _showCursor;

        public MenuRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _random = new Random();

            // Create pixel texture for drawing lines
            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            // Initialize menu options
            _mainMenuOptions = new List<string>
            {
                "PLAY",
                "HIGH SCORES",
                "EXIT"
            };
            _selectedOption = 0;

            // Initialize background gameplay elements
            _menuObjects = new List<MenuObject>();
            _bulletTimer = 0f;
            _attackTimer = 0f;

            // Create ship
            _menuShip = CreateShip();
            _menuObjects.Add(_menuShip);

            // Create initial asteroids
            for (int i = 0; i < NumAsteroids; i++)
            {
                _menuObjects.Add(CreateRandomAsteroid());
            }

            // Initialize animations
            _titlePulse = 0f;
            _cursorBlinkTimer = 0f;
            _showCursor = true;
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
            Vector2 position;
            do
            {
                position = new Vector2(
                    _random.Next(GameConstants.ScreenWidth),
                    _random.Next(GameConstants.ScreenHeight)
                );
            } while (Vector2.Distance(position, new Vector2(GameConstants.ScreenWidth / 2, GameConstants.ScreenHeight / 2)) < 150);

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
            // Update background gameplay elements
            UpdateGameplay(deltaTime);

            // Update title animation
            _titlePulse += deltaTime * PulseSpeed;
            if (_titlePulse > MathHelper.TwoPi)
            {
                _titlePulse -= MathHelper.TwoPi;
            }

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
                    // Turn toward target
                    Vector2 direction = Vector2.Normalize(target.Position - _menuShip.Position);
                    _menuShip.Rotation = (float)Math.Atan2(direction.Y, direction.X) + MathHelper.PiOver2;

                    // Move toward target aggressively
                    _menuShip.Velocity = direction * 200f;

                    // Reset attack timer
                    _attackTimer = AttackDelay * (0.5f + (float)_random.NextDouble());
                }
                else
                {
                    // No targets, move randomly
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
                _menuObjects.Add(CreateBullet(_menuShip.Position + direction * 15, direction));
                _bulletTimer = BulletDelay;
            }

            // Update all objects
            for (int i = _menuObjects.Count - 1; i >= 0; i--)
            {
                _menuObjects[i].Update(deltaTime);

                // Handle screen wrapping
                WrapObject(_menuObjects[i]);

                // Remove bullets that exceed lifetime
                if (_menuObjects[i].IsBullet && _menuObjects[i].Lifetime > 2.0f)
                {
                    _menuObjects.RemoveAt(i);
                }
            }

            // Check for collisions between bullets and asteroids
            CheckCollisions();

            // Make sure we have enough asteroids
            while (CountAsteroids() < NumAsteroids)
            {
                _menuObjects.Add(CreateRandomAsteroid());
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
                    float distance = Vector2.Distance(obj.Position, _menuShip.Position);
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
            for (int i = _menuObjects.Count - 1; i >= 0; i--)
            {
                if (i >= _menuObjects.Count || !_menuObjects[i].IsBullet) continue;

                for (int j = _menuObjects.Count - 1; j >= 0; j--)
                {
                    if (j >= _menuObjects.Count || i >= _menuObjects.Count || !_menuObjects[j].IsAsteroid) continue;

                    // Simple distance check
                    if (Vector2.Distance(_menuObjects[i].Position, _menuObjects[j].Position) < GetObjectRadius(_menuObjects[j]))
                    {
                        // Split or remove the asteroid
                        SplitAsteroid(_menuObjects[j]);

                        // Remove asteroid and bullet
                        if (i < _menuObjects.Count)
                            _menuObjects.RemoveAt(i);

                        if (j < _menuObjects.Count && (j < i || i >= _menuObjects.Count))
                            _menuObjects.RemoveAt(j);

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
                    Vector2 newVel = new Vector2(
                        (float)Math.Cos(angle),
                        (float)Math.Sin(angle)
                    ) * asteroid.Velocity.Length() * 1.3f;

                    // Add new asteroid
                    _menuObjects.Add(new MenuObject
                    {
                        Position = asteroid.Position,
                        Velocity = newVel,
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

            // Draw title
            DrawTitle();

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
                    DrawLine(obj.Position - direction * 4, obj.Position + direction * 4, Color.White);
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
                float rotatedX = obj.Points[i].X * (float)Math.Cos(obj.Rotation) -
                                obj.Points[i].Y * (float)Math.Sin(obj.Rotation);
                float rotatedY = obj.Points[i].X * (float)Math.Sin(obj.Rotation) +
                                obj.Points[i].Y * (float)Math.Cos(obj.Rotation);

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
            _spriteBatch.DrawString(
                _font,
                "HIGH SCORES",
                new Vector2((GameConstants.ScreenWidth - _font.MeasureString("HIGH SCORES").X) / 2, 80),
                Color.Yellow
            );

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

        public void DrawNameEntry(string currentName)
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
                new Vector2((GameConstants.ScreenWidth - _font.MeasureString("NEW HIGH SCORE!").X) / 2, 100),
                Color.Yellow
            );

            // Draw instruction
            _spriteBatch.DrawString(
                _font,
                "ENTER YOUR NAME:",
                new Vector2((GameConstants.ScreenWidth - _font.MeasureString("ENTER YOUR NAME:").X) / 2, 180),
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
                250
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

            // Draw instructions
            _spriteBatch.DrawString(
                _font,
                "PRESS ENTER TO CONFIRM",
                new Vector2((GameConstants.ScreenWidth - _font.MeasureString("PRESS ENTER TO CONFIRM").X) / 2, 350),
                Color.Yellow
            );

            _spriteBatch.DrawString(
                _font,
                "PRESS ESC TO CANCEL",
                new Vector2((GameConstants.ScreenWidth - _font.MeasureString("PRESS ESC TO CANCEL").X) / 2, 380),
                Color.Yellow
            );
        }

        private void DrawTitle()
        {
            string title = "ASTEROIDS";
            Vector2 titleSize = _font.MeasureString(title) * 1.5f;
            Vector2 position = new Vector2(
                (GameConstants.ScreenWidth - titleSize.X) / 2,
                GameConstants.ScreenHeight / 4
            );

            // Draw the title with a slight pulse effect
            float scale = 1.5f + 0.1f * (float)Math.Sin(_titlePulse);
            _spriteBatch.DrawString(_font, title, position, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);

            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(
                    (int)start.X,
                    (int)start.Y,
                    (int)edge.Length(),
                    1),
                null,
                color,
                angle,
                Vector2.Zero,
                SpriteEffects.None,
                0);
        }

        private void DrawRect(Vector2 position, Vector2 size, Color color)
        {
            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y),
                color);
        }
    }
}