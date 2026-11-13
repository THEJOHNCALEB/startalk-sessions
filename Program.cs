using StarTalkSessions.Data;
using StarTalkSessions.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Serilog;
using dotenv.net;

var builder = WebApplication.CreateBuilder(args);
DotEnv.Load(options: new DotEnvOptions(probeForEnv: true, envFilePaths: new[] { ".env" }));
int portNumber = Environment.GetEnvironmentVariable("PORT") != null ? int.Parse(Environment.GetEnvironmentVariable("PORT")!) : 6200;

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

builder.WebHost.ConfigureKestrel(options =>
{
    Console.WriteLine($"Star Talk Sessions running on 0.0.0.0:{portNumber}");
    options.Listen(System.Net.IPAddress.Any, portNumber);
});

builder.WebHost.UseUrls();

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication("FURN_COOKIE")
    .AddCookie("FURN_COOKIE", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.Cookie.Name = "FURN_AUTH";
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.LogoutPath = "/Auth/Logout";
    });

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/startalksessions-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger());

builder.Services.AddTransient<CategoryDatabaseAccess>();
builder.Services.AddTransient<ProductDatabaseAccess>();
builder.Services.AddTransient<ProjectDatabaseAccess>();
builder.Services.AddTransient<TestimonialDatabaseAccess>();
builder.Services.AddTransient<BlogDatabaseAccess>();
builder.Services.AddTransient<UserDatabaseAccess>();
builder.Services.AddTransient<CustomQuoteDatabaseAccess>();
builder.Services.AddTransient<OrderDatabaseAccess>();
builder.Services.AddTransient<ServiceDatabaseAccess>();
builder.Services.AddTransient<InquiryDatabaseAccess>();
builder.Services.AddTransient<NewsletterDatabaseAccess>();
builder.Services.AddTransient<BannerAdDatabaseAccess>();
builder.Services.AddTransient<SupportTicketDatabaseAccess>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<StarTalkSessions.Services.NotificationService>();
builder.Services.AddTransient<StarTalkSessions.Services.AiSupportService>();

