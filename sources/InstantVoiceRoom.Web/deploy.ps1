<#
.SYNOPSIS
  Publishes and deploys an ASP.NET Core app to Ubuntu using user+password.

.PARAMETER Server
  The IP or DNS name of your Ubuntu server.

.PARAMETER User
  The SSH user (must have sudo privileges).

.PARAMETER Password
  The SSH password for that user.

.PARAMETER AppName
  A short name for your app (used for paths, service name, etc).

.PARAMETER ProjectPath
  Local path to your .csproj or solution directory.

.PARAMETER Configuration
  Build configuration: Debug or Release (default: Release).

.PARAMETER Runtime
  Target RID, e.g. linux-x64. Leave empty for framework-dependent.
#>
param(
  [Parameter(Mandatory)][string] $Server,
  [Parameter(Mandatory)][string] $User,
  [Parameter(Mandatory)][string] $Password,
  [Parameter(Mandatory)][string] $AppName,
  [Parameter(Mandatory)][string] $ProjectPath,
  [string] $Configuration = "Release",
  [string] $Runtime
)

# 0. Ensure Posh-SSH is available
if (-not (Get-Module -ListAvailable -Name Posh-SSH)) {
  Write-Host "Installing Posh-SSH module..."
  Install-Module -Name Posh-SSH -Force -Scope CurrentUser
}
Import-Module Posh-SSH

# 1. dotnet publish
Write-Host "Publishing $AppName..."
Push-Location $ProjectPath
$publishArgs = @("publish", "-c", $Configuration, "-o", "publish")
if ($Runtime) {
  $publishArgs += "-r"; $publishArgs += $Runtime; $publishArgs += "--self-contained"
}
dotnet @publishArgs
Pop-Location

# 2. Define remote paths
$remoteRoot    = "/var/www/$AppName"
$remotePublish = "$remoteRoot/publish"
$servicePath   = "/etc/systemd/system/$AppName.service"
$nginxAvail    = "/etc/nginx/sites-available/$AppName"
$nginxEnable   = "/etc/nginx/sites-enabled/$AppName"

# 3. Create SSH session
$securePass = ConvertTo-SecureString $Password -AsPlainText -Force
$cred       = New-Object System.Management.Automation.PSCredential ($User, $securePass)
$sess       = New-SSHSession -ComputerName $Server -Credential $cred -AcceptKey

# 4. Create remote dirs + permissions
Write-Host "Creating remote folders..."
Invoke-SSHCommand -SessionId $sess.SessionId -Command @"
sudo mkdir -p $remotePublish
sudo chown -R $User:$User $remoteRoot
"@

# 5. Upload the published files
Write-Host "Uploading files..."
New-SFTPSession -ComputerName $Server -Credential $cred -AcceptKey -OutVariable sftp
Set-SFTPDirectory -SessionId $sftp.SessionId -Path $remotePublish -Force
Get-ChildItem -Path "$ProjectPath/publish" -Recurse |
  ForEach-Object {
    $relative = $_.FullName.Substring("$ProjectPath/publish".Length + 1)
    $target   = Join-Path $remotePublish $relative
    if ($_.PSIsContainer) {
      Set-SFTPDirectory -SessionId $sftp.SessionId -Path $target -Force
    } else {
      Set-SFTPFile -SessionId $sftp.SessionId -LocalFile $_.FullName -RemotePath $target
    }
  }
Remove-SFTPSession -SessionId $sftp.SessionId

# 6. Deploy systemd service
$serviceDef = @"
[Unit]
Description=$AppName service
After=network.target

[Service]
WorkingDirectory=$remotePublish
ExecStart=/usr/bin/dotnet $remotePublish/$AppName.dll
Restart=always
User=$User
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
"@

Write-Host "Writing systemd service..."
# use a heredoc via stdin
$bytes = [Text.Encoding]::UTF8.GetBytes($serviceDef)
Invoke-SSHCommand -SessionId $sess.SessionId -Command "sudo tee $servicePath > /dev/null" -InputStream ([System.IO.MemoryStream]::new($bytes))

# 7. Reload & start the service
Write-Host "Enabling & restarting service..."
Invoke-SSHCommand -SessionId $sess.SessionId -Command @"
sudo systemctl daemon-reload
sudo systemctl enable $AppName
sudo systemctl restart $AppName
"@

# 8. (Optional) Nginx reverse proxy
$nginxConf = @"
server {
    listen 80;
    server_name  YOUR.DOMAIN.OR.IP;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade \$http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header   X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto \$scheme;
    }
}
"@

Write-Host "Deploying nginx config..."
$bytes = [Text.Encoding]::UTF8.GetBytes($nginxConf)
Invoke-SSHCommand -SessionId $sess.SessionId -Command "sudo tee $nginxAvail > /dev/null" -InputStream ([System.IO.MemoryStream]::new($bytes))

Invoke-SSHCommand -SessionId $sess.SessionId -Command @"
sudo ln -sf $nginxAvail $nginxEnable
sudo nginx -t
sudo systemctl reload nginx
"@

# 9. Cleanup
Remove-SSHSession -SessionId $sess.SessionId

Write-Host "`n✅ Deployment of $AppName complete!"
