using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace AsteroidsGame
{
    public class Player : GameObject
    {
        // Shooting mechanics
        private float _bulletTimer;
        private Texture2D _bulletTexture;

        // Thruster status for rendering effects
        private bool _thrusterActive;

        public Player(Vector2 position, Texture2D texture)
            : base(position, Vector2.Zero, texture)
        {
            _bulletTimer = 0;
            _thrusterActive = false;
        }

        public void HandleInput(InputManager inputManager)
        {
            // Handle rotation
            if (inputManager.IsLeftPressed)
            {
                Rotation -= GameConstants.PlayerRotationSpeed * 0.05f;
            }

            if (inputManager.IsRightPressed)
            {
                Rotation += GameConstants.PlayerRotationSpeed * 0.05f;
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

                // Apply thrust force
                Velocity += thrustDirection * GameConstants.PlayerThrustForce * 0.05f;
            }
        }

        public override void Update(float deltaTime)
        {
            // Apply drag to slow the ship down
            Velocity *= (1 - GameConstants.PlayerDrag * deltaTime);

            // Update bullet cooldown
            if (_bulletTimer > 0)
            {
                _bulletTimer -= deltaTime;
            }

            // Update position
            base.Update(deltaTime);
        }

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

        // Return velocity for a new bullet
        public Vector2 GetBulletVelocity()
        {
            // Calculate bullet direction
            Vector2 direction = new Vector2(
                (float)Math.Sin(Rotation),
                -(float)Math.Cos(Rotation)
            );

            // Reset cooldown timer
            _bulletTimer = GameConstants.BulletCooldown;

            // Return bullet velocity
            return direction * GameConstants.BulletSpeed;
        }

        public void SetBulletTexture(Texture2D bulletTexture)
        {
            _bulletTexture = bulletTexture;
        }

        public bool IsThrusterActive()
        {
            return _thrusterActive;
        }

        public void Reset(Vector2 position)
        {
            Position = position;
            Velocity = Vector2.Zero;
            Rotation = 0;
            _bulletTimer = 0;
            _thrusterActive = false;
        }
    }
}