builder.Services.AddSingleton(_ =>
{
    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECT");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.WriteLine("DB_CONNECT environment variable is not set.");
        throw new InvalidOperationException("DB_CONNECT environment variable is not set.");
    }
    var databaseName = "startalk_sessions";
    var settings = MongoClientSettings.FromConnectionString(connectionString);
    var client = new MongoClient(settings);
    return client.GetDatabase(databaseName);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

await SeedDatabaseAsync(app.Services);

app.Run();

async Task SeedDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var categoryDb = scope.ServiceProvider.GetRequiredService<CategoryDatabaseAccess>();
    var productDb = scope.ServiceProvider.GetRequiredService<ProductDatabaseAccess>();
    var projectDb = scope.ServiceProvider.GetRequiredService<ProjectDatabaseAccess>();
    var testimonialDb = scope.ServiceProvider.GetRequiredService<TestimonialDatabaseAccess>();
    var blogDb = scope.ServiceProvider.GetRequiredService<BlogDatabaseAccess>();
    var serviceDb = scope.ServiceProvider.GetRequiredService<ServiceDatabaseAccess>();
    var userDb = scope.ServiceProvider.GetRequiredService<UserDatabaseAccess>();

    var ctl = CancellationToken.None;

    var existingCategories = await categoryDb.QueryManyAsync(c => c.IsActive, ctl);
    if (existingCategories.Count > 0)
    {
        logger.LogInformation("Catalog already seeded ({CategoryCount} categories).", existingCategories.Count);
        return;
    }

    logger.LogInformation("Seeding Star Talk Sessions catalog...");

    var categories = new List<Category>
    {
        new() { Name = "Podcast", Slug = "podcast", Description = "Long-form conversations with people shaping the future.", ImageUrl = "https://images.unsplash.com/photo-1590602847861-f357a9332bbc?q=80&w=1600&auto=format&fit=crop", SortOrder = 1 },
        new() { Name = "Radio", Slug = "radio", Description = "A live pulse of ideas, music and community.", ImageUrl = "https://images.unsplash.com/photo-1598387993281-cecf8b71a8f8?q=80&w=1600&auto=format&fit=crop", SortOrder = 2 },
        new() { Name = "Television", Slug = "television", Description = "Visual stories built for the big screen and the small one.", ImageUrl = "https://images.unsplash.com/photo-1574717024653-61fd2cf4d44d?q=80&w=1600&auto=format&fit=crop", SortOrder = 3 },
        new() { Name = "News", Slug = "news", Description = "Clear reporting and context for a changing world.", ImageUrl = "https://images.unsplash.com/photo-1504711434969-e33886168f5c?q=80&w=1600&auto=format&fit=crop", SortOrder = 4 },
        new() { Name = "Videos", Slug = "videos", Description = "Short films, field reports and stories from the community.", ImageUrl = "https://images.unsplash.com/photo-1485846234645-a62644f84728?q=80&w=1600&auto=format&fit=crop", SortOrder = 5 },
        new() { Name = "Live Broadcast", Slug = "live-broadcast", Description = "Meet the moment through live conversations and events.", ImageUrl = "https://images.unsplash.com/photo-1492684223066-81342ee5ff30?q=80&w=1600&auto=format&fit=crop", SortOrder = 6 },
        new() { Name = "Star Talk Store", Slug = "star-talk-store", Description = "Limited shirts, caps, books and future brand collaborations.", ImageUrl = "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?q=80&w=1600&auto=format&fit=crop", SortOrder = 7 },
        new() { Name = "Community", Slug = "community", Description = "Education, skills, healthcare and community outreach.", ImageUrl = "https://images.unsplash.com/photo-1504159506876-f8338247a14a?q=80&w=1600&auto=format&fit=crop", SortOrder = 8 }
    };
    foreach (var c in categories) await categoryDb.InsertAsync(c, ctl);

    var sofa = categories[0];
    var bed = categories[1];
    var dining = categories[2];
    var office = categories[3];
    var storage = categories[4];
    var media = categories[5];
    var outdoor = categories[6];
    var custom = categories[7];

    var products = new List<Product>
    {
        new()
        {
            Name = "Luna Modular Sectional", Slug = "luna-modular-sectional", Sku = "FURN-SF-001", CategoryId = sofa.Id, CategoryName = sofa.Name,
            Price = 2890, CompareAtPrice = 3400, Style = "Luxury Contemporary",
            ShortDescription = "A deep-seated modular sectional with down-filled cushions and a low, sculptural profile.",
            Description = "The Luna Modular Sectional anchors any living room in quiet luxury. Segmented modules allow flexible configurations (L-shaped, U-shaped or split islands), while kiln-dried hardwood frames and feather-blend cushions deliver endless sit-comfort. Each module ships individually so it fits through standard doorways with ease.",
            Materials = new List<VariantOption>
            {
                new() { Name = "Ivory Bouclé", Swatch = "#EFE9E0", PriceDelta = 0 },
                new() { Name = "Champagne Velvet", Swatch = "#D8C3A5", PriceDelta = 120 },
                new() { Name = "Forest Brushed", Swatch = "#3F5A3C", PriceDelta = 180 }
            },
            Finishes = new List<VariantOption>
            {
                new() { Name = "Natural Oak Legs", Swatch = "#C9A66B", PriceDelta = 0 },
                new() { Name = "Obsidian Steel Legs", Swatch = "#111111", PriceDelta = 90 }
            },
            Sizes = new List<VariantOption>
            {
                new() { Name = "3-Seater", Swatch = "", PriceDelta = 0 },
                new() { Name = "4-Seater", Swatch = "", PriceDelta = 420 },
                new() { Name = "5-Seater + Ottoman", Swatch = "", PriceDelta = 980 }
            },
            Badges = new List<string> { "Best Seller", "Customizable" },
            Images = new List<string>
            {
                "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1493663284031-b7e3aefcae8e?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1549187774-b4e9b0445b41?q=80&w=1600&auto=format&fit=crop"
            },
            Dimensions = "Modules: 96\"W x 38\"D x 34\"H (each)", Weight = "72 kg / module",
            CareInstructions = "Rotate cushions weekly; vacuum with soft brush; spot-clean with mild soap.",
            LeadTime = "4–6 weeks", Warranty = "10-year structural guarantee",
            Stock = 14, Rating = 4.9, ReviewCount = 132, IsFeatured = true, IsBestSeller = true, IsCustomizable = true, CreatedAt = DateTime.Now.AddDays(-40)
        },
        new()
        {
            Name = "Aria Four-Poster Bed", Slug = "aria-four-poster-bed", Sku = "FURN-BD-002", CategoryId = bed.Id, CategoryName = bed.Name,
            Price = 4200, Style = "Japanese Minimalist",
            ShortDescription = "A monumental sculpted-oak four-poster with integrated bedside lighting.",
            Description = "The Aria Four-Poster Bed is a meditation on proportion. Turned solid-oak posts, floating slat headboard and hidden ambient downlighting create a hotel-level sleeping sanctuary. Frame accepts standard platform or slatted foundations in King and Queen.",
            Materials = new List<VariantOption>
            {
                new() { Name = "Smoked Oak", Swatch = "#6B4F35", PriceDelta = 0 },
                new() { Name = "White Ash", Swatch = "#E6DCC8", PriceDelta = 140 }
            },
            Finishes = new List<VariantOption>
            {
                new() { Name = "Matte Lacquer", Swatch = "#DDDDDD", PriceDelta = 0 },
                new() { Name = "Hand-Rubbed Oil", Swatch = "#B99256", PriceDelta = 190 }
            },
            Sizes = new List<VariantOption>
            {
                new() { Name = "Queen", Swatch = "", PriceDelta = 0 },
                new() { Name = "King", Swatch = "", PriceDelta = 480 },
                new() { Name = "California King", Swatch = "", PriceDelta = 750 }
            },
            Badges = new List<string> { "New", "Customizable" },
            Images = new List<string>
            {
                "https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?q=80&w=1600&auto=format&fit=crop"
            },
            Dimensions = "Queen: 63\"W x 83\"L x 88\"H", Weight = "110 kg",
            CareInstructions = "Dust weekly with microfiber; condition oak twice a year.",
            LeadTime = "6–8 weeks", Warranty = "Lifetime on solid-wood frame",
            Stock = 5, Rating = 5.0, ReviewCount = 48, IsFeatured = true, IsNew = true, IsCustomizable = true, CreatedAt = DateTime.Now.AddDays(-12)
        },
        new()
        {
            Name = "Eden 8-Seat Dining Table", Slug = "eden-8-seat-dining-table", Sku = "FURN-DN-003", CategoryId = dining.Id, CategoryName = dining.Name,
            Price = 2380, CompareAtPrice = 2600, Style = "Mid-Century Modern",
            ShortDescription = "A sculpted live-edge dining table in solid walnut with brass inlay.",
            Description = "The Eden table pairs a century-old walnut slab with precision-milled steel legs and brushed brass accents. Seats eight with ease, and the oil-wax finish deepens beautifully with age.",
            Materials = new List<VariantOption>
            {
                new() { Name = "Black Walnut", Swatch = "#4A3320", PriceDelta = 0 },
                new() { Name = "European Oak", Swatch = "#C7A97C", PriceDelta = 90 }
            },
            Finishes = new List<VariantOption>
            {
                new() { Name = "Brushed Brass", Swatch = "#C9A227", PriceDelta = 0 },
                new() { Name = "Matte Black Steel", Swatch = "#1A1A1A", PriceDelta = 60 }
            },
            Sizes = new List<VariantOption>
            {
                new() { Name = "6-Seat (78\")", Swatch = "", PriceDelta = 0 },
                new() { Name = "8-Seat (94\")", Swatch = "", PriceDelta = 380 },
                new() { Name = "10-Seat (110\")", Swatch = "", PriceDelta = 760 }
            },
            Badges = new List<string> { "Best Seller" },
            Images = new List<string>
            {
                "https://images.unsplash.com/photo-1617806118233-18e1de247200?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1519643381401-22c77e60520e?q=80&w=1600&auto=format&fit=crop"
            },
            Dimensions = "94\"W x 42\"D x 30\"H (8-seat)", Weight = "85 kg",
            CareInstructions = "Use coasters; avoid direct sunlight; re-oil biannually.",
            LeadTime = "3–5 weeks", Warranty = "10-year wood guarantee",
            Stock = 9, Rating = 4.8, ReviewCount = 74, IsFeatured = true, IsBestSeller = true, CreatedAt = DateTime.Now.AddDays(-60)
        },
        new()
        {
            Name = "Vertex Executive Desk", Slug = "vertex-executive-desk", Sku = "FURN-OF-004", CategoryId = office.Id, CategoryName = office.Name,
            Price = 1740, Style = "Industrial",
            ShortDescription = "A standing-height executive desk in powder-coated steel and blackened oak.",
            Description = "Designed for the modern studio, the Vertex desk offers silent cable management, a floating storage tray and dual sit-stand height positions. Its blackened oak top is sealed against coffee rings and late nights alike.",
            Materials = new List<VariantOption>
            {
                new() { Name = "Blackened Oak", Swatch = "#2E2A26", PriceDelta = 0 },
                new() { Name = "Clear Ash", Swatch = "#E9DFC9", PriceDelta = 120 }
            },
            Finishes = new List<VariantOption>
            {
                new() { Name = "Powder Black", Swatch = "#171717", PriceDelta = 0 },
                new() { Name = "Warm Zinc", Swatch = "#B7B1A5", PriceDelta = 80 }
            },
            Sizes = new List<VariantOption>
            {
                new() { Name = "52\"W", Swatch = "", PriceDelta = 0 },
                new() { Name = "64\"W", Swatch = "", PriceDelta = 260 },
                new() { Name = "72\"W", Swatch = "", PriceDelta = 420 }
            },
            Badges = new List<string> { "New", "Customizable" },
            Images = new List<string>
            {
                "https://images.unsplash.com/photo-1524758631624-e2822e304c36?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1518455027359-f3f8164ba6bd?q=80&w=1600&auto=format&fit=crop"
            },
            Dimensions = "64\"W x 30\"D x 29–44\"H (adjustable)", Weight = "46 kg",
            CareInstructions = "Wipe with damp cloth; avoid abrasive cleaners.",
            LeadTime = "2–4 weeks", Warranty = "10-year frame warranty",
            Stock = 18, Rating = 4.7, ReviewCount = 39, IsNew = true, IsCustomizable = true, CreatedAt = DateTime.Now.AddDays(-20)
        },
        new()
        {
            Name = "Noir Walk-In Wardrobe System", Slug = "noir-walk-in-wardrobe", Sku = "FURN-WS-005", CategoryId = storage.Id, CategoryName = storage.Name,
            Price = 3650, Style = "Luxury Contemporary",
            ShortDescription = "A fully modular wardrobe system with marble laminate and ambient LED.",
            Description = "The Noir system turns a blank wall into a dressing room. Powder-coated steel frames, warm LED channels and smoked-glass doors arrange into endless layouts with hanging, shelving and shoe display zones.",
            Materials = new List<VariantOption>
            {
                new() { Name = "Smoked Glass + Oak", Swatch = "#5C5652", PriceDelta = 0 },
                new() { Name = "Marble Laminate", Swatch = "#E7E2D8", PriceDelta = 340 }
            },
            Finishes = new List<VariantOption>
            {
                new() { Name = "Matte Bronze", Swatch = "#8C6B4A", PriceDelta = 0 },
                new() { Name = "Brushed Nickel", Swatch = "#C0C5CB", PriceDelta = 110 }
            },
            Sizes = new List<VariantOption>
            {
                new() { Name = "3 Module (96\")", Swatch = "", PriceDelta = 0 },
                new() { Name = "4 Module (126\")", Swatch = "", PriceDelta = 920 },
                new() { Name = "5 Module (156\")", Swatch = "", PriceDelta = 1780 }
            },
            Badges = new List<string> { "Customizable" },
            Images = new List<string>
            {
                "https://images.unsplash.com/photo-1594620302200-9a762244a156?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1556020685-ae41abfc9365?q=80&w=1600&auto=format&fit=crop"
            },
            Dimensions = "96\"W x 24\"D x 92\"H (3 module)", Weight = "140 kg",
            CareInstructions = "Clean glass with non-ammonia spray; wipe steel with dry cloth.",
            LeadTime = "6–9 weeks", Warranty = "15-year structural guarantee",
            Stock = 4, Rating = 4.9, ReviewCount = 27, IsCustomizable = true, CreatedAt = DateTime.Now.AddDays(-30)
        },
        new()
        {
            Name = "Mono Media Console", Slug = "mono-media-console", Sku = "FURN-MD-006", CategoryId = media.Id, CategoryName = media.Name,
            Price = 990, CompareAtPrice = 1150, Style = "Japanese Minimalist",
            ShortDescription = "A floating console with woven rattan doors and hidden A/V compartments.",
            Description = "Mono keeps entertainment minimal. Sliding woven rattan panels conceal a wire-managed A/V bay, while the floating profile makes small spaces feel larger.",
            Materials = new List<VariantOption>
            {
                new() { Name = "Light Oak", Swatch = "#D9B98F", PriceDelta = 0 },
                new() { Name = "Charcoal Ash", Swatch = "#4B4540", PriceDelta = 70 }
            },
            Finishes = new List<VariantOption>
            {
                new() { Name = "Natural Rattan", Swatch = "#E0C9A3", PriceDelta = 0 },
                new() { Name = "Blackened", Swatch = "#222222", PriceDelta = 40 }
            },
            Sizes = new List<VariantOption>
            {
                new() { Name = "60\"W", Swatch = "", PriceDelta = 0 },
                new() { Name = "72\"W", Swatch = "", PriceDelta = 150 }
            },
            Badges = new List<string> { "Best Seller" },
            Images = new List<string>
            {
                "https://images.unsplash.com/photo-1593784991095-a205069470b6?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?q=80&w=1600&auto=format&fit=crop"
            },
            Dimensions = "60\"W x 16\"D x 20\"H", Weight = "24 kg",
            CareInstructions = "Dust rattan with soft brush; wipe frame with damp cloth.",
            LeadTime = "2–3 weeks", Warranty = "5-year warranty",
            Stock = 22, Rating = 4.8, ReviewCount = 61, IsFeatured = true, IsBestSeller = true, CreatedAt = DateTime.Now.AddDays(-50)
        },
        new()
        {
            Name = "Solara Teak Club Chair", Slug = "solara-teak-club-chair", Sku = "FURN-OD-007", CategoryId = outdoor.Id, CategoryName = outdoor.Name,
            Price = 640, Style = "Mid-Century Modern",
            ShortDescription = "A weatherproof teak club chair with sculpted comfort for raw coastal air.",
            Description = "The Solara breaks in beautifully. Sustainably harvested teak, marine-grade stainless hardware and a slatted profile that sheds rain make it an heirloom for patios, verandas and pool decks.",
            Materials = new List<VariantOption>
            {
                new() { Name = "Natural Teak", Swatch = "#B98E5F", PriceDelta = 0 },
                new() { Name = "Weathered Teak", Swatch = "#8A8A88", PriceDelta = 40 }
            },
            Finishes = new List<VariantOption>
            {
                new() { Name = "Unsealed (weathers)", Swatch = "#C9A66B", PriceDelta = 0 },
                new() { Name = "Outdoor Oil", Swatch = "#A97B45", PriceDelta = 30 }
            },
            Sizes = new List<VariantOption>
            {
                new() { Name = "Standard", Swatch = "", PriceDelta = 0 },
                new() { Name = "Deep Lounger", Swatch = "", PriceDelta = 120 }
            },
            Badges = new List<string> { "New" },
            Images = new List<string>
            {
                "https://images.unsplash.com/photo-1600210492486-724fe5c67fb0?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1559000357-e45e7d74e36e?q=80&w=1600&auto=format&fit=crop"
            },
            Dimensions = "26\"W x 30\"D x 30\"H", Weight = "14 kg",
            CareInstructions = "Rinse with fresh water; oil annually for maintained color.",
            LeadTime = "1–2 weeks", Warranty = "10-year teak guarantee",
            Stock = 31, Rating = 4.9, ReviewCount = 88, IsNew = true, CreatedAt = DateTime.Now.AddDays(-8)
        },
        new()
        {
            Name = "Atelier Hand-Levelled Walnut Sideboard", Slug = "atelier-walnut-sideboard", Sku = "FURN-CC-008", CategoryId = custom.Id, CategoryName = custom.Name,
            Price = 0, Style = "Bespoke",
            ShortDescription = "Fully bespoke sideboard — dimensions, woods and hardware made entirely to your brief.",
            Description = "The Atelier program is our made-to-order workshop. From a hand sketch or a set of CAD drawings, our master joiners build exactly what your space demands — any dimension, any wood, any finish. Includes white-glove delivery and on-site installation.",
            Materials = new List<VariantOption>
            {
                new() { Name = "Any Solid Wood", Swatch = "#8A6B45", PriceDelta = 0 },
                new() { Name = "Marble Top", Swatch = "#E9E4DB", PriceDelta = 0 }
            },
            Finishes = new List<VariantOption>
            {
                new() { Name = "Custom Match", Swatch = "#DDDDDD", PriceDelta = 0 },
                new() { Name = "Hand-Rubbed Oil", Swatch = "#B99256", PriceDelta = 0 }
            },
            Sizes = new List<VariantOption>
            {
                new() { Name = "To Your Spec", Swatch = "", PriceDelta = 0 }
            },
            Badges = new List<string> { "Customizable" },
            Images = new List<string>
            {
                "https://images.unsplash.com/photo-1533090161767-e6ffed986c88?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1618221195710-dd6b41faaea6?q=80&w=1600&auto=format&fit=crop"
            },
            Dimensions = "Bespoke — built to your brief", Weight = "Varies",
            CareInstructions = "Provided with the piece's care booklet on delivery.",
            LeadTime = "8–14 weeks", Warranty = "Lifetime guarantee",
            Stock = 0, Rating = 5.0, ReviewCount = 12, IsFeatured = true, IsCustomizable = true, CreatedAt = DateTime.Now.AddDays(-90)
        }
    };
    foreach (var p in products) await productDb.InsertAsync(p, ctl);

    var projects = new List<Project>
    {
        new()
        {
            Title = "Maitama Residence", Slug = "maitama-residence", Client = "Private Residence", Segment = "Luxury Homes", Location = "Maitama, Abuja", Year = "2026",
            Brief = "A family home in Maitama wanted interiors that could handle humid tropical weather and three generations under one roof without looking heavy.",
            Challenge = "Solid hardwood that would not cup or check through a wet season, delivered to a tight plot with no crane access.",
            Solution = "We built the program in iroko and walnut with a hand-rubbed oil finish, delivered in modules through the gate and assembled in the courtyard by our own joiners.",
            CoverImage = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?q=80&w=2000&auto=format&fit=crop",
            BeforeImage = "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?q=80&w=1600&auto=format&fit=crop",
            AfterImage = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?q=80&w=1600&auto=format&fit=crop",
            Gallery = new List<string>
            {
                "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1616486338812-3dadae4b4ace?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1618221195710-dd6b41faaea6?q=80&w=1600&auto=format&fit=crop"
            },
            FeaturedProductNames = new List<string> { "Eden 8-Seat Dining Table", "Luna Modular Sectional" },
            IsFeatured = true
        },
        new()
        {
            Title = "Casa Serena", Slug = "casa-serena", Client = "Private Residence", Segment = "Luxury Homes", Location = "Lake Como, Italy", Year = "2026",
            Brief = "A 7,000 sq ft lakefront villa requested a warm, tactile interior that balanced gallery-like restraint with family-friendly durability.",
            Challenge = "Deliver 40+ custom pieces across three floors in twelve weeks — with a strict brief of sustainably sourced European oak only.",
            Solution = "Our atelier built the full program in-smoke: a 12-seat sculpted dining table, hand-stitched bouclé sofas and a floating library wall, all kiln-dried and installed by our white-glove crew.",
            CoverImage = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?q=80&w=2000&auto=format&fit=crop",
            BeforeImage = "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?q=80&w=1600&auto=format&fit=crop",
            AfterImage = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?q=80&w=1600&auto=format&fit=crop",
            Gallery = new List<string>
            {
                "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1616486338812-3dadae4b4ace?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1618221195710-dd6b41faaea6?q=80&w=1600&auto=format&fit=crop"
            },
            FeaturedProductNames = new List<string> { "Eden 8-Seat Dining Table", "Luna Modular Sectional" },
            IsFeatured = true
        },
        new()
        {
            Title = "The Meridian Hotel Lobby", Slug = "meridian-hotel-lobby", Client = "Meridian Hotels Group", Segment = "Boutique Hotels", Location = "Lisbon, Portugal", Year = "2025",
            Brief = "A 120-key boutique hotel needed a statement lobby and guestroom media program that felt residential and Instagram-worthy.",
            Challenge = "Contract-grade durability with residential softness, delivered across 120 identical rooms without a single variation in finish.",
            Solution = "We engineered a modular lounge system and supplied 380 pieces: fire-retardant bouclé, marble-topped consoles and pedestal beds on a single production line to hold a ±1mm tolerance.",
            CoverImage = "https://images.unsplash.com/photo-1600607687920-4e2a09cf159d?q=80&w=2000&auto=format&fit=crop",
            BeforeImage = "https://images.unsplash.com/photo-1600607687644-c7171b42498f?q=80&w=1600&auto=format&fit=crop",
            AfterImage = "https://images.unsplash.com/photo-1600607687920-4e2a09cf159d?q=80&w=1600&auto=format&fit=crop",
            Gallery = new List<string>
            {
                "https://images.unsplash.com/photo-1600607687920-4e2a09cf159d?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1618773928121-c32242e63f39?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1590490360182-c33d57733427?q=80&w=1600&auto=format&fit=crop"
            },
            FeaturedProductNames = new List<string> { "Luna Modular Sectional", "Mono Media Console" },
            IsFeatured = true
        },
        new()
        {
            Title = "Northwind HQ", Slug = "northwind-hq", Client = "Northwind Capital", Segment = "Executive Offices", Location = "Singapore", Year = "2025",
            Brief = "A private investment firm wanted an executive floor that projected permanence: deep woods, quiet lighting and zero visible cables.",
            Challenge = "Integrating sit-stand desks and secure A/V kit into bespoke media without a single exposed wire.",
            Solution = "Blackened-oak walls hid an integrated power spine. Our Vertes desk line was customized with marble returns, and every meeting suite received a hand-stitched full-grain leather conference table.",
            CoverImage = "https://images.unsplash.com/photo-1497366811353-6870744d04b2?q=80&w=2000&auto=format&fit=crop",
            BeforeImage = "https://images.unsplash.com/photo-1497366754035-f200968a6e72?q=80&w=1600&auto=format&fit=crop",
            AfterImage = "https://images.unsplash.com/photo-1497366811353-6870744d04b2?q=80&w=1600&auto=format&fit=crop",
            Gallery = new List<string>
            {
                "https://images.unsplash.com/photo-1497366811353-6870744d04b2?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1497366216548-37526070297c?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1524758631624-e2822e304c36?q=80&w=1600&auto=format&fit=crop"
            },
            FeaturedProductNames = new List<string> { "Vertex Executive Desk" },
            IsFeatured = false
        },
        new()
        {
            Title = "Ember Rooftop Restaurant", Slug = "ember-rooftop-restaurant", Client = "Ember Hospitality", Segment = "Restaurants", Location = "Dubai, UAE", Year = "2024",
            Brief = "A 90-cover rooftop restaurant needed outdoor-grade media that felt curated, not utilitarian.",
            Challenge = "UAE coastal humidity and 45°C heat demanded materials that would not warp, rot or fade beyond a two-year horizon.",
            Solution = "Thermally modified teak and marine-grade stainless were paired with weather-proof bouclé that breathes. A full pack-down restyle program keeps the terrace evolving with each season.",
            CoverImage = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?q=80&w=2000&auto=format&fit=crop",
            BeforeImage = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?q=80&w=1600&auto=format&fit=crop",
            AfterImage = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?q=80&w=1600&auto=format&fit=crop",
            Gallery = new List<string>
            {
                "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?q=80&w=1600&auto=format&fit=crop",
                "https://images.unsplash.com/photo-1600210492486-724fe5c67fb0?q=80&w=1600&auto=format&fit=crop"
            },
            FeaturedProductNames = new List<string> { "Solara Teak Club Chair" },
            IsFeatured = false
        }
    };
    foreach (var prj in projects) await projectDb.InsertAsync(prj, ctl);

    var testimonials = new List<Testimonial>
    {
        new() { Name = "Alina V.", Role = "Homeowner", Location = "Vienna, AT", Rating = 5, Tag = "Custom Commission", Content = "The hand-built dining table is the most beautiful object in our home. The joinery is flawless and the oil finish smells incredible. Worth every week of the wait.", CreatedAt = DateTime.Now.AddDays(-20) },
        new() { Name = "Marcus T.", Role = "Interior Designer", Location = "London, UK", Rating = 5, Tag = "Trade Partner", Content = "I've specified Star Talk Sessions across three hotel projects now. Their tolerance, finish control and delivery communication are the best I've encountered in a decade of trade work.", CreatedAt = DateTime.Now.AddDays(-45) },
        new() { Name = "Sofia R.", Role = "Creative Director", Location = "Dubai, AE", Rating = 4, Tag = "Office Refit", Content = "They kitted our entire studio in cohesive walnut and steel. The integrated cable management actually works — no cable dystopia. Our clients comment on the space constantly.", CreatedAt = DateTime.Now.AddDays(-60) },
        new() { Name = "Adaeze O.", Role = "Homeowner", Location = "Abuja, NG", Rating = 5, Tag = "Custom Commission", Content = "The dining table is the centre of our home. Solid iroko, finished by hand, and it has not moved a millimetre through two rainy seasons.", CreatedAt = DateTime.Now.AddDays(-15) },
        new() { Name = "Daniel & Ewa K.", Role = "Homeowners", Location = "Lagos, NG", Rating = 5, Tag = "White-Glove Delivery", Content = "Two couches, a console and a king bed delivered, assembled and the packaging removed — all by the same two polite technicians. Seamless.", CreatedAt = DateTime.Now.AddDays(-90) }
    };
    foreach (var t in testimonials) await testimonialDb.InsertAsync(t, ctl);

    var serviceList = new List<Service>
    {
        new()
        {
            Title = "Custom Star Talk Sessions Manufacturing", Slug = "custom-manufacturing", Icon = "hammer", Eyebrow = "Atelier",
            Description = "From a single bespoke statement piece to a serialized media program, our joiners craft contract-grade media to blueprints or a hand sketch.",
            Features = new List<string> { "Single pieces to bulk production", "Any wood species, fabric or finish", "CAD-to-shipping project management", "±1mm joinery tolerance" },
            SortOrder = 1
        },
        new()
        {
            Title = "Turnkey Interior Furnishing", Slug = "turnkey-interiors", Icon = "grid", Eyebrow = "Full Service",
            Description = "We plan, source and install complete interiors — from space planning to the final styling cushion — for residences and commercial suites alike.",
            Features = new List<string> { "Space planning & moodboards", "Curated product programs", "Lighting and styling", "Single-point accountability" },
            SortOrder = 2
        },
        new()
        {
            Title = "Corporate & Office Outfitting", Slug = "corporate-office", Icon = "briefcase", Eyebrow = "Commercial",
            Description = "Ergonomic workstations, executive suites and touchdown media that project permanence and survive daily use.",
            Features = new List<string> { "Ergonomic sit-stand systems", "Integrated cable & power managment", "Boardroom & lounge zones", "Bulk contract pricing" },
            SortOrder = 3
        },
        new()
        {
            Title = "Hotel & Hospitality Furnishing", Slug = "hospitality", Icon = "moon", Eyebrow = "Hospitality",
            Description = "Fire-retardant, high-abuse hospitality media with the softness of home — built to contract grade for lodges and hotel groups.",
            Features = new List<string> { "FR fabrics & contract seating", "Tolerance-matched production runs", "Replacement-part programmes", "Lobby to guest-room scope" },
            SortOrder = 4
        },
        new()
        {
            Title = "White-Glove Delivery & Installation", Slug = "white-glove-delivery", Icon = "truck", Eyebrow = "Logistics",
            Description = "Fully insured worldwide logistics. We deliver, assemble, level and remove every scrap of packaging — anywhere on the planet.",
            Features = new List<string> { "Fully insured international shipping", "On-site assembly & placement", "Waste packaging removal", "Surveyors for high-value installs" },
            SortOrder = 5
        }
    };
    foreach (var s in serviceList) await serviceDb.InsertAsync(s, ctl);

    var posts = new List<BlogPost>
    {
        new()
        {
            Title = "How to Choose a Sofa That Actually Lasts", Slug = "choose-a-sofa-that-lasts", Tag = "Buying Guide", Author = "Star Talk Sessions Studio",
            ReadTime = "6 min read", CoverImage = "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?q=80&w=1600&auto=format&fit=crop",
            Excerpt = "Frame construction, cushion density and fabric rub-count — the three numbers that separate a ten-year sofa from a two-year sofa.",
            Content = "A great sofa is 85% unseen. The kiln-dried hardwood frame, the webbing tension, the cushion fill blend and the fabric's Martindale rub count do the heavy lifting long after the studio lighting fades.\n\nLook first at the frame: hard maple or kiln-dried hardwood with dowelled and corner-blocked joinery. Avoid particleboard and plastics at the load points.\n\nCushion fill matters more than you think. A high-resilience foam core wrapped in down gives support and softness; full-down seats look luxurious but sag within two seasons.\n\nFinally, flip the fabric label. Interiors-grade fabrics should exceed 30,000 rubs (Martindale). Bouclé, performance velvets and crypton-treated wools all clear the bar while staying impossible to stain.\n\nOur advice: buy the construction, not the leather sales pitch. The frame outlasts the upholstery by decades, and re-upholstery is always cheaper than replacement.",
            IsActive = true
        },
        new()
        {
            Title = "The Quiet Luxury Trend Is a Return to Craft", Slug = "quiet-luxury-craft", Tag = "Trends", Author = "Star Talk Sessions Studio",
            ReadTime = "4 min read", CoverImage = "https://images.unsplash.com/photo-1616486338812-3dadae4b4ace?q=80&w=1600&auto=format&fit=crop",
            Excerpt = "Why the loudest interiors in the world are now the quietest — and why that's better for your media's longevity.",
            Content = "Quiet luxury is the aesthetic of subtraction: fewer pieces, better materials, visible craftsmanship and zero logos. For media, this means solid wood instead of veneers, hand-stitched upholstery instead of bonded leather, and joinery that asks to be touched.\n\nIt is also a sustainability play. A single smoked-oak table that lasts fifty years beats three particleboard tables that last five. Invest in fewer, better objects — then let them age.\n\nThis season's tilt continues toward warm neutrals, sculptural silhouettes and timeless finishes that ignore trend cycles entirely.",
            IsActive = true
        },
        new()
        {
            Title = "Caring for Solid Wood: A Field Guide", Slug = "caring-for-solid-wood", Tag = "Care Guides", Author = "Star Talk Sessions Studio",
            ReadTime = "5 min read", CoverImage = "https://images.unsplash.com/photo-1493663284031-b7e3aefcae8e?q=80&w=1600&auto=format&fit=crop",
            Excerpt = "Dust, humidity and sunlight are the three enemies of a hardwood table. Here is exactly what to do — and what never to do.",
            Content = "Solid wood breathes. It expands and contracts with humidity, which is why we build floating joinery into our tabletops. Your job is to keep humidity between 40–55% and to avoid extreme swings.\n\nClean with a barely damp microfiber cloth — never soaking wet, never detergents. For oiled finishes, re-apply a food-safe oil every six to twelve months. For lacquers, simply dust.\n\nUse coasters and trivets; hot dishes can cloud any finish. And keep tables out of direct, prolonged sunlight, which fades and greys wood unevenly.\n\nIf a scratch appears, embrace it — or spend ten minutes with fine-grit paper and a matching stain. Oiled wood is endlessly renewable.",
            IsActive = true
        }
    };
    foreach (var b in posts) await blogDb.InsertAsync(b, ctl);

    var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "info@solutionconcept.org";
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Sass@123456";
    var adminName = Environment.GetEnvironmentVariable("ADMIN_NAME") ?? "Star Talk Sessions";
    var existingAdmin = await userDb.QueryOneAsync(u => u.Email == adminEmail && u.IsAdmin, ctl);
    if (existingAdmin == null)
    {
        var admin = new UserModel
        {
            Email = adminEmail.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(adminPassword),
            FirstName = adminName.Split(' ').FirstOrDefault() ?? adminName,
            LastName = adminName.Split(' ').Skip(1).FirstOrDefault() ?? "",
            Phone = "0000000000",
            IsActive = true,
            IsAdmin = true,
            CreatedAt = DateTime.Now
        };
        await userDb.InsertAsync(admin, ctl);
        logger.LogInformation("Default admin seeded: {Email}", adminEmail);
    }

    logger.LogInformation("Star Talk Sessions seed complete: {ProductCount} products, {ProjectCount} projects.", products.Count, projects.Count);
}