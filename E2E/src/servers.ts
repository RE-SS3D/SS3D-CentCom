import 'mocha';
import chai, { expect } from 'chai';
import chai_http from 'chai-http';

chai.use(chai_http);

interface Server {
  ip: string;
  port: string;
  name: string;
  lastUpdate: Date;
}

context("/api/servers", function run() {
  // If there is an error, stop running further tests.
  // This is because future tests rely on previous ones being successful.
  this.bail(true);

  const server_url = process.env.API_URL || "http://localhost:4110";
  const server = chai.request(server_url);

  it("should get all existing servers", async () => {
    const response = await server
      .get('/api/servers');

    expect(response).to.have.status(200);

    const { body } = response;
    expect(body).to.be.an('array');
    if((body as []).length > 0) {
      expect(body[0]).to.have.property('ip');
      expect(body[0]).to.have.property('port');
      expect(body[0]).to.have.property('name');
    }
  });

  let my_own_server_uri = "";
  let last_update_time: Date | undefined;

  it("should add a server via post", async () => {
    const response = await server
      .post('/api/servers')
      .send({ name: 'My Own Test Server' });

    expect(response).to.have.status(201);

    expect(response.body).to.have.property('ip');
    expect(response.body).to.have.property('port');
    expect(response.body).to.have.property('name');

    // eslint-disable-next-line no-unused-expressions
    expect(response.header.location).to.exist;

    my_own_server_uri = (response.header.location as string).replace(server_url, '');
    last_update_time = response.body.lastUpdate as Date | undefined;
  });

  it("should get the created server", async () => {
    const response = await server
      .get(my_own_server_uri);

    expect(response).to.have.status(200);
    expect(response.body).to.deep.contain({ name: 'My Own Test Server' });
  });

  it("should update the server", async () => {
    const response = await server
      .put(my_own_server_uri)
      .send({ name: 'Updated Test Server' });

    expect(response).to.have.status(200);
  });

  it("should find the updated server in the list", async () => {
    const response = await server
      .get('/api/servers');

    const server_object = (response.body as Server[]).find(s => s.name === 'Updated Test Server');
    expect(server_object).to.not.equal(null);

    // Ensure the object was updated
    if(server_object && server_object.lastUpdate && last_update_time) {
      expect(server_object.lastUpdate).to.be.greaterThan(last_update_time);
      last_update_time = server_object.lastUpdate;
    }
  });

  it("should refresh the server's last-update on heartbeat", async () => {
    const response = await server
      .post(my_own_server_uri);

    expect(response).to.have.status(200);

    // Get the item
    const get_response = await server
      .get(my_own_server_uri);

    if(get_response.body.lastUpdate && last_update_time)
      expect(get_response.body.lastUpdate).to.be.greaterThan(last_update_time);
  });

  it("should delete the server", async () => {
    const response = await server
      .delete(my_own_server_uri);

    expect(response).to.have.status(204);
  });

  it("should be unable to find the server", async () => {
    const response = await server
      .get(my_own_server_uri);

    expect(response).to.have.status(404);
  });
});
