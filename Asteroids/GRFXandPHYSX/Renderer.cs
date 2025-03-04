using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace AsteroidsGame
{
    public class Renderer
    {
        private GraphicsDevice _graphicsDevice;
        private ContentManager _content;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;

        // Textures for collisions only (not rendering)
        public Texture2D ShipTexture { get; private set; }
        public Texture2D BulletTexture { get; private set; }
        private Texture2D _asteroidLargeTexture;
        private Texture2D _asteroidMediumTexture;
        private Texture2D _asteroidSmallTexture;
        private Texture2D _pixelTexture;

        // Vector shapes for rendering
        private List<Vector2[]> _shipVectors;
        private List<Vector2[]> _asteroidLargeVectors;
        private List<Vector2[]> _asteroidMediumVectors;
        private List<Vector2[]> _asteroidSmallVectors;

        // Cached transformations to reduce per-frame calculations
        private Vector2[] _transformedPoints;
        private const int MaxTransformedPoints = 20; // Maximum number of points in any shape

        private Random _random;

        public Renderer(GraphicsDevice graphicsDevice, ContentManager content, SpriteBatch spriteBatch)
        {
            _graphicsDevice = graphicsDevice;
            _content = content;
            _spriteBatch = spriteBatch;
            _random = new Random();

            // Create pixel texture for primitive drawing
            _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            // Initialize transformed points array
            _transformedPoints = new Vector2[MaxTransformedPoints];

            // Create collision textures (these are invisible)
            CreateCollisionTextures();

            // Create vector shape outlines
            CreateVectorShapes();

            // Try to load font, but we have a fallback if it fails
            try
            {
                _font = _content.Load<SpriteFont>("Font");
            }
            catch
            {
                // We'll use primitive text rendering if font loading fails
            }
        }

        private void CreateCollisionTextures()
        {
            // These textures are only used for collision detection
            // We'll create simple hitboxes that won't be visible
            ShipTexture = CreateCircleTexture(20);
            BulletTexture = CreateCircleTexture(4);
            _asteroidLargeTexture = CreateCircleTexture(40);
            _asteroidMediumTexture = CreateCircleTexture(30);
            _asteroidSmallTexture = CreateCircleTexture(15);
        }

        private Texture2D CreateCircleTexture(int diameter)
        {
            Texture2D texture = new Texture2D(_graphicsDevice, diameter, diameter);

            Color[] data = new Color[diameter * diameter];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Color.Transparent;
            }

            // Fill the circle
            int radius = diameter / 2;
            int centerX = radius;
            int centerY = radius;

            for (int x = 0; x < diameter; x++)
            {
                for (int y = 0; y < diameter; y++)
                {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        data[x + y * diameter] = Color.White;
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }

        private void CreateVectorShapes()
        {
            // Create ship outline (triangle)
            _shipVectors = new List<Vector2[]>();
            _shipVectors.Add(new Vector2[] {
                new Vector2(0, -10),   // Top
                new Vector2(-7, 10),   // Bottom left
                new Vector2(0, 5),     // Bottom middle
                new Vector2(7, 10),    // Bottom right
                new Vector2(0, -10)    // Back to top to close the shape
            });

            // Create asteroid outlines
            _asteroidLargeVectors = CreateAsteroidVectors(20, 8);
            _asteroidMediumVectors = CreateAsteroidVectors(15, 7);
            _asteroidSmallVectors = CreateAsteroidVectors(7, 6);
        }

        private List<Vector2[]> CreateAsteroidVectors(int radius, int vertices)
        {
            List<Vector2[]> shapes = new List<Vector2[]>();

            // Generate a few different asteroid shapes
            for (int shape = 0; shape < 3; shape++)
            {
                Vector2[] points = new Vector2[vertices + 1]; // +1 to close the shape

                // Generate points around a circle with some randomness
                for (int i = 0; i < vertices; i++)
                {
                    float angle = i * MathHelper.TwoPi / vertices;
                    float distance = radius * (0.8f + 0.4f * (float)_random.NextDouble());

                    points[i] = new Vector2(
                        (float)Math.Cos(angle) * distance,
                        (float)Math.Sin(angle) * distance
                    );
                }

                // Close the shape by returning to the first point
                points[vertices] = points[0];
                shapes.Add(points);
            }

            return shapes;
        }

        public Texture2D GetAsteroidTexture(AsteroidSize size)
        {
            switch (size)
            {
                case AsteroidSize.Small:
                    return _asteroidSmallTexture;
                case AsteroidSize.Medium:
                    return _asteroidMediumTexture;
                default:
                    return _asteroidLargeTexture;
            }
        }

        public Vector2[] GetAsteroidVectors(AsteroidSize size, int index)
        {
            List<Vector2[]> shapes;

            switch (size)
            {
                case AsteroidSize.Small:
                    shapes = _asteroidSmallVectors;
                    break;
                case AsteroidSize.Medium:
                    shapes = _asteroidMediumVectors;
                    break;
                default:
                    shapes = _asteroidLargeVectors;
                    break;
            }

            // Use the index to pick a specific shape or wrap around
            return shapes[index % shapes.Count];
        }

        public void DrawPlayer(Player player)
        {
            // If player is dying, draw the death animation
            if (player.IsDying())
            {
                // Draw ship fragments
                List<Vector2[]> fragments = player.GetDeathFragments();
                List<float> rotations = player.GetFragmentRotations();

                // Draw each fragment
                for (int i = 0; i < fragments.Count; i++)
                {
                    DrawVectorShape(fragments[i], player.Position, rotations[i], Color.White);
                }
                return;
            }

            // If player is invulnerable but not visible due to blinking, skip drawing
            if (player.IsInvulnerable() && !player.IsVisible())
            {
                return;
            }

            // Normal draw - use white color normally, or slightly transparent if invulnerable
            Color playerColor = player.IsInvulnerable() ?
                new Color(255, 255, 255, 128) :  // Semi-transparent when invulnerable
                Color.White;                     // Normal color

            // Draw the ship as vector outline
            DrawVectorShape(_shipVectors[0], player.Position, player.Rotation, playerColor);

            // Draw thruster if active
            if (player.IsThrusterActive())
            {
                // Calculate thruster position at the back of the ship
                Vector2 thrusterDirection = new Vector2(
                    (float)Math.Sin(player.Rotation + MathHelper.Pi),
                    -(float)Math.Cos(player.Rotation + MathHelper.Pi)
                );

                // Draw simple thruster flame
                Vector2 thrusterBase = player.Position + thrusterDirection * 5;
                Vector2 thrusterTip = thrusterBase + thrusterDirection * 8;
                Vector2 thrusterLeft = thrusterBase + new Vector2(
                    (float)Math.Sin(player.Rotation + MathHelper.Pi - 0.5f),
                    -(float)Math.Cos(player.Rotation + MathHelper.Pi - 0.5f)
                ) * 3;
                Vector2 thrusterRight = thrusterBase + new Vector2(
                    (float)Math.Sin(player.Rotation + MathHelper.Pi + 0.5f),
                    -(float)Math.Cos(player.Rotation + MathHelper.Pi + 0.5f)
                ) * 3;

                // Draw thruster lines directly without allocating an array
                DrawLine(thrusterBase, thrusterLeft, playerColor, 1);
                DrawLine(thrusterLeft, thrusterTip, playerColor, 1);
                DrawLine(thrusterTip, thrusterRight, playerColor, 1);
                DrawLine(thrusterRight, thrusterBase, playerColor, 1);
            }
        }

        public void DrawBullet(Bullet bullet)
        {
            // Draw bullets as simple straight lines - optimized to avoid normalizing velocity
            // Use cached direction if possible
            Vector2 direction = bullet.Direction;
            Vector2 start = bullet.Position - direction * 3;
            Vector2 end = bullet.Position + direction * 3;

            DrawLine(start, end, Color.White, 1);
        }

        // Update your DrawAsteroid method in the Renderer class

        public void DrawAsteroid(Asteroid asteroid)
        {
            // Choose color based on asteroid type
            Color asteroidColor;

            if (asteroid.Type == AsteroidType.Gold)
            {
                // Gold/yellow color for gold asteroids
                asteroidColor = new Color(255, 215, 0);  // Gold color (RGB: 255, 215, 0)
            }
            else
            {
                // Default white color for normal asteroids
                asteroidColor = Color.White;
            }

            // Draw asteroid as a vector outline with the selected color
            Vector2[] shape = GetAsteroidVectors(asteroid.Size, asteroid.ShapeIndex);
            DrawVectorShape(shape, asteroid.Position, asteroid.Rotation, asteroidColor);

            // For gold asteroids, add a subtle glow effect by drawing a second, slightly larger outline
            if (asteroid.Type == AsteroidType.Gold)
            {
                // Scale the shape slightly larger for the glow effect
                Vector2[] glowShape = new Vector2[shape.Length];
                for (int i = 0; i < shape.Length; i++)
                {
                    glowShape[i] = shape[i] * 1.1f;  // 10% larger
                }

                // Draw the glow with reduced opacity
                Color glowColor = new Color(255, 215, 0, 128);  // Semi-transparent gold
                DrawVectorShape(glowShape, asteroid.Position, asteroid.Rotation, glowColor);
            }
        }
        // Update the DrawEnemyShip method in your Renderer class

        public void DrawEnemyShip(EnemyShip enemyShip)
        {
            // Create a UFO-like shape
            Vector2[] ufoShape = new Vector2[]
            {
        // Top dome
        new Vector2(-5, -2),   // Left top edge
        new Vector2(-10, 0),   // Left middle
        new Vector2(-15, 5),   // Left bottom edge
        new Vector2(-10, 8),   // Left cabin bottom
        new Vector2(10, 8),    // Right cabin bottom
        new Vector2(15, 5),    // Right bottom edge
        new Vector2(10, 0),    // Right middle
        new Vector2(5, -2),    // Right top edge
        new Vector2(-5, -2),   // Back to start to close shape
        
        // Draw cabin (separate shape)
        new Vector2(-10, 0),   // Left cabin top
        new Vector2(-8, 8),    // Left cabin bottom
        new Vector2(8, 8),     // Right cabin bottom
        new Vector2(10, 0),    // Right cabin top
        new Vector2(-10, 0),   // Back to start to close cabin
            };

            // Draw the UFO in a purplish color
            Color ufoColor = new Color(200, 150, 255);  // Light purple
            DrawVectorShape(ufoShape, enemyShip.Position, 0, ufoColor);

            // Add blinking lights around the edge
            float time = (float)DateTime.Now.Millisecond / 1000.0f;
            DrawBlinkingLights(enemyShip.Position, time);
        }

        // Helper method to draw blinking lights around the UFO
        private void DrawBlinkingLights(Vector2 position, float time)
        {
            // Draw 6 lights around the edge that blink in sequence
            int numLights = 6;
            for (int i = 0; i < numLights; i++)
            {
                // Calculate light position around the saucer
                float angle = i * MathHelper.TwoPi / numLights;
                Vector2 lightPos = new Vector2(
                    position.X + (float)Math.Cos(angle) * 12,
                    position.Y + (float)Math.Sin(angle) * 6 + 4  // Offset to bottom half
                );

                // Calculate blinking pattern - lights turn on in sequence
                float blinkPhase = (time * 5 + i * MathHelper.TwoPi / numLights) % MathHelper.TwoPi;
                bool lightOn = blinkPhase < MathHelper.PiOver2;

                // Draw the light as a small dot
                if (lightOn)
                {
                    // Alternate between two colors
                    Color lightColor = i % 2 == 0 ? Color.Yellow : Color.Red;

                    // Draw a tiny rectangle for the light
                    _spriteBatch.Draw(
                        _pixelTexture,
                        new Rectangle((int)lightPos.X - 1, (int)lightPos.Y - 1, 2, 2),
                        lightColor
                    );
                }
            }
        }

        private void DrawVectorShape(Vector2[] points, Vector2 position, float rotation, Color color)
        {
            int count = points.Length;

            // Ensure the transformed points array is large enough
            if (count > _transformedPoints.Length)
            {
                _transformedPoints = new Vector2[count];
            }

            // Transform all points at once
            for (int i = 0; i < count; i++)
            {
                _transformedPoints[i] = TransformPoint(points[i], position, rotation);
            }

            // Draw each line segment of the shape
            for (int i = 0; i < count - 1; i++)
            {
                // Draw the line segment using pre-calculated transformed points
                DrawLine(_transformedPoints[i], _transformedPoints[i + 1], color, 1);
            }
        }

        private Vector2 TransformPoint(Vector2 point, Vector2 position, float rotation)
        {
            // Optimize rotation calculations
            float sin = (float)Math.Sin(rotation);
            float cos = (float)Math.Cos(rotation);

            // Rotate the point
            float rotatedX = point.X * cos - point.Y * sin;
            float rotatedY = point.X * sin + point.Y * cos;

            // Translate to the position
            return new Vector2(rotatedX + position.X, rotatedY + position.Y);
        }

        // Updated DrawUI signature to match the expected parameter count
        public void DrawUI(int score, int lives, bool gameOver)
        {
            // Original implementation
            if (_font != null)
            {
                // Draw score and lives
                _spriteBatch.DrawString(_font, $"Score: {score}", new Vector2(10, 10), Color.White);
                _spriteBatch.DrawString(_font, $"Lives: {lives}", new Vector2(10, 40), Color.White);

                // Draw game over message if needed
                if (gameOver)
                {
                    string gameOverText = "GAME OVER - Press Enter to Restart";
                    Vector2 textSize = _font.MeasureString(gameOverText);
                    _spriteBatch.DrawString(
                        _font,
                        gameOverText,
                        new Vector2(
                            (GameConstants.ScreenWidth - textSize.X) / 2,
                            (GameConstants.ScreenHeight - textSize.Y) / 2
                        ),
                        Color.White
                    );
                }
            }
            else
            {
                // Fallback text rendering when font isn't available
                DrawText($"Score: {score}", new Vector2(10, 10), Color.White);
                DrawText($"Lives: {lives}", new Vector2(10, 40), Color.White);

                if (gameOver)
                {
                    DrawText("GAME OVER - Press Enter to Restart",
                        new Vector2(GameConstants.ScreenWidth / 2 - 150, GameConstants.ScreenHeight / 2),
                        Color.White);
                }
            }
        }

        // Alternative version with difficulty info
        public void DrawUI(int score, int lives, bool gameOver, int difficultyLevel, float speedMultiplier)
        {
            // Draw basic UI first
            DrawUI(score, lives, gameOver);

            // Add difficulty info if applicable
            if (difficultyLevel > 0 && _font != null)
            {
                string difficultyText = $"Level: {difficultyLevel}  Speed: {speedMultiplier:F2}x";
                _spriteBatch.DrawString(_font, difficultyText, new Vector2(10, 70), Color.Yellow);
            }
            else if (difficultyLevel > 0)
            {
                // Fallback text rendering
                DrawText($"Level: {difficultyLevel}  Speed: {speedMultiplier:F2}x", new Vector2(10, 70), Color.Yellow);
            }
        }

        // Primitive text drawing methods when SpriteFont is unavailable
        private void DrawText(string text, Vector2 position, Color color)
        {
            int charWidth = 10;
            int charHeight = 15;
            int spacing = 2;

            for (int i = 0; i < text.Length; i++)
            {
                Vector2 charPos = new Vector2(position.X + i * (charWidth + spacing), position.Y);
                DrawCharacter(text[i], charPos, charWidth, charHeight, color);
            }
        }

        // Cached character vectors for common characters to avoid repeated calculations
        private static readonly Dictionary<char, Vector2[][]> _characterVectors = new Dictionary<char, Vector2[][]>() {
            {'S', new Vector2[][] {
                new Vector2[] { new Vector2(0, 0), new Vector2(1, 0) },
                new Vector2[] { new Vector2(0, 0), new Vector2(0, 0.5f) },
                new Vector2[] { new Vector2(0, 0.5f), new Vector2(1, 0.5f) },
                new Vector2[] { new Vector2(1, 0.5f), new Vector2(1, 1) },
                new Vector2[] { new Vector2(0, 1), new Vector2(1, 1) }
            }},
            {'c', new Vector2[][] {
                new Vector2[] { new Vector2(1, 0.25f), new Vector2(0, 0.25f) },
                new Vector2[] { new Vector2(0, 0.25f), new Vector2(0, 0.75f) },
                new Vector2[] { new Vector2(0, 0.75f), new Vector2(1, 0.75f) }
            }},
            // Additional characters would be defined here
        };

        private void DrawCharacter(char c, Vector2 position, int width, int height, Color color)
        {
            // Optimized character drawing using cached vectors if available
            if (_characterVectors.TryGetValue(c, out Vector2[][] vectors))
            {
                foreach (Vector2[] line in vectors)
                {
                    DrawLine(
                        position + new Vector2(line[0].X * width, line[0].Y * height),
                        position + new Vector2(line[1].X * width, line[1].Y * height),
                        color
                    );
                }
                return;
            }

            // Fallback to switch case for characters not in the cache
            switch (c)
            {
                case 'o':
                    DrawLine(position + new Vector2(0, height / 4), position + new Vector2(width, height / 4), color);
                    DrawLine(position + new Vector2(width, height / 4), position + new Vector2(width, height * 3 / 4), color);
                    DrawLine(position + new Vector2(width, height * 3 / 4), position + new Vector2(0, height * 3 / 4), color);
                    DrawLine(position + new Vector2(0, height * 3 / 4), position + new Vector2(0, height / 4), color);
                    break;
                // Additional case statements for other characters

                case ' ':
                    // Space - nothing to draw
                    break;
                default:
                    // For other characters, just draw a box outline
                    DrawLine(position, position + new Vector2(width, 0), color);
                    DrawLine(position + new Vector2(width, 0), position + new Vector2(width, height), color);
                    DrawLine(position + new Vector2(width, height), position + new Vector2(0, height), color);
                    DrawLine(position + new Vector2(0, height), position, color);
                    break;
            }
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