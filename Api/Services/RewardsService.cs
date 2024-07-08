using GpsUtil.Location;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;

namespace TourGuide.Services;

public class RewardsService : IRewardsService
{
    private const double StatuteMilesPerNauticalMile = 1.15077945;
    private readonly int _defaultProximityBuffer = 10;
    private int _proximityBuffer;
    private readonly int _attractionProximityRange = 200;
    private readonly IGpsUtil _gpsUtil;
    private readonly IRewardCentral _rewardsCentral;

    public RewardsService(IGpsUtil gpsUtil, IRewardCentral rewardCentral)
    {
        _gpsUtil = gpsUtil;
        _rewardsCentral =rewardCentral;
        _proximityBuffer = _defaultProximityBuffer;
    }

    public void SetProximityBuffer(int proximityBuffer)
    {
        _proximityBuffer = proximityBuffer;
    }

    public void SetDefaultProximityBuffer()
    {
        _proximityBuffer = _defaultProximityBuffer;
    }

    // Calcule les récompenses pour un utilisateur
    public async Task CalculateRewards(User user) /* ici perf */
    {
        // Récupère la liste des lieux visités par l’utilisateur.
        List<VisitedLocation> userLocations = user.VisitedLocations;
        // Le mot-clé await est utilisé pour attendre que la tâche se termine avant de continuer l’exécution
        var attractions = await _gpsUtil.GetAttractions();

        // Les boucles for imbiquées parcourent chaque lieu visité par l’utilisateur et chaque attraction
        for (int i = 0; i < userLocations.Count; i++)
        {
            for (int j = 0; j < attractions.Count; j++)
            {
                // Cette condition vérifie si l’utilisateur est à proximité de l’attraction
                if (NearAttraction(userLocations[i], attractions[j]) && IsNotRewarded(user, attractions[j]))
                {
                    // Si l’utilisateur est à proximité de l’attraction et n’a pas encore été récompensé pour cette attraction,
                    // une nouvelle récompense est créée et ajoutée à la liste des récompenses de l’utilisateur
                    user.AddUserReward(new UserReward(userLocations[i], attractions[j], GetRewardPoints(attractions[j], user)));
                }
            }
        }
    }
    // Méthode qui vérifie si un utilisateur a déjà été récompensé pour une attraction spécifique
    private static bool IsNotRewarded(User user, Attraction attraction)
    {
        for (int k = 0; k < user.UserRewards.Count; k++)
        {
            if (user.UserRewards[k].Attraction.AttractionName == attraction.AttractionName)
            {
                return false;
            }
        }
        return true;
    }
    public bool IsWithinAttractionProximity(Attraction attraction, Locations location)
    {
        Console.WriteLine(GetDistance(attraction, location));
        return GetDistance(attraction, location) <= _attractionProximityRange;
    }

    private bool NearAttraction(VisitedLocation visitedLocation, Attraction attraction)
    {
        return GetDistance(attraction, visitedLocation.Location) <= _proximityBuffer;
    }

    private int GetRewardPoints(Attraction attraction, User user)
    {
        return _rewardsCentral.GetAttractionRewardPoints(attraction.AttractionId, user.UserId);
    }

    public double GetDistance(Locations loc1, Locations loc2)
    {
        double lat1 = Math.PI * loc1.Latitude / 180.0;
        double lon1 = Math.PI * loc1.Longitude / 180.0;
        double lat2 = Math.PI * loc2.Latitude / 180.0;
        double lon2 = Math.PI * loc2.Longitude / 180.0;

        double angle = Math.Acos(Math.Sin(lat1) * Math.Sin(lat2)
                                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2));

        double nauticalMiles = 60.0 * angle * 180.0 / Math.PI;
        return StatuteMilesPerNauticalMile * nauticalMiles;
    }
}
