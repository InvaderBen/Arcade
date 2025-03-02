using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SpaceInvaders
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
        public bool IsFireButtonPressed { get; private set; }

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

            // Fire (space, X key, or X button/A button)
            IsFireButtonPressed = _currentKeyboardState.IsKeyDown(Keys.Space) ||
                                 _currentKeyboardState.IsKeyDown(Keys.X) ||
                                 _currentGamePadState.Buttons.X == ButtonState.Pressed ||
                                 _currentGamePadState.Buttons.A == ButtonState.Pressed;
        }

        public bool IsExitRequested()
        {
            return _currentKeyboardState.IsKeyDown(Keys.Escape) ||
                   _currentGamePadState.Buttons.Back == ButtonState.Pressed;
        }

        public bool IsRestartButtonPressed()
        {
            // Check if restart button was just pressed this frame (not held down)
            bool keyboardRestart = _currentKeyboardState.IsKeyDown(Keys.Enter) &&
                                  !_previousKeyboardState.IsKeyDown(Keys.Enter);

            bool gamepadRestart = (_currentGamePadState.Buttons.Start == ButtonState.Pressed &&
                                  _previousGamePadState.Buttons.Start == ButtonState.Released);

            return keyboardRestart || gamepadRestart;
        }
    }
}