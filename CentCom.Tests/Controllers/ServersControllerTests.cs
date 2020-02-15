using System;
using System.Net;
using System.Linq;
using Xunit;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Http;
using CentCom.Tests.Models;
using CentCom.Models;
using CentCom.Controllers;
using CentCom.Tests.Services;
using Microsoft.AspNetCore.Mvc;

namespace CentCom.Tests.Controllers
{
    public class ServersControllerTests
    {
        public ServersControllerTests()
        {
            // Reset data after every test.
            MockDataContext.Reset();
        }

        [Fact]
        public async void GetServers_WithOutdated_OnlyReturnsCurrent()
        {
            using (var context = MockDataContext.GetContext()) {
                context.Servers.Add(new Server { Name = "A", Address = new IPEndPoint(100, 1), LastUpdate = DateTime.Now });
                context.Servers.Add(new Server {
                    Name = "B",
                    Address = new IPEndPoint(200, 2),
                    LastUpdate = DateTime.Now - TimeSpan.FromMinutes(2)
                });
                // This one should definitely be out of date
                context.Servers.Add(new Server {
                    Name = "C",
                    Address = new IPEndPoint(300, 3),
                    LastUpdate = DateTime.Now - TimeSpan.FromMinutes(6)
                });
                context.SaveChanges();
            }

            // Use a separate, fresh context for testing.
            using (var context = MockDataContext.GetContext()) {
                var controller = new ServersController(context, new MockClientInfoService());
                
                IEnumerable<Server> servers = await controller.GetServers().ToListAsync();
                Assert.Equal(2, servers.Count());
                Assert.DoesNotContain(servers, server => server.Name == "C");
            }
        }

        [Fact]
        public async void PostNewServer_WithNew_IsSuccessful()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"), 15006);
            using (var context = MockDataContext.GetContext()) {
                var controller = new ServersController(context, new MockClientInfoService(endPoint));

                var result = controller.PostNewServer(new Dtos.ServerDto { Name = "Server A" });
                Assert.Equal(StatusCodes.Status201Created, (result as IStatusCodeActionResult).StatusCode);
            }
            
            using (var context = MockDataContext.GetContext()) {
                var controller = new ServersController(context, new MockClientInfoService(endPoint));
                var servers = await controller.GetServers().ToListAsync();
                Assert.Single(servers);
                Assert.Equal(endPoint, servers[0].Address);
            }
        }

        [Fact]
        public async void PostNewServer_WithExisting_GivesError()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"), 15006);

            // Setup
            using (var context = MockDataContext.GetContext()) {
                context.Servers.Add(new Server { Address = endPoint, Name = "Server Not A", LastUpdate = DateTime.Now });
                context.SaveChanges();
            }

            using (var context = MockDataContext.GetContext()) {
                var controller = new ServersController(context, new MockClientInfoService(endPoint));

                var result = controller.PostNewServer(new Dtos.ServerDto { Name = "Server A" });
                Assert.Equal(StatusCodes.Status409Conflict, (result as IStatusCodeActionResult).StatusCode);
            }

            using (var context = MockDataContext.GetContext()) {
                var controller = new ServersController(context, new MockClientInfoService(endPoint));
                var servers = await controller.GetServers().ToListAsync();
                Assert.Single(servers);
                Assert.Equal("Server Not A", servers[0].Name); // Nothing about the original entry should have changed.
            }
        }

        [Fact]
        public async void PutServer_WithNormal_WillAddToList()
        {
            using var context = MockDataContext.GetContext();
            var endPoint = new IPEndPoint(101, 1001);

            var controller = new ServersController(context, new MockClientInfoService(endPoint));
            // Test the Put was successful
            var result = controller.PutServer(endPoint.ToString(), new Dtos.ServerDto { Name = "Test Server" });

            Assert.Equal(StatusCodes.Status201Created, (result as IStatusCodeActionResult).StatusCode);

            var servers = await controller.GetServers().ToListAsync();
            Assert.Single(servers);
        }

        [Fact]
        public async void PutServer_WithExisting_WillChangeItem()
        {
            var endPoint = new IPEndPoint(100, 1);

            using (var context = MockDataContext.GetContext()) {
                context.Servers.Add(new Server { Name = "A", Address = endPoint, LastUpdate = DateTime.Now - TimeSpan.FromMinutes(2) });
                context.SaveChanges();
            }

            // Use a separate, fresh context for testing.
            using (var context = MockDataContext.GetContext()) {
                var controller = new ServersController(context, new MockClientInfoService(endPoint));

                var result = controller.PutServer(endPoint.ToString(), new Dtos.ServerDto { Name = "Not A" });

                // Should have 200 status code, given that the item already exists.
                Assert.Equal(StatusCodes.Status200OK, (result as IStatusCodeActionResult).StatusCode);

                var servers = await controller.GetServers().ToListAsync();
                Assert.Single(servers);
                Assert.Equal("Not A", servers[0].Name);
                
                // Check that the LastUpdate has changed to now too
                Assert.True(servers[0].LastUpdate > DateTime.Now - TimeSpan.FromSeconds(10));
            }
        }

        [Fact]
        public async void PostServerHeartBeat_WithExisting_UpdatesTime()
        {
            using var context = MockDataContext.GetContext();

            // SETUP
            var endPoint = new IPEndPoint(100, 1);
            var controller = new ServersController(context, new MockClientInfoService(endPoint));

            // Create the server entry
            controller.PutServer(endPoint.ToString(), new Dtos.ServerDto { Name = "A" });

            // Manually push back the last update, so that we really know LastUpdate was reset by the Post.
            context.Servers.First(server => server.Name == "A").LastUpdate = DateTime.Now - TimeSpan.FromMinutes(2);
            context.SaveChanges();

            // ACTUAL TEST

            // Use same IP as was originally given
            controller.PostHeartBeatUpdate(endPoint.ToString());

            // Get the server
            var servers = await controller.GetServers().ToListAsync();
            Assert.Single(servers);

            // Check that the LastUpdate has changed
            Assert.True(servers[0].LastUpdate > DateTime.Now - TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void PostServerHeartBeat_WithNonExistant_GivesNotFound()
        {
            var endPoint = new IPEndPoint(100, 1);
            using var context = MockDataContext.GetContext();
            var controller = new ServersController(context, new MockClientInfoService(endPoint));

            // Note: This assumes the resource format
            var response = controller.PostHeartBeatUpdate(endPoint.ToString());

            Assert.Equal(StatusCodes.Status404NotFound, (response as IStatusCodeActionResult).StatusCode);
        }

        [Fact]
        public void PostServerHeartBeat_WithBadForm_GivesBadRequest()
        {
            using var context = MockDataContext.GetContext();
            var controller = new ServersController(context, new MockClientInfoService());

            var response = controller.PostHeartBeatUpdate("fsafsfa");

            Assert.Equal(StatusCodes.Status400BadRequest, (response as IStatusCodeActionResult).StatusCode);
        }

        [Fact]
        public void PostServerHeartBeat_WithDifferentIp_GivesForbidden()
        {
            var endPoint = new IPEndPoint(100, 1);
            using var context = MockDataContext.GetContext();
            // Setup
            var firstController = new ServersController(context, new MockClientInfoService(endPoint));
            // Create the server entry
            firstController.PutServer(endPoint.ToString(), new Dtos.ServerDto { Name = "A" });

            // Test
            var secondController = new ServersController(context, new MockClientInfoService(101, 1));

            // Actual Test
            var response = secondController.PostHeartBeatUpdate(endPoint.ToString());

            Assert.IsType<ForbidResult>(response);
        }
    }
}
