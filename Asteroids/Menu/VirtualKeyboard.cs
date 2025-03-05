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
        private GamePadState _prevGamePadState;
        private KeyboardState _prevKeyboardState;
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

            // Initialize input states
            _prevGamePadState = GamePad.GetState(PlayerIndex.One);
            _prevKeyboardState = Keyboard.GetState();
        }

        public void Update(GameTime gameTime, InputManager inputManager)
        {
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

            // Get current states
            GamePadState currentPad = GamePad.GetState(PlayerIndex.One);
            KeyboardState currentKeys = Keyboard.GetState();

            // Handle directional input
            bool moved = false;

            // Up
            if (IsNewButtonPress(Buttons.DPadUp, currentPad) ||
                currentPad.ThumbSticks.Left.Y > 0.5f ||
                IsNewKeyPress(Keys.Up, currentKeys))
            {
                _selectedRow = Math.Max(0, _selectedRow - 1);
                _selectedCol = Math.Min(_selectedCol, _keyboardLayout[_selectedRow].Length - 1);
                moved = true;
            }
            // Down
            else if (IsNewButtonPress(Buttons.DPadDown, currentPad) ||
                     currentPad.ThumbSticks.Left.Y < -0.5f ||
                     IsNewKeyPress(Keys.Down, currentKeys))
            {
                _selectedRow = Math.Min(_keyboardLayout.Length - 1, _selectedRow + 1);
                _selectedCol = Math.Min(_selectedCol, _keyboardLayout[_selectedRow].Length - 1);
                moved = true;
            }
            // Left
            else if (IsNewButtonPress(Buttons.DPadLeft, currentPad) ||
                     currentPad.ThumbSticks.Left.X < -0.5f ||
                     IsNewKeyPress(Keys.Left, currentKeys))
            {
                _selectedCol = Math.Max(0, _selectedCol - 1);
                moved = true;
            }
            // Right
            else if (IsNewButtonPress(Buttons.DPadRight, currentPad) ||
                     currentPad.ThumbSticks.Left.X > 0.5f ||
                     IsNewKeyPress(Keys.Right, currentKeys))
            {
                _selectedCol = Math.Min(_keyboardLayout[_selectedRow].Length - 1, _selectedCol + 1);
                moved = true;
            }

            if (moved)
            {
                _inputTimer = _inputDelay;
            }

            // Handle key selection
            if (IsNewButtonPress(Buttons.A, currentPad) || IsNewKeyPress(Keys.Enter, currentKeys))
            {
                string selectedKey = GetSelectedKeyText();
                OnKeyPress?.Invoke(selectedKey);
                _inputTimer = _inputDelay;
            }

            // Handle special buttons for common actions
            if (IsNewButtonPress(Buttons.X, currentPad))
            {
                OnKeyPress?.Invoke(DEL_KEY);
                _inputTimer = _inputDelay;
            }

            if (IsNewButtonPress(Buttons.Y, currentPad))
            {
                OnKeyPress?.Invoke(SPACE_KEY);
                _inputTimer = _inputDelay;
            }

            if (IsNewButtonPress(Buttons.RightTrigger, currentPad, 0.5f))
            {
                OnKeyPress?.Invoke(DONE_KEY);
                _inputTimer = _inputDelay;
            }

            // Store current states
            _prevGamePadState = currentPad;
            _prevKeyboardState = currentKeys;
        }

        private bool IsNewButtonPress(Buttons button, GamePadState current)
        {
            return current.IsButtonDown(button) && !_prevGamePadState.IsButtonDown(button);
        }

        private bool IsNewButtonPress(Buttons button, GamePadState current, float threshold)
        {
            if (button == Buttons.LeftTrigger)
            {
                return current.Triggers.Left > threshold && _prevGamePadState.Triggers.Left <= threshold;
            }
            else if (button == Buttons.RightTrigger)
            {
                return current.Triggers.Right > threshold && _prevGamePadState.Triggers.Right <= threshold;
            }
            else
            {
                return current.IsButtonDown(button) && !_prevGamePadState.IsButtonDown(button);
            }
        }

        private bool IsNewKeyPress(Keys key, KeyboardState current)
        {
            return current.IsKeyDown(key) && !_prevKeyboardState.IsKeyDown(key);
        }

        private string GetSelectedKeyText()
        {
            if (_selectedRow < _keyboardLayout.Length)
            {
                string row = _keyboardLayout[_selectedRow];

                // Handle special keys in bottom row
                if (_selectedRow == _keyboardLayout.Length - 1)
                {
                    int pos = 0;

                    // Check for SPACE
                    if (_selectedCol >= pos && _selectedCol < pos + SPACE_KEY.Length)
                    {
                        return SPACE_KEY;
                    }

                    pos += SPACE_KEY.Length + 1;

                    // Check for DEL
                    if (_selectedCol >= pos && _selectedCol < pos + DEL_KEY.Length)
                    {
                        return DEL_KEY;
                    }

                    pos += DEL_KEY.Length + 1;

                    // Check for DONE
                    if (_selectedCol >= pos && _selectedCol < pos + DONE_KEY.Length)
                    {
                        return DONE_KEY;
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

            // Draw input instructions
            DrawInstructions(spriteBatch, y + 20);
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

        private void DrawInstructions(SpriteBatch spriteBatch, float y)
        {
            float centerX = _position.X + 300;

            // Title
            spriteBatch.DrawString(
                _font,
                "CONTROLLER CONTROLS",
                new Vector2(centerX - 100, y),
                Color.White
            );

            y += 30;

            // Draw controls
            DrawInstruction(spriteBatch, "A Button", "Select Key", centerX - 150, y);
            DrawInstruction(spriteBatch, "X Button", "Delete", centerX + 50, y);

            y += 25;

            DrawInstruction(spriteBatch, "Y Button", "Space", centerX - 150, y);
            DrawInstruction(spriteBatch, "RT", "Confirm", centerX + 50, y);

            y += 25;

            DrawInstruction(spriteBatch, "B Button", "Cancel", centerX - 150, y);
            DrawInstruction(spriteBatch, "D-Pad/Stick", "Navigate", centerX + 50, y);
        }

        private void DrawInstruction(SpriteBatch spriteBatch, string button, string action, float x, float y)
        {
            spriteBatch.DrawString(_font, button + ": ", new Vector2(x, y), Color.Yellow);
            spriteBatch.DrawString(_font, action, new Vector2(x + 80, y), Color.White);
        }
    }
}