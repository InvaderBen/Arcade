using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceInvaders
{
    public class GameObject
    {
        public Vector2 Position { get; set; }
        public Texture2D Texture { get; private set; }

        public GameObject(Vector2 position, Texture2D texture)
        {
            Position = position;
            Texture = texture;
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(
                Texture,
                Position,
                null,
                Color.White,
                0f,
                new Vector2(Texture.Width / 2, Texture.Height / 2),
                1.0f,
                SpriteEffects.None,
                0f
            );
        }
    }
}
