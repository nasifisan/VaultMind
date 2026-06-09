# Utility script to start Ollama and pull the phi3 model
Write-Host "Starting Ollama service..." -ForegroundColor Cyan

$ollamaExe = "$env:LOCALAPPDATA\Programs\Ollama\ollama.exe"

if (-not (Test-Path $ollamaExe)) {
    Write-Error "Ollama executable not found at $ollamaExe. Please install it first."
    exit 1
}

# Start the Ollama background process if not already running
if (-not (Get-Process -Name "ollama" -ErrorAction SilentlyContinue)) {
    Start-Process -FilePath $ollamaExe -ArgumentList "serve" -WindowStyle Hidden
    Write-Host "Ollama process launched in the background." -ForegroundColor Cyan
} else {
    Write-Host "Ollama process is already running." -ForegroundColor Green
}

# Wait for Ollama to become responsive on port 11434
$ready = $false
for ($i = 1; $i -le 15; $i++) {
    try {
        $check = Invoke-RestMethod -Uri "http://localhost:11434"
        $ready = $true
        break
    } catch {
        # ignore error
    }
    Write-Host "Waiting for Ollama API to become ready (attempt $i/15)..." -ForegroundColor Yellow
    Start-Sleep -Seconds 2
}

if ($ready) {
    Write-Host "Ollama API is ready! Checking for 'phi3' model..." -ForegroundColor Green
    
    # List models to see if phi3 is already present
    $models = Invoke-RestMethod -Uri "http://localhost:11434/api/tags"
    $hasPhi3 = $false
    if ($models -and $models.models) {
        foreach ($model in $models.models) {
            if ($model.name -like "*phi3*") {
                $hasPhi3 = $true
                break
            }
        }
    }
    
    if ($hasPhi3) {
        Write-Host "Model 'phi3' is already downloaded and ready to use." -ForegroundColor Green
    } else {
        Write-Host "Model 'phi3' not found locally. Pulling 'phi3' model (this may take a few minutes)..." -ForegroundColor Cyan
        & $ollamaExe pull phi3
        Write-Host "Model 'phi3' successfully downloaded and ready!" -ForegroundColor Green
    }
} else {
    Write-Error "Ollama failed to start or was not reachable on port 11434."
    exit 1
}
