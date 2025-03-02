using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceInvaders
{
    public class Player : GameObject
    {
        public float Speed { get; private set; }
        private float _shootTimer;
        private const float ShootCooldown = 0.5f;

        public Player(Vector2 position, Texture2D texture, float speed)
            : base(position, texture)
        {
            Speed = speed;
            _shootTimer = 0f;
        }

        public void Update(float deltaTime)
        {
            if (_shootTimer > 0)
            {
                _shootTimer -= deltaTime;
            }
        }

        public bool CanShoot()
        {
            return _shootTimer <= 0;
        }

        public void ResetShootTimer()
        {
            _shootTimer = ShootCooldown;
        }
    }
}