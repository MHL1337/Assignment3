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
    public class Cinemas
    {
        public int ID { get; set; }
        [MaxLength(255)]
        [Required]
        public string Name { get; set; }
        [MaxLength(255)]
        [Required]
        public string City { get; set; }

        public List<Screenings> Screenings { get; set; }
    }

    public class Tickets
    {
        public int ID { get; set; }
        [Required]
        public int ScreeningID { get; set; }
        [ForeignKey("ScreeningID")]
        public Screenings Screenings { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime TimePurchased { get; set; }
    }

    public class Screenings
    {
        public int ID { get; set; }
        [Column(TypeName = "time(0)")]
        public TimeSpan Time { get; set; }

        [Required]
        public int CinemaID { get; set; }
        [ForeignKey("CinemaID")]
        public Cinemas Cinemas { get; set; }

        [Required]
        public int MovieID { get; set; }
        [ForeignKey("MovieID")]
        public Movies Movies { get; set; }
    }

    public class Movies
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

        public List<Screenings> Screenings { get; set; }
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
            public DbSet<Movies> movies { get; set; }
            public DbSet<Screenings> screenings { get; set; }
            public DbSet<Tickets> tickets { get; set; }
            public DbSet<Cinemas> cinemas { get; set; }

            protected override void OnModelCreating(ModelBuilder builder)
            {
                builder.Entity<Cinemas>(entity => {
                    entity.HasIndex(e => e.Name).IsUnique();
                });
            }

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
                cities = database.cinemas.Select(c => c.City).Distinct().ToList();

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
                foreach (var item in database.cinemas)
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

                List<Screenings> screeningList = new List<Screenings>();
                foreach (var item in from s in database.screenings
                                     join c in database.cinemas on s.CinemaID equals c.ID
                                     join m in database.movies on s.MovieID equals m.ID
                                     where c.Name == cinema
                                     orderby s.Time
                                     select s)
                {
                    screeningList.Add(item);
                }


                for (int i = 0; i < screeningList.Count; i++)
                {
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
                    int screeningID = screeningList[i].ID;

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

                    var movieId = screeningList[i].MovieID;
                    var posterPath = database.movies.Where(m => m.ID == movieId).Select(m => m.PosterPath).FirstOrDefault();
                    var title = database.movies.Where(m => m.ID == movieId).Select(m => m.Title).FirstOrDefault();
                    var releasedate = database.movies.Where(m => m.ID == movieId).Select(m => m.ReleaseDate).FirstOrDefault();
                    var runTime = database.movies.Where(m => m.ID == movieId).Select(m => m.Runtime).FirstOrDefault();

                    var image = CreateImage(@"Posters\" + posterPath);
                    image.Width = 50;
                    image.Margin = spacing;
                    image.ToolTip = new ToolTip { Content = title };
                    AddToGrid(grid, image, 0, 0);
                    Grid.SetRowSpan(image, 3);

                    var time = (TimeSpan)screeningList[i].Time;
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
                var TicketItem = new Tickets { TimePurchased = DateTime.Now };
                TicketItem.Screenings = database.screenings.First(s => s.ID == screeningID);
                List<Tickets> TicketList = new List<Tickets>();
                int count = 0;

                foreach (var item in database.tickets)
                {
                    TicketList.Add(item); //Checks if we already have a ticket for this screening
                    if (item.Screenings == TicketItem.Screenings)
                    {
                        count = 1;
                        MessageBox.Show("You already Have a ticket");
                    }

                }
                if (count == 0) //If we dont have, we add the ticket
                {
                    database.tickets.Add(TicketItem);
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

                List<Tickets> ticketList = new List<Tickets>();
                foreach (var item in from t in database.tickets
                                     join s in database.screenings on t.ScreeningID equals s.ID
                                     join m in database.movies on s.MovieID equals m.ID
                                     join c in database.cinemas on s.CinemaID equals c.ID
                                     orderby t.TimePurchased
                                     select t)
                {
                    ticketList.Add(item);
                }

                // For each ticket:
                for (int i = 0; i < ticketList.Count; i++)
                {
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
                    int ticketID = Convert.ToInt32(ticketList[i].ID);

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

                    var screeningId = ticketList[i].ScreeningID;
                    var movieId = database.screenings.Where(s => s.ID == screeningId).Select(s => s.MovieID).FirstOrDefault();
                    var cinemaId = database.screenings.Where(s => s.ID == screeningId).Select(s => s.CinemaID).FirstOrDefault();
                    var posterPath = database.movies.Where(m => m.ID == movieId).Select(m => m.PosterPath).FirstOrDefault();
                    var title = database.movies.Where(m => m.ID == movieId).Select(m => m.Title).FirstOrDefault();
                    var timeVariable = database.screenings.Where(s => s.ID == screeningId).Select(s => s.Time).FirstOrDefault();
                    var nameVariable = database.cinemas.Where(c => c.ID == cinemaId).Select(c => c.Name).FirstOrDefault();

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
                List<Tickets> TicketList = new List<Tickets>();

                foreach (var ticket in database.tickets)
                {
                    TicketList.Add(ticket);
                }


                Tickets ticketObject = TicketList.First(x => x.ID == ticketID);
                database.tickets.Remove(ticketObject);
                database.SaveChanges();
                //string deleteSql = "DELETE FROM Tickets WHERE ID = @TicketID";
                //var command = new SqlCommand(deleteSql, connection);
                //command.Parameters.AddWithValue("@TicketID", ticketID);
                //command.ExecuteNonQuery();

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
