using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace SpaceInvaders
{
    public class Barrier : GameObject
    {
        private bool[,] _damageMap;

        public Barrier(Vector2 position, Texture2D texture)
            : base(position, texture)
        {
            // Initialize damage map (all pixels intact)
            _damageMap = new bool[texture.Width, texture.Height];
        }

        public bool CheckProjectileCollision(Vector2 projectilePosition)
        {
            // Convert world position to barrier-local position
            int localX = (int)(projectilePosition.X - (Position.X - Texture.Width / 2));
            int localY = (int)(projectilePosition.Y - (Position.Y - Texture.Height / 2));

            // Check if within bounds
            if (localX >= 0 && localX < Texture.Width && localY >= 0 && localY < Texture.Height)
            {
                // Check if this part of the barrier is intact
                if (!_damageMap[localX, localY])
                {
                    // Create damage in a small radius around hit point
                    int radius = 4;
                    for (int y = Math.Max(0, localY - radius); y < Math.Min(Texture.Height, localY + radius); y++)
                    {
                        for (int x = Math.Max(0, localX - radius); x < Math.Min(Texture.Width, localX + radius); x++)
                        {
                            if (Math.Sqrt((x - localX) * (x - localX) + (y - localY) * (y - localY)) <= radius)
                            {
                                _damageMap[x, y] = true;
                            }
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Get texture data to modify (in a real game, you'd use a more efficient approach)
            Color[] textureData = new Color[Texture.Width * Texture.Height];
            Texture.GetData(textureData);

            // Apply damage map
            for (int y = 0; y < Texture.Height; y++)
            {
                for (int x = 0; x < Texture.Width; x++)
                {
                    if (_damageMap[x, y])
                    {
                        textureData[y * Texture.Width + x] = Color.Transparent;
                    }
                }
            }

            // Update texture
            Texture.SetData(textureData);

            // Draw the barrier
            base.Draw(spriteBatch);
        }
    }
}
