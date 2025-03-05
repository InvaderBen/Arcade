using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework.Input;

namespace AsteroidsGame
{
    // Class to store a single high score entry
    public class ScoreEntry
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }
        public DateTime Date { get; set; }

        public ScoreEntry()
        {
            // Default constructor for serialization
            PlayerName = "Player";
            Score = 0;
            Date = DateTime.Now;
        }

        public ScoreEntry(string name, int score)
        {
            PlayerName = name;
            Score = score;
            Date = DateTime.Now;
        }
    }

    // Class to manage high scores
    public class HighScoreManager
    {
        private const int MaxHighScores = 10;
        private const string HighScoreFile = "highscores.json";
        private List<ScoreEntry> _highScores;

        // Current score entry being created (for name input)
        private ScoreEntry _pendingScore;
        private string _currentName = "Player";
        private bool _isEnteringName = false;
        private const int MaxNameLength = 8;

        // Virtual keyboard integration
        private bool _usingVirtualKeyboard = false;
        private VirtualKeyboard _virtualKeyboard = null;

        // Properties for name entry
        public bool IsEnteringName => _isEnteringName;
        public string CurrentName => _currentName;
        public bool UsingVirtualKeyboard => _usingVirtualKeyboard;
        public VirtualKeyboard VirtualKeyboard => _virtualKeyboard;

        public HighScoreManager()
        {
            _highScores = new List<ScoreEntry>();
            LoadHighScores();
        }

        public void SetVirtualKeyboard(VirtualKeyboard keyboard)
        {
            _virtualKeyboard = keyboard;
            _usingVirtualKeyboard = keyboard != null;

            if (_virtualKeyboard != null)
            {
                // Subscribe to the virtual keyboard's key press event
                _virtualKeyboard.OnKeyPress += HandleVirtualKeyPress;
            }
        }

        private void HandleVirtualKeyPress(string key)
        {
            if (!_isEnteringName)
                return;

            switch (key)
            {
                case "SPACE":
                    if (_currentName.Length < MaxNameLength)
                    {
                        _currentName += " ";
                    }
                    break;

                case "DEL":
                    if (_currentName.Length > 0)
                    {
                        _currentName = _currentName.Substring(0, _currentName.Length - 1);
                    }
                    break;

                case "DONE":
                    if (_currentName.Length > 0)
                    {
                        ConfirmNameEntry();
                    }
                    break;

                default:
                    // Regular character key
                    if (_currentName.Length < MaxNameLength)
                    {
                        _currentName += key;
                    }
                    break;
            }
        }

        public List<ScoreEntry> GetHighScores()
        {
            return _highScores;
        }

        public bool IsHighScore(int score)
        {
            // Check if this score qualifies for the high score list
            return _highScores.Count < MaxHighScores || score > _highScores.Min(x => x.Score);
        }

        // Check if this player name already exists in high scores
        public bool PlayerExists(string name)
        {
            return _highScores.Any(x => x.PlayerName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        // Find the existing score for this player
        public int GetExistingScore(string name)
        {
            var entry = _highScores.FirstOrDefault(x => x.PlayerName.Equals(name, StringComparison.OrdinalIgnoreCase));
            return entry?.Score ?? 0;
        }

        // Check if the new score is higher than the existing score for this player
        public bool IsHigherScore(string name, int score)
        {
            int existingScore = GetExistingScore(name);
            return score > existingScore;
        }

        public void StartNameEntry(int score)
        {
            _pendingScore = new ScoreEntry(_currentName, score);
            _isEnteringName = true;
        }

        public void ConfirmNameEntry()
        {
            if (_isEnteringName && !string.IsNullOrEmpty(_currentName))
            {
                _pendingScore.PlayerName = _currentName;

                // Check if player already exists
                var existingEntry = _highScores.FirstOrDefault(x =>
                    x.PlayerName.Equals(_pendingScore.PlayerName, StringComparison.OrdinalIgnoreCase));

                if (existingEntry != null)
                {
                    // Player exists - update score if new score is higher
                    if (_pendingScore.Score > existingEntry.Score)
                    {
                        existingEntry.Score = _pendingScore.Score;
                        existingEntry.Date = DateTime.Now;
                    }
                }
                else
                {
                    // New player - add to high scores
                    _highScores.Add(_pendingScore);
                }

                // Keep only the top scores, sorted by score (descending)
                _highScores = _highScores.OrderByDescending(x => x.Score).Take(MaxHighScores).ToList();

                SaveHighScores();

                _isEnteringName = false;
                _currentName = "Player"; // Reset for next time
            }
        }

        public void CancelNameEntry()
        {
            _isEnteringName = false;
            _currentName = "Player"; // Reset for next time
        }

        private void AddHighScore(ScoreEntry entry)
        {
            // First check if player already exists
            var existingEntry = _highScores.FirstOrDefault(x =>
                x.PlayerName.Equals(entry.PlayerName, StringComparison.OrdinalIgnoreCase));

            if (existingEntry != null)
            {
                // Update score if new score is higher
                if (entry.Score > existingEntry.Score)
                {
                    existingEntry.Score = entry.Score;
                    existingEntry.Date = DateTime.Now;
                }
            }
            else
            {
                // Add new entry
                _highScores.Add(entry);
            }

            // Keep only the top scores, sorted by score (descending)
            _highScores = _highScores.OrderByDescending(x => x.Score).Take(MaxHighScores).ToList();

            SaveHighScores();
        }

        private void LoadHighScores()
        {
            try
            {
                if (File.Exists(HighScoreFile))
                {
                    string jsonString = File.ReadAllText(HighScoreFile);
                    _highScores = JsonSerializer.Deserialize<List<ScoreEntry>>(jsonString);
                }
                else
                {
                    // Create sample high scores for first run
                    _highScores = new List<ScoreEntry>
                    {
                        new ScoreEntry("ALPHA", 5000),
                        new ScoreEntry("BRAVO", 4000),
                        new ScoreEntry("CHARLIE", 3000),
                        new ScoreEntry("DELTA", 2000),
                        new ScoreEntry("ECHO", 1000)
                    };
                    SaveHighScores();
                }
            }
            catch (Exception ex)
            {
                // If there's any error, start with empty high scores
                _highScores = new List<ScoreEntry>();
                Console.WriteLine($"Error loading high scores: {ex.Message}");
            }
        }

        private void SaveHighScores()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string jsonString = JsonSerializer.Serialize(_highScores, options);
                File.WriteAllText(HighScoreFile, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving high scores: {ex.Message}");
            }
        }

        public bool HandleKeypress(Keys key)
        {
            if (!_isEnteringName)
                return false;

            // Always process keyboard entry, even when using virtual keyboard
            if ((key >= Keys.A && key <= Keys.Z) || (key >= Keys.D0 && key <= Keys.D9))
            {
                if (_currentName.Length < MaxNameLength)
                {
                    // Convert the key to a character
                    char character = '\0';

                    if (key >= Keys.A && key <= Keys.Z)
                    {
                        // Uppercase letters
                        character = (char)('A' + (key - Keys.A));
                    }
                    else if (key >= Keys.D0 && key <= Keys.D9)
                    {
                        // Numbers
                        character = (char)('0' + (key - Keys.D0));
                    }

                    if (character != '\0')
                    {
                        _currentName += character;
                        return true;
                    }
                }
            }
            else if (key == Keys.Back)
            {
                if (_currentName.Length > 0)
                {
                    _currentName = _currentName.Substring(0, _currentName.Length - 1);
                    return true;
                }
            }
            else if (key == Keys.Enter)
            {
                ConfirmNameEntry();
                return true;
            }
            else if (key == Keys.Escape)
            {
                CancelNameEntry();
                return true;
            }
            else if (key == Keys.Space)
            {
                if (_currentName.Length < MaxNameLength)
                {
                    _currentName += " ";
                    return true;
                }
            }

            return false;
        }
    }
}