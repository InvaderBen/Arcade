using Microsoft.Xna.Framework;
using System;

namespace AsteroidsGame
{
    /// <summary>
    /// Class for menu background objects (ships, asteroids, bullets)
    /// </summary>
    public class MenuObject
    {
        // Vector2 is a struct (immutable), so need to replace the whole object when modifying
        private Vector2 _position;
        private Vector2 _velocity;

        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public Vector2 Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }

        public Vector2[] Points { get; set; }
        public float Rotation { get; set; }
        public float RotationSpeed { get; set; }
        public float Lifetime { get; set; }
        public bool IsAsteroid { get; set; }
        public bool IsShip { get; set; }
        public bool IsBullet { get; set; }

        public void Update(float deltaTime)
        {
            // Update position - create a new Vector2 instead of modifying components
            _position += _velocity * deltaTime;

            // Update rotation
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

            // Update lifetime for bullets
            if (IsBullet)
            {
                Lifetime += deltaTime;
            }
        }
    }
}