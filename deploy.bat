@echo off

SET ROOT=%~d0%~p0%
SET BINARYDIR="%ROOT%build_output"
SET DEPLOYDIR="%ROOT%ReleaseBinaries"
SET LIB="%ROOT%lib"

IF EXIST %BINARYDIR% (
  rmdir /Q /S %BINARYDIR%
)
mkdir %BINARYDIR%

IF EXIST %DEPLOYDIR% (
  rmdir /Q /S %DEPLOYDIR%
)
mkdir %DEPLOYDIR%

mkdir %DEPLOYDIR%\f#-files
mkdir %DEPLOYDIR%\f#-files\preserved-data
mkdir %DEPLOYDIR%\f#-files\preserved-data\create
mkdir %DEPLOYDIR%\f#-files\preserved-data\new
mkdir %DEPLOYDIR%\f#-files\preserved-data\script-template
mkdir %DEPLOYDIR%\f#-files\preserved-data\rscript-template

%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe %ROOT%src\OpenIDE.F-Sharp.sln  /property:OutDir=%BINARYDIR%\;Configuration=Release /target:rebuild

copy %ROOT%\resources\f#.oilnk %DEPLOYDIR%\f#.oilnk
copy %ROOT%\resources\package.json %DEPLOYDIR%\f#-files\package.json
copy %BINARYDIR%\f#.exe %DEPLOYDIR%\f#-files\f#.exe
xcopy /S /I /E %ROOT%\resources\templates\script %DEPLOYDIR%\f#-files\preserved-data\script-template
copy %BINARYDIR%\build.exe %DEPLOYDIR%\f#-files\preserved-data\script-template
xcopy /S /I /E %ROOT%\resources\templates\rscript %DEPLOYDIR%\f#-files\preserved-data\rscript-template
copy %BINARYDIR%\build.exe %DEPLOYDIR%\f#-files\preserved-data\rscript-template

xcopy /S /I /E %ROOT%\resources\create %DEPLOYDIR%\f#-files\preserved-data\create
xcopy /S /I /E %ROOT%\resources\new %DEPLOYDIR%\f#-files\preserved-data\new

REM Building packages
ECHO Building packages

oi package build "ReleaseBinaries\f#" %DEPLOYDIR%

