using GpsUtil.Location;
using System.Globalization;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;
using TourGuide.Utilities;
using TripPricer;

namespace TourGuide.Services;

public class TourGuideService : ITourGuideService
{
    private readonly ILogger _logger;
    private readonly IGpsUtil _gpsUtil;
    private readonly IRewardsService _rewardsService;
    private readonly TripPricer.TripPricer _tripPricer;
    public Tracker Tracker { get; private set; }
    private readonly Dictionary<string, User> _internalUserMap = new();
    private const string TripPricerApiKey = "test-server-api-key";
    private bool _testMode = true;

    public TourGuideService(ILogger<TourGuideService> logger, IGpsUtil gpsUtil, IRewardsService rewardsService, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _tripPricer = new();
        _gpsUtil = gpsUtil;
        _rewardsService = rewardsService;

        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        if (_testMode)
        {
            _logger.LogInformation("TestMode enabled");
            _logger.LogDebug("Initializing users");
            InitializeInternalUsers();
            // Crée un nouvel utilisateur "xavier"
            Guid userId = Guid.NewGuid(); // Génère un nouvel identifiant unique
            string userName = "xavier";
            string phoneNumber = "123-456-7890"; // Remplacez par le numéro de téléphone réel
            string emailAddress = "xavier@example.com"; // Remplacez par l'adresse e-mail réelle

            User xavier = new(userId, userName, phoneNumber, emailAddress);

            // Ajoute "xavier" à la carte des utilisateurs internes
            _internalUserMap.Add(xavier.UserName, xavier);
            _logger.LogDebug("Finished initializing users");
        }

        var trackerLogger = loggerFactory.CreateLogger<Tracker>();

        Tracker = new Tracker(this, trackerLogger);
        AddShutDownHook();
    }

    public List<UserReward> GetUserRewards(User user)
    {
        return user.UserRewards;
    }

    public async Task<VisitedLocation> GetUserLocation(User user)
    {
        return user.VisitedLocations.Any() ? user.GetLastVisitedLocation() : await TrackUserLocation(user);
    }

    public User GetUser(string userName)
    {
        /*  l’implémentation de l’indexeur recherche une valeur Null en appelant la méthode IDictionary.ContainsKey deux recherches sont effectuées quand une seule est nécessaire */
        return _internalUserMap.TryGetValue(userName, out User? value) ? value : null; 
    }

    public List<User> GetAllUsers()
    {
        return _internalUserMap.Values.ToList();
    }

    public void AddUser(User user)
    {
        if (!_internalUserMap.ContainsKey(user.UserName))
        {
            _internalUserMap.Add(user.UserName, user);
        }
    }

    public List<Provider> GetTripDeals(User user)
    {
        int cumulativeRewardPoints = user.UserRewards.Sum(i => i.RewardPoints);
        List<Provider> providers = _tripPricer.GetPrice(TripPricerApiKey, user.UserId,
            user.UserPreferences.NumberOfAdults, user.UserPreferences.NumberOfChildren,
            user.UserPreferences.TripDuration, cumulativeRewardPoints);
        user.TripDeals = providers;
        return providers;
    }

    public async Task<VisitedLocation> TrackUserLocation(User user) /* ici perf */
    {
        VisitedLocation visitedLocation = await _gpsUtil.GetUserLocation(user.UserId);
        user.AddToVisitedLocations(visitedLocation);
        await _rewardsService.CalculateRewards(user);
        return visitedLocation;
    }

    // Cette classe permet de stocker une attraction et sa distance
    internal class AttractionDistance
    {
        public Attraction Attraction { get; set; }
        public double Distance { get; set; }

        public AttractionDistance(Attraction attraction, double distance)
        {
            Attraction = attraction;
            Distance = distance;
        }
    }
    public async Task<List<Attraction>> GetNearByAttractions(VisitedLocation visitedLocation) /* ici perf */
    {
        List<Attraction> nearbyAttractions = await _gpsUtil.GetAttractions();
        List<AttractionDistance> attractionsDistance = new();

        for ( int i = 0; i < nearbyAttractions.Count; i++ )
        {
            attractionsDistance.Add(new AttractionDistance(
                nearbyAttractions[i],
                _rewardsService.GetDistance(nearbyAttractions[i], visitedLocation.Location)));
        }

        return attractionsDistance.OrderBy(a => a.Distance).Select(b => b.Attraction).Take(5).ToList();
    }

    private void AddShutDownHook()
    {
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => Tracker.StopTracking();
    }

    /**********************************************************************************
    * 
    * Methods Below: For Internal Testing
    * 
    **********************************************************************************/

    private void InitializeInternalUsers() 
    {
        for (int i = 0; i < InternalTestHelper.GetInternalUserNumber(); i++)
        {
            var userName = $"internalUser{i}";
            var user = new User(Guid.NewGuid(), userName, "000", $"{userName}@tourGuide.com");
            GenerateUserLocationHistory(user);
            _internalUserMap.Add(userName, user);
        }

        _logger.LogDebug($"Created {InternalTestHelper.GetInternalUserNumber()} internal test users.");
    }

    private void GenerateUserLocationHistory(User user) 
    {
        for (int i = 0; i < 3; i++)
        {
            var visitedLocation = new VisitedLocation(user.UserId, new Locations(GenerateRandomLatitude(), GenerateRandomLongitude()), GetRandomTime());
            user.AddToVisitedLocations(visitedLocation);
        }
    }

    private static readonly Random random = new();

    private double GenerateRandomLongitude() /* ici perf */
    {
        return  random.NextDouble() * (180 - (-180)) + (-180);/* enlever le "new Random().NextDouble()"*/
    }

    private double GenerateRandomLatitude() /* ici perf */
    {
        return random.NextDouble() * (90 - (-90)) + (-90); /* enlever le "new Random().NextDouble() * (90 - (-90)) + (-90)"; */
    }

    private DateTime GetRandomTime()
    {
        return DateTime.UtcNow.AddDays(-new Random().Next(30));
    }
}
