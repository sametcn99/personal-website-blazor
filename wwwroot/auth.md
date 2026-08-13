# auth.md

## Agent Registration

This service supports the Auth.md standard for agent authentication and registration.
The intended audience is software agents acting on behalf of users who need access
to this site's public APIs.

### OAuth Protected Resource Metadata

This resource server publishes OAuth Protected Resource Metadata at
`/.well-known/oauth-protected-resource` for agent discovery.

### Authorization Server

Agents should use the authorization server advertised in the Protected Resource
Metadata for authentication and token issuance. Its discovery document is
available at `/.well-known/oauth-authorization-server`.

### Registration

Agents can register by following the OAuth 2.0 Dynamic Client Registration
protocol at `/agent/register`, as specified in the authorization server metadata.
The endpoint accepts a `POST` request with an `application/json` registration
payload and returns the client credentials issued by the authorization server.

The authorization endpoint is `/agent/auth` and the token endpoint is
`/agent/token`. Present the resulting access token as an HTTP
`Authorization: Bearer <token>` header when calling protected APIs.

### Supported Methods

- OAuth 2.0 Bearer Token (HTTP Header)
- OAuth 2.0 Bearer Token (URI Query Parameter)

### Contact

For questions about agent authentication, contact: sametcn99@gmail.com
