# Master Server List Protocol

This protocol governs how a game server may advertise itself to a master server, and how information is retrieved from the game server.
Unlike other server list protocols, this uses HTTP for ease of implementation and communication. Most of the actual process is still somewhat similar however.

The security of this protocol relies on initially ensuring the created server's ip and query port are valid (by sending a request to them),
and then on all later interactions checking that the ip of the incoming query is equal to the ip of the server being accessed.

Additionally, there is a greater communication of port info here, rather than a reliance on identifying ports by
checking ip packet information, so as to correctly work with NAT.

Note: There may be a future drive to also implement the source protocol in TCP for game servers.

## Terminology

Master Server - Hosts the server list
Game Server - Hosts a game that players can connect to.
Query Port - The port on the game server used for query information and other http or tcp requests
Game Port - The port on the game server dedicated to the game.

## Protocol

The below is more an explanation of the sequence of events. To understand what interfaces exactly need to be implemented, look at the
openapi document.

### Game Server to Master Server

1. The Game Server must already know address of master server.

2. POST to `/api/servers` with the body containing the [Game Server's details](#Game-Server-Object)

    1. Master server then makes a POST to `/connect?master=[master hostname]&version=[master server version]&challenge=[random challenge number]`

    2. The game server responds with 200 OK, with the body containing the challenge id as `{ challenge: int }`

        NOTE: This requires that the Game Server can handle multiple simultaneous HTTP requests. Please ensure this is possible.

    3. If successful the server will respond to the original POST with a 201 and the future address to use in the Location header.
      The location will be of format `api/server/[server-id]`. Additionally, [a body](#Connection-Response-Object) containing master server
      info is returned. This contains a token that must be in all future communication.
      If this token is lost, the connection can be restarted by starting the whole process again.

    The connection is now established and an entry is added into the master server's list.

3. The game server must, at least as often as `heartbeatPeriod`, send updates to the server either through:
    - PUT `/api/servers/[server-id]`, with [same body format](#Game-Server-Object), same as location provided by the original POST
    - POST `/api/servers/[server-id]`, with no body.

    The former is used to update information about the server, and the latter is used if there is no information to update,
    but a heartbeat is still needed.

4. When the game server is ready to close the connection, they send a DELETE `api/servers/[server-id]`

If the game server fails to send an update within the heartbeat period, the master server will delete the game server entry.
The game server can re-add themselves to the master server by starting the process over again.

### Client to Master Server

The client can get a list of servers by performing a GET on `/api/servers`. Query parameters that are supported are:

- game=`string`
- branch=`string`
- round=`[restarting, lobby, playing]`
- minPlayers=`int`
- maxPlayers=`int`

The master server returns a list of [Game Server Objects](#Game-Server-Object),
containing the ip/hostname and port.

### Client to Game Server

A client may request info from the game server by making a GET to `/info`. This returns the [Game Server Object](#Game-Server-Object).

## Objects

### Game Server Object

```javascript
{
    name: string, // Name of server.
    tagLine: string, // Optional. Custom text appearing below name.

    address: string,
    gamePort: int, // Port used for connecting to the game.
    queryPort: int, // Port used for querying game information. All http requests should go to here.

    players: int,
    maxPlayers: int, // Optional. -1 implies infinite

    roundStatus: "restarting" | "lobby" | "playing" | string, // Ideally should be one of the fixed strings listed.
    roundStartTime: int, // In seconds since epoch. Should be time since last status change.

    map: string, // Optional
    gamemode: string, // Optional

    game: "SS3D" | string, // Note: Should almost always be SS3D. Different codebases should change 'branch' instead.
    branch: "root" | string, // Optional. The specific codebase this game is running.
    version: string, // Optional. Should be in format major.minor.patch, year-month-day, or GIT hash
}
```

Additional fields are explicitly allowed in the object. The default master server will ignore them. This allows other games to
pass more specific details in their place.

Fields marked as optional do not have to be present.

### Connection Response Object

```javascript
{
    heartbeatPeriod: int, // in seconds. Time after which master server will drop the game server. Default is 60 seconds.
}
```

## Routes Mentioned

For more info on these look at the openapi docs.

### Master Server

- GET `/api/servers/` - Gets the list of game servers
- POST `/api/servers/` - Creates a new entry for a game server
- PUT `/api/servers/[id]` - Updates game server information, and resets heartbeat
- POST `/api/servers/[id]` - Resets heartbeat
- DELETE `/api/servers/[id]` - Removes game server from list

### Game Server

- POST `/connect` - connects the master server to the game server, sends a challenge and master server info.
- GET `/info` - gets information about the game server

## Future Improvements

- Configure secure connections from the master server to the game server

## Security, Safety, and Usage concerns

- The critical point in connection is the (HTTP) request from master server to game server.
  The request is necessary to ensure that someone isn't maliciously posting server entries for
  other hosts, so we must ensure this weak point can't be abused in any way.
