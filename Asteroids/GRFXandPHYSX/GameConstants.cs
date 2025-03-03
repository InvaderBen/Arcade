namespace AsteroidsGame
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
        public const float BulletSpeed = 500.0f;  // Increased bullet speed
        public const float BulletLifetime = 2.0f;

        // Asteroid settings
        public const int InitialAsteroidCount = 4;
        public const int MaxAsteroids = 10;
        public const float AsteroidMinSpeed = 60.0f;
        public const float AsteroidMaxSpeed = 120.0f;
        public const int AsteroidPoints = 100;

        // Collision detection
        public const float CollisionScale = 0.75f;  // More forgiving collisions

        // Visual style
        public const bool UseVectorGraphics = true;  // Use vector outlines instead of filled shapes
    }

    public enum AsteroidSize
    {
        Small,
        Medium,
        Large
    }
}