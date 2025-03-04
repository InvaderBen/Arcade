using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace AsteroidsGame
{
    public class Player : GameObject
    {
        // Shooting mechanics
        private float _bulletTimer;
        private Texture2D _bulletTexture;

        // Thruster status for rendering effects
        private bool _thrusterActive;

        // Invincibility after death
        private bool _isInvulnerable;
        private float _invulnerabilityTimer;
        private const float InvulnerabilityDuration = 3.0f; // 3 seconds of invincibility after death
        private float _blinkTimer;
        private const float BlinkInterval = 0.1f; // Blink every 0.1 seconds
        private bool _isVisible = true;

        // Death animation
        private bool _isDying;
        private float _deathTimer;
        private const float DeathAnimationDuration = 1.0f;
        private List<Vector2[]> _shipFragments;
        private List<Vector2> _fragmentVelocities;
        private List<float> _fragmentRotations;
        private List<float> _fragmentRotationSpeeds;

        // Controller rumble
        private Action<float, float, float> _rumbleCallback;



        public Player(Vector2 position, Texture2D texture)
            : base(position, Vector2.Zero, texture)
        {
            _bulletTimer = 0;
            _thrusterActive = false;
            _isInvulnerable = false;
            _invulnerabilityTimer = 0;
            _isDying = false;
        }

        // Set the callback for controller rumble
        public void SetRumbleCallback(Action<float, float, float> rumbleCallback)
        {
            _rumbleCallback = rumbleCallback;
        }


        public void HandleInput(InputManager inputManager)
        {
            // Don't process input if dying
            if (_isDying) return;

            // Handle rotation using speed controller
            if (inputManager.IsLeftPressed)
            {
                Rotation -= GameConstants.PlayerRotationSpeed * GameSpeedController.PlayerRotation * 0.01f;
            }

            if (inputManager.IsRightPressed)
            {
                Rotation += GameConstants.PlayerRotationSpeed * GameSpeedController.PlayerRotation * 0.01f;
            }

            // Handle thrust
            _thrusterActive = inputManager.IsUpPressed;

            if (_thrusterActive)
            {
                // Calculate thrust direction based on current rotation
                Vector2 thrustDirection = new Vector2(
                    (float)Math.Sin(Rotation),
                    -(float)Math.Cos(Rotation)
                );

                // Apply thrust force using speed controller
                Velocity += thrustDirection * GameConstants.PlayerThrustForce * GameSpeedController.PlayerMovement * 0.01f;
            }
        }

        public override void Update(float deltaTime)
        {
            // If dying, update death animation
            if (_isDying)
            {
                UpdateDeathAnimation(deltaTime);
                return;
            }

            // Update invulnerability
            if (_isInvulnerable)
            {
                _invulnerabilityTimer -= deltaTime;

                // Update blink effect
                _blinkTimer -= deltaTime;
                if (_blinkTimer <= 0)
                {
                    _isVisible = !_isVisible;
                    _blinkTimer = BlinkInterval;
                }

                // Check if invulnerability is over
                if (_invulnerabilityTimer <= 0)
                {
                    _isInvulnerable = false;
                    _isVisible = true;
                }
            }

            // Apply drag to slow the ship down - no speed controller here since we want consistent drag
            Velocity *= (1 - GameConstants.PlayerDrag * deltaTime);

            // Update bullet cooldown
            if (_bulletTimer > 0)
            {
                _bulletTimer -= deltaTime;
            }

            // Update position
            base.Update(deltaTime);
        }

        public Vector2 GetBulletVelocity()
        {
            // Calculate bullet direction
            Vector2 direction = new Vector2(
                (float)Math.Sin(Rotation),
                -(float)Math.Cos(Rotation)
            );

            // Reset cooldown timer with fire rate controller
            _bulletTimer = GameConstants.BulletCooldown * GameSpeedController.BulletFireRate;

            // Return bullet velocity with speed controller
            return direction * GameConstants.BulletSpeed * GameSpeedController.BulletSpeed;
        }
        // Adjust bullet cooldown time
        public bool CanFire()
        {
            return _bulletTimer <= 0;
        }

        // Return position for a new bullet
        public Vector2 GetBulletPosition()
        {
            // Calculate bullet direction
            Vector2 direction = new Vector2(
                (float)Math.Sin(Rotation),
                -(float)Math.Cos(Rotation)
            );

            // Calculate bullet spawn position (in front of the ship)
            return Position + direction * 15;
        }

        public void SetBulletTexture(Texture2D bulletTexture)
        {
            _bulletTexture = bulletTexture;
        }

        public bool IsThrusterActive()
        {
            return _thrusterActive && !_isDying;
        }

        public bool IsInvulnerable()
        {
            return _isInvulnerable || _isDying;
        }

        public bool IsVisible()
        {
            return _isVisible && !_isDying;
        }

        public bool IsDying()
        {
            return _isDying;
        }

        public void StartDeathAnimation()
        {
            _isDying = true;
            _deathTimer = DeathAnimationDuration;

            // Create ship fragments
            CreateShipFragments();

            // Trigger controller rumble
            if (_rumbleCallback != null)
            {
                _rumbleCallback(0.5f, 0.5f, DeathAnimationDuration);
            }
        }

        private void CreateShipFragments()
        {
            // Create 4-6 fragments
            int numFragments = new Random().Next(4, 7);
            _shipFragments = new List<Vector2[]>();
            _fragmentVelocities = new List<Vector2>();
            _fragmentRotations = new List<float>();
            _fragmentRotationSpeeds = new List<float>();

            for (int i = 0; i < numFragments; i++)
            {
                // Create a random triangular fragment
                _shipFragments.Add(CreateRandomFragment());

                // Create a random velocity for each fragment
                float angle = (float)new Random().NextDouble() * MathHelper.TwoPi;
                float speed = 50f + (float)new Random().NextDouble() * 100f;
                _fragmentVelocities.Add(new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed
                ));

                // Create random rotation for each fragment
                _fragmentRotations.Add(Rotation);
                _fragmentRotationSpeeds.Add((float)(new Random().NextDouble() - 0.5) * 10.0f);
            }
        }

        private Vector2[] CreateRandomFragment()
        {
            Random random = new Random();

            // Create a random triangular fragment
            return new Vector2[]
            {
                new Vector2(random.Next(-10, 10), random.Next(-10, 10)),
                new Vector2(random.Next(-10, 10), random.Next(-10, 10)),
                new Vector2(random.Next(-10, 10), random.Next(-10, 10)),
                new Vector2(random.Next(-10, 10), random.Next(-10, 10)) // Close the shape
            };
        }

        private void UpdateDeathAnimation(float deltaTime)
        {
            _deathTimer -= deltaTime;

            // Update fragment positions and rotations
            for (int i = 0; i < _shipFragments.Count; i++)
            {
                // Update position
                Vector2 newPos = Position + _fragmentVelocities[i] * (DeathAnimationDuration - _deathTimer);

                // Update rotation
                _fragmentRotations[i] += _fragmentRotationSpeeds[i] * deltaTime;
            }

            // Check if animation is complete
            if (_deathTimer <= 0)
            {
                _isDying = false;
                Reset(new Vector2(GameConstants.ScreenWidth / 2, GameConstants.ScreenHeight / 2));
            }
        }

        public void Reset(Vector2 position)
        {
            Position = position;
            Velocity = Vector2.Zero;
            Rotation = 0;
            _bulletTimer = 0;
            _thrusterActive = false;
            _isInvulnerable = true;
            _invulnerabilityTimer = InvulnerabilityDuration;
            _blinkTimer = BlinkInterval;
            _isVisible = true;
            _isDying = false;
        }

        public List<Vector2[]> GetDeathFragments()
        {
            return _shipFragments;
        }

        public List<float> GetFragmentRotations()
        {
            return _fragmentRotations;
        }
    }
}