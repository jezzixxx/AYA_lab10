using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;

class lab10
{
    public class TodaysCondition
    {
        public int id { get; set; }
        public int tickerId { get; set; }
        public string tickerName { get; set; }
        public Ticker ticker { get; set; }
        public double state { get; set; }
    }
    public class Ticker
    {
        public int id { get; set; }
        public string name { get; set; }
        public List<Price> Prices { get; set; } = new();
        public TodaysCondition Condition { get; set; } = new();
    }
    public class Price
    {
        public int id { get; set; }
        public int tickerId { get; set; }
        public string tickerName { get; set; }
        public Ticker? ticker { get; set; }
        public double price { get; set; }
        public DateOnly date { get; set; }
    }
    public class ApplicationContext : DbContext
    {
        public DbSet<Ticker> Tickers { get; set; } = null!;
        public DbSet<Price> Prices { get; set; } = null!;
        public DbSet<TodaysCondition> Conditions { get; set; } = null!;
        public ApplicationContext()
        {
            Database.EnsureCreatedAsync();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source = ../../../tickerstest.sql");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Ticker>().HasKey(u => new { u.id, u.name });
            modelBuilder.Entity<Price>().HasOne(p => p.ticker).WithMany(q => q.Prices)
                .HasForeignKey(r => new { r.tickerId, r.tickerName });
            modelBuilder.Entity<TodaysCondition>().HasOne(p => p.ticker).WithOne(q => q.Condition)
                .HasForeignKey<TodaysCondition>(e => new { e.tickerId, e.tickerName });
        }
    }
    static async Task Main()
    {
        async Task FeelDB()
        {
            using (StreamReader datar = new StreamReader("C:\\Users\\pkapa\\source\\repos\\lab10\\ticker.txt"))
            {
                List<string> data = new List<string>();
                while (!datar.EndOfStream)
                {
                    string? line1 = datar.ReadLine();
                    data.Add(line1);
                }

                await Task.WhenAll(data.ConvertAll(PrintAsync));
            }
            async Task PrintAsync(string data)
            {
                string url = $"https://query1.finance.yahoo.com/v7/finance/download/{data}?period1={DateTimeOffset.Now.ToUnixTimeSeconds() - 31556926}&period2={DateTimeOffset.Now.ToUnixTimeSeconds()}&interval=1d&events=history&includeAdjustedClose=true";
                HttpClient httpClient = new HttpClient();
                string data1;
                HttpResponseMessage response = await httpClient.GetAsync(url);
                using (Stream stream = await response.Content.ReadAsStreamAsync())
                using (StreamReader sr1 = new StreamReader(stream))
                { data1 = sr1.ReadToEnd(); }
                ApplicationContext db = new ApplicationContext();
                Ticker A = new Ticker();
                A.name = data;
                await db.Tickers.AddAsync(A);
                List<string> days = new List<string>(data1.Split('\n'));
                days.RemoveAt(0);
                List<Price> temp = new List<Price>();
                foreach (string day in days)
                {
                    string[] main = day.Split(',');
                    double CurrentPrice = Convert.ToDouble(main[2].Replace('.', ',')) - Convert.ToDouble(main[3].Replace('.', ',')) / 2;
                    DateOnly CurrentDate = DateOnly.Parse(main[0]);
                    Price B = new Price();
                    B.price = CurrentPrice;
                    B.date = CurrentDate;
                    B.ticker = A;
                    temp.Add(B);
                }
                List<string> day1 = new List<string>(days[days.Count - 1].Split(','));
                double day1price = Convert.ToDouble(day1[2].Replace('.', ',')) - Convert.ToDouble(day1[3].Replace('.', ',')) / 2;
                List<string> day2 = new List<string>(days[days.Count - 2].Split(','));
                double day2price = Convert.ToDouble(day2[2].Replace('.', ',')) - Convert.ToDouble(day2[3].Replace('.', ',')) / 2;
                double Change = day1price - day2price;
                TodaysCondition C = new TodaysCondition() { ticker = A, state = Change };
                await db.Conditions.AddAsync(C);
                await db.Prices.AddRangeAsync(temp);
                await db.SaveChangesAsync();
            }
        }
        FeelDB();
        Console.WriteLine("Enter ticker: ");
        string ans = Console.ReadLine();
        ApplicationContext db = new ApplicationContext();
        Ticker? ticker = db.Tickers.FirstOrDefault(p => p.name == ans);
        if (ticker != null)
        {
            db.Conditions.Where(u => u.tickerId == ticker.id).Load();
            Console.WriteLine($"\n{ticker.name}:");
            if (ticker.Condition.state > 0)
            {
                Console.WriteLine($"Stock rose by {ticker.Condition.state}" + '$');
            }
            else if (ticker.Condition.state < 0)
            {
                Console.WriteLine($"Stock fell by {-(ticker.Condition.state)}" + '$');
            }
            else
            {
                Console.WriteLine("Share price has not changed");
            }
        }
    }
}