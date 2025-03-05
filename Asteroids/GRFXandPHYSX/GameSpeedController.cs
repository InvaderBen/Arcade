using System;

namespace AsteroidsGame
{
    /// <summary>
    /// Centralized controller for game speed adjustments
    /// </summary>
    public static class GameSpeedController
    {
        // Master speed multiplier - adjust this to change overall game speed
        private static float _masterSpeedMultiplier = 1f; // 0.5 = half speed, 1.0 = normal speed, 2.0 = double speed

        // Individual system multipliers (all relative to master speed)
        private static float _playerMovementMultiplier = 1.5f;
        private static float _playerRotationMultiplier = 1.0f;
        private static float _bulletSpeedMultiplier = 1.0f;
        private static float _bulletFireRateMultiplier = 0.5f;
        private static float _asteroidSpeedMultiplier = 1.0f;
        private static float _enemySpeedMultiplier = 1.0f;

        // Public getters for speed multipliers
        public static float MasterSpeed => _masterSpeedMultiplier;

        // Calculated multipliers for various systems
        public static float PlayerMovement => _masterSpeedMultiplier * _playerMovementMultiplier;
        public static float PlayerRotation => _masterSpeedMultiplier * _playerRotationMultiplier;
        public static float BulletSpeed => _masterSpeedMultiplier * _bulletSpeedMultiplier;
        public static float BulletFireRate => 1.0f / (_masterSpeedMultiplier * _bulletFireRateMultiplier); // Inverted for cooldown
        public static float AsteroidSpeed => _masterSpeedMultiplier * _asteroidSpeedMultiplier;
        public static float EnemySpeed => _masterSpeedMultiplier * _enemySpeedMultiplier;

        // Methods to adjust individual systems
        public static void SetMasterSpeed(float multiplier)
        {
            _masterSpeedMultiplier = Math.Max(0.1f, Math.Min(multiplier, 2.0f)); // Clamp between 0.1 and 2.0
        }

        public static void SetPlayerMovementMultiplier(float multiplier)
        {
            _playerMovementMultiplier = Math.Max(0.1f, multiplier);
        }

        public static void SetPlayerRotationMultiplier(float multiplier)
        {
            _playerRotationMultiplier = Math.Max(0.1f, multiplier);
        }

        public static void SetBulletSpeedMultiplier(float multiplier)
        {
            _bulletSpeedMultiplier = Math.Max(0.1f, multiplier);
        }

        public static void SetBulletFireRateMultiplier(float multiplier)
        {
            _bulletFireRateMultiplier = Math.Max(0.1f, multiplier);
        }

        public static void SetAsteroidSpeedMultiplier(float multiplier)
        {
            _asteroidSpeedMultiplier = Math.Max(0.1f, multiplier);
        }

        public static void SetEnemySpeedMultiplier(float multiplier)
        {
            _enemySpeedMultiplier = Math.Max(0.1f, multiplier);
        }

        // Reset all multipliers to default
        public static void ResetToDefaults()
        {
            _masterSpeedMultiplier = 0.5f;
            _playerMovementMultiplier = 1.0f;
            _playerRotationMultiplier = 1.0f;
            _bulletSpeedMultiplier = 1.0f;
            _bulletFireRateMultiplier = 1.0f;
            _asteroidSpeedMultiplier = 1.0f;
            _enemySpeedMultiplier = 1.0f;
        }
    }
}