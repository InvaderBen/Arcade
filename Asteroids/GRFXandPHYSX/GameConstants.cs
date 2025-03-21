﻿namespace AsteroidsGame
{
    public static class GameConstants
    {
        // Screen dimensions
        public const int ScreenWidth = 800;
        public const int ScreenHeight = 600;

        // Player settings
        public const float PlayerRotationSpeed = 3.0f;
        public const float PlayerThrustForce = 200.0f;
        public const float PlayerDrag = 0.5f;
        public const float BulletCooldown = 0.25f;
        public const int InitialLives = 3;

        // Bullet settings
        public const float BulletSpeed = 500.0f;
        public const float BulletLifetime = 2.0f;

        // Asteroid settings
        public const int InitialAsteroidCount = 4;
        public const int MaxAsteroids = 10;
        public const float AsteroidMinSpeed = 60.0f;
        public const float AsteroidMaxSpeed = 120.0f;
        public const int AsteroidPoints = 100;

        // Special asteroid settings
        public const int GoldAsteroidPoints = 500;
        public const float GoldAsteroidChance = 0.1f; // 10% chance of spawning a gold asteroid
        public const float GoldAsteroidSpeedMultiplier = 1.5f;

        // Enemy ship settings
        public const int EnemyShipPoints = 1000;
        public const float EnemyShipSpeed = 150.0f;
        public const float EnemyShipFireRate = 1.5f;
        public const int EnemyShipScoreThreshold = 50000;

        // Difficulty progression
        public const int DifficultyIncreaseThreshold = 10000; // Increase difficulty every 10k points
        public const float SpeedIncreaseFactor = 0.15f; // 15% speed increase per difficulty level
        public const float MaxSpeedMultiplier = 2.5f; // Maximum speed is 2.5x the base speed
        public const int AsteroidFrequencyIncrease = 1; // Increase max asteroids by 1 per difficulty level
        public const int MaxAsteroidLimit = 20; // Maximum number of asteroids regardless of difficulty

        // Visual settings
        public const bool UseVectorGraphics = true;
        public const float CollisionScale = 0.75f;  // More forgiving collisions
    }

    public enum AsteroidSize
    {
        Small,
        Medium,
        Large
    }

    public enum AsteroidType
    {
        Normal,
        Gold
    }
}