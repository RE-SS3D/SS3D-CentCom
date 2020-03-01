using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;
using Api.SystemTests.Helpers;
using Api.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace Api.SystemTests.Controllers
{
    [TestCaseOrderer("Api.SystemTests.Helpers.PriorityOrderer", "Api.SystemTests")]
    public class ServersControllerTests : IClassFixture<AppClientFactory>
    {
        public ServersControllerTests(AppClientFactory appClientFactory)
        {
            client = appClientFactory.Client;
        }

        // Test adding, getting, and removing
        [Fact, TestPriority(1)]
        public async void CanCreateServer()
        {
            gameServer.Start();

            var response = await client.PostAsync(
                "/api/servers/",
                new JsonContent(
                    new GameServer {
                        Name = "Test Server (Delete if seen)",
                        TagLine = "This is a temporary listing created during testing. Please delete if seen.",
                        QueryPort = 100,
                        GamePort = 1,
                        Players = 0,
                        RoundStatus = "invalid",
                        RoundStartTime = DateTime.Now,
                        Map = "None",
                        Game = "SS3D",
                        Gamemode = "TEST"
                    }
                )
            );
            gameServer.Stop();

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);

            serverLocation = response.Headers.Location;
        }

        [Fact, TestPriority(2)]
        public async void CanFindServerInList()
        {
            var response = await client.GetAsync("/api/servers");

            var servers = await JsonSerializer.DeserializeAsync<List<GameServer>>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            GameServer foundServer = servers.Find(server => server.Name == "Test Server (Delete if seen)");
            
            Assert.NotNull(foundServer);
            Assert.False(string.IsNullOrEmpty(foundServer.Address));
        }

        [Fact, TestPriority(2)]
        public async void CanGetServerFromLocation()
        {
            var response = await client.GetAsync(serverLocation);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var server = await JsonSerializer.DeserializeAsync<GameServer>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.Equal("Test Server (Delete if seen)", server.Name);
        }

        [Fact, TestPriority(3)]
        public async void CanUpdateServerInfo()
        {
            var response = await client.PutAsync(
                serverLocation,
                new JsonContent(
                    new GameServer {
                        // Leave address and query port out
                        Name = "Test Server #2 (Delete if seen)",
                        TagLine = "This is a temporary listing created during testing. Please delete if seen.",
                        GamePort = 1,
                        Players = 0,
                        MaxPlayers = 5, // Adding a new field
                        RoundStatus = "invalid",
                        RoundStartTime = DateTime.Now,
                        Map = "None",
                        Game = "SS3D",
                        Gamemode = "TEST"
                    }
                )
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact, TestPriority(4)]
        public async void CanGetUpdatedInfo()
        {
            var response = await client.GetAsync(serverLocation);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var server = await JsonSerializer.DeserializeAsync<GameServer>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.Equal("Test Server #2 (Delete if seen)", server.Name);
        }

        [Fact, TestPriority(5)]
        public async void CantRecreate()
        {
            gameServer.Start();
            var response = await client.PostAsync(
                "/api/servers",
                new JsonContent(
                    new GameServer {
                        Name = "Test Server #3 (Delete if seen)",
                        TagLine = "This is a test that should only be visible if the test failed.",
                        QueryPort = 100,
                        GamePort = 500,
                        Players = 1000000,
                        RoundStatus = "error",
                        RoundStartTime = DateTime.Now,
                        Map = "error",
                        Game = "SS3D",
                        Gamemode = "TEST"
                    }
                )
            );
            gameServer.Stop();

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact, TestPriority(5)]
        public async void CantPutToChangeEndpoint()
        {
            gameServer.Start();
            var response = await client.PutAsync(
                serverLocation,
                new JsonContent(
                    new GameServer {
                        Address = "127.0.0.2",
                        Name = "Test Server #2 (Delete if seen)",
                        TagLine = "This is a temporary listing created during testing. Please delete if seen.",
                        GamePort = 1,
                        Players = 0,
                        MaxPlayers = 5, // Adding a new field
                        RoundStatus = "invalid",
                        RoundStartTime = DateTime.Now,
                        Map = "None",
                        Game = "SS3D",
                        Gamemode = "TEST"
                    }
                )
            );
            gameServer.Stop();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact, TestPriority(6)]
        public async void CanDelete()
        {
            var response = await client.DeleteAsync(serverLocation);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact, TestPriority(7)]
        public async void CantGetAnyMore()
        {
            var response = await client.GetAsync(serverLocation);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private HttpClient client;
        private GameServerEmulator gameServer = new GameServerEmulator(100);
        private static Uri serverLocation;
    }
}
