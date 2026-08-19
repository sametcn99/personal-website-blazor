# auth.md

## Agent Registration

This service supports the Auth.md standard for agent authentication and registration.
The intended audience is software agents acting on behalf of users who need access
to this site's public APIs.

This site advertises anonymous agent registration. Agents should use the published
OAuth metadata rather than probing registration or authorization endpoints.

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

### Agent Metadata

- **Identity types:** `anonymous`
- **Credential types:** `client_secret_basic`, `client_secret_post`, `bearer`
- **Profile claim URI:** `/api/profile`
- **Revocation URI:** `/agent/revoke`
- **Registration URI:** `/agent/register`
- **Authorization server metadata:** `/.well-known/oauth-authorization-server`
- **Protected resource metadata:** `/.well-known/oauth-protected-resource`
- **Web Bot Auth key directory:** `/.well-known/http-message-signatures-directory`

### Web Bot Auth Request Signing

The site publishes its HTTP Message Signatures verification key as a JWKS at
`/.well-known/http-message-signatures-directory`. Agents that send signed requests
can advertise this directory with the `Signature-Agent` header and use the matching
private key outside this repository.

The profile claim URI contains public identity and professional context for the
website owner. It is not an authorization decision and must not be treated as a
trust signal for arbitrary external agents.

### Supported Methods

- OAuth 2.0 Bearer Token (HTTP Header)
- OAuth 2.0 Bearer Token (URI Query Parameter)

### Contact

For questions about agent authentication, contact: sametcn99@gmail.com
