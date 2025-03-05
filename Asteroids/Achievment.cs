using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AsteroidsGame
{
    // Enum for achievement types - add more as needed
    public enum AchievementType
    {
        ScoreReached,       // Based on score milestones
        AsteroidsDestroyed, // Based on total asteroids destroyed
        GoldAsteroidsFound, // Based on gold asteroids found
        ShipsDestroyed,     // Based on enemy ships destroyed
        LivesLeft,          // Based on completing level with X lives left
        NoHit,              // Based on not getting hit for a period of time
        NoShoot,            // Based on not shooting for a period of time
        ThrusterTime,       // Based on continuous thruster usage
        GameCompletion,     // Based on game progression
        Special             // Special conditions
    }

    // A single achievement definition
    public class Achievement
    {
        // Basic properties
        public string Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public AchievementType Type { get; private set; }
        public bool IsUnlocked { get; private set; }
        public DateTime? UnlockTime { get; private set; }
        public int ProgressCurrent { get; private set; }
        public int ProgressTarget { get; private set; }
        public bool IsSecret { get; private set; }

        // Visual properties
        public string IconUnlocked { get; private set; }
        public string IconLocked { get; private set; }

        // Animation properties
        private float _displayTime = 0f;
        private const float DisplayDuration = 5f; // 5 seconds
        public bool IsDisplaying => _displayTime > 0;
        public float DisplayAlpha => MathHelper.Clamp(_displayTime / DisplayDuration, 0f, 1f);

        // Constructor
        public Achievement(
            string id,
            string title,
            string description,
            AchievementType type,
            int progressTarget,
            bool isSecret = false,
            string iconUnlocked = "achievement_unlocked",
            string iconLocked = "achievement_locked")
        {
            Id = id;
            Title = title;
            Description = description;
            Type = type;
            IsUnlocked = false;
            UnlockTime = null;
            ProgressCurrent = 0;
            ProgressTarget = progressTarget;
            IsSecret = isSecret;
            IconUnlocked = iconUnlocked;
            IconLocked = iconLocked;
        }

        // Update progress
        public void UpdateProgress(int newProgress)
        {
            if (IsUnlocked) return;

            ProgressCurrent = Math.Min(newProgress, ProgressTarget);

            // Check if achievement is now unlocked
            if (ProgressCurrent >= ProgressTarget)
            {
                Unlock();
            }
        }

        // Increment progress
        public void IncrementProgress(int amount = 1)
        {
            if (IsUnlocked) return;

            UpdateProgress(ProgressCurrent + amount);
        }

        // Unlock achievement
        public void Unlock()
        {
            if (IsUnlocked) return;

            IsUnlocked = true;
            UnlockTime = DateTime.Now;
            ProgressCurrent = ProgressTarget;
            _displayTime = DisplayDuration;
        }

        // Reset achievement (for debugging)
        public void Reset()
        {
            IsUnlocked = false;
            UnlockTime = null;
            ProgressCurrent = 0;
            _displayTime = 0f;
        }

        // Update achievement display animation
        public void Update(float deltaTime)
        {
            if (_displayTime > 0)
            {
                _displayTime -= deltaTime;
            }
        }

        // Trigger display animation
        public void DisplayNotification()
        {
            if (IsUnlocked)
            {
                _displayTime = DisplayDuration;
            }
        }

        // Progress as percentage
        public float GetProgressPercentage()
        {
            return (float)ProgressCurrent / ProgressTarget;
        }
    }
}
