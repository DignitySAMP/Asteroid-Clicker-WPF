using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;

namespace AsteroidClicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // TODO: Add a button for muting volume
        #region Global Variables
        // Global variables
        double amountOfAsteroids = 0.0; // Cookies
        double amountOfScore = 0.0; // Score
        #endregion
        
        public MainWindow()
        {
            InitializeComponent();

            DispatcherTimer ms_timer = new DispatcherTimer();
            ms_timer.Interval = TimeSpan.FromMilliseconds(10);
            ms_timer.Tick += MS_Timer;
            ms_timer.Start();

            blast_timer.Interval = TimeSpan.FromMilliseconds(25);
            blast_timer.Tick += Blast_Timer;

            SetupUpgradeButtons(); // setup button design(s)
        }

        #region Image Click Events
        bool IsMouseInsideImage = false;
        bool ValidMouseClick = false;
        private void ImgAsteroid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseInsideImage) // Check if the pointer is actually inside our image
            {
                amountOfAsteroids++;
                amountOfScore++;

                AdjustInfoLabels();

                Random random = new Random();
                ImgAsteroid.Width = random.Next(100, 120);

                CreateFallingParticles();
                CreateBlastParticle();
                ValidMouseClick = true; // Validate our click. This is used in cross reference for additional features (i.e. MouseUp, ...)
            }
        }
        private void ImgAsteroid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ValidMouseClick)
            { // BUG FIX: We only call our modifiers if the mouse click was valid: (inside image + clicked on image)
                ImgAsteroid.Width = 128;
                ValidMouseClick = false;
            }
        }
        private void ImgAsteroid_MouseEnter(object sender, MouseEventArgs e)
        {
            IsMouseInsideImage = true;
        }

        private void ImgAsteroid_MouseLeave(object sender, MouseEventArgs e)
        {   // BUG FIX 1: Reset width to default incase they leave the image without MouseUp being called.
            // BUG FIX 2: Nullify valid mouse click and reset "IsMouseInsideImage" variable appropriately. 
            ImgAsteroid.Width = 128;
            IsMouseInsideImage = false;
            ValidMouseClick = false;
        }
        #endregion
        #region Custom Functions
        private void MS_Timer(object sender, EventArgs e)
        {
            MoveFallingParticles();
            ProcessUpgradeOutput();
        }
        private void AdjustInfoLabels()
        {
            LblAmount.Content = $"{Math.Floor(amountOfAsteroids)}";
            this.Title = $"Asteroid Clicker ({LblAmount.Content} asteroids)";

            HandleUpgradeButtons(); // TODO: Make this automatically append when cookies are automatically harvested
        }
        #endregion
        #region Shop/Upgrade System
        static int MAX_UPGRADES = 5;

        int[] boughtUpgrades = new int[MAX_UPGRADES];

        private void SetupUpgradeButtons()
        {
            for (int i = 0; i < MAX_UPGRADES; i++)
            {
                var UpgradeData = GetUpgradeData(i);

                UpgradeData.wrapper.Children.Clear();

                // Setup button design
                Image BtnImage = new Image();
                BtnImage.Source = new BitmapImage(new Uri(UpgradeData.icon, UriKind.Relative));
                BtnImage.Width = 32;
                BtnImage.Height = 32;
                UpgradeData.wrapper.Children.Add(BtnImage);

                Label BtnName = new Label();
                BtnName.HorizontalContentAlignment = HorizontalAlignment.Left;
                StringBuilder btnInfo = new StringBuilder();
                btnInfo.AppendLine(UpgradeData.name);
                btnInfo.AppendLine($"Kost {DisplayUpgradePrice(i)} asteroids");
                btnInfo.AppendLine($"{boughtUpgrades[i]} in bezit");
                BtnName.HorizontalContentAlignment = HorizontalAlignment.Center;
                BtnName.Content = btnInfo;
                UpgradeData.wrapper.Children.Add(BtnName);
            }
            HandleUpgradeButtons(); // disable buttons
        }

        private void HandleUpgradeButtons()
        {
            StringBuilder buttonString = new StringBuilder();
            for (int i = 0; i < MAX_UPGRADES; i ++)
            {
                var UpgradeData = GetUpgradeData(i);

                // Add tooltip to buttons
                buttonString.Clear();
                buttonString.AppendLine($"{UpgradeData.name}");
                buttonString.AppendLine($"{UpgradeData.description}");
                buttonString.AppendLine($"");
                buttonString.Append($"Opbrengst: +{UpgradeData.output * 100}/seconde");
                UpgradeData.button.ToolTip = buttonString.ToString();

                // Enable/disable tooltip based on current asteroids
                if (amountOfAsteroids >= Math.Ceiling(GetUpgradePrice(i))) // ceil it here so they can't buy when button shows "18" (ceiled visually) but they have "17.X"
                {
                    (UpgradeData.button).IsEnabled = true;
                }
                else (UpgradeData.button).IsEnabled = false;
            }           
        }

        private (string name, string description, double price, double output, Button button, WrapPanel wrapper, string icon) GetUpgradeData(int index)
        {
            // TODO: Add exception for index out of bounds, maybe use an enum with constants?
            var upgradeList = new (string, string, double, double, Button, WrapPanel, string)[]
            {
               ("Astronaut",  "Een astronaut verzameld automatisch asteroïden.", 15.0, 0.001, BtnUpgrade1, WrapBtnContent_1, "/assets/images/icons/thumb_astronaut.png"),
               ("Mine Blaster",  "Een mine blaster veroorzaakt meer debris, dus meer asteroïden.", 100.0, 0.01, BtnUpgrade2, WrapBtnContent_2, "/assets/images/icons/thumb_blaster.png"),
               ("Space Ship",  "Een extra space ship versneld de vluchten heen en terug.", 1100.0, 0.08, BtnUpgrade3, WrapBtnContent_3, "/assets/images/icons/thumb_rocket.png"),
               ("Mining Colony",  "Een mining colony verzameld efficiënt meerdere asteroïden.", 12000.0, 0.47, BtnUpgrade4, WrapBtnContent_4, "/assets/images/icons/thumb_miningcolony.png"),
               ("Space Station",  "Een space station wordt op een asteroïde geplaatst. Vluchten heen en weer zijn overbodig.", 130000.0, 2.60, BtnUpgrade5, WrapBtnContent_5, "/assets/images/icons/thumb_spacestation.png"),
            };

            return upgradeList[index];
        }

        private void BtnUpgrade_Click(object sender, RoutedEventArgs e) 
        {
            // TODO: Add an option to buy 1, buy 5, buy 10 or buy MAX of each option.
            // Maybe make it show a panel with these options when you click on the button?
            Button button = sender as Button;
            ProcessUpgradePurchase(button); 
        }

        private void ProcessUpgradePurchase(Button button)
        {
            int specifier = GetIndexOfUpgrade(button);

            if (specifier != -1)
            {
                var UpgradeData = GetUpgradeData(specifier);
                if (GetUpgradePrice(specifier) <= amountOfAsteroids)
                {
                    amountOfAsteroids -= GetUpgradePrice(specifier);

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"Je hebt een {UpgradeData.name} gekocht voor {DisplayUpgradePrice(specifier)} asteroïden.");
                    sb.AppendLine($"Je hebt nu nog {Math.Floor(amountOfAsteroids)} asteroïden over.");
                    sb.AppendLine("");
                    sb.AppendLine(UpgradeData.description);
                    sb.AppendLine("");
                    sb.AppendLine($"Automatische opbrengst: {UpgradeData.output*100}/seconde");

                    ShowFadeMessage($"{UpgradeData.name} gekocht", sb.ToString());

                    boughtUpgrades[specifier]++;
                    AdjustInfoLabels(); // update game labels (title/score)
                    SetupUpgradeButtons(); // update button labels
                }
            }
            else MessageBox.Show("Er is iets misgegaan met de upgrade. (Knop is niet geinitialiseerd)");
        }

        private int GetIndexOfUpgrade(Button button)
        {
            int specifier = -1;

            for(int i = 0; i < MAX_UPGRADES; i ++)
            {
                var UpgradeData = GetUpgradeData(i);

                if(button == UpgradeData.button)
                {
                    specifier = i;
                    break;
                }
            }

            return specifier;
        }

        private double GetUpgradePrice(int upgrade)
        {
            var UpgradeData = GetUpgradeData(upgrade);
            double buy_price = UpgradeData.price * (1.15 * boughtUpgrades[upgrade]);
            if(buy_price <= 0)
            {
                buy_price = UpgradeData.price;
            }
            return buy_price;
        }

        private string DisplayUpgradePrice(int upgrade)
        {
            string price;

            price = $"{Math.Ceiling(GetUpgradePrice(upgrade))}";

            return price;
        }

        #endregion

        #region Upgrade Effects

        private void ProcessUpgradeOutput()
        {
            for(int i = 0; i < MAX_UPGRADES; i ++)
            {
                var UpgradeData = GetUpgradeData(i);
                if (boughtUpgrades[i] > 0)
                {
                    amountOfAsteroids += (UpgradeData.output * boughtUpgrades[i]);
                    Console.WriteLine($"[{UpgradeData.name}] Adding {UpgradeData.output * boughtUpgrades[i]} (default: {UpgradeData.output}) for index {i}, new amount: {amountOfAsteroids}");
                    AdjustInfoLabels(); 
                }
            }
        }

        #endregion

        #region Particle System (Blast/Debris)
        // Blast Effect
        string[] blastParticleImages =
        {
            "/assets/particles/blast/click_blast_0.png", "/assets/particles/blast/click_blast_1.png", "/assets/particles/blast/click_blast_2.png",
            "/assets/particles/blast/click_blast_3.png", "/assets/particles/blast/click_blast_4.png", "/assets/particles/blast/click_blast_5.png",
            "/assets/particles/blast/click_blast_6.png", "/assets/particles/blast/click_blast_7.png", "/assets/particles/blast/click_blast_8.png",
            "/assets/particles/blast/click_blast_9.png"
        };

        Image blastParticleImg = new Image();
        DispatcherTimer blast_timer = new DispatcherTimer();
        int blastCurrentImage = 0;

        private void CreateBlastParticle()
        {
            ClearBlastParticle();

            blastParticleImg = new Image();
            blastParticleImg.Source = new BitmapImage(new Uri(blastParticleImages[0], UriKind.Relative));

            blastParticleImg.Width = 96;
            blastParticleImg.Height = 96;
            blastParticleImg.IsHitTestVisible = false; // can click through image

            blastCurrentImage = 0;

            // Load sound from memory (added as resource)
            var blastSound = new System.Media.SoundPlayer();
            blastSound.Stream = AsteroidClicker.Properties.Resources.blaster;
            blastSound.Play();

            GridBlastZone.Children.Add(blastParticleImg);

            blast_timer.Start();
        }
        private void Blast_Timer(object sender, EventArgs e)
        {
            blastCurrentImage++;
            if (blastCurrentImage >= blastParticleImages.Length)
            {
                blastCurrentImage = 0;
                GridBlastZone.Children.Remove(blastParticleImg);
                blast_timer.Stop();
            }
            else blastParticleImg.Source = new BitmapImage(new Uri(blastParticleImages[blastCurrentImage], UriKind.Relative));
        }

        private void ClearBlastParticle()
        {
            blastCurrentImage = 0;
            GridBlastZone.Children.Remove(blastParticleImg);
            blast_timer.Stop();
        }

        // Dropping debris/particle effecft
        string[] fallingParticleImages =
        {
            "/assets/particles/drop/click_anim_0.png", "/assets/particles/drop/click_anim_1.png", "/assets/particles/drop/click_anim_2.png",
            "/assets/particles/drop/click_anim_3.png", "/assets/particles/drop/click_anim_4.png"
        };

        private static int MAX_FALLING_PARTICLES = 15;
        List<int> fallingParticleIndexes = new List<int>();
        Image[] fallingParticleImg = new Image[MAX_FALLING_PARTICLES];
        private void CreateFallingParticles()
        {
            ClearFallingParticles();

            Random random = new Random();
            int MAX_SPAWNED_PARTICLES = random.Next(5, MAX_FALLING_PARTICLES);

            for (int index = 0; index < MAX_SPAWNED_PARTICLES; index++)
            {
                // Storing random sprite
                fallingParticleImg[index] = new Image();
                fallingParticleImg[index].Source = new BitmapImage(new Uri(fallingParticleImages[random.Next(fallingParticleImages.Length)], UriKind.Relative));

                // Set random size
                fallingParticleImg[index].Width = random.Next(12, 32);
                fallingParticleImg[index].Height = random.Next(0, 32);

                // Setting random position
                double pos_x = random.Next(0, (int)CanvasDropParticles.Width - 20);
                double pos_y = random.Next(-32, -16);

                Canvas.SetLeft(fallingParticleImg[index], pos_x);
                Canvas.SetTop(fallingParticleImg[index], pos_y);

                // Showing inside particle canvas & adding to list
                CanvasDropParticles.Children.Add(fallingParticleImg[index]);
                fallingParticleIndexes.Add(index);
            }
        }
        private void MoveFallingParticles()
        {
            Random random = new Random();
            foreach (int index in fallingParticleIndexes)
            {
                double pos_y = Canvas.GetTop(fallingParticleImg[index]);

                pos_y += random.Next(1, 5);
                fallingParticleImg[index].Opacity -= 0.035;

                Canvas.SetTop(fallingParticleImg[index], pos_y);

                if (pos_y >= CanvasDropParticles.Height)
                {
                    CanvasDropParticles.Children.Remove(fallingParticleImg[index]);
                    fallingParticleIndexes.Remove(index);
                    break;
                }
            }
        }

        private void ClearFallingParticles()
        {
            foreach (int index in fallingParticleIndexes)
            {
                CanvasDropParticles.Children.Remove(fallingParticleImg[index]);
            }
            fallingParticleIndexes.Clear();
        }
        #endregion
        #region Custom Message System


        private async void ShowFadeMessage(string title, string text)
        {
            LblUpdateTitle.Content = title;
            LblUpdateText.Content = text;

            StckMessageOverlay.Opacity = 0.0;
            StckMessageOverlay.Visibility = Visibility.Visible;
            while (StckMessageOverlay.Opacity <= 1.0)
            {
                await Task.Delay(20);
                StckMessageOverlay.Opacity += 0.15;
            }
            StckMessageOverlay.Opacity = 1.0;

            await Task.Delay(3500);

            while (StckMessageOverlay.Opacity > 0.0)
            {
                await Task.Delay(20);
                StckMessageOverlay.Opacity -= 0.15;
            }
            StckMessageOverlay.Opacity = 0.0;
            StckMessageOverlay.Visibility = Visibility.Hidden;
        }
        #endregion
    }
}
