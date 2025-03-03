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

        // Properties for name entry
        public bool IsEnteringName => _isEnteringName;
        public string CurrentName => _currentName;

        public HighScoreManager()
        {
            _highScores = new List<ScoreEntry>();
            LoadHighScores();
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
                AddHighScore(_pendingScore);
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
            _highScores.Add(entry);

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

            // Handle key presses for name entry
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