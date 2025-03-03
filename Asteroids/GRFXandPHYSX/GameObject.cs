using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AsteroidsGame
{
    public class GameObject
    {
        // Position and movement
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Rotation { get; set; }

        // Appearance
        public Texture2D Texture { get; private set; }
        protected Vector2 Origin { get; set; }
        protected float Scale { get; set; }

        public GameObject(Vector2 position, Vector2 velocity, Texture2D texture, float rotation = 0f)
        {
            Position = position;
            Velocity = velocity;
            Rotation = rotation;
            Texture = texture;
            Scale = 1.0f;

            if (Texture != null)
            {
                Origin = new Vector2(Texture.Width / 2, Texture.Height / 2);
            }
        }

        public virtual void Update(float deltaTime)
        {
            // Update position based on velocity
            Position += Velocity * deltaTime;

            // Wrap around screen boundaries
            WrapPosition();
        }

        protected void WrapPosition()
        {
            // Wrap X coordinate
            if (Position.X < 0)
                Position = new Vector2(GameConstants.ScreenWidth, Position.Y);
            else if (Position.X > GameConstants.ScreenWidth)
                Position = new Vector2(0, Position.Y);

            // Wrap Y coordinate
            if (Position.Y < 0)
                Position = new Vector2(Position.X, GameConstants.ScreenHeight);
            else if (Position.Y > GameConstants.ScreenHeight)
                Position = new Vector2(Position.X, 0);
        }
    }
}