using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceInvaders
{
    public class Invader : GameObject
    {
        public int PointValue { get; private set; }
        private int _currentFrame;
        private const int MaxFrames = 2;

        public Invader(Vector2 position, Texture2D texture, int pointValue)
            : base(position, texture)
        {
            PointValue = pointValue;
            _currentFrame = 0;
        }

        public void NextFrame()
        {
            _currentFrame = (_currentFrame + 1) % MaxFrames;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // In a real game with animation frames, you'd use a sprite sheet
            // Here we just draw the texture with slight horizontal offset based on frame
            Vector2 drawPosition = Position;
            if (_currentFrame == 1)
            {
                drawPosition.X += 2; // Slight movement for "animation"
            }

            spriteBatch.Draw(
                Texture,
                drawPosition,
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