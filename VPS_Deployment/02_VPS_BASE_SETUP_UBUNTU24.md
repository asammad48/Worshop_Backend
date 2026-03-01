# 02 — VPS base setup (Ubuntu 24.04)

sudo apt update && sudo apt upgrade -y
sudo apt install -y nginx postgresql postgresql-contrib ufw curl git unzip jq rsync
id -u deploy >/dev/null 2>&1 || sudo adduser --disabled-password --gecos "" deploy
sudo usermod -aG sudo deploy
sudo mkdir -p /opt/apps/myapp/api/publish /opt/apps/myapp/web/dist/dist /opt/apps/myapp/shared /opt/apps/myapp/backups
sudo chown -R deploy:deploy /opt/apps/myapp
sudo mkdir -p /home/deploy/.ssh && sudo chown -R deploy:deploy /home/deploy/.ssh && sudo chmod 700 /home/deploy/.ssh
nano /home/deploy/.ssh/authorized_keys
sudo chown deploy:deploy /home/deploy/.ssh/authorized_keys && sudo chmod 600 /home/deploy/.ssh/authorized_keys
sudo ufw allow OpenSSH && sudo ufw allow 80 && sudo ufw allow 443 && sudo ufw --force enable && sudo ufw status
