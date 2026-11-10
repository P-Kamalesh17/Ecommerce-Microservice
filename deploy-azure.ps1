# Deploys the 5 APIs + gateway to Azure Container Apps using the existing Azure SQL databases.
# Run from the solution root:  .\deploy-azure.ps1
# Reads secrets from .env (never prints them). Re-runnable: create-or-update.
$ErrorActionPreference = "Stop"
$az  = "C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd"
$rg  = "rg-lab-kamalesh-training-20260921"
$loc = "centralindia"
$acr = "ecommercekamalesh26"
$env = "ecommerce-env"
$sqlServer = "ecommercemicroservice"
$frontendOrigins = ""   # e.g. "https://my-frontend.azurestaticapps.net" (comma separated); localhost:5173/3000 always allowed

# --- load .env ---
$cfg = @{}
Get-Content .env | Where-Object { $_ -match '^\s*[^#=\s][^=]*=' } | ForEach-Object {
  $k, $v = $_ -split '=', 2; $cfg[$k.Trim()] = $v.Trim()
}
foreach ($k in 'SQL_AUTH','SQL_PRODUCT','SQL_CART','SQL_ORDER','SQL_PAYMENT','JWT_KEY','JWT_ENCRYPTION_KEY','INTERNAL_API_KEY') {
  if (-not $cfg[$k]) { throw "Missing $k in .env" }
}

# --- infra ---
& $az acr create -g $rg -n $acr --sku Basic --admin-enabled true -l $loc -o none
& $az containerapp env create -g $rg -n $env -l $loc -o none
# let Azure services (Container Apps) reach Azure SQL
& $az sql server firewall-rule create -g $rg -s $sqlServer -n AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 -o none

# --- build images in ACR (no local push needed) ---
$services = @{ authapi='AuthAPI'; productapi='ProductAPI'; cartapi='CartAPI'; orderapi='OrderAPI'; paymentapi='PaymentAPI'; gateway='gateway' }
foreach ($name in $services.Keys) {
  $dir = $services[$name]
  & $az acr build -r $acr -t "ecommerce/${name}:latest" -f "$dir/Dockerfile" . -o none
}

$server = & $az acr show -n $acr --query loginServer -o tsv
$acrUser = & $az acr credential show -n $acr --query username -o tsv
$acrPass = & $az acr credential show -n $acr --query "passwords[0].value" -o tsv

function Deploy($name, $external, $secrets, $envVars) {
  $common = @("ASPNETCORE_ENVIRONMENT=Docker","Jwt__Issuer=ECommerce.AuthAPI","Jwt__Audience=ECommerce.Services",
              "Jwt__Key=secretref:jwtkey","Jwt__EncryptionKey=secretref:jwtenc")
  $allSecrets = @("jwtkey=$($cfg.JWT_KEY)","jwtenc=$($cfg.JWT_ENCRYPTION_KEY)") + $secrets
  $ingress = if ($external) { "external" } else { "internal" }
  & $az containerapp create -g $rg -n $name --environment $env `
    --image "$server/ecommerce/${name}:latest" `
    --registry-server $server --registry-username $acrUser --registry-password $acrPass `
    --target-port 8080 --ingress $ingress --min-replicas 1 --max-replicas 1 `
    --secrets @allSecrets --env-vars @($common + $envVars) -o none
}

Deploy authapi    $false @("sql=$($cfg.SQL_AUTH)") @("ConnectionStrings__DefaultConnectionString=secretref:sql")
Deploy productapi $false @("sql=$($cfg.SQL_PRODUCT)","apikey=$($cfg.INTERNAL_API_KEY)") @("ConnectionStrings__DefaultConnectionString=secretref:sql","Internal__ApiKey=secretref:apikey")
Deploy cartapi    $false @("sql=$($cfg.SQL_CART)") @("ConnectionStrings__DefaultConnectionString=secretref:sql","ProductApi__BaseUrl=http://productapi/")
Deploy paymentapi $false @("sql=$($cfg.SQL_PAYMENT)") @("ConnectionStrings__DefaultConnectionString=secretref:sql","OrderApi__BaseUrl=http://orderapi/")
Deploy orderapi   $false @("sql=$($cfg.SQL_ORDER)","apikey=$($cfg.INTERNAL_API_KEY)") @(
  "ConnectionStrings__DefaultConnectionString=secretref:sql","Internal__ApiKey=secretref:apikey",
  "ProductApi__BaseUrl=http://productapi/","CartApi__BaseUrl=http://cartapi/",
  "PaymentApi__BaseUrl=http://paymentapi/","AuthApi__BaseUrl=http://authapi/")
# ocelot.Azure.json = same routes as ocelot.Docker.json but port 80 (Container Apps internal ingress)
Deploy gateway    $true  @() @("OCELOT_FILE=ocelot.Azure.json","Cors__Origins=$frontendOrigins")

$fqdn = & $az containerapp show -g $rg -n gateway --query properties.configuration.ingress.fqdn -o tsv
Write-Host ""
Write-Host "FRONTEND API BASE URL:  https://$fqdn" -ForegroundColor Green
Write-Host "Example: https://$fqdn/api/Product/get-all-products"
