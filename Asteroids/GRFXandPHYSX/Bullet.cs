using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AsteroidsGame
{
    public class Bullet : GameObject
    {
        private float _lifetime;

        public Bullet(Vector2 position, Vector2 velocity, Texture2D texture)
            : base(position, velocity, texture)
        {
            _lifetime = 0;
            Scale = 1.0f;
        }

        public override void Update(float deltaTime)
        {
            // Increment lifetime
            _lifetime += deltaTime;

            // Update position
            base.Update(deltaTime);
        }

        public bool ShouldRemove()
        {
            // Bullet should be removed if it exceeds its maximum lifetime
            return _lifetime >= GameConstants.BulletLifetime;
        }

        public void ResetLifetime()
        {
            _lifetime = 0;
        }

        // Direction vector for line rendering
        public Vector2 Direction
        {
            get { return Vector2.Normalize(Velocity); }
        }
    }
}