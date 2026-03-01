# 01 — SSH key rotation (Windows Git Bash + VPS)

## Windows (Git Bash)
mkdir -p ~/ssh_backup && cp -f ~/.ssh/github_actions_myapp* ~/ssh_backup/ 2>/dev/null || true
rm -f ~/.ssh/github_actions_myapp ~/.ssh/github_actions_myapp.pub 2>/dev/null || true
ssh-keygen -t ed25519 -C "github-actions-myapp" -f ~/.ssh/github_actions_myapp -N ""
cat ~/.ssh/github_actions_myapp.pub
cat ~/.ssh/github_actions_myapp

## VPS: remove old key + add new (deploy user)
ssh -p 22 deploy@YOUR_SERVER_IP
mkdir -p ~/.ssh && chmod 700 ~/.ssh
nano ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys

## Test from Windows with specific key
ssh -i ~/.ssh/github_actions_myapp -p 22 deploy@YOUR_SERVER_IP

## GitHub Secrets (BOTH repos)
# VPS_HOST, VPS_USER, VPS_PORT, VPS_SSH_KEY
