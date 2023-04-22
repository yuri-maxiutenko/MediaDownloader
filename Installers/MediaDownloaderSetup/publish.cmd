
setlocal ENABLEDELAYEDEXPANSION

set SOLUTION_DIR=%~1
set PROJECT_DIR=%~2
set CONFIGURATION_NAME=%~3
set PLATFORM_NAME=%~4

set PUBLISH_PROFILE="FolderProfile"
if "%CONFIGURATION_NAME%"=="Debug" (
    echo DEBUG CONFIG!!!
    set PUBLISH_PROFILE="FolderProfileDebug"
    )

set MEDIA_DOWNLOADER_DIR=%SOLUTION_DIR%MediaDownloader
set THIRD_PARTY_DIR=%SOLUTION_DIR%third-party
set PUBLISH_DIR=%SOLUTION_DIR%MediaDownloader\bin\%CONFIGURATION_NAME%\net7.0-windows\win-%PLATFORM_NAME%\publish

set JSON_FILES_PATH=%MEDIA_DOWNLOADER_DIR%\*.json
set CONFIG_FILES_PATH=%MEDIA_DOWNLOADER_DIR%\*.config

set HEAT_EXECUTABLE_PATH=C:\Program Files (x86)\WiX Toolset v3.11\bin\heat.exe
set HEAT_FITLERS_PATH=%PROJECT_DIR%HeatFilters.xslt
set WIX_COMPONENTS_FILE_PATH=%PROJECT_DIR%Components.wxs
set WIX_ROOT_DIRECTORY=INSTALLDIR
set WIX_COMPONENT_GROUP=ProductComponents
set WIX_SOURCE_DIR_VARIABLE=var.SourceDir

cd "%MEDIA_DOWNLOADER_DIR%"

dotnet publish /p:Configuration=%CONFIGURATION_NAME% /p:PublishProfile=%PUBLISH_PROFILE%

xcopy "%THIRD_PARTY_DIR%" "%PUBLISH_DIR%\" /E /Y
copy "%JSON_FILES_PATH%" "%PUBLISH_DIR%\" /Y
copy "%CONFIG_FILES_PATH%" "%PUBLISH_DIR%\" /Y
"%HEAT_EXECUTABLE_PATH%" dir "%PUBLISH_DIR%" -gg -out "%WIX_COMPONENTS_FILE_PATH%" -srd -scom -sfrag -sreg -dr %WIX_ROOT_DIRECTORY% -cg %WIX_COMPONENT_GROUP% -var %WIX_SOURCE_DIR_VARIABLE% -t "%HEAT_FITLERS_PATH%"