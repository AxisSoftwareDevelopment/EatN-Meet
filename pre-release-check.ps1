# Pre-Release Verification Script for Spots App
# Run this BEFORE building release version

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "🚀 Spots App - Pre-Release Checklist" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$releaseKeystorePath = "C:\Repos\Spots\release.keystore"
$allChecksPassed = $true

# Check 1: Release Keystore Exists
Write-Host "Checking for release keystore..." -NoNewline
if (Test-Path $releaseKeystorePath) {
    Write-Host " ✅ Found" -ForegroundColor Green
} else {
    Write-Host " ❌ NOT FOUND" -ForegroundColor Red
    Write-Host "`n⚠️  WARNING: Release keystore not found!" -ForegroundColor Yellow
    Write-Host "   Location: $releaseKeystorePath" -ForegroundColor Yellow
    Write-Host "`n   Create it with:" -ForegroundColor White
    Write-Host '   keytool -genkey -v -keystore "C:\Repos\Spots\release.keystore" -alias spots_release -keyalg RSA -keysize 2048 -validity 10000' -ForegroundColor Cyan
    $allChecksPassed = $false
}

# Check 2: Keystore in .gitignore
Write-Host "Checking .gitignore..." -NoNewline
$gitignoreContent = Get-Content ".gitignore" -Raw
if ($gitignoreContent -match "\*\.keystore") {
    Write-Host " ✅ Keystore files ignored" -ForegroundColor Green
} else {
    Write-Host " ⚠️  Warning: Keystore may not be in .gitignore" -ForegroundColor Yellow
}

# Check 3: Build Configuration
Write-Host "`n📋 Pre-Release Checklist:" -ForegroundColor Yellow
Write-Host "   [ ] Updated version in Spots.csproj?" -ForegroundColor White
Write-Host "   [ ] Added release SHA-1 to Firebase?" -ForegroundColor White
Write-Host "   [ ] Added release SHA-1 to Google Maps API?" -ForegroundColor White
Write-Host "   [ ] Backed up release keystore (3 locations)?" -ForegroundColor White
Write-Host "   [ ] Tested release build on physical device?" -ForegroundColor White
Write-Host "   [ ] Configured signing in Visual Studio?" -ForegroundColor White

# Check 4: Keystore SHA-1 (if exists)
if (Test-Path $releaseKeystorePath) {
    Write-Host "`n🔑 Get Release SHA-1:" -ForegroundColor Cyan
    Write-Host "   Run this to get your SHA-1 for Firebase/Google Cloud:" -ForegroundColor White
    Write-Host '   keytool -list -v -keystore "C:\Repos\Spots\release.keystore" -alias spots_release' -ForegroundColor Cyan
}

# Final message
Write-Host "`n========================================" -ForegroundColor Cyan
if ($allChecksPassed) {
    Write-Host "✅ Ready to proceed with release build" -ForegroundColor Green
    Write-Host "   Review the checklist above!" -ForegroundColor White
} else {
    Write-Host "❌ Issues found - fix before release" -ForegroundColor Red
}
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "📄 See RELEASE_CHECKLIST.md for full details`n" -ForegroundColor Yellow

# Prompt user
$response = Read-Host "Do you want to continue? (Y/N)"
if ($response -ne "Y" -and $response -ne "y") {
    Write-Host "`n🛑 Release process cancelled" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ Proceeding with release...`n" -ForegroundColor Green
