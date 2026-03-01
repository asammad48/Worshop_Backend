# 06 — GitHub Actions (2 repos) + rsync wipe fix

echo "deploy ALL=NOPASSWD:/bin/systemctl restart myapp-api,/bin/systemctl reload nginx" | sudo tee /etc/sudoers.d/deploy-myapp >/dev/null && sudo chmod 440 /etc/sudoers.d/deploy-myapp

# Secrets in BOTH repos: VPS_HOST, VPS_USER, VPS_PORT, VPS_SSH_KEY
# Backend repo extra: BACKEND_DEPLOY_DIR=/opt/apps/myapp/api/publish
# Frontend repo extra: FRONTEND_DEPLOY_DIR=/opt/apps/myapp/web/dist/dist

# Add workflow files:
# Backend repo: templates/github/workflows/deploy-backend.yml → .github/workflows/deploy-backend.yml
# Frontend repo: templates/github/workflows/deploy-frontend.yml → .github/workflows/deploy-frontend.yml

# CRITICAL: edit backend csproj path inside deploy-backend.yml before running
