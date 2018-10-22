@echo off
setlocal EnableDelayedExpansion 

:: Requirements
if not exist "C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" (
    echo ERROR: Microsoft.NET framework 3.5 not found. Make sure you have Virtual Studio installed.
    goto _exit
) 
set msbuild="C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"
     
set sevenz=7z
if exist "C:\Program Files\7-Zip\7z.exe" (
    set sevenz="C:\Program Files\7-Zip\7z.exe"
    goto prep
)

if exist "C:\Program Files(x86)\7-Zip\7z.exe" (
    set sevenz="C:\Program Files (x86)\7-Zip\7z.exe"
    goto prep
)  

where 7z >nul 2>nul
if not %errorlevel%==0 (
    echo ERROR: 7zip was not found. Make sure that you have it installed and in the PATH.
    goto _exit
)


:prep
set target=Debug
set target_unsigned=Debug
if %1.==release. goto _if_setrelease
goto _ifj_setrelease
:_if_setrelease
set target=Release
set target_unsigned=Release-Unsigned
:_ifj_setrelease

:: Prepare the build directory
set build_base=build
set build_mtg=MTG-DIST
set "build=%build_base%\%build_mtg%"
set "build_zip=%build_base%\%build_mtg%.zip"

if exist "%build_base%" rmdir /q /s "%build_base%"
mkdir "%build_base%" 2>nul
mkdir "%build%" 2>nul

:: Build
where xbuild >nul 2>nul
if %errorlevel%==0 (
  ::call xbuild
  rem
) else (
  call %msbuild%
)

for /f "tokens=*" %%L in (build-files) do (
  set "line=%%L"
  setlocal enabledelayedexpansion
  echo !line!
  set str=!line:{TARGET}=%target%!
  echo !str!
  set "line=!line:/=\!"
  endlocal
)

rem for /f "tokens=*" %%L in (build-files) do (
rem   set "line=%%L"
rem   set "line=!line:/=\!"
rem   if not "!line:~0,1!"=="#" (
rem     set "file_ex=!line:{TARGET}=%target%!"
rem     set "file=!file_ex:{TARGET-UNSIGNED}=%target_unsigned%!"
rem     for %%f in (!file!) do set target=%%~nxf
rem       rem

rem     echo !line!
rem     set "file_ex=!line:{TARGET}=A!"
rem     echo !file_ex!
rem     call echo %%line%%
rem     echo Copying '!file!' to '%build%/!target!'

rem     for %%i in (!file!) do (
rem       if exist %%~si\nul (
rem         robocopy "!file!" "%build%/!target!" /s /e
rem       ) else (
rem         copy "!file!" "%build%/!target!"
rem       )
rem     )
rem   )
rem )

:: Zipping it all up
pushd "%build%"
%sevenz% a MTG-DIST.zip *
popd
move "%build%\MTG-DIST.zip" "%build_zip%"

:: The End
:_exit
