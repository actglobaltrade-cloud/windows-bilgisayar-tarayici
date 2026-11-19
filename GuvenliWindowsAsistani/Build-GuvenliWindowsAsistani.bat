@echo off
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo .NET Framework C# compiler not found.
  exit /b 1
)
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /out:"%~dp0GuvenliWindowsAsistani.exe" /reference:System.Windows.Forms.dll /reference:System.Management.dll /reference:System.Web.Extensions.dll "%~dp0GuvenliWindowsAsistani.cs"
if errorlevel 1 exit /b 1
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /out:"%~dp0GuvenliWindowsAsistani-Setup.exe" /reference:System.Windows.Forms.dll /resource:"%~dp0GuvenliWindowsAsistani.exe",GuvenliWindowsAsistaniSetup.GuvenliWindowsAsistani.exe "%~dp0GuvenliWindowsAsistaniSetup.cs"
if errorlevel 1 exit /b 1
echo Created: "%~dp0GuvenliWindowsAsistani-Setup.exe"
