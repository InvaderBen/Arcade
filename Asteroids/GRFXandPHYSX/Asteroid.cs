using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace AsteroidsGame
{
    public class Asteroid : GameObject
    {
        public AsteroidSize Size { get; private set; }
        private float _rotationSpeed;

        // Store a shape index for rendering variety
        public int ShapeIndex { get; private set; }

        public Asteroid(Vector2 position, Vector2 velocity, Texture2D texture, AsteroidSize size)
            : base(position, velocity, texture)
        {
            Size = size;

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
    }
}