@echo off
setlocal enabledelayedexpansion

REM UTF-8 인코딩 활성화
chcp 65001
echo [LOG] Character encoding set to UTF-8

REM 경로 설정
set "VENV_DIR=C:\venvs\spleeter_env"
set "FFMPEG_DIR=C:\venvs\ffmpeg"
set "FFMPEG_BIN=%FFMPEG_DIR%\bin"
set "FFMPEG_EXE=%FFMPEG_BIN%\ffmpeg.exe"
set "FFMPEG_ZIP_URL=https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
set "FFMPEG_ZIP=C:\venvs\ffmpeg.zip"

REM 환경 변수 강제 설정
set PATH=%PATH%;C:\venvs\ffmpeg\bin

REM C:\venvs 폴더가 없으면 생성
if not exist "C:\venvs" (
    echo [LOG] Creating C:\venvs directory...
    mkdir "C:\venvs"
) else (
    echo [LOG] C:\venvs directory already exists.
    exit /b 0
)

REM FFmpeg 다운로드 및 설치
if not exist "%FFMPEG_EXE%" (
    echo [LOG] FFmpeg not found. Downloading...
    
    REM 기존 FFmpeg 압축 파일 삭제 후 재다운로드
    if exist "%FFMPEG_ZIP%" del /f /q "%FFMPEG_ZIP%"
    
    powershell -Command "Invoke-WebRequest -Uri '%FFMPEG_ZIP_URL%' -OutFile '%FFMPEG_ZIP%'"
    
    if not exist "%FFMPEG_ZIP%" (
        echo [ERROR] FFmpeg download failed. Exiting...
        exit /b 1
    )

    REM 압축 해제
    echo [LOG] Extracting FFmpeg...
    powershell -Command "Expand-Archive -Path '%FFMPEG_ZIP%' -DestinationPath 'C:\venvs\' -Force"

    REM 압축 해제된 폴더 찾기
    set "FFMPEG_TEMP_DIR="
    for /d %%i in ("C:\venvs\ffmpeg-*") do (
        echo [LOG] Found extracted folder: %%i
        set "FFMPEG_TEMP_DIR=%%i"
    )

    if not defined FFMPEG_TEMP_DIR (
        echo [ERROR] FFmpeg extraction failed. Exiting...
        exit /b 1
    )

    REM 압축 해제된 폴더 안의 파일들을 추가 생성한 ffmpeg 폴더로 이동.
    echo [LOG] Moving FFmpeg files...
    robocopy "!FFMPEG_TEMP_DIR!" "%FFMPEG_DIR%" /E /MOVE /NFL /NDL
    rmdir /s /q "!FFMPEG_TEMP_DIR!"

    REM 임시 파일 삭제
    del "%FFMPEG_ZIP%"
    echo [LOG] FFmpeg installation completed.
) else (
    echo [LOG] FFmpeg is already installed.
)

REM FFmpeg 환경 변수 추가
echo [LOG] Adding FFmpeg to system PATH...
echo %PATH% | findstr /I /C:"%FFMPEG_BIN%" >nul
if %errorlevel% neq 0 (
    setx /M PATH "%PATH%;%FFMPEG_BIN%"
    echo [LOG] FFmpeg path added successfully!
) else (
    echo [LOG] FFmpeg path is already set.
)

REM 가상 환경 확인 및 생성
if not exist "%VENV_DIR%" (
    echo [LOG] Creating virtual environment in %VENV_DIR%...
    python -m venv "%VENV_DIR%"
) else (
    echo [LOG] Virtual environment already exists.
)

"%VENV_DIR%\Scripts\python.exe" -c "import subprocess; subprocess.run(['ffmpeg', '-version'])"
if %errorlevel% neq 0 (
    echo [ERROR] Python subprocess cannot find ffmpeg!
    exit /b 1
)

REM pip 최신화
echo [LOG] Upgrading pip...
"%VENV_DIR%\Scripts\python.exe" -m pip install --upgrade pip


REM ffmpeg-python 설치 후 확인
echo [LOG] Installing required packages...
"%VENV_DIR%\Scripts\python.exe" -m pip install spleeter==2.4.0 librosa==0.10.2.post1 numpy==1.23.5 audioread==3.0.1 ffmpeg-python==0.2.0

REM 입력 인자 확인
echo [LOG] Received argument: %1

REM UNITY_MODE 값 확인 및 처리
set "UNITY_MODE="
set "ARGUMENTS=%*"
echo [LOG] Full arguments: %ARGUMENTS%

REM UNITY_MODE=1 이 포함되어 있는지 체크
echo %ARGUMENTS% | findstr /I "UNITY_MODE=1" >nul
if %errorlevel%==0 (
    set "UNITY_MODE=1"
)


REM UNITY_MODE 값 확인 후 처리
if "%UNITY_MODE%"=="1" (
    echo [LOG] Running in UNITY_MODE. Skipping pause.
    exit /b 0
)

echo [LOG] Running in normal mode.
pause

exit /b 0