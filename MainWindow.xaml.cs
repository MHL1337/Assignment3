using System;
using System.Collections.Generic;
using System.Linq;
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
using GeographyTools;
using Windows.Devices.Geolocation;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assignment3
{
    [Table("Cinemas")]
    [Index(nameof(Name), IsUnique = true)]
    public class Cinema
    {
        public int ID { get; set; }
        [MaxLength(255)]
        [Required]
        public string Name { get; set; }
        [MaxLength(255)]
        [Required]
        public string City { get; set; }

        public List<Screening> Screenings { get; set; }
    }

    [Table("Tickets")]
    public class Ticket
    {
        public int ID { get; set; }
        [Required]
        public int ScreeningID { get; set; }
        [ForeignKey("ScreeningID")]
        public Screening Screenings { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime TimePurchased { get; set; }
    }

    [Table("Screenings")]
    public class Screening
    {
        public int ID { get; set; }
        [Column(TypeName = "time(0)")]
        public TimeSpan Time { get; set; }

        [Required]
        public int CinemaID { get; set; }
        [ForeignKey("CinemaID")]
        public Cinema Cinemas { get; set; }

        [Required]
        public int MovieID { get; set; }
        [ForeignKey("MovieID")]
        public Movie Movies { get; set; }
    }

    [Table("Movies")]
    public class Movie
    {
        public int ID { get; set; }
        [Required]
        [MaxLength(255)]
        public string Title { get; set; }
        public Int16 Runtime { get; set; }
        [Column(TypeName = "date")]
        public DateTime ReleaseDate { get; set; }
        [Required]
        [MaxLength(255)]
        public string PosterPath { get; set; }

        public List<Screening> Screenings { get; set; }
    }


    public partial class MainWindow : Window
    {
        private static AppDbContext database;

        private Thickness spacing = new Thickness(5);
        private FontFamily mainFont = new FontFamily("Constantia");

        // Some GUI elements that we need to access in multiple methods.
        private ComboBox cityComboBox;
        private ListBox cinemaListBox;
        private StackPanel screeningPanel;
        private StackPanel ticketPanel;

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }

        public class AppDbContext : DbContext
        {
            public DbSet<Movie> Movies { get; set; }
            public DbSet<Screening> Screenings { get; set; }
            public DbSet<Ticket> Tickets { get; set; }
            public DbSet<Cinema> Cinemas { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder options)
            {
                options.UseSqlServer(@"Server=(local)\SQLExpress;Database=DataAccessGUIAssignment;Integrated Security=SSPI;");
            }
        }

        private void Start()
        {
            // Window options
            Title = "Cinemania";
            Width = 1000;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Brushes.Black;

            // Main grid
            var grid = new Grid();
            Content = grid;
            grid.Margin = spacing;
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            AddToGrid(grid, CreateCinemaGUI(), 0, 0);
            AddToGrid(grid, CreateScreeningGUI(), 0, 1);
            AddToGrid(grid, CreateTicketGUI(), 0, 2);
        }

        // Create the cinema part of the GUI: the left column.
        private UIElement CreateCinemaGUI()
        {
            var grid = new Grid
            {
                MinWidth = 200
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var title = new TextBlock
            {
                Text = "Select Cinema",
                FontFamily = mainFont,
                Foreground = Brushes.White,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = spacing
            };
            AddToGrid(grid, title, 0, 0);

            // Create the dropdown of cities.
            cityComboBox = new ComboBox
            {
                Margin = spacing
            };
            foreach (string city in GetCities())
            {
                cityComboBox.Items.Add(city);
            }
            cityComboBox.SelectedIndex = 0;
            AddToGrid(grid, cityComboBox, 1, 0);

            // When we select a city, update the GUI with the cinemas in the currently selected city.
            cityComboBox.SelectionChanged += (sender, e) =>
            {
                UpdateCinemaList();
            };

            // Create the dropdown of cinemas.
            cinemaListBox = new ListBox
            {
                Margin = spacing
            };
            AddToGrid(grid, cinemaListBox, 2, 0);
            UpdateCinemaList();

            // When we select a cinema, update the GUI with the screenings in the currently selected cinema.
            cinemaListBox.SelectionChanged += (sender, e) =>
            {
                UpdateScreeningList();
            };

            return grid;
        }

        // Create the screening part of the GUI: the middle column.
        private UIElement CreateScreeningGUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var title = new TextBlock
            {
                Text = "Select Screening",
                FontFamily = mainFont,
                Foreground = Brushes.White,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = spacing
            };
            AddToGrid(grid, title, 0, 0);

            var scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            AddToGrid(grid, scroll, 1, 0);

            screeningPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            scroll.Content = screeningPanel;

            UpdateScreeningList();

            return grid;
        }

        // Create the ticket part of the GUI: the right column.
        private UIElement CreateTicketGUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var title = new TextBlock
            {
                Text = "My Tickets",
                FontFamily = mainFont,
                Foreground = Brushes.White,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = spacing
            };
            AddToGrid(grid, title, 0, 0);

            var scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            AddToGrid(grid, scroll, 1, 0);

            ticketPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            scroll.Content = ticketPanel;

            // Update the GUI with the initial list of tickets.
            UpdateTicketList();

            return grid;
        }

        // Get a list of all cities that have cinemas in them.
        private IEnumerable<string> GetCities()
        {
            using (database = new AppDbContext())
            {
                List<string> cities = new List<string>();
                cities = database.Cinemas.Select(c => c.City).Distinct().ToList();

                return cities;
            }
        }


        // Get a list of all cinemas in the currently selected city.
        private IEnumerable<string> GetCinemasInSelectedCity()
        {
            using (database = new AppDbContext())
            {
                string currentCity = (string)cityComboBox.SelectedItem;
                var cinemas = new List<string>();
                foreach (var item in database.Cinemas)
                {
                    if (item.City == currentCity)
                    {
                        cinemas.Add(item.Name);
                    }
                }

                return cinemas;
            }
        }

        // Update the GUI with the cinemas in the currently selected city.
        private void UpdateCinemaList()
        {
            cinemaListBox.Items.Clear();
            foreach (string cinema in GetCinemasInSelectedCity())
            {
                cinemaListBox.Items.Add(cinema);
            }
        }

        // Update the GUI with the screenings in the currently selected cinema.
        private void UpdateScreeningList()
        {
            using (database = new AppDbContext())
            {
                screeningPanel.Children.Clear();
                if (cinemaListBox.SelectedIndex == -1)
                {
                    return;
                }

                string cinema = (string)cinemaListBox.SelectedItem;

                var screeningList = database.Screenings
                    .Include(s => s.Movies)
                    .Include(s => s.Cinemas)
                    .Where(s => s.Cinemas.Name == cinema).ToList();

                foreach (var screening in screeningList)
                {
                    var movieId = screening.MovieID;
                    var posterPath = database.Movies.Where(m => m.ID == movieId).Select(m => m.PosterPath).FirstOrDefault();
                    var title = database.Movies.Where(m => m.ID == movieId).Select(m => m.Title).FirstOrDefault();
                    var releasedate = database.Movies.Where(m => m.ID == movieId).Select(m => m.ReleaseDate).FirstOrDefault();
                    var runTime = database.Movies.Where(m => m.ID == movieId).Select(m => m.Runtime).FirstOrDefault();


                    // Create the button that will show all the info about the screening and let us buy a ticket for it.
                    var button = new Button
                    {
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = spacing,
                        Cursor = Cursors.Hand,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch
                    };
                    screeningPanel.Children.Add(button);
                    int screeningID = screening.ID;

                    // When we click a screening, buy a ticket for it and update the GUI with the latest list of tickets.
                    button.Click += (sender, e) =>
                    {
                        BuyTicket(screeningID);
                    };

                    // The rest of this method is just creating the GUI element for the screening.
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.RowDefinitions.Add(new RowDefinition());
                    grid.RowDefinitions.Add(new RowDefinition());
                    grid.RowDefinitions.Add(new RowDefinition());
                    button.Content = grid;

                    var image = CreateImage(@"Posters\" + posterPath);
                    image.Width = 50;
                    image.Margin = spacing;
                    image.ToolTip = new ToolTip { Content = title };
                    AddToGrid(grid, image, 0, 0);
                    Grid.SetRowSpan(image, 3);

                    //var time = (TimeSpan)screeningList[i].Time;
                    var time = (TimeSpan)screening.Time;
                    var timeHeading = new TextBlock
                    {
                        Text = TimeSpanToString(time),
                        Margin = spacing,
                        FontFamily = new FontFamily("Corbel"),
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Yellow
                    };
                    AddToGrid(grid, timeHeading, 0, 1);

                    var titleHeading = new TextBlock
                    {
                        Text = Convert.ToString(title),
                        Margin = spacing,
                        FontFamily = mainFont,
                        FontSize = 16,
                        Foreground = Brushes.White,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    AddToGrid(grid, titleHeading, 1, 1);

                    var releaseDate = Convert.ToDateTime(releasedate);
                    int runtimeMinutes = Convert.ToInt32(runTime);
                    var runtime = TimeSpan.FromMinutes(runtimeMinutes);
                    string runtimeString = runtime.Hours + "h " + runtime.Minutes + "m";
                    var details = new TextBlock
                    {
                        Text = "📆 " + releaseDate.Year + "     ⏳ " + runtimeString,
                        Margin = spacing,
                        FontFamily = new FontFamily("Corbel"),
                        Foreground = Brushes.Silver
                    };
                    AddToGrid(grid, details, 2, 1);
                }
            }
        }

        // Buy a ticket for the specified screening and update the GUI with the latest list of tickets.
        private void BuyTicket(int screeningID)
        {
            using (database = new AppDbContext())
            {
                var TicketItem = new Ticket { TimePurchased = DateTime.Now };
                TicketItem.Screenings = database.Screenings.First(s => s.ID == screeningID);
                List<Ticket> TicketList = new List<Ticket>();
                int count = 0;

                foreach (var item in database.Tickets)
                {
                    TicketList.Add(item); //Checks if we already have a ticket for this screening
                    if (item.Screenings == TicketItem.Screenings)
                    {
                        count = 1;
                    }
                }
                if (count == 0) //If we dont have, we add the ticket
                {
                    database.Tickets.Add(TicketItem);
                    database.SaveChanges();
                }

                UpdateTicketList();
            }
        }

        // Update the GUI with the latest list of tickets
        private void UpdateTicketList()
        {
            using (database = new AppDbContext())
            {
                ticketPanel.Children.Clear();

                var ticketList = database.Tickets
                   .Include(t => t.Screenings)
                   .ThenInclude(s => s.Cinemas)
                   .Include(t => t.Screenings)
                   .ThenInclude(s => s.Movies)
                   .ToList();

                foreach (var ticket in ticketList)
                {
                    var screeningId = ticket.ScreeningID;
                    var movieId = database.Screenings.Where(s => s.ID == screeningId).Select(s => s.MovieID).FirstOrDefault();
                    var cinemaId = database.Screenings.Where(s => s.ID == screeningId).Select(s => s.CinemaID).FirstOrDefault();
                    var posterPath = database.Movies.Where(m => m.ID == movieId).Select(m => m.PosterPath).FirstOrDefault();
                    var title = database.Movies.Where(m => m.ID == movieId).Select(m => m.Title).FirstOrDefault();
                    var timeVariable = database.Screenings.Where(s => s.ID == screeningId).Select(s => s.Time).FirstOrDefault();
                    var nameVariable = database.Cinemas.Where(c => c.ID == cinemaId).Select(c => c.Name).FirstOrDefault();

                    // Create the button that will show all the info about the ticket and let us remove it.
                    var button = new Button
                    {
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = spacing,
                        Cursor = Cursors.Hand,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch
                    };
                    ticketPanel.Children.Add(button);
                    int ticketID = Convert.ToInt32(ticket.ID);

                    // When we click a ticket, remove it and update the GUI with the latest list of tickets.
                    button.Click += (sender, e) =>
                    {
                        RemoveTicket(ticketID);
                    };

                    // The rest of this method is just creating the GUI element for the screening.
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.RowDefinitions.Add(new RowDefinition());
                    grid.RowDefinitions.Add(new RowDefinition());
                    button.Content = grid;

                    var image = CreateImage(@"Posters\" + posterPath);
                    image.Width = 30;
                    image.Margin = spacing;
                    image.ToolTip = new ToolTip { Content = title };
                    AddToGrid(grid, image, 0, 0);
                    Grid.SetRowSpan(image, 2);

                    var titleHeading = new TextBlock
                    {
                        Text = Convert.ToString(title),
                        Margin = spacing,
                        FontFamily = mainFont,
                        FontSize = 14,
                        Foreground = Brushes.White,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    AddToGrid(grid, titleHeading, 0, 1);

                    var time = (TimeSpan)timeVariable;
                    string timeString = TimeSpanToString(time);
                    var timeAndCinemaHeading = new TextBlock
                    {
                        Text = timeString + " - " + nameVariable,
                        Margin = spacing,
                        FontFamily = new FontFamily("Corbel"),
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Yellow,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    AddToGrid(grid, timeAndCinemaHeading, 1, 1);
                }
            }
        }

        // Remove the ticket for the specified screening and update the GUI with the latest list of tickets.

        public void RemoveTicket(int ticketID)
        {
            using (database = new AppDbContext())
            {
                List<Ticket> TicketList = new List<Ticket>();

                foreach (var ticket in database.Tickets)
                {
                    TicketList.Add(ticket);
                }


                Ticket ticketObject = TicketList.First(x => x.ID == ticketID);
                database.Tickets.Remove(ticketObject);
                database.SaveChanges();

                UpdateTicketList();
            }
        }

        // Helper method to add a GUI element to the specified row and column in a grid.
        private void AddToGrid(Grid grid, UIElement element, int row, int column)
        {
            grid.Children.Add(element);
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
        }

        // Helper method to create a high-quality image for the GUI.
        private Image CreateImage(string filePath)
        {
            ImageSource source = new BitmapImage(new Uri(filePath, UriKind.RelativeOrAbsolute));
            Image image = new Image
            {
                Source = source,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            return image;
        }

        // Helper method to turn a TimeSpan object into a string, such as 2:05.
        private string TimeSpanToString(TimeSpan timeSpan)
        {
            string hourString = timeSpan.Hours.ToString().PadLeft(2, '0');
            string minuteString = timeSpan.Minutes.ToString().PadLeft(2, '0');
            string timeString = hourString + ":" + minuteString;
            return timeString;
        }
    }
}
