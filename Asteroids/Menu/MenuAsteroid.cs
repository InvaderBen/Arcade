using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System;

namespace AsteroidsGame
{
    // Simple class for background asteroids in menu
    public class MenuAsteroid
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; private set; }
        public Vector2[] Points { get; private set; }
        public float Rotation { get; private set; }
        public float RotationSpeed { get; private set; }

        public MenuAsteroid(Vector2 position, Vector2 velocity, Vector2[] points, float rotationSpeed)
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
}