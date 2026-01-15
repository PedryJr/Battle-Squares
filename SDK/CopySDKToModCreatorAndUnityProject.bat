@echo off

REM Source DLL
set SRC="C:\Users\Pedry\Documents\GitHub\Battle-Squares\Assets\Plugins\BSMOD\Release\netstandard2.1\BattleSquaresSDK.dll"

REM Destination directory (NOT the file)
set DST="C:\Users\Pedry\source\repos\BattleSquaresModBuilder\sdk"

REM Create destination folder if it doesn't exist
if not exist %DST% (
    mkdir %DST%
)

REM Copy file (overwrite without prompt)
copy /Y %SRC% %DST%

REM Optional logging
echo BattleSquaresSDK.dll copied to %DST%
