# CentCom
RESTful API for managing user registration, authentication and character storage for RE:SS3D

### Installation

- Clone repository
- Update `appsettings.json`. Specifically the JWT secret, database name, database user and database password.
- Make a build of the API
- Prepare a server with a MySql database. A dump of the database structure can be found in the [docs folder](docs/database_dump.sql).
- Deploy your build to the server of choice. Move your `appsettings.json` to the same folder as `CentCom.dll`
- Launch with `dotnet CentCom.dll`
- The API listens on port 5001 for HTTPS and 5000 for HTTP

### API SPEC
A swagger specification for the API can be found in the [docs folder]((docs/swagger.yaml)).

TODO:

update migration needs to unload all projects

running integrations requires locally running mysql instance.