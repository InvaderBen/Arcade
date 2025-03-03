using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;

namespace AsteroidsGame
{
    public class InputManager
    {
        // Keyboard state
        private KeyboardState _currentKeyboardState;
        private KeyboardState _previousKeyboardState;

        // Gamepad state
        private GamePadState _currentGamePadState;
        private GamePadState _previousGamePadState;

        // Input state properties
        public bool IsLeftPressed { get; private set; }
        public bool IsRightPressed { get; private set; }
        public bool IsUpPressed { get; private set; }
        public bool IsFireButtonPressed { get; private set; }
        public bool IsRestartPressed { get; private set; }
        public bool IsEnterPressed { get; private set; }

        // Deadzone for thumbstick input
        private const float ThumbstickDeadzone = 0.25f;

        public InputManager()
        {
            _currentKeyboardState = Keyboard.GetState();
            _previousKeyboardState = _currentKeyboardState;

            _currentGamePadState = GamePad.GetState(PlayerIndex.One);
            _previousGamePadState = _currentGamePadState;
        }

        public void Update()
        {
            // Store previous state
            _previousKeyboardState = _currentKeyboardState;
            _previousGamePadState = _currentGamePadState;

            // Get current state
            _currentKeyboardState = Keyboard.GetState();
            _currentGamePadState = GamePad.GetState(PlayerIndex.One);

            // Update button states combining keyboard and gamepad input

            // Left movement (left arrow, A key, or left on thumbstick/dpad)
            IsLeftPressed = _currentKeyboardState.IsKeyDown(Keys.Left) ||
                           _currentKeyboardState.IsKeyDown(Keys.A) ||
                           _currentGamePadState.DPad.Left == ButtonState.Pressed ||
                           _currentGamePadState.ThumbSticks.Left.X < -ThumbstickDeadzone;

            // Right movement (right arrow, D key, or right on thumbstick/dpad)
            IsRightPressed = _currentKeyboardState.IsKeyDown(Keys.Right) ||
                            _currentKeyboardState.IsKeyDown(Keys.D) ||
                            _currentGamePadState.DPad.Right == ButtonState.Pressed ||
                            _currentGamePadState.ThumbSticks.Left.X > ThumbstickDeadzone;

            // Thrust (up arrow, W key, or A button/RT)
            IsUpPressed = _currentKeyboardState.IsKeyDown(Keys.Up) ||
                         _currentKeyboardState.IsKeyDown(Keys.W) ||
                         _currentGamePadState.Buttons.A == ButtonState.Pressed ||
                         _currentGamePadState.Triggers.Right > 0.5f;

            // Fire (space, X key, or X button/RB)
            IsFireButtonPressed = _currentKeyboardState.IsKeyDown(Keys.Space) ||
                                 _currentKeyboardState.IsKeyDown(Keys.X) ||
                                 _currentGamePadState.Buttons.X == ButtonState.Pressed ||
                                 _currentGamePadState.Buttons.RightShoulder == ButtonState.Pressed;

            // Restart (Enter key, Y button)
            IsRestartPressed = _currentKeyboardState.IsKeyDown(Keys.Enter) ||
                              _currentGamePadState.Buttons.Y == ButtonState.Pressed ||
                              _currentGamePadState.Buttons.Start == ButtonState.Pressed;

            // Enter (Enter key, A button)
            IsEnterPressed = _currentKeyboardState.IsKeyDown(Keys.Enter) ||
                            _currentGamePadState.Buttons.A == ButtonState.Pressed;
        }

        public bool IsExitRequested()
        {
            // Only exit on Escape when there's no way to return to a previous screen
            // For example, in the main menu but nowhere else
            return (_currentKeyboardState.IsKeyDown(Keys.Escape) &&
                    _previousKeyboardState.IsKeyUp(Keys.Escape) &&
                    // Add additional condition to check state or add property for current state
                    _inMainMenu) ||
                   (_currentGamePadState.Buttons.Back == ButtonState.Pressed &&
                    _previousGamePadState.Buttons.Back == ButtonState.Released &&
                    _inMainMenu);
        }

        // Add this property to InputManager class to track when we're in the main menu
        private bool _inMainMenu = true;

