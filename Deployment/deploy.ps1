$ErrorActionPreference = "Stop"

if (!$Env:BRANCH) {
    Write-Error "BRANCH variable not set"
    exit 1
}
if (!$Env:DEPLOY_NAME) {
    Write-Error "DEPLOY_NAME variable not set"
    exit 1
}
if (!$Env:DEPLOY_ARTIFACT_PATH) {
    Write-Error "DEPLOY_ARTIFACT_PATH variable not set"
    exit 1
}
if (!$Env:DEPLOY_SVN_URL) {
    Write-Error "DEPLOY_SVN_URL variable not set"
    exit 1
}
if (!$Env:DEPLOY_SVN_USER) {
    Write-Error "DEPLOY_SVN_USER variable not set"
    exit 1
}
if (!$Env:DEPLOY_SVN_PASSWORD) {
    Write-Error "DEPLOY_SVN_PASSWORD variable not set"
    exit 1
}
if (!$Env:PAHKAT_EXEC_PATH) {
    Write-Error "PAHKAT_EXEC_PATH variable not set"
    exit 1
}

Try
{
    # determine the current version of the pahkat client
    ..\Pahkat\bin\x86\Release\DivvunInstaller.exe -semver .\app-semver | Out-Null
    $deployVersion = Get-Content .\app-semver
    $deployAs = "divvun-installer-$Env:BRANCH-$deployVersion-$((Get-Date).ToString('yyyyMMdd')).exe"

    # checkout the svn repo to use for deployment
    svn checkout --depth immediates $Env:DEPLOY_SVN_URL
    if ($LastExitCode -ne 0) { throw }
    Set-Location $Env:DEPLOY_NAME
    svn up packages --set-depth=infinity
    if ($LastExitCode -ne 0) { throw }
    svn up virtuals --set-depth=infinity
    if ($LastExitCode -ne 0) { throw }
    svn up index.json
    if ($LastExitCode -ne 0) { throw }

    # determine if the application binary has already 
    # been added to version control, then do add or up
    $ErrorActionPreference = "Continue"
    svn info .\artifacts\$deployAs
    $ErrorActionPreference = "Stop"

    if ($LastExitCode -eq 0) {
        svn up .\artifacts\$deployAs
        if ($LastExitCode -ne 0) { throw }
        Copy-Item $Env:DEPLOY_ARTIFACT_PATH .\artifacts\$deployAs
    }
    else {
        Copy-Item $Env:DEPLOY_ARTIFACT_PATH .\artifacts\$deployAs
        svn add .\artifacts\$deployAs
        if ($LastExitCode -ne 0) { throw }
    }

    # update the pahkat package description
    $fileSize = (Get-Item ".\artifacts\$deployAs").length
    $template = Get-Content ..\pahkat-template.json -Raw
    $template = $template -replace "DEPLOY_VERSION", $deployVersion
    $template = $template -replace "DEPLOY_SVN_URL", $Env:DEPLOY_SVN_URL
    $template = $template -replace "DEPLOY_FILE_NAME", $deployAs
    $template = $template -replace "DEPLOY_FILE_SIZE", $fileSize

    # write updated package description to repo
    $utf8 = New-Object System.Text.UTF8Encoding $false
    Set-Content -Value $utf8.GetBytes($template) -Encoding Byte -Path ".\packages\divvun-installer-windows\index.json"

    # re-index using pahkat
    $ErrorActionPreference = "Continue"
    Invoke-Expression "$Env:PAHKAT_EXEC_PATH repo index"
    $ErrorActionPreference = "Stop"

    if ($LastExitCode -ne 0) { throw }
    svn status
    if ($LastExitCode -ne 0) { throw }

    if ($Env:DEPLOY_SVN_COMMIT) {
        svn commit -m "Automated Deploy to $Env:DEPLOY_NAME: $deployAs" --username=$Env:DEPLOY_SVN_USER --password=$Env:DEPLOY_SVN_PASSWORD
        if ($LastExitCode -ne 0) { throw }
    }
    else {
        Write-Host "Warning: DEPLOY_SVN_COMMIT not set, ie. changes to repo will not be committed"
    }

    Set-Location ..
}
Catch [Exception]
{
    Write-Error $_.Exception.Message
    exit 1
}
