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

namespace AsteroidClicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Global variables
        double amountOfAsteroids = 0.0; // Cookies
        double amountOfScore = 0.0; // Score

        // Native functions
        public MainWindow()
        {
            InitializeComponent();

            DispatcherTimer ms_timer = new DispatcherTimer();
            ms_timer.Interval = TimeSpan.FromMilliseconds(10);
            ms_timer.Tick += MS_Timer;
            ms_timer.Start();

            blast_timer.Interval = TimeSpan.FromMilliseconds(25);
            blast_timer.Tick += Blast_Timer;
        }

        bool IsMouseInsideImage = false;
        bool ValidMouseClick = false;
        private void ImgAsteroid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseInsideImage) // Check if the pointer is actually inside our image
            {
                amountOfAsteroids++;
                amountOfScore++;

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

        /*************************************************************************************************************************************
        **************************************************************************************************************************************/
        // Custom functions
        private void MS_Timer(object sender, EventArgs e)
        {
            MoveFallingParticles();
        }

        /*************************************************************************************************************************************
        **************************************************************************************************************************************
        **************************************************************************************************************************************
        **************************************************************************************************************************************/
        // Particle system (blast OnClick + falling particles)

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

        /*************************************************************************************************************************************
        **************************************************************************************************************************************/
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

        /*************************************************************************************************************************************
        **************************************************************************************************************************************
        **************************************************************************************************************************************
        **************************************************************************************************************************************/
    }
}
