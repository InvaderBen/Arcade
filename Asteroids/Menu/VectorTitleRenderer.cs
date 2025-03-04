using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace AsteroidsGame
{
    public class VectorTitleRenderer
    {
        private SpriteBatch _spriteBatch;
        private Texture2D _pixelTexture;
        private float _pulse;
        private const float PulseSpeed = 1.5f;
        private const float PulseAmount = 0.15f;

        // Letter definition points (normalized 0.0-1.0 coordinates)
        private readonly Dictionary<char, Vector2[]> _letterVectors;

        // Title configuration
        private Vector2 _position;
        private float _scale;
        private float _characterSpacing = 1.2f; // Spacing between letters as multiple of width
        private Color _color = Color.White;

        // Reusable vectors to avoid GC
        private Vector2 _reusableVec1 = new Vector2();
        private Vector2 _reusableVec2 = new Vector2();

        public VectorTitleRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
        {
            _spriteBatch = spriteBatch;

            // Create pixel texture for line drawing
            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            // Initialize pulse animation
            _pulse = 0f;

            // Initialize letter definitions
            _letterVectors = new Dictionary<char, Vector2[]>();
            InitializeLetterVectors();
        }

        private void InitializeLetterVectors()
        {
            // A
            _letterVectors['A'] = new Vector2[]
            {
                new Vector2(0.0f, 1.0f),    // bottom left
                new Vector2(0.5f, 0.0f),    // top center
                new Vector2(1.0f, 1.0f),    // bottom right
                new Vector2(0.2f, 0.6f),    // left middle bar
                new Vector2(0.8f, 0.6f)     // right middle bar
            };

            // S
            _letterVectors['S'] = new Vector2[]
            {
                new Vector2(1.0f, 0.2f),    // top right
                new Vector2(0.5f, 0.0f),    // top center
                new Vector2(0.0f, 0.2f),    // top left
                new Vector2(0.0f, 0.4f),    // middle left
                new Vector2(0.5f, 0.5f),    // middle center
                new Vector2(1.0f, 0.6f),    // middle right
                new Vector2(1.0f, 0.8f),    // bottom right
                new Vector2(0.5f, 1.0f),    // bottom center
                new Vector2(0.0f, 0.8f)     // bottom left
            };

            // T
            _letterVectors['T'] = new Vector2[]
            {
                new Vector2(0.0f, 0.0f),    // top left
                new Vector2(1.0f, 0.0f),    // top right
                new Vector2(0.5f, 0.0f),    // top center
                new Vector2(0.5f, 1.0f)     // bottom center
            };

            // E
            _letterVectors['E'] = new Vector2[]
            {
                new Vector2(1.0f, 0.0f),    // top right
                new Vector2(0.0f, 0.0f),    // top left
                new Vector2(0.0f, 1.0f),    // bottom left
                new Vector2(1.0f, 1.0f),    // bottom right
                new Vector2(0.0f, 0.5f),    // middle left
                new Vector2(0.8f, 0.5f)     // middle right
            };

            // R
            _letterVectors['R'] = new Vector2[]
            {
                new Vector2(0.0f, 0.0f),    // top left
                new Vector2(0.8f, 0.0f),    // top right
                new Vector2(1.0f, 0.2f),    // right curve top
                new Vector2(1.0f, 0.4f),    // right curve bottom
                new Vector2(0.8f, 0.5f),    // middle right
                new Vector2(0.0f, 0.5f),    // middle left
                new Vector2(0.0f, 1.0f),    // bottom left
                new Vector2(0.0f, 0.5f),    // back to middle
                new Vector2(0.5f, 0.5f),    // middle center
                new Vector2(1.0f, 1.0f)     // diagonal to bottom right
            };

            // O
            _letterVectors['O'] = new Vector2[]
            {
                new Vector2(0.5f, 0.0f),    // top center
                new Vector2(0.0f, 0.3f),    // top left
                new Vector2(0.0f, 0.7f),    // bottom left
                new Vector2(0.5f, 1.0f),    // bottom center
                new Vector2(1.0f, 0.7f),    // bottom right
                new Vector2(1.0f, 0.3f),    // top right
                new Vector2(0.5f, 0.0f)     // back to top center
            };

            // I
            _letterVectors['I'] = new Vector2[]
            {
                new Vector2(0.0f, 0.0f),    // top left
                new Vector2(1.0f, 0.0f),    // top right
                new Vector2(0.5f, 0.0f),    // top center
                new Vector2(0.5f, 1.0f),    // bottom center
                new Vector2(0.0f, 1.0f),    // bottom left
                new Vector2(1.0f, 1.0f)     // bottom right
            };

            // D
            _letterVectors['D'] = new Vector2[]
            {
                new Vector2(0.0f, 0.0f),    // top left
                new Vector2(0.7f, 0.0f),    // top right
                new Vector2(1.0f, 0.3f),    // curve top
                new Vector2(1.0f, 0.7f),    // curve bottom
                new Vector2(0.7f, 1.0f),    // bottom right
                new Vector2(0.0f, 1.0f),    // bottom left
                new Vector2(0.0f, 0.0f)     // back to top left
            };
        }

        public void Update(float deltaTime)
        {
            // Update pulse animation
            _pulse += deltaTime * PulseSpeed;
            if (_pulse > MathHelper.TwoPi)
            {
                _pulse -= MathHelper.TwoPi;
            }
        }

        public void DrawTitle(string title, Vector2 position, float scale, Color color)
        {
            _position = position;
            _scale = scale * (1.0f + (float)Math.Sin(_pulse) * PulseAmount);
            _color = color;

            float currentX = position.X;
            float letterWidth = scale * 40;
            float letterHeight = scale * 60;

            // Draw each letter
            foreach (char c in title)
            {
                if (_letterVectors.TryGetValue(c, out Vector2[] vectors))
                {
                    DrawLetter(vectors, new Vector2(currentX, position.Y), letterWidth, letterHeight, _color);
                    currentX += letterWidth * _characterSpacing;
                }
                else if (c == ' ')
                {
                    // Space
                    currentX += letterWidth * 0.8f;
                }
            }
        }

        private void DrawLetter(Vector2[] points, Vector2 position, float width, float height, Color color)
        {
            // Some letters have disconnected segments so we need to track when to start a new line
            Vector2? lastPoint = null;

            foreach (Vector2 point in points)
            {
                // Scale from normalized coordinates to pixel coordinates
                _reusableVec1.X = position.X + point.X * width;
                _reusableVec1.Y = position.Y + point.Y * height;

                if (lastPoint.HasValue)
                {
                    if (point.X == -1 && point.Y == -1)
                    {
                        // Special marker for line break
                        lastPoint = null;
                        continue;
                    }

                    // Draw line from last point to current point
                    DrawLine(lastPoint.Value, _reusableVec1, color, 2);
                }

                lastPoint = _reusableVec1;
            }
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 1)
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