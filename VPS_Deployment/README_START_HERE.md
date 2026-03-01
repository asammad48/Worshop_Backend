# VPS Deploy A–Z (Ubuntu 24.04) — Step-by-step (2 repos) + Key Rotation + Backend Deploy Script

This pack defines placeholders first, then gives step-by-step actions.

## Replace THESE placeholders everywhere
- YOUR_SERVER_IP = VPS IP (e.g., 123.45.67.89)
- YOUR_DOMAIN = DNS domain pointing to VPS (e.g., app.example.com)
- VPS_PORT = 22 (unless changed)
- BACKEND_CSProj = your backend API csproj path (inside backend repo)
- API_DLL = Api.dll
- BACKEND_DEPLOY_DIR = /opt/apps/myapp/api/publish
- FRONTEND_DEPLOY_DIR = /opt/apps/myapp/web/dist/dist

## What you do (order)
1) Windows: generate keypair → add PUBLIC to VPS → add PRIVATE to GitHub Secrets (both repos)
2) VPS: install packages + create dirs
3) VPS: PostgreSQL create db/user
4) VPS: .NET runtime + systemd + backend deploy script (missing earlier)
5) VPS: nginx config + optional SSL
6) GitHub: add 2 workflows (backend + frontend) with SAFE rsync guards

Included:
- Key rotation steps (delete old key from server + add new)
- Server-side backend deploy script (optional) for manual deploy or troubleshooting
- Two workflows + guard rails to prevent rsync wipe
