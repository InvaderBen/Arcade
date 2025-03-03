using Microsoft.Xna.Framework;
using System;

namespace AsteroidsGame
{
    // Menu game objects namespace to avoid naming conflicts
    namespace MenuGameplay
    {
        // Class for background asteroids in menu
        public class Asteroid
        {
            public Vector2 Position { get; set; }
            public Vector2 Velocity { get; set; }
            public Vector2[] Points { get; private set; }
            public float Rotation { get; set; }
            public float RotationSpeed { get; set; }

            public Asteroid(Vector2 position, Vector2 velocity, Vector2[] points, float rotationSpeed)
            {
                Position = position;
                Velocity = velocity;
                Points = points;
                Rotation = 0f;
                RotationSpeed = rotationSpeed;
            }

            public void Update(float deltaTime)
            {
                Position += Velocity * deltaTime;
                Rotation += RotationSpeed * deltaTime;

                // Keep rotation in bounds
                if (Rotation > MathHelper.TwoPi)
                {
                    Rotation -= MathHelper.TwoPi;
                }
                else if (Rotation < 0)
                {
                    Rotation += MathHelper.TwoPi;
                }
            }
        }

        // Ship class for menu background
        public class Ship
        {
            public Vector2 Position { get; set; }
            public Vector2 Velocity { get; set; }
            public float Rotation { get; set; }

            public Ship(Vector2 position, Vector2 velocity)
            {
                Position = position;
                Velocity = velocity;
                Rotation = (float)Math.Atan2(velocity.Y, velocity.X) + MathHelper.PiOver2;
            }

            public void Update(float deltaTime)
            {
                Position += Velocity * deltaTime;
            }
        }

        // Bullet class for menu background
        public class Bullet
        {
            public Vector2 Position { get; set; }
            public Vector2 Velocity { get; private set; }
            public float Lifetime { get; private set; }

            public Bullet(Vector2 position, Vector2 velocity)
            {
                Position = position;
                Velocity = velocity;
                Lifetime = 0f;
            }

            public void Update(float deltaTime)
            {
                Position += Velocity * deltaTime;
                Lifetime += deltaTime;
            }
        }
    }
}