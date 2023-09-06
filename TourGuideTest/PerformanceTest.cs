using GpsUtil.Location;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;
using TourGuide.Utilities;

namespace TourGuideTest
{
    public class PerformanceTest : IClassFixture<DependencyFixture>
    {
        private readonly DependencyFixture _fixture;

        public PerformanceTest(DependencyFixture fixture)
        {
            _fixture = fixture;
            //Initialiser ici le nombre d'utilisateur que vous voulez
            _fixture.Initialize(1000);
        }

        public void Dispose()
        {
            _fixture.Cleanup();
        }

        [Fact]
        public void HighVolumeTrackLocation()
        {            
            List<User> allUsers = _fixture.TourGuideService.GetAllUsers();

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            int count = 0;
            foreach (var user in allUsers)
            {
                _fixture.TourGuideService.TrackUserLocation(user);
                Console.WriteLine(count++);
            }
            stopWatch.Stop();
            _fixture.TourGuideService.Tracker.StopTracking();

            Console.WriteLine($"highVolumeTrackLocation: Time Elapsed: {stopWatch.Elapsed.TotalSeconds} seconds.");
            Assert.True(TimeSpan.FromMinutes(15).TotalSeconds >= stopWatch.Elapsed.TotalSeconds);
        }

        [Fact]
        public void HighVolumeGetRewards()
        {           
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            Attraction attraction = _fixture.GpsUtil.GetAttractions()[0];
            List<User> allUsers = _fixture.TourGuideService.GetAllUsers();
            allUsers.ForEach(u => u.AddToVisitedLocations(new VisitedLocation(u.UserId, attraction, DateTime.Now)));

            allUsers.ForEach(u => _fixture.RewardsService.CalculateRewards(u));

            foreach (var user in allUsers)
            {
                Assert.True(user.UserRewards.Count > 0);
            }
            stopWatch.Stop();
            _fixture.TourGuideService.Tracker.StopTracking();

            Console.WriteLine($"highVolumeGetRewards: Time Elapsed: {stopWatch.Elapsed.TotalSeconds} seconds.");
            Assert.True(TimeSpan.FromMinutes(20).TotalSeconds >= stopWatch.Elapsed.TotalSeconds);
        }
    }
}
