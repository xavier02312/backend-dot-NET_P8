using GpsUtil.Location;
using Microsoft.AspNetCore.Mvc;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;
using TripPricer;

namespace TourGuide.Controllers;

[ApiController]
[Route("[controller]")]
public class TourGuideController : ControllerBase
{
    private readonly ITourGuideService _tourGuideService;
    private readonly IRewardsService _rewardsService;
    private readonly IGpsUtil _gpsUtil;

    public TourGuideController(ITourGuideService tourGuideService, IRewardsService rewardsService, IGpsUtil gpsUtil)
    {
        _tourGuideService = tourGuideService;
        _rewardsService = rewardsService;   
        _gpsUtil = gpsUtil;
    }

    [HttpGet("getLocation")]
    public ActionResult<VisitedLocation> GetLocation([FromQuery] string userName)
    {
        var location = _tourGuideService.GetUserLocation(GetUser(userName));
        return Ok(location);
    }

    // TODO: Change this method to no longer return a List of Attractions.
    // Instead: Get the closest five tourist attractions to the user - no matter how far away they are.
    // Return a new JSON object that contains:
    // Name of Tourist attraction, 
    // Tourist attractions lat/long, 
    // The user's location lat/long, 
    // The distance in miles between the user's location and each of the attractions.
    // The reward points for visiting each Attraction.
    //    Note: Attraction reward points can be gathered from RewardsCentral
    [HttpGet("getNearbyAttractions")]
    public ActionResult<List<object>> GetNearbyAttractions([FromQuery] string userName)
    {
        var user = GetUser(userName);
        var visitedLocation = _gpsUtil.GetUserLocation(user.UserId);
        var attractions = _tourGuideService.GetNearByAttractions(visitedLocation);

        // Crée une liste pour stocker les objets JSON
        List<object> response = new();

        foreach (var attraction in attractions)
        {
            // La distance entre l'utilisateur et l'attraction
            double distance = _rewardsService.GetDistance(visitedLocation.Location, attraction);

            // Les points de récompense pour cette attraction
            _rewardsService.CalculateRewards(user);

            // Obtient les points de récompense pour cette attraction
            int rewardPoints = user.UserRewards.FirstOrDefault(r => r.Attraction.AttractionName == attraction.AttractionName)?.RewardPoints ?? 0;

            // Crée un nouvel objet JSON
            var jsonObject = new
            {
                Name = attraction.AttractionName,
                AttractionLatLong = new { attraction.Latitude, attraction.Longitude },
                UserLatLong = new { visitedLocation.Location.Latitude, visitedLocation.Location.Longitude },
                Distance = distance,
                RewardPoints = rewardPoints
            };

            // Ajoute l'objet JSON à la liste
            response.Add(jsonObject);
        }

        return Ok(response);
    }

    [HttpGet("getRewards")]
    public ActionResult<List<UserReward>> GetRewards([FromQuery] string userName)
    {
        var rewards = _tourGuideService.GetUserRewards(GetUser(userName));
        return Ok(rewards);
    }

    [HttpGet("getTripDeals")]
    public ActionResult<List<Provider>> GetTripDeals([FromQuery] string userName)
    {
        var deals = _tourGuideService.GetTripDeals(GetUser(userName));
        return Ok(deals);
    }

    private User GetUser(string userName)
    {
        return _tourGuideService.GetUser(userName);
    }
}
