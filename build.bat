@echo off
echo === DesktopKit.ImageResize ビルド＆パブリッシュ ===
cd %~dp0ImageResize
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ..\publish\
echo.
echo 完了しました。publish\ フォルダにexeがあります。
pause
