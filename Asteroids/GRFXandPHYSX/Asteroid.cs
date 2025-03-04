using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace AsteroidsGame
{
    public class Asteroid : GameObject
    {
        public AsteroidSize Size { get; private set; }
        public AsteroidType Type { get; private set; }
        private float _rotationSpeed;

        // Store a shape index for rendering variety
        public int ShapeIndex { get; private set; }

        public Asteroid(Vector2 position, Vector2 velocity, Texture2D texture, AsteroidSize size, AsteroidType type = AsteroidType.Normal)
            : base(position, velocity, texture)
        {
            Size = size;
            Type = type;

            // Set random rotation
            Rotation = (float)new Random().NextDouble() * MathHelper.TwoPi;

            // Set random rotation speed
            _rotationSpeed = (float)(new Random().NextDouble() - 0.5) * 2.0f;

            // Randomize shape index
            ShapeIndex = new Random().Next(3);

            // Set scale based on size
            switch (Size)
            {
                case AsteroidSize.Small:
                    Scale = 0.5f;
                    break;
                case AsteroidSize.Medium:
                    Scale = 0.75f;
                    break;
                case AsteroidSize.Large:
                    Scale = 1.0f;
                    break;
            }

            // Gold asteroids move faster
            if (Type == AsteroidType.Gold)
            {
                Velocity *= GameConstants.GoldAsteroidSpeedMultiplier;
            }
        }

        public override void Update(float deltaTime)
        {
            // Rotate the asteroid
            Rotation += _rotationSpeed * deltaTime;

            // Update position
            base.Update(deltaTime);
        }

        // Method to reset asteroid when reusing from object pool
        public void ResetRotation(Random random)
        {
            Rotation = (float)random.NextDouble() * MathHelper.TwoPi;
            _rotationSpeed = (float)(random.NextDouble() - 0.5) * 2.0f;
            ShapeIndex = random.Next(3);
        }

        // Get the point value for this asteroid
        public int GetPointValue()
        {
            int basePoints = GameConstants.AsteroidPoints;

            // Small asteroids are worth more
            switch (Size)
            {
                case AsteroidSize.Small:
                    basePoints = GameConstants.AsteroidPoints * 3;
                    break;
                case AsteroidSize.Medium:
                    basePoints = GameConstants.AsteroidPoints * 2;
                    break;
                default:
                    basePoints = GameConstants.AsteroidPoints;
                    break;
            }

            // Gold asteroids are worth more
            if (Type == AsteroidType.Gold)
            {
                return GameConstants.GoldAsteroidPoints;
            }

            return basePoints;
        }
    }
}