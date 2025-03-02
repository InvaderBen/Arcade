using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceInvaders
{
    public class Projectile : GameObject
    {
        public Vector2 Velocity { get; set; }

        public Projectile(Vector2 position, Vector2 velocity, Texture2D texture)
            : base(position, texture)
        {
            Velocity = velocity;
        }

        public void Update(float deltaTime)
        {
            Position += Velocity * deltaTime;
        }
    }
}