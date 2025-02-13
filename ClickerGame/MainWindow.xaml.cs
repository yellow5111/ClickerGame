// using System;
using System.IO;
// using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Steamworks;

namespace WpfClickerGame
{
    public partial class MainWindow : Window
    {
        private int score = 0;
        private double multiplier = 1;
        private int upgradeCost = 100;
        private double autoScoreIncrement = 1;
        private DispatcherTimer autoScoreTimer;
        private Random random = new Random();
        private double fluctuationFactor = 1.0; 
        private string saveFilePath;

        public MainWindow()
        {
            InitializeComponent();

            
            string saveDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SaveGames");
            Directory.CreateDirectory(saveDirectory); 

            saveFilePath = Path.Combine(saveDirectory, "SaveGame.ylwfo");

            
            LoadScoreFromFile();

            
            autoScoreTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            autoScoreTimer.Tick += AutoScoreTimer_Tick;
            autoScoreTimer.Start();

            
            if (!SteamAPI.Init())
            {
                MessageBox.Show("Steamworks initialization failed! Score and progress will not be uploaded to Steam!");
                
            }
        }

        
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            
            SaveScoreToFile();

            
            SteamAPI.Shutdown();
        }

        private void ClickButton_Click(object sender, RoutedEventArgs e)
        {
            score += (int)(1 * multiplier);
            ScoreLabel.Content = "SCORE = " + score;
            UpdateSteamStat();

            
            if (score >= 1000)
            {
                GrantAchievement("ACH_1000_POINTS");
            }

            ButtonClickAnimation();
            ChangeBackgroundColor();
        }

        private void AutoScoreTimer_Tick(object sender, EventArgs e)
        {
            
            score += (int)(autoScoreIncrement * multiplier * fluctuationFactor);
            ScoreLabel.Content = "SCORE = " + score;
            UpdateSteamStat();

            
            fluctuationFactor = 1.0 + (random.NextDouble() * 2.0); // fluct btwn 1/3
            
            autoScoreTimer.Interval = TimeSpan.FromSeconds(1 / fluctuationFactor);
        }

        private void UpgradeButton_Click(object sender, RoutedEventArgs e)
        {
            if (score >= upgradeCost)
            {
                score -= upgradeCost;
                autoScoreIncrement += 0.5;
                upgradeCost *= 2;

                ScoreLabel.Content = "SCORE = " + score;
                MultiplierLabel.Content = "AUTOMATIC SCORE = +" + autoScoreIncrement + " /SEC";
                UpgradeButton.Content = "UPGRADE AUTO SCORE (" + upgradeCost + ")";

                MainGrid.Background = new SolidColorBrush(Colors.GreenYellow);
                ResetBackgroundColorAfterDelay();
            }
            else
            {
                MainGrid.Background = new SolidColorBrush(Colors.Red);
                ResetBackgroundColorAfterDelay();
            }
        }

        private async void ResetBackgroundColorAfterDelay()
        {
            await Task.Delay(500);
            ChangeBackgroundColor();
        }

        private void ChangeBackgroundColor()
        {
            if (score > 1000)
            {
                MainGrid.Background = new SolidColorBrush(Colors.DarkRed);
            }
            else if (score > 500)
            {
                MainGrid.Background = new SolidColorBrush(Colors.Orange);
            }
            else
            {
                MainGrid.Background = new SolidColorBrush(Colors.LightSkyBlue);
            }
        }

        private void ButtonClickAnimation()
        {
            ScaleTransform scaleTransform = new ScaleTransform(1.0, 1.0);
            ClickButton.RenderTransform = scaleTransform;
            ClickButton.RenderTransformOrigin = new Point(0.5, 0.5);

            var scaleUpAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = 1.2,
                Duration = TimeSpan.FromMilliseconds(100),
                AutoReverse = true
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUpAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUpAnimation);
        }

        
        private void UpdateSteamStat()
        {
            try
            {
                //SteamUserStats.SetStat("STAT_SCORE", score);
                // SteamUserStats.StoreStats(); 
            }
            catch (AccessViolationException ex)
            {
                MessageBox.Show($"Error updating Steam stat: {ex.Message}");
            }
        }

        
        private void GrantAchievement(string achievementId)
        {
            if (SteamUserStats.GetAchievement(achievementId, out bool achieved) && !achieved)
            {
                SteamUserStats.SetAchievement(achievementId);
                SteamUserStats.StoreStats(); 
            }
        }

        
        private void RunSteamCallbacks(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                SteamAPI.RunCallbacks(); 
            });
        }

        
        private void LoadScoreFromFile()
        {
            if (File.Exists(saveFilePath))
            {
                try
                {
                    string savedScore = File.ReadAllText(saveFilePath);
                    if (int.TryParse(savedScore, out int loadedScore))
                    {
                        score = loadedScore;
                        ScoreLabel.Content = "Score: " + score;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading score from file: {ex.Message}");
                }
            }
        }

        
        private void SaveScoreToFile()
        {
            try
            {
                File.WriteAllText(saveFilePath, score.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving score to file: {ex.Message}");
            }
        }
    }
}
