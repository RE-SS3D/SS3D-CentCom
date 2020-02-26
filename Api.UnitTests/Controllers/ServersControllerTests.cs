using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;
using CentCom.Tests.Models;
using CentCom.Models;
using CentCom.Controllers;
using Api.UnitTests.Helpers;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace CentCom.Tests.Controllers
{
    public class ServersControllerTests
    {
        public ServersControllerTests(ITestOutputHelper output)
        {
            logger = new NullLogger<ServersController>();
            // Reset data after every test.
            MockDataContext.Reset();
        }

        [Fact]
        public async void GetServers_WithOutdated_OnlyReturnsCurrent()
        {
            using (var context = MockDataContext.GetContext()) {
                context.Servers.Add(new GameServer {
                    Name = "A",
                    Address = "127.0.0.1",
                    QueryPort = 100,
                    GamePort = 100,
                    RoundStatus = "starting",
                    RoundStartTime = DateTime.Now,
                    Game = "SS3D",
                    LastUpdate = DateTime.Now
                });
                context.Servers.Add(new GameServer {
                    Name = "B",
                    Address = "127.0.0.2",
                    QueryPort = 200,
                    GamePort = 200,
                    RoundStatus = "starting",
                    RoundStartTime = DateTime.Now,
                    Game = "SS3D",
                    LastUpdate = DateTime.Now - TimeSpan.FromMinutes(2)
                });
                // This one should definitely be out of date
                context.Servers.Add(new GameServer {
                    Name = "C",
                    Address = "127.0.0.3",
                    QueryPort = 300,
                    GamePort = 300,
                    RoundStatus = "starting",
                    RoundStartTime = DateTime.Now,
                    Game = "SS3D",
                    LastUpdate = DateTime.Now - TimeSpan.FromMinutes(6)
                });
                context.SaveChanges();
            }

            // Use a separate, fresh context for testing.
            using (var context = MockDataContext.GetContext()) {
                var controller = new ServersController(context, HttpFactoryMock.CreateMock().Object, logger);
                
                IEnumerable<GameServer> servers = await controller.GetServers().ToListAsync();
                Assert.Equal(2, servers.Count());
                Assert.DoesNotContain(servers, server => server.Name == "C");
            }
        }

        [Fact]
        public async void PostNewServer_WithNew_IsSuccessful()
        {
            using (var context = MockDataContext.GetContext()) {
                // A mock which returns the input challenge.
                var controller = new ServersController(context, ConstructChallengeRespondingMock(), logger);

                var result = await controller.PostNewServer(new GameServer {
                    Name = "Server A",
                    Address = "127.0.0.1",
                    QueryPort = 100,
                    GamePort = 100,
                    RoundStatus = "starting",
                    RoundStartTime = DateTime.Now,
                    Game = "SS3D"
                }).ConfigureAwait(false);
                Assert.Equal(StatusCodes.Status201Created, (result as IStatusCodeActionResult).StatusCode);
            }
            
            using (var context = MockDataContext.GetContext()) {
                Assert.Single(context.Servers);
            }
        }

        [Fact]
        public async void PostNewServer_WithExisting_GivesError()
        {
            // Setup
            using (var context = MockDataContext.GetContext()) {
                context.Servers.Add(new GameServer {
                    Name = "Server Not A",
                    Address = "2001:0db8:85a3:0000:0000:8a2e:0370:7334",
                    QueryPort = 100,
                    GamePort = 100,
                    RoundStatus = "starting",
                    RoundStartTime = DateTime.Now,
                    Game = "SS3D",
                    LastUpdate = DateTime.Now
                });
                context.SaveChanges();
            }

            using (var context = MockDataContext.GetContext()) {
                var controller = new ServersController(context, ConstructChallengeRespondingMock(), logger);

                var result = await controller.PostNewServer(new GameServer {
                    Name = "Server A",
                    Address = "2001:0db8:85a3:0000:0000:8a2e:0370:7334",
                    QueryPort = 100,
                    GamePort = 100,
                    RoundStatus = "starting",
                    RoundStartTime = DateTime.Now,
                }).ConfigureAwait(false);
                Assert.Equal(StatusCodes.Status409Conflict, (result as IStatusCodeActionResult).StatusCode);
            }

            using (var context = MockDataContext.GetContext()) {
                var controller = new ServersController(context, ConstructChallengeRespondingMock(), logger);
                var servers = await controller.GetServers().ToListAsync();
                Assert.Single(servers);
                Assert.Equal("Server Not A", servers[0].Name); // Nothing about the original entry should have changed.
            }
        }

        [Fact]
        public async void PutServer_WithExisting_WillChangeItem()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 100);

            using (var context = MockDataContext.GetContext()) {
                context.Servers.Add(new GameServer {
                    Name = "Server Not A",
                    Address = endPoint.Address.ToString(),
                    QueryPort = endPoint.Port,
                    GamePort = 100,
                    RoundStatus = "starting",
                    RoundStartTime = DateTime.Now,
                    Game = "SS3D",
                    LastUpdate = DateTime.Now - TimeSpan.FromMinutes(2)
                });
                context.SaveChanges();
            }

            // Use a separate, fresh context for testing.
            using (var context = MockDataContext.GetContext()) {
                var controller = new ServersController(context, ConstructChallengeRespondingMock(), logger);
                MockControllerConnection(controller, endPoint.Address);

                var result = controller.PutServer(endPoint.ToString(), new GameServer {
                    Name = "Not A",
                    GamePort = 100,
                    RoundStatus = "starting",
                    RoundStartTime = DateTime.Now,
                    Game = "SS3D"
                });

                // Should have 200 status code, given that the item already exists.
                Assert.Equal(StatusCodes.Status200OK, (result as IStatusCodeActionResult).StatusCode);
            }

            using (var postContext = MockDataContext.GetContext()) {
                var onlyServer = Assert.Single(postContext.Servers);
                Assert.Equal("Not A", onlyServer.Name);
                
                // Check that the LastUpdate has changed to now too
                Assert.True(onlyServer.LastUpdate > DateTime.Now - TimeSpan.FromSeconds(10));
            }
        }

        [Fact]
        public void PutServer_WithMissing_WillGiveError()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 100);
            using var context = MockDataContext.GetContext();

            var controller = new ServersController(context, ConstructChallengeRespondingMock(), logger);
            MockControllerConnection(controller, endPoint.Address);

            var result = controller.PutServer(endPoint.ToString(), new GameServer { Name = "Not A" });

            // Should have 200 status code, given that the item already exists.
            Assert.Equal(StatusCodes.Status404NotFound, (result as IStatusCodeActionResult).StatusCode);
        }

        [Fact]
        public void PostServerHeartBeat_WithExisting_UpdatesTime()
        {
            // SETUP
            var endPoint = new IPEndPoint(100, 1);
            using (var preContext = MockDataContext.GetContext()) {
                preContext.Servers.Add(new GameServer {
                    Name = "A",
                    Address = endPoint.Address.ToString(),
                    QueryPort = endPoint.Port,
                    GamePort = 100,
                    RoundStatus = "starting",
                    RoundStartTime = DateTime.Now,
                    Game = "SS3D",
                    LastUpdate = DateTime.Now - TimeSpan.FromMinutes(2)
                });
                preContext.SaveChanges();
            }

            using var context = MockDataContext.GetContext();

            var controller = new ServersController(context, ConstructChallengeRespondingMock(), logger);
            MockControllerConnection(controller, endPoint.Address);

            // ACTUAL TEST

            // Use same IP as was originally given
            var response = controller.PostHeartBeatUpdate(endPoint.ToString());

            // Should be successful
            Assert.Equal(StatusCodes.Status200OK, (response as IStatusCodeActionResult).StatusCode);

            // Get the server
            using (var postContext = MockDataContext.GetContext()) {
                var onlyServer = Assert.Single(postContext.Servers);

                // Check that the LastUpdate has changed
                Assert.True(onlyServer.LastUpdate > DateTime.Now - TimeSpan.FromSeconds(10));
            }
        }

        [Fact]
        public void PostServerHeartBeat_WithNonExistant_GivesNotFound()
        {
            var endPoint = new IPEndPoint(100, 1);
            using var context = MockDataContext.GetContext();
            var controller = new ServersController(context, ConstructChallengeRespondingMock(), logger);
            MockControllerConnection(controller, endPoint.Address);

            // Note: This assumes the resource format
            var response = controller.PostHeartBeatUpdate(endPoint.ToString());

            Assert.Equal(StatusCodes.Status404NotFound, (response as IStatusCodeActionResult).StatusCode);
        }

        [Fact]
        public void PostServerHeartBeat_WithBadForm_GivesBadRequest()
        {
            var endPoint = new IPEndPoint(100, 1);
            using (var preContext = MockDataContext.GetContext()) {
                preContext.Servers.Add(new GameServer {
                    Name = "A",
                    Address = endPoint.Address.ToString(),
                    QueryPort = endPoint.Port,
                    GamePort = 200,
                    RoundStatus = "starting",
                    RoundStartTime = DateTime.Now,
                    Game = "SS3D",
                    LastUpdate = DateTime.Now - TimeSpan.FromMinutes(2)
                });
                preContext.SaveChanges();
            }

            using var context = MockDataContext.GetContext();
            var controller = new ServersController(context, ConstructChallengeRespondingMock(), logger);
            MockControllerConnection(controller, endPoint.Address);

            var response = controller.PostHeartBeatUpdate("fsafsfa");

            Assert.Equal(StatusCodes.Status400BadRequest, (response as IStatusCodeActionResult).StatusCode);
        }

        [Fact]
        public void PostServerHeartBeat_WithDifferentIp_GivesForbidden()
        {
            var endPoint = new IPEndPoint(100, 1);
            using (var preContext = MockDataContext.GetContext()) {
                preContext.Servers.Add(new GameServer {
                    Name = "A",
                    Address = endPoint.Address.ToString(),
                    QueryPort = endPoint.Port,
                    GamePort = 200,
                    RoundStatus = "starting",
                    RoundStartTime = DateTime.Now,
                    Game = "SS3D",
                    LastUpdate = DateTime.Now - TimeSpan.FromMinutes(2)
                });
                preContext.SaveChanges();
            }

            using var context = MockDataContext.GetContext();
            // Test
            var controller = new ServersController(context, ConstructChallengeRespondingMock(), logger);
            MockControllerConnection(controller, new IPAddress(200));

            // Actual Test
            var response = controller.PostHeartBeatUpdate(endPoint.ToString());

            Assert.IsType<ForbidResult>(response);
        }
    
        [Fact]
        public void Delete_WithExisting_IsSuccessful()
        {
            var endPoint = new IPEndPoint(100, 1);
            using (var context = MockDataContext.GetContext()) {
                context.Servers.Add(new GameServer {
                    Name = "C",
                    Address = endPoint.Address.ToString(),
                    QueryPort = endPoint.Port,
                    GamePort = 300,
                    RoundStatus = "starting",
                    RoundStartTime = DateTime.Now,
                    Game = "SS3D",
                    LastUpdate = DateTime.Now - TimeSpan.FromMinutes(6)
                });
                context.SaveChanges();
            }

            using (var context = MockDataContext.GetContext()) {
                var controller = new ServersController(context, ConstructChallengeRespondingMock(), logger);
                
                IActionResult response = controller.DeleteServer(endPoint.ToString());
                
                Assert.Equal(StatusCodes.Status204NoContent, (response as IStatusCodeActionResult).StatusCode);
            }
        }

        [Fact]
        public void Delete_WithNonExistant_GivesNotFound()
        {
            var endPoint = new IPEndPoint(100, 1);
            using var context = MockDataContext.GetContext();
            var controller = new ServersController(context, ConstructChallengeRespondingMock(), logger);

            IActionResult response = controller.DeleteServer(endPoint.ToString());

            Assert.Equal(StatusCodes.Status404NotFound, (response as IStatusCodeActionResult).StatusCode);
        }

        [Fact]
        public void Delete_WithDifferentIp_IsForbidden()
        {
            var endPoint = new IPEndPoint(101, 1);
            using (var context = MockDataContext.GetContext()) {
                context.Servers.Add(new GameServer {
                    Name = "C",
                    Address = endPoint.Address.ToString(),
                    QueryPort = endPoint.Port,
                    GamePort = 300,
                    RoundStatus = "starting",
                    RoundStartTime = DateTime.Now,
                    Game = "SS3D",
                    LastUpdate = DateTime.Now - TimeSpan.FromMinutes(6)
                });
                context.SaveChanges();
            }

            using (var context = MockDataContext.GetContext()) {
                var controller = new ServersController(context, ConstructChallengeRespondingMock(), logger);
                MockControllerConnection(controller, new IPAddress(202));

                IActionResult response = controller.DeleteServer(endPoint.ToString());

                Assert.Equal(StatusCodes.Status204NoContent, (response as IStatusCodeActionResult).StatusCode);
            }
        }

        public static IHttpClientFactory ConstructChallengeRespondingMock()
        {
            // A mock which returns the input challenge.
            return HttpFactoryMock.CreateMock((message, _) => {
                var challenge = int.Parse(Regex.Match(message.RequestUri.Query, "(?<=challenge=)\\d+").Value, CultureInfo.CurrentCulture);

                return Task.FromResult(
                    new HttpResponseMessage {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent($"{{ \"challenge\": {challenge} }}")
                    }
                );
            }).Object;
        }

        public static void MockControllerConnection(ServersController controller, IPAddress remoteAddress)
        {
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.Connection.RemoteIpAddress = remoteAddress;
        }

        private readonly ILogger<ServersController> logger;
    }
}
