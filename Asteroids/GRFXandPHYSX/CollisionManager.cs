using Microsoft.Xna.Framework;
using System;

namespace AsteroidsGame
{
    public class CollisionManager
    {
        // Disable debug output for performance
        private bool _debugCollisions = false;

        public CollisionManager()
        {
        }

        public bool CheckCollision(GameObject a, GameObject b)
        {
            if (a == null || b == null || a.Texture == null || b.Texture == null)
                return false;

            // More precise circle collision detection
            float radiusA = (a.Texture.Width / 2) * GameConstants.CollisionScale;
            float radiusB = (b.Texture.Width / 2) * GameConstants.CollisionScale;

            // Special handling for bullets
            if (a is Bullet)
                radiusA = 8f; // Larger radius for bullet collision
            if (b is Bullet)
                radiusB = 8f; // Larger radius for bullet collision

            // Use squared distance for better performance (avoids square root calculation)
            float dx = a.Position.X - b.Position.X;
            float dy = a.Position.Y - b.Position.Y;
            float distanceSquared = dx * dx + dy * dy;

            // Compare with squared sum of radii
            float radiusSum = radiusA + radiusB;
            float radiusSumSquared = radiusSum * radiusSum;

            // Collision occurs if squared distance is less than squared sum of radii
            return distanceSquared < radiusSumSquared;
        }

        // Check if object is off screen
        public bool IsOffScreen(GameObject obj, int screenWidth, int screenHeight)
        {
            if (obj == null || obj.Texture == null)
                return false;

            float radius = obj.Texture.Width / 2;

            return (obj.Position.X + radius < 0 ||
                    obj.Position.X - radius > screenWidth ||
                    obj.Position.Y + radius > screenHeight ||
                    obj.Position.Y - radius < 0);
        }

        // Wrap object position around screen
        public void WrapPosition(GameObject obj, int screenWidth, int screenHeight)
        {
            if (obj == null)
                return;

            Vector2 position = obj.Position;

            // Wrap X coordinate
            if (position.X < -20)
                position.X = screenWidth + 10;
            else if (position.X > screenWidth + 20)
                position.X = -10;

            // Wrap Y coordinate
            if (position.Y < -20)
                position.Y = screenHeight + 10;
            else if (position.Y > screenHeight + 20)
                position.Y = -10;

            obj.Position = position;
        }
    }
}