        // Method to set main menu status
        public void SetInMainMenu(bool inMainMenu)
        {
            _inMainMenu = inMainMenu;
        }

        public bool IsFirePressed()
        {
            return IsFireButtonPressed;
        }

        public bool IsRestartButtonPressed()
        {
            // Check if restart button was just pressed this frame (not held down)
            bool keyboardRestart = _currentKeyboardState.IsKeyDown(Keys.Enter) &&
                                  !_previousKeyboardState.IsKeyDown(Keys.Enter);

            bool gamepadRestart = (_currentGamePadState.Buttons.Y == ButtonState.Pressed &&
                                  _previousGamePadState.Buttons.Y == ButtonState.Released) ||
                                 (_currentGamePadState.Buttons.Start == ButtonState.Pressed &&
                                  _previousGamePadState.Buttons.Start == ButtonState.Released);

            return keyboardRestart || gamepadRestart;
        }

        public bool IsMenuConfirmPressed()
        {
            // Check if enter/A button was just pressed this frame (not held down)
            bool keyboardConfirm = _currentKeyboardState.IsKeyDown(Keys.Enter) &&
                                  !_previousKeyboardState.IsKeyDown(Keys.Enter);

            bool gamepadConfirm = (_currentGamePadState.Buttons.A == ButtonState.Pressed &&
                                  _previousGamePadState.Buttons.A == ButtonState.Released) ||
                                 (_currentGamePadState.Buttons.Start == ButtonState.Pressed &&
                                  _previousGamePadState.Buttons.Start == ButtonState.Released);

            return keyboardConfirm || gamepadConfirm;
        }

        // Detect if a key was just pressed this frame
        public bool IsKeyPressed(Keys key)
        {
            return _currentKeyboardState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
        }

        // Detect if a button was just pressed this frame
        public bool IsButtonPressed(Buttons button)
        {
            return _currentGamePadState.IsButtonDown(button) && !_previousGamePadState.IsButtonDown(button);
        }

        // Detect if a key was just released this frame
        public bool IsKeyReleased(Keys key)
        {
            return !_currentKeyboardState.IsKeyDown(key) && _previousKeyboardState.IsKeyDown(key);
        }

        // Detect if a button was just released this frame
        public bool IsButtonReleased(Buttons button)
        {
            return !_currentGamePadState.IsButtonDown(button) && _previousGamePadState.IsButtonDown(button);
        }

        // Returns all keys that were just pressed this frame
        public Keys[] GetPressedKeys()
        {
            List<Keys> pressedKeys = new List<Keys>();
            Keys[] allKeys = (Keys[])Enum.GetValues(typeof(Keys));

            foreach (Keys key in allKeys)
            {
                if (IsKeyPressed(key))
                {
                    pressedKeys.Add(key);
                }
            }

            return pressedKeys.ToArray();
        }

        // Helper method to determine if a key is a letter or number
        public bool IsLetterOrDigit(Keys key)
        {
            return (key >= Keys.A && key <= Keys.Z) ||
                   (key >= Keys.D0 && key <= Keys.D9) ||
                   (key >= Keys.NumPad0 && key <= Keys.NumPad9);
        }

        // Convert key to character
        public char KeyToChar(Keys key)
        {
            if (key >= Keys.A && key <= Keys.Z)
            {
                // Check shift for uppercase/lowercase
                return _currentKeyboardState.IsKeyDown(Keys.LeftShift) || _currentKeyboardState.IsKeyDown(Keys.RightShift)
                    ? (char)('A' + (key - Keys.A))
                    : (char)('a' + (key - Keys.A));
            }
            else if (key >= Keys.D0 && key <= Keys.D9)
            {
                return (char)('0' + (key - Keys.D0));
            }
            else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                return (char)('0' + (key - Keys.NumPad0));
            }

            // Special characters
            switch (key)
            {
                case Keys.Space: return ' ';
                case Keys.OemMinus: return '-';
                case Keys.OemPeriod: return '.';
                case Keys.OemComma: return ',';
                case Keys.OemQuestion: return '?';
                case Keys.OemPlus: return '+';
                default: return '\0'; // Null character for unsupported keys
            }
        }



    }


}