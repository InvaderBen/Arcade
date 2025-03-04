using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace AsteroidsGame
{
    public class EnemyShip : GameObject
    {
        // Movement constants
        private const float HorizontalSpeed = 120.0f;
        private const float VerticalOffset = 40.0f;  // Distance from top of screen
        private const float FireRate = 1.5f;         // Shots per second

        // State tracking
        private bool _movingRight = true;
        private float _fireTimer;
        private readonly Random _random;

        // Reference to the player for targeting
        private Player _player;

        // Texture to use for bullets
        private Texture2D _bulletTexture;

        // Add health properties
        private int _health = 3; // Default health
        private float _hitFlashTimer = 0f;
        private const float HitFlashDuration = 0.2f;

        // Method to take damage and return true if destroyed
        public bool TakeDamage()
        {
            _health--;
            _hitFlashTimer = HitFlashDuration;

            return _health <= 0; // Return true if destroyed
        }

        // Property to check if ship is currently flashing from being hit
        public bool IsHitFlashing => _hitFlashTimer > 0;


        // Callback for firing bullets
        private Action<Vector2, Vector2> _fireBulletCallback;

        public EnemyShip(Vector2 position, Texture2D texture, Player player, Texture2D bulletTexture, Action<Vector2, Vector2> fireBulletCallback)
            : base(position, Vector2.Zero, texture)
        {
            _random = new Random();
            _fireTimer = 0f;
            _player = player;
            _bulletTexture = bulletTexture;
            _fireBulletCallback = fireBulletCallback;

            // Set initial vertical position near the top of the screen
            Position = new Vector2(position.X, VerticalOffset);

            // Set initial horizontal velocity
            Velocity = new Vector2(HorizontalSpeed * GameSpeedController.EnemySpeed, 0);
        }

        public override void Update(float deltaTime)
        {
            // Update fire timer
            _fireTimer -= deltaTime;
            if (_fireTimer <= 0)
            {
                FireBullet();
                _fireTimer = FireRate / GameSpeedController.EnemySpeed;  // Adjust fire rate with game speed
            }

            // Update position
            base.Update(deltaTime);

            // Check for screen edges and reverse direction
            if (_movingRight && Position.X > GameConstants.ScreenWidth - 50)
            {
                _movingRight = false;
                Velocity = new Vector2(-HorizontalSpeed * GameSpeedController.EnemySpeed, 0);
            }
            else if (!_movingRight && Position.X < 50)
            {
                _movingRight = true;
                Velocity = new Vector2(HorizontalSpeed * GameSpeedController.EnemySpeed, 0);
            }

            // Update hit flash timer
            if (_hitFlashTimer > 0)
            {
                _hitFlashTimer -= deltaTime;
            }

            // Keep vertical position fixed
            Position = new Vector2(Position.X, VerticalOffset);
        }

        private void FireBullet()
        {
            if (_fireBulletCallback != null)
            {
                // Always fire downward
                Vector2 direction = new Vector2(0, 1);

                // Add slight randomness to aim
                if (_random.NextDouble() < 0.7f)  // 70% chance to aim at player
                {
                    // Calculate direction toward player with slight randomness
                    float playerRelativeX = _player.Position.X - Position.X;
                    float randomOffset = (float)(_random.NextDouble() * 60 - 30);  // ±30 pixels

                    direction = Vector2.Normalize(
                        new Vector2(playerRelativeX + randomOffset, GameConstants.ScreenHeight)
                    );
                }

                // Fire the bullet
                _fireBulletCallback(
                    new Vector2(Position.X, Position.Y + 20),  // Spawn bullet below the ship
                    direction * GameConstants.BulletSpeed * 0.7f * GameSpeedController.EnemySpeed
                );
            }
        }
    }
}