# Utility script to start Docker Desktop and launch local MongoDB container
Write-Host "Starting Docker Desktop..." -ForegroundColor Cyan

# Start Docker Desktop if not already running
if (-not (Get-Process -Name "Docker Desktop" -ErrorAction SilentlyContinue)) {
    Start-Process -FilePath "C:\Program Files\Docker\Docker\Docker Desktop.exe"
}

$ready = $false
for ($i = 1; $i -le 30; $i++) {
    try {
        $check = docker ps 2>$null
        if ($LASTEXITCODE -eq 0) {
            $ready = $true
            break
        }
    } catch {
        # ignore error
    }
    Write-Host "Waiting for Docker daemon to become ready (attempt $i/30)..." -ForegroundColor Yellow
    Start-Sleep -Seconds 3
}

if ($ready) {
    Write-Host "Docker daemon is ready!" -ForegroundColor Green

    # Define build context
    $projectRoot = Resolve-Path "$PSScriptRoot/.."
    $dockerContext = Join-Path $projectRoot "local/mongodb"

    Write-Host "Building local MongoDB Docker image from $dockerContext..." -ForegroundColor Cyan
    docker build -t vaultmind-mongodb-local $dockerContext
    
    # Check if container already exists
    $existing = docker ps -a --filter "name=mongodb" --format "{{.Names}}"
    if ($existing -eq "mongodb") {
        # Check if it is running
        $running = docker ps --filter "name=mongodb" --format "{{.Names}}"
        if ($running -eq "mongodb") {
            Write-Host "MongoDB container 'mongodb' is already running." -ForegroundColor Green
        } else {
            Write-Host "MongoDB container exists but is stopped. Starting container..." -ForegroundColor Cyan
            docker start mongodb
            Write-Host "MongoDB container started successfully!" -ForegroundColor Green
        }
    } else {
        Write-Host "MongoDB container does not exist. Creating and starting container..." -ForegroundColor Cyan
        docker run -d --name mongodb -p 27017:27017 -v vaultmind_mongo_data:/data/db vaultmind-mongodb-local
        Write-Host "MongoDB container created and started successfully!" -ForegroundColor Green
    }
} else {
    Write-Error "Docker Desktop failed to start within the timeout period. Please start it manually and try again."
    exit 1
}
