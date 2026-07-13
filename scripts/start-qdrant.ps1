# Utility script to start Docker Desktop and launch local Qdrant container
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
    $dockerContext = Join-Path $projectRoot "local/qdrant"

    Write-Host "Building local Qdrant Docker image from $dockerContext..." -ForegroundColor Cyan
    docker build -t vaultmind-qdrant-local $dockerContext
    
    # Check if container already exists
    $existing = docker ps -a --filter "name=qdrant" --format "{{.Names}}"
    if ($existing -eq "qdrant") {
        # Check if it is running
        $running = docker ps --filter "name=qdrant" --format "{{.Names}}"
        if ($running -eq "qdrant") {
            Write-Host "Qdrant container 'qdrant' is already running." -ForegroundColor Green
        } else {
            Write-Host "Qdrant container exists but is stopped. Starting container..." -ForegroundColor Cyan
            docker start qdrant
            Write-Host "Qdrant container started successfully!" -ForegroundColor Green
        }
    } else {
        Write-Host "Qdrant container does not exist. Creating and starting container..." -ForegroundColor Cyan
        # Expose both 6333 (REST/dashboard) and 6334 (gRPC) ports, and mount a persistent volume
        docker run -d --name qdrant -p 6333:6333 -p 6334:6334 -v vaultmind_qdrant_data:/qdrant/storage vaultmind-qdrant-local
        Write-Host "Qdrant container created and started successfully!" -ForegroundColor Green
    }
    
    Write-Host "Qdrant is running at: HTTP: http://localhost:6333 | Dashboard: http://localhost:6333/dashboard" -ForegroundColor Green
} else {
    Write-Error "Docker Desktop failed to start within the timeout period. Please start it manually and try again."
    exit 1
}
