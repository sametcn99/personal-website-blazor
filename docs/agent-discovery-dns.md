# Agent Discovery DNS

This site publishes its HTTP discovery documents and a read-only MCP transport at:

- `https://sametcc.me/llms.txt`
- `https://sametcc.me/auth.md`
- `https://sametcc.me/.well-known/mcp/server-card.json`
- `https://sametcc.me/mcp`

DNS-AID records must be added to the authoritative DNS zone for `sametcc.me`; they cannot be published by the ASP.NET application itself.

## DNS-AID Records

Publish these records in the `sametcc.me` zone:

```dns
; Organization-level agent index
_index._agents.sametcc.me. 3600 IN SVCB 1 sametcc.me. alpn="mcp" port=443 mandatory=alpn,port

; MCP service discovery alias
_mcp._agents.sametcc.me. 3600 IN SVCB 1 sametcc.me. alpn="mcp" port=443 mandatory=alpn,port
```

The SVCB target and port identify the HTTPS service. The MCP Server Card supplies the HTTP path and capability metadata at `/.well-known/mcp/server-card.json`. The application exposes read-only MCP initialization, resource listing/reading, and content search operations at `/mcp`.

Do not advertise an A2A record until an A2A endpoint is deployed. DNS-AID records should describe a reachable, supported protocol and not only a planned capability.

## DNSSEC

The public zone must be signed and its DS record must be published at the `.me` registrar:

1. Enable DNSSEC for the `sametcc.me` zone at the authoritative DNS provider.
2. Copy the provider's DS record to the domain registrar when the provider does not manage the registrar delegation.
3. Wait for the DS and DNSKEY chain to propagate.
4. Validate the SVCB response with a DNSSEC-validating resolver.

Example checks:

```bash
dig +dnssec _index._agents.sametcc.me SVCB
dig +dnssec _mcp._agents.sametcc.me SVCB
dig +dnssec sametcc.me DNSKEY
```

The response must contain the `RRSIG` data and a validating resolver should mark the answer authenticated. With DNS-over-HTTPS, the response should contain `AD: true`.

## Current Verification

At the time this file was added, the public DNS-over-HTTPS response for the DNS-AID names returned no SVCB answer and `AD: false`. The application changes in this repository do not replace the required DNS provider and registrar changes.
