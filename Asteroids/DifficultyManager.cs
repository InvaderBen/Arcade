using System;

namespace AsteroidsGame
{
    public class DifficultyManager
    {
        // Current difficulty level
        private int _difficultyLevel = 0;

        // Last score threshold we passed
        private int _lastThresholdPassed = 0;

        // Speed multiplier based on current difficulty
        private float _speedMultiplier = 1.0f;

        // Maximum number of asteroids allowed
        private int _maxAsteroids = GameConstants.MaxAsteroids;

        // Track if an enemy ship should spawn
        private bool _enemyShipReady = false;

        // Track if we passed a threshold for enemy ship spawn
        private int _enemyShipThresholdsPassed = 0;

        public DifficultyManager()
        {
            Reset();
        }

        public void Reset()
        {
            _difficultyLevel = 0;
            _lastThresholdPassed = 0;
            _speedMultiplier = 1.0f;
            _maxAsteroids = GameConstants.MaxAsteroids;
            _enemyShipReady = false;
            _enemyShipThresholdsPassed = 0;
        }

        public void UpdateDifficulty(int currentScore)
        {
            // Calculate how many thresholds we've passed
            int thresholdsPassed = currentScore / GameConstants.DifficultyIncreaseThreshold;

            // Check if we need to update difficulty
            if (thresholdsPassed > _lastThresholdPassed)
            {
                // Calculate how many new levels we've earned
                int newLevels = thresholdsPassed - _lastThresholdPassed;
                _difficultyLevel += newLevels;
                _lastThresholdPassed = thresholdsPassed;

                // Increase speed multiplier (capped at maximum)
                _speedMultiplier = Math.Min(
                    1.0f + (_difficultyLevel * GameConstants.SpeedIncreaseFactor),
                    GameConstants.MaxSpeedMultiplier
                );

                // Increase max asteroids (capped at maximum)
                _maxAsteroids = Math.Min(
                    GameConstants.MaxAsteroids + (_difficultyLevel * GameConstants.AsteroidFrequencyIncrease),
                    GameConstants.MaxAsteroidLimit
                );

                // Check for enemy ship threshold
                int enemyShipThresholds = currentScore / GameConstants.EnemyShipScoreThreshold;
                if (enemyShipThresholds > _enemyShipThresholdsPassed)
                {
                    _enemyShipReady = true;
                    _enemyShipThresholdsPassed = enemyShipThresholds;
                }
            }
        }

        // Get current asteroid speed range with difficulty adjustment
        public float GetAsteroidMinSpeed()
        {
            return GameConstants.AsteroidMinSpeed * _speedMultiplier;
        }

        public float GetAsteroidMaxSpeed()
        {
            return GameConstants.AsteroidMaxSpeed * _speedMultiplier;
        }

        // Get current maximum number of asteroids
        public int GetMaxAsteroids()
        {
            return _maxAsteroids;
        }

        // Check if we should spawn an enemy ship and reset the flag
        public bool ShouldSpawnEnemyShip()
        {
            if (_enemyShipReady)
            {
                _enemyShipReady = false; // Reset so we only spawn one
                return true;
            }
            return false;
        }

        // Get the current difficulty level
        public int GetDifficultyLevel()
        {
            return _difficultyLevel;
        }

        // Get the speed multiplier for display
        public float GetSpeedMultiplier()
        {
            return _speedMultiplier;
        }
    }
}