# 04 — Backend deploy script + systemd

sudo apt update && sudo apt install -y aspnetcore-runtime-8.0
printf "%s\n" "ASPNETCORE_ENVIRONMENT=Production" "ASPNETCORE_URLS=http://127.0.0.1:5000" "ConnectionStrings__Default=Host=127.0.0.1;Port=5432;Database=workshopdb;Username=workshopuser;Password=CHANGE_ME" "Jwt__Key=CHANGE_ME_LONG" | sudo tee /opt/apps/myapp/shared/myapp.env >/dev/null
sudo chown deploy:deploy /opt/apps/myapp/shared/myapp.env && sudo chmod 600 /opt/apps/myapp/shared/myapp.env
sudo cp templates/systemd/myapp-api.service /etc/systemd/system/myapp-api.service
sudo systemctl daemon-reload && sudo systemctl enable myapp-api && sudo systemctl restart myapp-api && sudo systemctl status myapp-api --no-pager
sudo cp scripts/server_deploy_backend.sh /opt/apps/myapp/shared/server_deploy_backend.sh && sudo chmod +x /opt/apps/myapp/shared/server_deploy_backend.sh
/opt/apps/myapp/shared/server_deploy_backend.sh
curl -i http://127.0.0.1:5000/health
sudo journalctl -u myapp-api -n 200 --no-pager
