# Deploying the server

The server is a normal ASP.NET Core app, so it runs anywhere .NET or Docker runs. These notes
describe the container deployment behind a reverse proxy (the setup used for
`unhallowed.cubiq.lt`).

The WinForms **client is Windows-only** and is never deployed — only the server is containerised.
Players run the client on their own Windows machines and connect to the public URL.

## Architecture

```
Player's client  ──wss──▶  Cloudflare  ──▶  cloudflared tunnel  ──▶  Caddy  ──ws──▶  unhallowed:5080
   (WinForms)             (terminates TLS)                        (reverse proxy)     (this server)
```

- **Cloudflare** terminates TLS, so the public address is `https://` / `wss://`.
- **Caddy** runs with `auto_https off` and reverse-proxies plain HTTP by container name. It forwards
  WebSocket upgrades automatically, which is all SignalR needs.
- The server container joins the shared external **`proxy`** Docker network, so Caddy reaches it as
  `unhallowed:5080`.

## 1. Run the container

From this project's root on the server:

```bash
docker compose up -d --build
```

That builds the image and starts one container named `unhallowed` on the `proxy` network. Verify it
answers from inside the network:

```bash
docker run --rm --network proxy curlimages/curl -s http://unhallowed:5080/
```

You should get the server's JSON status.

> Run **exactly one** instance. All match state lives in memory in the process, so a second replica
> would host a separate, invisible set of games. Do not put it behind a load balancer.

## 2. Add the Caddy site

Add this block to the Caddyfile (`/home/mycius/caddy/Caddyfile`) and reload Caddy:

```
http://unhallowed.cubiq.lt {
	encode gzip zstd
	reverse_proxy unhallowed:5080
}
```

```bash
docker exec caddy caddy validate --config /etc/caddy/Caddyfile
docker exec caddy caddy reload --config /etc/caddy/Caddyfile
```

## 3. Route the hostname through Cloudflare (manual, dashboard)

The Cloudflare Tunnel is token-based, so its routes live in the Cloudflare dashboard, not on the
server. Add a public hostname:

- **Zero Trust → Networks → Tunnels →** your tunnel **→ Public Hostnames → Add**
- Subdomain `unhallowed`, domain `cubiq.lt`
- Service: `HTTP` → `caddy:80`

Cloudflare creates the DNS record automatically. WebSockets are enabled by default on Cloudflare, so
no extra toggle is needed.

## 4. Connect

In the client's join dialog, set the server to:

```
https://unhallowed.cubiq.lt
```

Players who share a match code share a game (up to 4 per lobby); different codes are independent
games and the server can host many at once.
