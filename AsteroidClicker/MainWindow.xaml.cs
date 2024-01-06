using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.VisualBasic;

namespace AsteroidClicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Global Variables
        // Global variables
        decimal amountOfAsteroids = 0.0M; // Cookies
        decimal amountOfScore = 0.0M; // Score
        decimal additionAmount = 1; // debug purposes (default: 1) 
        #endregion
        #region MainWindow
        public MainWindow()
        {
            InitializeComponent();

            DispatcherTimer ms_timer = new DispatcherTimer();
            ms_timer.Interval = TimeSpan.FromMilliseconds(10);
            ms_timer.Tick += MS_Timer;
            ms_timer.Start();

            DispatcherTimer s_timer = new DispatcherTimer();
            s_timer.Interval = TimeSpan.FromSeconds(1);
            s_timer.Tick += S_Timer;
            s_timer.Start();

            DispatcherTimer m_timer = new DispatcherTimer();
            m_timer.Interval = TimeSpan.FromMinutes(1);
            m_timer.Tick += M_Timer;
            m_timer.Start();

            blast_timer.Interval = TimeSpan.FromMilliseconds(25);
            blast_timer.Tick += Blast_Timer;
            
            ScrollCategories.Visibility = Visibility.Hidden; // only shown after purchase
            StckUpgrades.Visibility = Visibility.Hidden; // only show after unlock
        }
        #endregion
        #region Image Click Events
        bool IsMouseInsideImage = false;
        bool ValidMouseClick = false;
        private void ImgAsteroid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseInsideImage) // Check if the pointer is actually inside our image
            {
                amountOfAsteroids += additionAmount;
                amountOfScore += additionAmount;
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
        #region Cookies Per Second

        private decimal GetCookiesPerSecond()
        {
            decimal amount = 0;
            for (int i = 0; i < MAX_UPGRADES; i++)
            {
                var UpgradeData = GetUpgradeData(i);
                if (boughtUpgrades[i] > 0)
                {
                    amount += (UpgradeData.output * boughtUpgrades[i]) * (GetBonusUpgradeMultiplier(i));
                }
            }
            return (amount*100);
        }

        private decimal GetCookiesPerUpgrade(int upgrade)
        {
            var UpgradeData = GetUpgradeData(upgrade);
            return (UpgradeData.output * boughtUpgrades[upgrade]) * (GetBonusUpgradeMultiplier(upgrade));
        }

        private async void ShowCookiesPerSecond()
        {
            if (GetCookiesPerSecond() <= 0) return;
            Random random = new Random();
            var brushConverter = new BrushConverter();
            Label asteroidPerSecondGain = new Label
            {
                Opacity = 1.0,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Content = $"{FormatNumber(GetCookiesPerSecond(), true)}/s",
                Foreground = (Brush)brushConverter.ConvertFrom("#FF00c531"),
                Width = CookiesPerSecondParticles.Width,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                IsHitTestVisible = false
            };
            Canvas.SetTop(asteroidPerSecondGain, 50);

            PlaySyncedSound(Properties.Resources.passiveblast);

            // Create blast on random position
            Image smallBlastParticleImg = new Image
            {
                Source = new BitmapImage(new Uri(blastParticleImages[0], UriKind.Relative)),
                Width = 32,
                Height = 32,
                IsHitTestVisible = false, // can click through image
            };
            Canvas.SetLeft(smallBlastParticleImg, random.Next(0, (int)CookiesPerSecondParticles.Width - 20));
            Canvas.SetTop(smallBlastParticleImg, random.Next(16, 32));

            // Blast animation
            int smallBlastCurrentImage = 0;
            CookiesPerSecondParticles.Children.Add(smallBlastParticleImg);
            while (blastCurrentImage < blastParticleImages.Length)
            {
                await Task.Delay(60);
                smallBlastCurrentImage++;
                if (smallBlastCurrentImage >= blastParticleImages.Length) break;
                else
                {
                    smallBlastParticleImg.Source = new BitmapImage(new Uri(blastParticleImages[smallBlastCurrentImage], UriKind.Relative));
                }
            }
            CookiesPerSecondParticles.Children.Remove(smallBlastParticleImg);

            // Label animation
            double temp_pos_y = 50;
            CookiesPerSecondParticles.Children.Add(asteroidPerSecondGain);
            while (temp_pos_y > -30 || asteroidPerSecondGain.Opacity > 0.0)
            {
                await Task.Delay(15);
                asteroidPerSecondGain.Opacity -= 0.01;
                await Task.Delay(30);
                temp_pos_y--;
                Canvas.SetTop(asteroidPerSecondGain, temp_pos_y);
            }
            CookiesPerSecondParticles.Children.Remove(asteroidPerSecondGain);
        }
        #endregion
        #region Custom Functions
        private void MS_Timer(object sender, EventArgs e)
        {
            ProcessUpgradeOutput();
            UpdateVisuals();
            MoveFallingParticles();
        }
        private void S_Timer(object sender, EventArgs e)
        {
            if (!isShopPanelUnlocked && AccessToUpgrades())
            {
                CreateShopLayout();
            }
            AddUpgradeButtons();
            AdjustUpgradeButtons();
            ToggleBonusUpgradeButtons();
            ShowCookiesPerSecond();
        }
        private void M_Timer(object sender, EventArgs e)
        {
            SpawnGoldenCookie();
        }

        private void AdjustInfoLabels()
        {
            LblAmount.Content = $"{FormatNumber(Math.Floor(amountOfAsteroids))} (totaal: {FormatNumber(Math.Floor(amountOfScore))})";
            this.Title = $"Asteroid Clicker ({LblAmount.Content} asteroids)";

            CheckQuestProgress_TotalAsteroids();
        }
        private void UpdateVisuals()
        {
            AdjustInfoLabels();
            AdjustUpgradeButtons();
        }

        private string FormatNumber(decimal input, bool separator = false)
        {
            var digitFormat = new NumberFormatInfo { 
                NumberGroupSeparator = " ",
            };

            string number;
            if (!separator) number = input.ToString("#,0", digitFormat);
            else number = input.ToString("#,0.0", digitFormat);

            decimal[] numberArray = new decimal[] { 1000000, 1000000000, 1000000000000, 1000000000000000, 1000000000000000000 };
            string[] numberName = new string[] { "miljoen", "miljard", "biljoen", "biljard", "triljoen" };

            int caught_index = -1;
            for (int i = 0; i < numberArray.Length; i++)
            {
                if (input >= numberArray[i])
                {
                    caught_index = i;
                }
            }

            if (caught_index > -1)
            {
                string numberConcat = (input / numberArray[caught_index]).ToString("N2");
                number = $" {numberConcat} {numberName[caught_index]}";
            }

            return number;
        }
        #endregion
        #region Upgrade Buttons
        Button[] upgradeButton = new Button[MAX_UPGRADES];
        WrapPanel[] upgradeButtonWrapper = new WrapPanel[MAX_UPGRADES];
        private void AddUpgradeButtons()
        {
            if (!isShopPanelUnlocked || !AccessToUpgrades())
            {
                return;
            }

            for (int i = 0; i < MAX_UPGRADES; i++)
            {
                if (upgradeButton[i] != null) continue;
                var UpgradeData = GetUpgradeData(i);
                if (amountOfScore < UpgradeData.price) continue;

                StackPanel buttonContainer = new StackPanel
                {
                    Margin = new Thickness
                    {
                        Top = 5,
                        Bottom = 5,
                        Left = 5,
                        Right = 5
                    }
                };

                upgradeButtonWrapper[i] = new WrapPanel
                {
                    Orientation = Orientation.Horizontal,

                };
                upgradeButton[i] = new Button
                {
                    BorderThickness = new Thickness
                    {
                        Top = 0,
                        Bottom = 0,
                        Left = 0,
                        Right = 0,
                    },
                    Background = Brushes.AliceBlue,
                    Width = 200,
                    Height = 60,
                    Margin = new Thickness
                    {
                        Top = 0,
                        Bottom = 0,
                        Left = 5,
                        Right = 5,
                    },
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                };
    
                upgradeButton[i].Click += BtnUpgrade_Click;
                upgradeButton[i].Content = upgradeButtonWrapper[i];
                buttonContainer.Children.Add(upgradeButton[i]);

                CreateBonusButton(buttonContainer, i);

                shopStackPanel.Children.Add(buttonContainer);
                SetUpgradeButtonText(i);
            }
        }

        private void SetUpgradeButtonText(int index)
        {
            var UpgradeData = GetUpgradeData(index);

            upgradeButtonWrapper[index].Children.Clear();

            Grid btnGrid = new Grid
            {
                //ShowGridLines = true, // debug
                Width = 200,
                Height = 120
            };

            RowDefinition btnGridRow = new RowDefinition();
            btnGridRow.Height = new GridLength(60, GridUnitType.Pixel);
            btnGrid.RowDefinitions.Add(btnGridRow);

            ColumnDefinition btnGridCol1 = new ColumnDefinition();
            ColumnDefinition btnGridCol2 = new ColumnDefinition();
            btnGridCol1.Width = new GridLength(45, GridUnitType.Pixel);
            btnGrid.ColumnDefinitions.Add(btnGridCol1);
            btnGrid.ColumnDefinitions.Add(btnGridCol2);

            // Setup button design
            Image BtnImage = new Image
            {
                Source = new BitmapImage(new Uri(UpgradeData.icon, UriKind.Relative)),
                Width = 32,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            btnGrid.Children.Add(BtnImage);
            Grid.SetColumn(BtnImage, 0);

            StringBuilder btnInfo = new StringBuilder();
            btnInfo.AppendLine(UpgradeData.name);
            btnInfo.AppendLine($"Kost {DisplayUpgradePrice(index)} asteroids");
            btnInfo.AppendLine($"{boughtUpgrades[index]} in bezit");

            Label BtnName = new Label
            {
                Content = btnInfo
            };

            btnGrid.Children.Add(BtnName);
            Grid.SetColumn(BtnName, 1);

            upgradeButtonWrapper[index].Children.Add(btnGrid);

            // Add tooltip to buttons
            StringBuilder tooltipInfo = new StringBuilder();
            tooltipInfo.AppendLine($"{UpgradeData.name}");
            tooltipInfo.AppendLine($"{UpgradeData.description}");
            tooltipInfo.AppendLine($"");
            tooltipInfo.Append($"Opbrengst: +{GetCookiesPerUpgrade(index)}/seconde");
            upgradeButton[index].ToolTip = tooltipInfo.ToString();

            // Enable/disable tooltip based on current asteroids
            if (amountOfAsteroids >= Math.Ceiling(GetUpgradePrice(index))) // ceil it here so they can't buy when button shows "18" (ceiled visually) but they have "17.X"
            {
                upgradeButton[index].IsEnabled = true;
            }
            else upgradeButton[index].IsEnabled = false;
        }

        private void AdjustUpgradeButtons()
        {
            for (int i = 0; i < MAX_UPGRADES; i++)
            {
                if (upgradeButton[i] != null && HasUpgradeUnlocked(i))
                {
                    if (upgradeButton[i].Visibility == Visibility.Visible)
                    {
                        SetUpgradeButtonText(i);
                    }
                }
            }
        }

        private void BtnUpgrade_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ProcessUpgradePurchase(button);
        }
        #endregion
        #region Bonus Upgrades

        Button[] BtnBonusUpgrade = new Button[MAX_UPGRADES]; 
        int[] amountOfBonusUpgrades = new int[MAX_UPGRADES];
        private void CreateBonusButton(StackPanel container, int index)
        {
            BtnBonusUpgrade[index] = new Button
            {
                Width = 200,
                Height = 55,
                Margin = new Thickness
                {
                    Top = 2.5,
                    Bottom = 2.5,
                    Left = 5,
                    Right = 5,
                },
                BorderThickness = new Thickness
                {
                    Top = 0,
                    Bottom = 0,
                    Left = 0,
                    Right = 0,
                },
                Background = Brushes.Gold
            };
            BtnBonusUpgrade[index].Click += OnBonusStoreClick;

            UpdateBonusUpgradeButtonText(index);
            ToggleBonusUpgradeButton(index);

            container.Children.Add(BtnBonusUpgrade[index]);
        }
        private void OnBonusStoreClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ProcessBonusPurchase(button);
        }


        private int GetIndexOfBonusUpgrade(Button button)
        {
            int specifier = -1;

            for (int i = 0; i < MAX_UPGRADES; i++)
            {
                if (button == BtnBonusUpgrade[i])
                {
                    specifier = i;
                    break;
                }
            }

            return specifier;
        }

        private void ProcessBonusPurchase(Button button)

        {
            int specifier = GetIndexOfBonusUpgrade(button);

            if (boughtUpgrades[specifier] == 0)
            {
                ShowFadeMessage("Aankoop Mislukt", "Je moet eerst de normale upgrade kopen alvorens je de bonus kunt aankopen.");
                return;
            }

            if (specifier != -1)
            {
                var UpgradeData = GetUpgradeData(specifier);
                if (GetUpgradePrice(specifier) <= amountOfAsteroids)
                {
                    amountOfAsteroids -= GetUpgradePrice(specifier);
                    amountOfBonusUpgrades[specifier]++;

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"Je hebt een {UpgradeData.name} bonus gekocht.");
                    sb.AppendLine($"Deze kostte {GetBonusUpgradeCost(specifier)} asteroïden.");
                    sb.AppendLine($"Je hebt nu nog {Math.Floor(amountOfAsteroids)} asteroïden over.");
                    sb.AppendLine("");
                    sb.AppendLine($"Alle opbrengsten worden nu {GetBonusUpgradeMultiplier(specifier)} keer verdubbeld.");
                    sb.AppendLine("");
                    sb.AppendLine($"Bonus opbrengst: {(GetCookiesPerUpgrade(specifier) * 100).ToString("N2")}/seconde");

                    ShowFadeMessage($"{UpgradeData.name} gekocht", sb.ToString());

                    AdjustInfoLabels(); // update game labels (title/score)
                    ToggleBonusUpgradeButtons(); // update bonus button labels
                    CheckQuestProgress_BuyBonusUpgrades(specifier);
                }
            }
            else MessageBox.Show("Er is iets misgegaan met de upgrade. (Knop is niet geinitialiseerd)");
        }

        private decimal GetBonusUpgradeCost(int index)
        {
            decimal basePrice = GetUpgradeData(index).bonusPrice;

            decimal defaultMultiplier = 100.0M;
            decimal factor = 0;
            for(int i = 0; i < amountOfBonusUpgrades[index]; i ++)
            {
                switch(factor)
                {
                    case 0:
                        defaultMultiplier = 100.0M;
                        break;
                    case 1:
                        defaultMultiplier *= 5;
                        break;
                    default:
                        defaultMultiplier *= 10;
                        break;
                }
                factor++;
            }

            if(amountOfBonusUpgrades[index] > 0)
            {
                return (basePrice * defaultMultiplier);
            }
            return basePrice;
        }

        private void ToggleBonusUpgradeButtons()
        {
            for(int i = 0; i < MAX_UPGRADES; i ++)
            {
                if (BtnBonusUpgrade[i] != null)
                {
                    UpdateBonusUpgradeButtonText(i);
                    ToggleBonusUpgradeButton(i);
                }
            }
        }

        private long GetBonusUpgradeMultiplier(int index)
        {
            long multiplier = 1;

            if (amountOfBonusUpgrades[index] > 0)
            {
                for (int i = 0; i < amountOfBonusUpgrades[index]; i++)
                {
                    multiplier *= 2;
                }
            }

            return multiplier;
        }

        private void UpdateBonusUpgradeButtonText(int index)
        {
            BtnBonusUpgrade[index].Content = "";

            var upgradeData = GetUpgradeData(index);

            Grid btnGrid = new Grid
            {
                Width = 200,
                Height = 120,
            };

            RowDefinition btnGridRow = new RowDefinition
            {
                Height = new GridLength(60, GridUnitType.Pixel)
            };
            btnGrid.RowDefinitions.Add(btnGridRow);

            ColumnDefinition btnGridCol1 = new ColumnDefinition
            {
                Width = new GridLength(45, GridUnitType.Pixel)
            };
            ColumnDefinition btnGridCol2 = new ColumnDefinition();
            btnGrid.ColumnDefinitions.Add(btnGridCol1);
            btnGrid.ColumnDefinitions.Add(btnGridCol2);

            // Setup button design
            Image BtnImage = new Image
            {
                Source = new BitmapImage(new Uri(upgradeData.bonusIcon, UriKind.Relative)),
                Width = 30,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness
                {
                    Top = 0,
                    Left = 0,
                    Right = 0,
                    Bottom = 0
                }
            };

            btnGrid.Children.Add(BtnImage);
            Grid.SetColumn(BtnImage, 0);

            StringBuilder btnInfo = new StringBuilder();
            btnInfo.AppendLine($"{upgradeData.name} Bonus: {GetBonusUpgradeMultiplier(index)}x");
            btnInfo.AppendLine($"Volgende Bonus: {GetBonusUpgradeMultiplier(index) * 2}x");
            btnInfo.AppendLine($"Koop voor {GetBonusUpgradeCost(index).ToString("c2")}");

            Label BtnName = new Label
            {
                Content = btnInfo.ToString(),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 10,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            btnInfo.Clear();

            btnGrid.Children.Add(BtnName);
            Grid.SetColumn(BtnName, 1);

            WrapPanel buttonWrapper = new WrapPanel();
            buttonWrapper.Children.Add(btnGrid);
            BtnBonusUpgrade[index].Content = buttonWrapper;
        }

        private void ToggleBonusUpgradeButton(int index)
        {
            if (GetBonusUpgradeCost(index) <= amountOfAsteroids)
            {
                BtnBonusUpgrade[index].IsEnabled = true;
            }
            else BtnBonusUpgrade[index].IsEnabled = false;
        }

        #endregion
        #region Shop/Upgrade System
        static int MAX_UPGRADES = 7;
        int[] boughtUpgrades = new int[MAX_UPGRADES];

        bool isShopPanelUnlocked = false;
        StackPanel shopStackPanel = new StackPanel
        {
            Margin = new Thickness
            {
                Top = 0,
                Bottom = 10,
                Left = 5,
                Right = 0
            },
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        private void CreateShopLayout()
        {
            if (isShopPanelUnlocked) return;
            if (!AccessToUpgrades()) return;

            isShopPanelUnlocked = true;
            StckUpgrades.Visibility = Visibility.Visible;
            StckUpgrades.Children.Clear();

            Viewbox viewbox = new Viewbox
            {
                Margin = new Thickness
                {
                    Top = 15,
                    Bottom = 10,
                    Left = 5,
                    Right = 5
                }
            };

            Label tmpStoreName = new Label
            {
                Foreground = Brushes.Purple,
                FontWeight = FontWeights.Bold,
                FontSize = 20,
                Effect = new DropShadowEffect
                {
                    ShadowDepth = 2,
                    Direction = 310,
                    BlurRadius = 2
                },
                Content = "Upgrade Shop",
                HorizontalAlignment = HorizontalAlignment.Center
            };

            shopStackPanel.Children.Add(tmpStoreName);

            ScrollViewer categoryScroller = new ScrollViewer
            {
                Margin = new Thickness
                {
                    Top = 35,
                    Bottom = 50,
                    Left = 20,
                    Right = 5,
                },
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = shopStackPanel,
                Width = 250,
                MaxHeight = 340
            };

            viewbox.Child = categoryScroller;
            StckUpgrades.Children.Add(viewbox);
        }

        private (string name, string description, decimal price, decimal output, string icon, string bonusIcon, decimal bonusPrice) GetUpgradeData(int index)
        {
            /*
                bonusPrices taken from https://cookieclicker.fandom.com/wiki/Upgrades (Farm Upgrades section)
                English ordinals converted to Dutch ordinals (million -> miljoen, billion -> miljard, etc)
             */
            var upgradeList = new (string, string, decimal, decimal, string, string, decimal)[]
            {
                ("Astronaut",  "Een astronaut verzameld automatisch asteroïden.", 15.0M, 0.001M, "/assets/images/icons/thumb_astronaut.png", "/assets/images/bonus_icons/bonus-astronaut.png", 11000.0M),
                ("Mine Blaster", "Een mine blaster veroorzaakt meer debris, dus meer asteroïden.", 100.0M, 0.01M, "/assets/images/icons/thumb_blaster.png", "/assets/images/bonus_icons/bonus-blaster.png", 55000.0M),
                ("Space Ship", "Een extra space ship versneld de vluchten heen en terug.", 1100.0M, 0.08M, "/assets/images/icons/thumb_rocket.png", "/assets/images/bonus_icons/bonus-rocket.png", 550000.0M),
                ("Mining Colony", "Een mining colony verzameld efficiënt meerdere asteroïden.", 12000.0M, 0.47M, "/assets/images/icons/thumb_miningcolony.png", "/assets/images/bonus_icons/bonus-miningcolony.png", 55000000.0M),
                ("Space Station", "Een space station wordt op een asteroïde geplaatst. Vluchten heen en weer zijn overbodig.", 130000.0M, 2.60M, "/assets/images/icons/thumb_spacestation.png", "/assets/images/bonus_icons/bonus-spacestation.png", 5500000000.0M),
                ("Hired Alien", "Een aliense huurling heeft buitenaardse technologie om meer te minen.", 1400000.0M, 14.00M, "/assets/images/icons/thumb_alien.png", "/assets/images/bonus_icons/bonus-alien.png", 550000000000.0M),
                ("Deathstar", "De nieuwste technology van de Galactic Empire: een laser schietende planeet.", 20000000.0M, 78.00M, "/assets/images/icons/thumb_deathstar.png", "/assets/images/bonus_icons/bonus-deathstar.png", 550000000000000.0M),
            };

            return upgradeList[index];
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
                    boughtUpgrades[specifier]++;
                    CheckQuestProgress_BuyUpgrades();
                    CheckQuestProgress_BuyUpgradeItem(specifier);

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"Je hebt een {UpgradeData.name} gekocht voor {DisplayUpgradePrice(specifier)} asteroïden.");
                    sb.AppendLine($"Je hebt nu nog {Math.Floor(amountOfAsteroids)} asteroïden over.");
                    sb.AppendLine("");
                    sb.AppendLine(UpgradeData.description);
                    sb.AppendLine("");
                    sb.AppendLine($"Automatische opbrengst: {UpgradeData.output*100}/seconde (x{GetBonusUpgradeMultiplier(specifier)} keer)");

                    ShowFadeMessage($"{UpgradeData.name} gekocht", sb.ToString());

                    AdjustInfoLabels(); // update game labels (title/score)
                    AdjustUpgradeButtons(); // update button labels
                    AdjustCategories(); // update categories
                }
            }
            else MessageBox.Show("Er is iets misgegaan met de upgrade. (Knop is niet geinitialiseerd)");
        }

        private int GetIndexOfUpgrade(Button button)
        {
            int specifier = -1;

            for(int i = 0; i < MAX_UPGRADES; i ++)
            {

                if(button == upgradeButton[i])
                {
                    specifier = i;
                    break;
                }
            }

            return specifier;
        }

        private decimal GetUpgradePrice(int upgrade)
        {
            var UpgradeData = GetUpgradeData(upgrade);
            decimal buy_price = UpgradeData.price * (1.15M * boughtUpgrades[upgrade]);
            if(buy_price <= 0)
            {
                buy_price = UpgradeData.price;
            }
            return buy_price;
        }

        private string DisplayUpgradePrice(int upgrade)
        {
            var digitFormat = new NumberFormatInfo
            {
                NumberGroupSeparator = " ",
            };
            string price = Math.Ceiling(GetUpgradePrice(upgrade)).ToString("#,0", digitFormat);
    
            return price;
        }

        private bool AccessToUpgrades()
        {
            bool unlocked = false;
            for (int i = 0; i < MAX_UPGRADES; i++)
            {
                var UpgradeData = GetUpgradeData(i);
                if (amountOfScore >= UpgradeData.price)
                {
                    unlocked = true;
                }
            }
            return unlocked;
        }
        private bool HasUpgradeUnlocked(int index)
        {
            var UpgradeData = GetUpgradeData(index);
            return (amountOfScore >= UpgradeData.price ? true : false);
        }

        #endregion
        #region Automatic Cookie Gain
        private void ProcessUpgradeOutput()
        {
            for(int i = 0; i < MAX_UPGRADES; i ++)
            {
                var UpgradeData = GetUpgradeData(i);
                if (boughtUpgrades[i] > 0)
                {
                    decimal addition = GetCookiesPerUpgrade(i);
                    amountOfAsteroids += addition;
                    amountOfScore += addition;
                    Console.WriteLine($"[{UpgradeData.name}] Adding {addition} ({GetBonusUpgradeMultiplier(i)} bonus (default: {UpgradeData.output}) for index {i}, new amount: {amountOfAsteroids}");
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


            PlaySyncedSound(Properties.Resources.blaster);

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
        #region Miner Namechanging
        private void LblMinerName_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // BUG: Cancelling returns 0 string size, so the error message is concieved incorrectly.
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Voer de gewenste naam van de miner in.");
            sb.AppendLine("");
            sb.Append("Opgelet: het veld mag niet leeg zijn of spaties bevatten.");

            string inputName = Interaction.InputBox(sb.ToString(), "Verander de naam", LblMinerName.Content.ToString());
            if(inputName.Length < 1 || inputName.Contains(" "))
            {
                ShowFadeMessage("Verandering van naam mislukt", "De nieuwe naam mag niet leeg zijn en mag geen spaties bevatten!");
                return;
            }
            LblMinerName.Content = inputName;
        }
        #endregion
        #region Categories
        // Dependant on upgrade index.
        string[] categoryImages =
        {
            "/assets/images/categories/surface_1.png", "/assets/images/categories/surface_2.png", "/assets/images/categories/surface_3.png",
            "/assets/images/categories/surface_4.png", "/assets/images/categories/surface_5.png", "/assets/images/categories/surface_6.png",
            "/assets/images/categories/surface_7.png"
        };

        private void AdjustCategories()
        {
            ScrollCategories.Visibility = Visibility.Visible; // only shown after purchase
            StckCategories.Children.Clear();

            Viewbox viewbox = new Viewbox
            {
                MaxWidth = 1000,
            };
            StackPanel stackpanel = new StackPanel
            {
                Width = 500
            };

            for (int i = 0; i < categoryImages.Length; i++)
            {
                if (boughtUpgrades[i] <= 0) continue; // skip creation incase no bought upgrades

                WrapPanel categoryWrapper = new WrapPanel
                {
                    Margin = new Thickness
                    {
                        Top = 5,
                        Left = 10,
                        Right = 10,
                        Bottom = 5
                    },
                    Height = 150,
                };
                stackpanel.Children.Add(categoryWrapper);

                ImageBrush categoryImg = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri($"pack://application:,,,{categoryImages[i]}")),
                    Stretch = Stretch.UniformToFill,
                    Opacity = 0.5
                };
                categoryWrapper.Background = categoryImg;

                SpawnCategoryTiles(categoryWrapper, i);
            }
            StckCategories.Children.Add(viewbox);
            viewbox.Child = stackpanel;
        }
        string[] categoryTiles =
        {
            "/assets/images/icons_big/upgrade_astronaut.png",
            "/assets/images/icons_big/upgrade_blaster.png",
            "/assets/images/icons_big/upgrade_rocket.png",
            "/assets/images/icons_big/upgrade_miningcolony.png",
            "/assets/images/icons_big/upgrade_spacestation.png",
            "/assets/images/icons_big/upgrade_ufo.png",
            "/assets/images/icons_big/upgrade_deathstar.png"
        };

        private void SpawnCategoryTiles(WrapPanel panel, int index) // index = category
        {
 
            WrapPanel tileContent = new WrapPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness
                {
                    Top = 10,
                    Bottom = 10
                }
            };

            for (int i = 0; i < boughtUpgrades[index]; i ++)
            {
                Image tileIcon = new Image
                {
                    Source = new BitmapImage(new Uri(categoryTiles[index], UriKind.Relative)),
                    Width = 96,
                    Height = 96
                };

                tileContent.Children.Add(tileIcon);
            }

            ScrollViewer categoryScroller = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                Content = tileContent,
                Height = 150
            };

            panel.Children.Add(categoryScroller);
        }
        #endregion
        #region Golden Cookie

        bool isGoldenCookieSpawned = false;
        int goldenCookieClicks = 0;
        private void SpawnGoldenCookie()
        {
            if(isGoldenCookieSpawned)
            {
                return;
            }

            Random rand = new Random();

            int spawn_chance = rand.Next(0, 100);
            Console.WriteLine($"Golden Cookie randomizer: {spawn_chance} (must be less than 30)");

            if (spawn_chance < 30)
            {
                CanvasGoldenCookie.Children.Clear();
                Image goldenCookie = new Image
                {
                    Source = new BitmapImage(new Uri("assets/images/goldenAsteroid.png", UriKind.Relative)),
                    Width = 64,
                    Height = 64,
                    IsHitTestVisible = true, // can click through image
                };
                goldenCookie.MouseDown += GoldenCookie_MouseDown;

                PlaySyncedSound(Properties.Resources.goldenCookieSound);

                Canvas.SetTop(goldenCookie, rand.Next(0, 300));
                Canvas.SetLeft(goldenCookie, rand.Next(0, 200));

                CanvasGoldenCookie.Children.Add(goldenCookie);
                isGoldenCookieSpawned = true;
            }
        }

        private decimal CalculateGoldenCookieReward()
        {
            // The following code snippet is ran per ms (see ProcessUpgradeOutput). 
            decimal reward = 0;
            for (int i = 0; i < MAX_UPGRADES; i++)
            {
                var UpgradeData = GetUpgradeData(i);
                if (boughtUpgrades[i] > 0)
                {
                    reward = GetCookiesPerUpgrade(i);
                }
            }

            return (reward * 900000); // 900 000ms = 15 minutes
        }

        private void GoldenCookie_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CanvasGoldenCookie.Children.Clear();
            isGoldenCookieSpawned = false;
            goldenCookieClicks++;
            CheckQuestProgress_GoldenCookieClicks();

            decimal addition = CalculateGoldenCookieReward();

            Random rand = new Random();
            if (addition == 0) addition += rand.Next(0, 150);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Je hebt de zeldzame gouden asteroïde gemined!");
            sb.AppendLine($"Deze is zeer waardevol. Je hebt {addition} verdient.");
            ShowFadeMessage("Gouden Asteroïden gevonden", sb.ToString());

            amountOfAsteroids += addition;
            amountOfScore += addition;
            Console.WriteLine($"[Golden Cookie] Adding {addition} (all gains * 15 minutes), new amount: {amountOfAsteroids}");
            AdjustInfoLabels();

        }
        #endregion
        #region Quests
        const int MAX_QUESTS = 110;

        enum QuestConstants
        {
            // Buy Quests,
            QUEST_BUY_UPGRADE_10 = 0,
            QUEST_BUY_UPGRADE_25,
            QUEST_BUY_UPGRADE_50,
            QUEST_BUY_UPGRADE_100,
            QUEST_BUY_UPGRADE_250,
            QUEST_BUY_UPGRADE_500,
            QUEST_BUY_UPGRADE_1000,
            QUEST_BUY_UPGRADE_2500,
            QUEST_BUY_UPGRADE_5000,
            QUEST_BUY_UPGRADE_10K,
            QUEST_BUY_UPGRADE_25K,
            QUEST_BUY_UPGRADE_50K,
            QUEST_BUY_UPGRADE_100K,
            QUEST_BUY_UPGRADE_250K,
            QUEST_BUY_UPGRADE_500K,
            QUEST_BUY_UPGRADE_1MIL,
            // Own Quests,
            QUEST_OWN_ASTRONAUTS_5,
            QUEST_OWN_ASTRONAUTS_10,
            QUEST_OWN_ASTRONAUTS_25,
            QUEST_OWN_ASTRONAUTS_50,
            QUEST_OWN_ASTRONAUTS_100,
            QUEST_OWN_MINEBLASTERS_5,
            QUEST_OWN_MINEBLASTERS_10,
            QUEST_OWN_MINEBLASTERS_25,
            QUEST_OWN_MINEBLASTERS_50,
            QUEST_OWN_MINEBLASTERS_100,
            QUEST_OWN_SPACESHIPS_5,
            QUEST_OWN_SPACESHIPS_10,
            QUEST_OWN_SPACESHIPS_25,
            QUEST_OWN_SPACESHIPS_50,
            QUEST_OWN_SPACESHIPS_100,
            QUEST_OWN_MININGCOLONIES_5,
            QUEST_OWN_MININGCOLONIES_10,
            QUEST_OWN_MININGCOLONIES_25,
            QUEST_OWN_MININGCOLONIES_50,
            QUEST_OWN_MININGCOLONIES_100,
            QUEST_OWN_SPACESTATIONS_5,
            QUEST_OWN_SPACESTATIONS_10,
            QUEST_OWN_SPACESTATIONS_25,
            QUEST_OWN_SPACESTATIONS_50,
            QUEST_OWN_SPACESTATIONS_100,
            QUEST_OWN_HIREDALIENS_5,
            QUEST_OWN_HIREDALIENS_10,
            QUEST_OWN_HIREDALIENS_25,
            QUEST_OWN_HIREDALIENS_50,
            QUEST_OWN_HIREDALIENS_100,
            QUEST_OWN_DEATHSTARS_5,
            QUEST_OWN_DEATHSTARS_10,
            QUEST_OWN_DEATHSTARS_25,
            QUEST_OWN_DEATHSTARS_50,
            QUEST_OWN_DEATHSTARS_100,
            // Buy Bonus Quests,
            QUEST_BUY_BONUS_ASTRONAUT_5,
            QUEST_BUY_BONUS_ASTRONAUT_10,
            QUEST_BUY_BONUS_ASTRONAUT_25,
            QUEST_BUY_BONUS_MINEBLASTER_5,
            QUEST_BUY_BONUS_MINEBLASTER_10,
            QUEST_BUY_BONUS_MINEBLASTER_25,
            QUEST_BUY_BONUS_SPACESHIP_5,
            QUEST_BUY_BONUS_SPACESHIP_10,
            QUEST_BUY_BONUS_SPACESHIP_25,
            QUEST_BUY_BONUS_SPACESTATION_5,
            QUEST_BUY_BONUS_SPACESTATION_10,
            QUEST_BUY_BONUS_SPACESTATION_25,
            QUEST_BUY_BONUS_MININGCOLONY_5,
            QUEST_BUY_BONUS_MININGCOLONY_10,
            QUEST_BUY_BONUS_MININGCOLONY_25,
            QUEST_BUY_BONUS_HIREDALIEN_5,
            QUEST_BUY_BONUS_HIREDALIEN_10,
            QUEST_BUY_BONUS_HIREDALIEN_25,
            QUEST_BUY_BONUS_DEATHSTAR_5,
            QUEST_BUY_BONUS_DEATHSTAR_10,
            QUEST_BUY_BONUS_DEATHSTAR_25,
            // Golden Cookie Quests,
            QUEST_CLICK_GOLDENCOOKIE_1,
            QUEST_CLICK_GOLDENCOOKIE_5,
            QUEST_CLICK_GOLDENCOOKIE_10,
            QUEST_CLICK_GOLDENCOOKIE_25,
            QUEST_CLICK_GOLDENCOOKIE_50,
            QUEST_CLICK_GOLDENCOOKIE_100,
            QUEST_CLICK_GOLDENCOOKIE_250,
            QUEST_CLICK_GOLDENCOOKIE_500,
            QUEST_CLICK_GOLDENCOOKIE_1000,
            // Asteroids Gained Quests,
            QUEST_ASTEROID_TOTAL_100,
            QUEST_ASTEROID_TOTAL_500,
            QUEST_ASTEROID_TOTAL_1K,
            QUEST_ASTEROID_TOTAL_5K,
            QUEST_ASTEROID_TOTAL_10K,
            QUEST_ASTEROID_TOTAL_25K,
            QUEST_ASTEROID_TOTAL_50K,
            QUEST_ASTEROID_TOTAL_100K,
            QUEST_ASTEROID_TOTAL_250K,
            QUEST_ASTEROID_TOTAL_500K,
            QUEST_ASTEROID_TOTAL_1000K,
            QUEST_ASTEROID_TOTAL_2500K,
            QUEST_ASTEROID_TOTAL_5000K,
            QUEST_ASTEROID_TOTAL_10M,
            QUEST_ASTEROID_TOTAL_25M,
            QUEST_ASTEROID_TOTAL_50M,
            QUEST_ASTEROID_TOTAL_100M,
            QUEST_ASTEROID_TOTAL_250M,
            QUEST_ASTEROID_TOTAL_500M,
            QUEST_ASTEROID_TOTAL_1000M,
            QUEST_ASTEROID_TOTAL_2500M,
            QUEST_ASTEROID_TOTAL_5000M,
            QUEST_ASTEROID_TOTAL_10B,
            QUEST_ASTEROID_TOTAL_25B,
            QUEST_ASTEROID_TOTAL_50B,
            QUEST_ASTEROID_TOTAL_100B,
            QUEST_ASTEROID_TOTAL_250B,
            QUEST_ASTEROID_TOTAL_500B,
            QUEST_ASTEROID_TOTAL_1T,
        };
        string[] questNames = new string[MAX_QUESTS]
        {
            "Buy 10 Upgrades",
            "Buy 25 Upgrades",
            "Buy 50 Upgrades",
            "Buy 100 Upgrades",
            "Buy 250 Upgrades",
            "Buy 500 Upgrades",
            "Buy 1,000 Upgrades",
            "Buy 2,500 Upgrades",
            "Buy 5,000 Upgrades",
            "Buy 10,000 Upgrades",
            "Buy 25,000 Upgrades",
            "Buy 50,000 Upgrades",
            "Buy 100,000 Upgrades",
            "Buy 250,000 Upgrades",
            "Buy 500,000 Upgrades",
            "Buy 1,000,000 Upgrades",
            "Own 5 Astronauts",
            "Own 10 Astronauts",
            "Own 25 Astronauts",
            "Own 50 Astronauts",
            "Own 100 Astronauts",
            "Own 5 Mine Blasters",
            "Own 10 Mine Blasters",
            "Own 25 Mine Blasters",
            "Own 50 Mine Blasters",
            "Own 100 Mine Blasters",
            "Own 5 Space Ships",
            "Own 10 Space Ships",
            "Own 25 Space Ships",
            "Own 50 Space Ships",
            "Own 100 Space Ships",
            "Own 5 Mining Colonies",
            "Own 10 Mining Colonies",
            "Own 25 Mining Colonies",
            "Own 50 Mining Colonies",
            "Own 100 Mining Colonies",
            "Own 5 Space Stations",
            "Own 10 Space Stations",
            "Own 25 Space Stations",
            "Own 50 Space Stations",
            "Own 100 Space Stations",
            "Own 5 Hired Aliens",
            "Own 10 Hired Aliens",
            "Own 25 Hired Aliens",
            "Own 50 Hired Aliens",
            "Own 100 Hired Aliens",
            "Own 5 Deathstars",
            "Own 10 Deathstars",
            "Own 25 Deathstars",
            "Own 50 Deathstars",
            "Own 100 Deathstars",
            "Buy 5 Astronaut Bonusses",
            "Buy 5 Mine Blasters Bonusses",
            "Buy 5 Space Ships Bonusses",
            "Buy 5 Mining Colonys Bonusses",
            "Buy 5 Space Stations Bonusses",
            "Buy 5 Hired Aliens Bonusses",
            "Buy 5 Deathstars Bonusses",
            "Buy 10 Astronaut Bonusses",
            "Buy 10 Mine Blasters Bonusses",
            "Buy 10 Space Ships Bonusses",
            "Buy 10 Mining Colonys Bonusses",
            "Buy 10 Space Stations Bonusses",
            "Buy 10 Hired Aliens Bonusses",
            "Buy 10 Deathstars Bonusses",
            "Buy 25 Astronaut Bonusses",
            "Buy 25 Mine Blasters Bonusses",
            "Buy 25 Space Ships Bonusses",
            "Buy 25 Mining Colonys Bonusses",
            "Buy 25 Space Stations Bonusses",
            "Buy 25 Hired Aliens Bonusses",
            "Buy 25 Deathstars Bonusses",
            "Click Golden Cookie 1 Time",
            "Click Golden Cookie 5 Times",
            "Click Golden Cookie 10 Times",
            "Click Golden Cookie 25 Times",
            "Click Golden Cookie 50 Times",
            "Click Golden Cookie 100 Times",
            "Click Golden Cookie 250 Times",
            "Click Golden Cookie 500 Times",
            "Click Golden Cookie 1000 Times",
            "Reach 100 Asteroids",
            "Reach 500 Asteroids",
            "Reach 1000 Asteroids",
            "Reach 5000 Asteroids",
            "Reach 10,000 Asteroids",
            "Reach 25,000 Asteroids",
            "Reach 50,000 Asteroids",
            "Reach 100,0000 Asteroids",
            "Reach 250,0000 Asteroids",
            "Reach 500,0000 Asteroids",
            "Reach 1,000,0000 Asteroids",
            "Reach 2,500,0000 Asteroids",
            "Reach 5,000,0000 Asteroids",
            "Reach 10,000,0000 Asteroids",
            "Reach 25,000,0000 Asteroids",
            "Reach 50,000,0000 Asteroids",
            "Reach 100,000,000 Asteroids",
            "Reach 250,000,000 Asteroids",
            "Reach 500,000,000 Asteroids",
            "Reach 1,000,000,000 Asteroids",
            "Reach 2,500,000,000 Asteroids",
            "Reach 5,000,000,000 Asteroids",
            "Reach 10,000,000,000 Asteroids",
            "Reach 25,000,000,000 Asteroids",
            "Reach 50,000,000,000 Asteroids",
            "Reach 100,000,000,000 Asteroids",
            "Reach 250,000,000,000 Asteroids",
            "Reach 500,000,000,000 Asteroids",
            "Reach 1,000,000,000,000 Asteroids"
        };
        bool[] questComplete = new bool[MAX_QUESTS];

        private void CheckQuestProgress_BuyBonusUpgrades(int index)
        {
            var upgradeData = GetUpgradeData(index);

            switch (upgradeData.name)
            {
                case "Astronaut":
                    if (amountOfBonusUpgrades[index] >= 25) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_ASTRONAUT_25);
                    else if (amountOfBonusUpgrades[index] >= 10) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_ASTRONAUT_10);
                    else if (amountOfBonusUpgrades[index] >= 5) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_ASTRONAUT_5);
                    break;
                case "Mine Blaster":
                    if (amountOfBonusUpgrades[index] >= 25) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_MINEBLASTER_25);
                    else if (amountOfBonusUpgrades[index] >= 10) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_MINEBLASTER_10);
                    else if (amountOfBonusUpgrades[index] >= 5) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_MINEBLASTER_5);
                    break;
                case "Space Ship":
                    if (amountOfBonusUpgrades[index] >= 25) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_SPACESHIP_25);
                    else if (amountOfBonusUpgrades[index] >= 10) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_SPACESHIP_10);
                    else if (amountOfBonusUpgrades[index] >= 5) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_SPACESHIP_5);
                    break;
                case "Mining Colony":
                    if (amountOfBonusUpgrades[index] >= 25) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_HIREDALIEN_25);
                    else if (amountOfBonusUpgrades[index] >= 10) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_MININGCOLONY_10);
                    else if (amountOfBonusUpgrades[index] >= 5) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_MININGCOLONY_5);
                    break;
                case "Space Station":
                    if (amountOfBonusUpgrades[index] >= 25) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_SPACESTATION_25);
                    else if (amountOfBonusUpgrades[index] >= 10) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_SPACESTATION_10);
                    else if (amountOfBonusUpgrades[index] >= 5) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_SPACESTATION_5);
                    break;
                case "Hired Alien":
                    if (amountOfBonusUpgrades[index] >= 25) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_MININGCOLONY_25);
                    else if (amountOfBonusUpgrades[index] >= 10) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_HIREDALIEN_10);
                    else if (amountOfBonusUpgrades[index] >= 5) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_HIREDALIEN_5);
                    break;
                case "Deathstar":
                    if (amountOfBonusUpgrades[index] >= 25) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_DEATHSTAR_25);
                    else if (amountOfBonusUpgrades[index] >= 10) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_DEATHSTAR_10);
                    else if (amountOfBonusUpgrades[index] >= 5) ProgressQuest(QuestConstants.QUEST_BUY_BONUS_DEATHSTAR_5);
                    break;
            }
        }
        private void CheckQuestProgress_GoldenCookieClicks()
        {
            if (goldenCookieClicks >= 1000) ProgressQuest(QuestConstants.QUEST_CLICK_GOLDENCOOKIE_1000);
            else if (goldenCookieClicks >= 500) ProgressQuest(QuestConstants.QUEST_CLICK_GOLDENCOOKIE_500);
            else if (goldenCookieClicks >= 250) ProgressQuest(QuestConstants.QUEST_CLICK_GOLDENCOOKIE_250);
            else if (goldenCookieClicks >= 100) ProgressQuest(QuestConstants.QUEST_CLICK_GOLDENCOOKIE_100);
            else if (goldenCookieClicks >= 50) ProgressQuest(QuestConstants.QUEST_CLICK_GOLDENCOOKIE_50);
            else if (goldenCookieClicks >= 25) ProgressQuest(QuestConstants.QUEST_CLICK_GOLDENCOOKIE_25);
            else if (goldenCookieClicks >= 10) ProgressQuest(QuestConstants.QUEST_CLICK_GOLDENCOOKIE_10);
            else if (goldenCookieClicks >= 5) ProgressQuest(QuestConstants.QUEST_CLICK_GOLDENCOOKIE_5);
            else if (goldenCookieClicks >= 1) ProgressQuest(QuestConstants.QUEST_CLICK_GOLDENCOOKIE_1);
        }
        private void CheckQuestProgress_BuyUpgrades()
        {
            long total = 0;
            for(int i = 0; i < MAX_UPGRADES; i ++)
            {
                if(boughtUpgrades[i] > 0)
                {
                    total += boughtUpgrades[i];
                }
            }
            if (total >= 1000000) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_1MIL);
            else if (total >= 500000) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_500K);
            else if (total >= 250000) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_250K);
            else if (total >= 100000) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_100K);
            else if (total >= 50000) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_50K);
            else if (total >= 25000) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_25K);
            else if (total >= 10000) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_10K);
            else if (total >= 5000) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_5000);
            else if (total >= 2500) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_2500);
            else if (total >= 1000) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_1000);
            else if (total >= 500) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_500);
            else if (total >= 250) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_250);
            else if (total >= 100) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_100);
            else if (total >= 50) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_50);
            else if (total >= 25) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_25);
            else if (total >= 10) ProgressQuest(QuestConstants.QUEST_BUY_UPGRADE_10);
        }
        private void CheckQuestProgress_TotalAsteroids()
        {
            if (amountOfAsteroids >= 1000000000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_1T);
            else if (amountOfAsteroids >= 100000000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_500B);
            else if (amountOfAsteroids >= 100000000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_250B);
            else if (amountOfAsteroids >= 100000000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_100B);
            else if (amountOfAsteroids >= 10000000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_50B);
            else if (amountOfAsteroids >= 10000000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_25B);
            else if (amountOfAsteroids >= 10000000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_10B);
            else if (amountOfAsteroids >= 1000000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_5000M);
            else if (amountOfAsteroids >= 1000000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_2500M);
            else if (amountOfAsteroids >= 1000000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_1000M);
            else if (amountOfAsteroids >= 100000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_500M);
            else if (amountOfAsteroids >= 100000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_250M);
            else if (amountOfAsteroids >= 100000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_100M);
            else if (amountOfAsteroids >= 50000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_50M);
            else if (amountOfAsteroids >= 25000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_25M);
            else if (amountOfAsteroids >= 10000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_10M);
            else if (amountOfAsteroids >= 5000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_5000K);
            else if (amountOfAsteroids >= 2500000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_2500K);
            else if (amountOfAsteroids >= 1000000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_1000K);
            else if (amountOfAsteroids >= 500000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_500K);
            else if (amountOfAsteroids >= 250000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_250K);
            else if (amountOfAsteroids >= 100000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_100K);
            else if (amountOfAsteroids >= 50000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_50K);
            else if (amountOfAsteroids >= 25000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_25K);
            else if (amountOfAsteroids >= 10000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_10K);
            else if (amountOfAsteroids >= 5000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_5K);
            else if (amountOfAsteroids >= 1000) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_1K);
            else if (amountOfAsteroids >= 500) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_500);
            else if (amountOfAsteroids >= 100) ProgressQuest(QuestConstants.QUEST_ASTEROID_TOTAL_100);
        }
        private void CheckQuestProgress_BuyUpgradeItem(int index)
        {
            var upgradeData = GetUpgradeData(index);

            switch(upgradeData.name)
            {
                case "Astronaut":
                    if (boughtUpgrades[index] >= 100) ProgressQuest(QuestConstants.QUEST_OWN_ASTRONAUTS_100);
                    else if (boughtUpgrades[index] >= 50) ProgressQuest(QuestConstants.QUEST_OWN_ASTRONAUTS_50);
                    else if (boughtUpgrades[index] >= 25) ProgressQuest(QuestConstants.QUEST_OWN_ASTRONAUTS_25);
                    else if (boughtUpgrades[index] >= 10) ProgressQuest(QuestConstants.QUEST_OWN_ASTRONAUTS_10);
                    else if (boughtUpgrades[index] >= 5) ProgressQuest(QuestConstants.QUEST_OWN_ASTRONAUTS_5);
                    break;
                case "Mine Blaster":                   
                    if (boughtUpgrades[index] >= 100) ProgressQuest(QuestConstants.QUEST_OWN_MINEBLASTERS_100);
                    else if (boughtUpgrades[index] >= 50) ProgressQuest(QuestConstants.QUEST_OWN_MINEBLASTERS_50);
                    else if (boughtUpgrades[index] >= 25) ProgressQuest(QuestConstants.QUEST_OWN_MINEBLASTERS_25);
                    else if (boughtUpgrades[index] >= 10) ProgressQuest(QuestConstants.QUEST_OWN_MINEBLASTERS_10);
                    else if (boughtUpgrades[index] >= 5) ProgressQuest(QuestConstants.QUEST_OWN_MINEBLASTERS_5);

                    break;
                case "Space Ship":
                    if (boughtUpgrades[index] >= 100) ProgressQuest(QuestConstants.QUEST_OWN_MINEBLASTERS_100);
                    else if (boughtUpgrades[index] >= 50) ProgressQuest(QuestConstants.QUEST_OWN_MINEBLASTERS_50);
                    else if (boughtUpgrades[index] >= 25) ProgressQuest(QuestConstants.QUEST_OWN_MINEBLASTERS_25);
                    else if (boughtUpgrades[index] >= 10) ProgressQuest(QuestConstants.QUEST_OWN_MINEBLASTERS_10);
                    else if (boughtUpgrades[index] >= 5) ProgressQuest(QuestConstants.QUEST_OWN_MINEBLASTERS_5);
                    break;
                case "Mining Colony":
                    if (boughtUpgrades[index] >= 100) ProgressQuest(QuestConstants.QUEST_OWN_MININGCOLONIES_100);
                    else if (boughtUpgrades[index] >= 50) ProgressQuest(QuestConstants.QUEST_OWN_MININGCOLONIES_50);
                    else if (boughtUpgrades[index] >= 25) ProgressQuest(QuestConstants.QUEST_OWN_MININGCOLONIES_25);
                    else if (boughtUpgrades[index] >= 10) ProgressQuest(QuestConstants.QUEST_OWN_MININGCOLONIES_10);
                    else if (boughtUpgrades[index] >= 5) ProgressQuest(QuestConstants.QUEST_OWN_MININGCOLONIES_5);
                    break;
                case "Space Station":
                    if (boughtUpgrades[index] >= 100) ProgressQuest(QuestConstants.QUEST_OWN_SPACESTATIONS_100);
                    else if (boughtUpgrades[index] >= 50) ProgressQuest(QuestConstants.QUEST_OWN_SPACESTATIONS_50);
                    else if (boughtUpgrades[index] >= 25) ProgressQuest(QuestConstants.QUEST_OWN_SPACESTATIONS_25);
                    else if (boughtUpgrades[index] >= 10) ProgressQuest(QuestConstants.QUEST_OWN_SPACESTATIONS_10);
                    else if (boughtUpgrades[index] >= 5) ProgressQuest(QuestConstants.QUEST_OWN_SPACESTATIONS_5);
                    break;
                case "Hired Alien":
                    if (boughtUpgrades[index] >= 100) ProgressQuest(QuestConstants.QUEST_OWN_HIREDALIENS_100);
                    else if (boughtUpgrades[index] >= 50) ProgressQuest(QuestConstants.QUEST_OWN_HIREDALIENS_50);
                    else if (boughtUpgrades[index] >= 25) ProgressQuest(QuestConstants.QUEST_OWN_HIREDALIENS_25);
                    else if (boughtUpgrades[index] >= 10) ProgressQuest(QuestConstants.QUEST_OWN_HIREDALIENS_10);
                    else if (boughtUpgrades[index] >= 5) ProgressQuest(QuestConstants.QUEST_OWN_HIREDALIENS_5);
                    break;
                case "Deathstar":
                    if (boughtUpgrades[index] >= 100) ProgressQuest(QuestConstants.QUEST_OWN_DEATHSTARS_100);
                    else if (boughtUpgrades[index] >= 50) ProgressQuest(QuestConstants.QUEST_OWN_DEATHSTARS_50);
                    else if (boughtUpgrades[index] >= 25) ProgressQuest(QuestConstants.QUEST_OWN_DEATHSTARS_25);
                    else if (boughtUpgrades[index] >= 10) ProgressQuest(QuestConstants.QUEST_OWN_DEATHSTARS_10);
                    else if (boughtUpgrades[index] >= 5) ProgressQuest(QuestConstants.QUEST_OWN_DEATHSTARS_5);
                    break;
            }
        }

        private void ProgressQuest(QuestConstants constant)
        {
            int index = (int)constant;
            Console.WriteLine($"Quest Completed for {index}: {questComplete[index]}");
            if (questComplete[index]) return;
            questComplete[index] = true;

            ShowQuestMessage("Quest Unlocked", $"{questNames[index]}");
        }

        private async void ShowQuestMessage(string title, string text)
        {
            // BUG: This often overlaps useful information. Make it so if this is visible (or FadeMessage), the other gets delayed. This can also be used for overlapping FadeMessages.
            LblQuestUpdateTitle.Content = title;
            LblQuestUpdateText.Content = text;

            StckQuestOverlay.Opacity = 0.0;
            StckQuestOverlay.Visibility = Visibility.Visible;
            while (StckQuestOverlay.Opacity <= 1.0)
            {
                await Task.Delay(20);
                StckQuestOverlay.Opacity += 0.15;
            }
            StckQuestOverlay.Opacity = 1.0;

            await Task.Delay(3500);

            while (StckQuestOverlay.Opacity > 0.0)
            {
                await Task.Delay(20);
                StckQuestOverlay.Opacity -= 0.15;
            }
            StckQuestOverlay.Opacity = 0.0;
            StckQuestOverlay.Visibility = Visibility.Hidden;
        }
        #endregion
        #region Quest List
        bool IsQuestListToggled = false;
        private void ToggleQuestList()
        {
            if (!IsQuestListToggled)
            {
                ViewboxQuestList.Visibility = Visibility.Hidden;
                IsQuestListToggled = true;
            }
            else
            {
                IsQuestListToggled = false;
                StackQuestList.Children.Clear();

                for (int i = 0; i < MAX_QUESTS; i++)
                {
                    if (questComplete[i])
                    {
                        AddQuestListItem(questNames[i]);
                    }
                }
                ViewboxQuestList.Visibility = Visibility.Visible;
            }
        }

        private void AddQuestListItem(string description)
        {
            WrapPanel listItem = new WrapPanel
            {
                Margin = new Thickness
                {
                    Left = 5,
                    Right = 5,
                    Top = 5,
                    Bottom = 5
                },
                Background = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri($"pack://application:,,,/assets/black.png")),
                    Stretch = Stretch.UniformToFill,
                    Opacity = 0.6
                }
            };

            Label listDesc = new Label
            {
                Foreground = Brushes.White,
                FontSize = 16
            };
            listDesc.Content = description;
            listItem.Children.Add(listDesc);

            StackQuestList.Children.Add(listItem);
        }
        #endregion
        #region Menu Buttons

        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OnClickMenu(button);
        }

        private void OnClickMenu(Button button)
        {
            switch(button.Name)
            {
                case "BtnMenuClose":
                    MessageBox.Show($"Game wordt gesloten. Score: {amountOfScore}");
                    this.Close();
                    break;
                case "BtnMenuQuest":
                    ToggleQuestList();
                    break;
                case "BtnMenuMute":
                    ToggleMute();
                    break;
            }
        }
        #endregion

        #region Muting Sound
        bool IsSoundMuted = false;

        private void ToggleMute()
        {
            if (IsSoundMuted)
            {

                Image btnImage = new Image
                {
                    Source = new BitmapImage(new Uri("/assets/images/btn_icons/btn_mute_off.png", UriKind.Relative)),
                };
                BtnMenuMute.Content = btnImage;
                IsSoundMuted = false;
            }
            else
            {
                IsSoundMuted = true;

                Image btnImage = new Image
                {
                    Source = new BitmapImage(new Uri("/assets/images/btn_icons/btn_mute_on.png", UriKind.Relative)),
                };

                BtnMenuMute.Content = btnImage;
            }
        }

        private void PlaySyncedSound(System.IO.Stream stream)
        {
            if (!IsSoundMuted)
            {
                var sound = new System.Media.SoundPlayer();
                sound.Stop();
                sound.Stream = stream;
                sound.Play();
            }
        }

        #endregion
    }
}
