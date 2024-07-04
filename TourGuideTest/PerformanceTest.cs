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
using Xunit.Abstractions;

namespace TourGuideTest
{
    public class PerformanceTest : IClassFixture<DependencyFixture>
    {
        /*
         * Note on performance improvements:
         * 
         * The number of generated users for high-volume tests can be easily adjusted using this method:
         * 
         *_fixture.Initialize(100000); (for example)
         * 
         * 
         * These tests can be modified to fit new solutions, as long as the performance metrics at the end of the tests remain consistent.
         * 
         * These are the performance metrics we aim to achieve:
         * 
         * highVolumeTrackLocation: 100,000 users within 15 minutes:
         * Assert.True(TimeSpan.FromMinutes(15).TotalSeconds >= stopWatch.Elapsed.TotalSeconds);
         *
         * highVolumeGetRewards: 100,000 users within 20 minutes:
         * Assert.True(TimeSpan.FromMinutes(20).TotalSeconds >= stopWatch.Elapsed.TotalSeconds);
        */

        private readonly DependencyFixture _fixture;

        private readonly ITestOutputHelper _output;

        public PerformanceTest(DependencyFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact]
        public async Task HighVolumeTrackLocation()
        {
            // Voici les mesures de performance que nous souhaitons atteindre 
            // On peut ici augmenter le nombre d'utilisateurs pour tester les performances
            _fixture.Initialize(10);

            List<User> allUsers = _fixture.TourGuideService.GetAllUsers();

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            
            /*foreach (var user in allUsers)
            {
                _fixture.TourGuideService.TrackUserLocation(user);
            }*/

            // Utilisons Task.Run pour déplacer le travail de suivi de l’emplacement de l’utilisateur sur un autre thread.
            var tasks = allUsers.Select(user => Task.Run(async () => await _fixture.TourGuideService.TrackUserLocation(user)));
            // Utilisons Task.WhenAll pour attendre que toutes les tâches se terminent
            await Task.WhenAll(tasks);

            stopWatch.Stop();
            _fixture.TourGuideService.Tracker.StopTracking();

            _output.WriteLine($"highVolumeTrackLocation: Time Elapsed: {stopWatch.Elapsed.TotalSeconds} seconds.");

            Assert.True(TimeSpan.FromMinutes(15).TotalSeconds >= stopWatch.Elapsed.TotalSeconds); /* = 900 Secondes */
        }

        [Fact]
        public async Task HighVolumeGetRewards()
        {
            // Voici les mesures de performance que nous souhaitons atteindre 
            // On peut ici augmenter le nombre d'utilisateurs pour tester les performances
            _fixture.Initialize(10);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            List<Attraction> attractions = await _fixture.GpsUtil.GetAttractions();
            Attraction attraction = attractions[0];
            List<User> allUsers = _fixture.TourGuideService.GetAllUsers();
            allUsers.ForEach(u => u.AddToVisitedLocations(new VisitedLocation(u.UserId, attraction, DateTime.Now)));

            /*allUsers.ForEach(u => _fixture.RewardsService.CalculateRewards(u));*/
            // Utilisons Task.Run pour déplacer le travail de calcul des récompenses sur un autre thread.
            var tasks = allUsers.Select(user => Task.Run(async () => await _fixture.RewardsService.CalculateRewards(user)));
            // Utilisons Task.WhenAll pour attendre que toutes les tâches se terminent
            await Task.WhenAll(tasks);

            foreach (var user in allUsers)
            {
                Assert.True(user.UserRewards.Count > 0);
            }
            stopWatch.Stop();
            _fixture.TourGuideService.Tracker.StopTracking();

            _output.WriteLine($"highVolumeGetRewards: Time Elapsed: {stopWatch.Elapsed.TotalSeconds} seconds.");
            Assert.True(TimeSpan.FromMinutes(20).TotalSeconds >= stopWatch.Elapsed.TotalSeconds); /* = 1200 Secondes */
        }
    }
}
