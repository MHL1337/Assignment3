using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
        [Key]
        public int ID { get; set; }
        [MaxLength(255)]
        [Required]
        public string Name { get; set; }
        [MaxLength(255)]
        [Required]
        public string City { get; set; }
        public List<Screenings> CinemaID { get; set; }

    }

    public class Tickets
    {
        [Key]
        public int ID { get; set; }
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime TimePurchased { get; set; }
        [Required]
        public Screenings Screening { get; set; }

    }

    public class Screenings
    {
        [Key]
        public int ID { get; set; }
        [Required]
        [Column(TypeName = "time(0)")]
        public TimeSpan Time { get; set; }
        [Required]
        public Movies Movie { get; set; }
        [Required]
        public Cinemas Cinema { get; set; }
        [Required]
        public List<Tickets> ScreeningID { get; set; }
    }


    public class Movies
    {
        [Key]
        public int ID { get; set; }
        [Required]
        [Column(TypeName = "nvarchar(255)")]
        public string Title { get; set; }
        [Required]
        [Column(TypeName = "smallint")]
        public int Runtime { get; set; }
        [Required]
        [Column(TypeName = "date")]
        public DateTime ReleaseDate { get; set; }
        [Required]
        [Column(TypeName = "nvarchar(255)")]
        public string PosterPath { get; set; }
        public List<Screenings> MovieID { get; set; }
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

        // An SQL connection that we will keep open for the entire program.
        private SqlConnection connection;

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

                //builder.Entity<Screenings>()
                //  .Property(p => p.Time)
                //  .HasColumnType("time(0)");

            }

            protected override void OnConfiguring(DbContextOptionsBuilder options)
            {
                options.UseSqlServer(@"Server=(local)\SQLExpress;Database=DataAccessGUIAssignment;Integrated Security=SSPI;");
            }
        }

        private void Start()
        {
            connection = new SqlConnection(@"Server=(local)\SQLExpress;Database=DataAccessGUIAssignment;Integrated Security=SSPI;");
            connection.Open();

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
            string sql = @"
                SELECT DISTINCT City
                FROM Cinemas
                ORDER BY City";
            using var command = new SqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            var cities = new List<string>();
            while (reader.Read())
            {
                cities.Add(Convert.ToString(reader["City"]));
            }
            return cities;
        }

        // Get a list of all cinemas in the currently selected city.
        private IEnumerable<string> GetCinemasInSelectedCity()
        {
            string sql = @"
                SELECT * FROM Cinemas
                WHERE City = @City
                ORDER BY Name";
            using var command = new SqlCommand(sql, connection);
            string currentCity = (string)cityComboBox.SelectedItem;
            command.Parameters.AddWithValue("@City", currentCity);
            using var reader = command.ExecuteReader();
            var cinemas = new List<string>();
            while (reader.Read())
            {
                cinemas.Add(Convert.ToString(reader["Name"]));
            }
            return cinemas;
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
            screeningPanel.Children.Clear();
            if (cinemaListBox.SelectedIndex == -1)
            {
                return;
            }

            string sql = @"
                SELECT * FROM Screenings
                JOIN Cinemas ON Screenings.CinemaID = Cinemas.ID
                JOIN Movies ON Screenings.MovieID = Movies.ID
                WHERE Cinemas.Name = @Cinema
                ORDER BY Time";
            using var command = new SqlCommand(sql, connection);
            string cinema = (string)cinemaListBox.SelectedItem;
            command.Parameters.AddWithValue("@Cinema", cinema);
            using var reader = command.ExecuteReader();

            // For each screening:
            while (reader.Read())
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
                int screeningID = Convert.ToInt32(reader["ID"]);

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

                var image = CreateImage(@"Posters\" + reader["PosterPath"]);
                image.Width = 50;
                image.Margin = spacing;
                image.ToolTip = new ToolTip { Content = reader["Title"] };
                AddToGrid(grid, image, 0, 0);
                Grid.SetRowSpan(image, 3);

                var time = (TimeSpan)reader["Time"];
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
                    Text = Convert.ToString(reader["Title"]),
                    Margin = spacing,
                    FontFamily = mainFont,
                    FontSize = 16,
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                AddToGrid(grid, titleHeading, 1, 1);

                var releaseDate = Convert.ToDateTime(reader["ReleaseDate"]);
                int runtimeMinutes = Convert.ToInt32(reader["Runtime"]);
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

        // Buy a ticket for the specified screening and update the GUI with the latest list of tickets.
        private void BuyTicket(int screeningID)
        {
            using (database = new AppDbContext()) //Funkar endast med en film
            {
                var TicketItem = new Tickets { TimePurchased = DateTime.Now };
                TicketItem.Screening = database.screenings.First(x => x.ID == screeningID);
                List<Tickets> TicketList = new List<Tickets>();
                int count = 0;

                foreach(var item in database.tickets)
                {
                    TicketList.Add(item);
                }

                foreach(var item2 in TicketList)
                {
                    if(item2.Screening.ScreeningID == TicketItem.Screening.ScreeningID)
                    {
                        count = 1;
                        MessageBox.Show("You already have a ticket for this Screening");
                    }
                }



                    //den här koden funkar
                    if (count == 0)
                    {
                        database.tickets.Add(TicketItem);
                        database.SaveChanges();
                    }
                TicketItem = null;
                TicketList.Clear();
                UpdateTicketList();
            }

                // First check if we already have a ticket for this screening.

                //string countSql = "SELECT COUNT(*) FROM Tickets WHERE ScreeningID = @ScreeningID";
                //var countCommand = new SqlCommand(countSql, connection);
                //countCommand.Parameters.AddWithValue("@ScreeningID", screeningID);
                //int count = Convert.ToInt32(countCommand.ExecuteScalar());



                // If we don't, add it.
                //if (count == 0)
                //{
                //    string insertSql = "INSERT INTO Tickets (ScreeningID, TimePurchased) VALUES (@ScreeningID, @TimePurchased)";
                //    using var insertCommand = new SqlCommand(insertSql, connection);
                //    insertCommand.Parameters.AddWithValue("@ScreeningID", screeningID);
                //    insertCommand.Parameters.AddWithValue("@TimePurchased", DateTime.Now);
                //    insertCommand.ExecuteNonQuery();

                //    UpdateTicketList();
                //}
        }

        // Update the GUI with the latest list of tickets
        private void UpdateTicketList()
        {
            ticketPanel.Children.Clear();

            string sql = @"
                SELECT * FROM Tickets
                JOIN Screenings ON Tickets.ScreeningID = Screenings.ID
                JOIN Movies ON Screenings.MovieID = Movies.ID
                JOIN Cinemas ON Screenings.CinemaID = Cinemas.ID
                ORDER BY Tickets.TimePurchased";
            using var command = new SqlCommand(sql, connection);
            using var reader = command.ExecuteReader();

            // For each ticket:
            while (reader.Read())
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
                int ticketID = Convert.ToInt32(reader["ID"]);

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

                var image = CreateImage(@"Posters\" + reader["PosterPath"]);
                image.Width = 30;
                image.Margin = spacing;
                image.ToolTip = new ToolTip { Content = reader["Title"] };
                AddToGrid(grid, image, 0, 0);
                Grid.SetRowSpan(image, 2);

                var titleHeading = new TextBlock
                {
                    Text = Convert.ToString(reader["Title"]),
                    Margin = spacing,
                    FontFamily = mainFont,
                    FontSize = 14,
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                AddToGrid(grid, titleHeading, 0, 1);

                var time = (TimeSpan)reader["Time"];
                string timeString = TimeSpanToString(time);
                var timeAndCinemaHeading = new TextBlock
                {
                    Text = timeString + " - " + reader["Name"],
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

        // Remove the ticket for the specified screening and update the GUI with the latest list of tickets.

        

        public void RemoveTicket(int ticketID)
        {
            using (database = new AppDbContext())
            {
                List<Tickets> listus = new List<Tickets>();

                foreach (var ticket in database.tickets)
                {
                    listus.Add(ticket);
                }


                Tickets ticketObject = listus.First(x => x.ID == ticketID);
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
