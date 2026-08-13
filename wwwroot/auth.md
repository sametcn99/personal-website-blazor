# auth.md

## Agent Registration

This service supports the Auth.md standard for agent authentication and registration.

### OAuth Protected Resource Metadata

This resource server publishes OAuth Protected Resource Metadata at `/.well-known/oauth-protected-resource` for agent discovery.

### Authorization Server

Agents should use the authorization server advertised in the Protected Resource Metadata for authentication and token issuance.

### Registration

Agents can register by following the OAuth 2.0 Dynamic Client Registration protocol as specified in the authorization server metadata.

### Supported Methods

- OAuth 2.0 Bearer Token (HTTP Header)
- OAuth 2.0 Bearer Token (URI Query Parameter)

### Contact

For questions about agent authentication, contact: sametcn99@gmail.com
