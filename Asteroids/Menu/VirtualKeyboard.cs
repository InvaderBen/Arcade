using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AsteroidsGame
{
    /// <summary>
    /// Simple virtual keyboard for gamepad users to enter text
    /// </summary>
    public class VirtualKeyboard
    {
        // Keyboard layout
        private static readonly string[] _keyboardLayout = new string[]
        {
            "1234567890",
            "QWERTYUIOP",
            "ASDFGHJKL",
            "ZXCVBNM",
            "SPACE DEL DONE"
        };

        // Enabled state - can be toggled
        public bool IsEnabled { get; private set; } = true;

        // Selected key position
        private int _selectedRow = 1;
        private int _selectedCol = 0;

        // Visual properties
        private readonly Vector2 _position;
        private readonly int _keyWidth = 50;
        private readonly int _keyHeight = 50;
        private readonly int _keyMargin = 5;
        private readonly Color _keyColor = new Color(50, 50, 70);
        private readonly Color _selectedKeyColor = new Color(100, 100, 200);
        private readonly Color _textColor = Color.White;
        private readonly Color _selectedTextColor = Color.Yellow;

        // Special keys
        private const string SPACE_KEY = "SPACE";
        private const string DEL_KEY = "DEL";
        private const string DONE_KEY = "DONE";

        // Input handling
        private float _inputDelay = 0.2f;
        private float _inputTimer = 0f;

        // Event to notify when a key is pressed
        public delegate void KeyPressHandler(string key);
        public event KeyPressHandler OnKeyPress;

        // Animation
        private float _pulseTime = 0f;
        private const float _pulseDuration = 0.8f;

        private SpriteFont _font;
        private Texture2D _pixel;

        public VirtualKeyboard(GraphicsDevice graphicsDevice, SpriteFont font, Vector2 position)
        {
            _position = position;
            _font = font;

            // Create a pixel for drawing
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        public void Update(GameTime gameTime, InputManager inputManager)
        {
            if (!IsEnabled)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update animation
            _pulseTime += deltaTime;
            if (_pulseTime > _pulseDuration) _pulseTime = 0f;

            // Handle input delay
            if (_inputTimer > 0)
            {
                _inputTimer -= deltaTime;
                return;
            }

            // Handle directional input
            bool moved = false;

            // Up
            if (inputManager.IsKeyPressed(Keys.Up) ||
                inputManager.IsButtonPressed(Buttons.DPadUp) ||
                inputManager.IsThumbstickUp())
            {
                _selectedRow = Math.Max(0, _selectedRow - 1);
                _selectedCol = Math.Min(_selectedCol, _keyboardLayout[_selectedRow].Length - 1);
                moved = true;
            }
            // Down
            else if (inputManager.IsKeyPressed(Keys.Down) ||
                     inputManager.IsButtonPressed(Buttons.DPadDown) ||
                     inputManager.IsThumbstickDown())
            {
                _selectedRow = Math.Min(_keyboardLayout.Length - 1, _selectedRow + 1);
                _selectedCol = Math.Min(_selectedCol, _keyboardLayout[_selectedRow].Length - 1);
                moved = true;
            }
            // Left
            if (inputManager.IsKeyPressed(Keys.Left) ||
                inputManager.IsButtonPressed(Buttons.DPadLeft) ||
                inputManager.IsThumbstickLeft())
            {
                _selectedCol = Math.Max(0, _selectedCol - 1);
                moved = true;
            }
            // Right
            else if (inputManager.IsKeyPressed(Keys.Right) ||
                     inputManager.IsButtonPressed(Buttons.DPadRight) ||
                     inputManager.IsThumbstickRight())
            {
                _selectedCol = Math.Min(_keyboardLayout[_selectedRow].Length - 1, _selectedCol + 1);
                moved = true;
            }

            if (moved)
            {
                _inputTimer = _inputDelay;
            }

            // Handle key selection (A button or Enter)
            if (inputManager.IsKeyPressed(Keys.Enter) || inputManager.IsButtonPressed(Buttons.A))
            {
                string selectedKey = GetSelectedKeyText();
                Console.WriteLine($"Selected key: {selectedKey}"); // Debug output
                OnKeyPress?.Invoke(selectedKey);
                _inputTimer = _inputDelay;
            }

            // Handle special buttons (separate from selection)
            if (inputManager.IsKeyPressed(Keys.Back) || inputManager.IsButtonPressed(Buttons.X))
            {
                OnKeyPress?.Invoke("DEL");
                _inputTimer = _inputDelay;
            }

            if (inputManager.IsKeyPressed(Keys.Space) || inputManager.IsButtonPressed(Buttons.Y))
            {
                OnKeyPress?.Invoke("SPACE");
                _inputTimer = _inputDelay;
            }

            // DONE can be activated with trigger buttons independently
            GamePadState padState = GamePad.GetState(PlayerIndex.One);
            if (inputManager.IsButtonPressed(Buttons.RightTrigger) ||
                inputManager.IsButtonPressed(Buttons.LeftTrigger) ||
                (padState.Triggers.Left > 0.5f && padState.Triggers.Right > 0.5f))
            {
                OnKeyPress?.Invoke("DONE");
                _inputTimer = _inputDelay;
            }
        }

        public void Toggle()
        {
            IsEnabled = !IsEnabled;

            // Reset selection position when re-enabling
            if (IsEnabled)
            {
                _selectedRow = 1;
                _selectedCol = 0;
                _inputTimer = 0f; // Reset input timer to prevent accidental keypresses
            }
        }

        private string GetSelectedKeyText()
        {
            if (_selectedRow < _keyboardLayout.Length)
            {
                string row = _keyboardLayout[_selectedRow];

                // Handle special keys in bottom row
                if (_selectedRow == _keyboardLayout.Length - 1)
                {
                    // Determine what section of the bottom row we're in
                    string bottomRow = _keyboardLayout[_keyboardLayout.Length - 1];
                    int spaceEndPos = "SPACE".Length;
                    int delStartPos = spaceEndPos + 1;
                    int delEndPos = delStartPos + "DEL".Length;
                    int doneStartPos = delEndPos + 1;

                    // Check for SPACE (first section)
                    if (_selectedCol < spaceEndPos)
                    {
                        return "SPACE";
                    }
                    // Check for DEL (second section)
                    else if (_selectedCol >= delStartPos && _selectedCol < delEndPos)
                    {
                        return "DEL";
                    }
                    // Check for DONE (third section)
                    else if (_selectedCol >= doneStartPos)
                    {
                        return "DONE";
                    }
                    return "";
                }
                else if (_selectedCol < row.Length)
                {
                    return row[_selectedCol].ToString();
                }
            }

            return "";
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsEnabled)
                return;

            float pulse = (float)Math.Sin(_pulseTime / _pulseDuration * MathHelper.TwoPi) * 0.2f + 1.0f;
            float y = _position.Y;

            // Draw each row
            for (int row = 0; row < _keyboardLayout.Length; row++)
            {
                float x = _position.X;
                string currentRow = _keyboardLayout[row];

                if (row == _keyboardLayout.Length - 1)
                {
                    // Draw special keys row
                    int spaceWidth = SPACE_KEY.Length * _keyWidth;
                    int delWidth = DEL_KEY.Length * _keyWidth;
                    int doneWidth = DONE_KEY.Length * _keyWidth;

                    // Draw SPACE key
                    bool spaceSelected = (row == _selectedRow && _selectedCol < SPACE_KEY.Length);
                    DrawKey(spriteBatch, x, y, spaceWidth, _keyHeight, SPACE_KEY, spaceSelected ? pulse : 1.0f);
                    x += spaceWidth + _keyMargin;

                    // Draw DEL key
                    bool delSelected = (row == _selectedRow && _selectedCol >= SPACE_KEY.Length && _selectedCol < SPACE_KEY.Length + DEL_KEY.Length);
                    DrawKey(spriteBatch, x, y, delWidth, _keyHeight, DEL_KEY, delSelected ? pulse : 1.0f);
                    x += delWidth + _keyMargin;

                    // Draw DONE key
                    bool doneSelected = (row == _selectedRow && _selectedCol >= SPACE_KEY.Length + DEL_KEY.Length);
                    DrawKey(spriteBatch, x, y, doneWidth, _keyHeight, DONE_KEY, doneSelected ? pulse : 1.0f);
                }
                else
                {
                    // Draw regular keys
                    for (int col = 0; col < currentRow.Length; col++)
                    {
                        bool selected = (row == _selectedRow && col == _selectedCol);
                        DrawKey(spriteBatch, x, y, _keyWidth, _keyHeight, currentRow[col].ToString(), selected ? pulse : 1.0f);
                        x += _keyWidth + _keyMargin;
                    }
                }

                y += _keyHeight + _keyMargin;
            }
        }

        private void DrawKey(SpriteBatch spriteBatch, float x, float y, float width, float height, string text, float scale)
        {
            bool selected = scale > 1.0f;

            // Apply scaling
            if (selected)
            {
                float offsetX = (width * scale - width) / 2;
                float offsetY = (height * scale - height) / 2;
                x -= offsetX;
                y -= offsetY;
                width *= scale;
                height *= scale;
            }

            // Draw border
            Color borderColor = selected ? Color.Yellow : new Color(80, 80, 100);
            spriteBatch.Draw(_pixel, new Rectangle((int)x - 2, (int)y - 2, (int)width + 4, (int)height + 4), borderColor);

            // Draw background
            Color keyBgColor = selected ? _selectedKeyColor : _keyColor;
            spriteBatch.Draw(_pixel, new Rectangle((int)x, (int)y, (int)width, (int)height), keyBgColor);

            // Determine text color
            Color textColor = _textColor;
            if (selected)
            {
                textColor = _selectedTextColor;
            }
            else if (text == DEL_KEY)
            {
                textColor = new Color(255, 150, 150);
            }
            else if (text == DONE_KEY)
            {
                textColor = new Color(150, 255, 150);
            }
            else if (text == SPACE_KEY)
            {
                textColor = new Color(150, 150, 255);
            }

            // Draw text
            Vector2 textSize = _font.MeasureString(text);
            Vector2 textPos = new Vector2(
                x + (width - textSize.X) / 2,
                y + (height - textSize.Y) / 2
            );
            spriteBatch.DrawString(_font, text, textPos, textColor);
        }
        }



}