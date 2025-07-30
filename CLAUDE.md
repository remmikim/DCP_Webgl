# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity-based drone autonomous mission and web control system project that combines Unity simulation with a Python Flask web server. The system enables real-time control and monitoring of drone operations through a web interface, with PLC integration for industrial automation.

## Key Architecture Components

### Unity Project Structure
- **Assets/BGT/**: Main project assets including models, scripts, and scenes
  - **PLC/**: PLC communication scripts (ActUtlManager.cs, Manager1.cs, etc.)
  - **Script/**: Core Unity scripts for drone simulation and web integration
  - **Scenes/**: Unity scenes including SampleScene.unity and Station.unity
- **Assets/JWK/**: Drone-specific components
  - **Scripts/Drone/**: Core drone controller, events, and data structures
  - **Scripts/DropSystem/**: Fire extinguisher dropping system
  - **Scripts/FireManager/**: Fire simulation and management
- **Assets/KWH/**: Station models and scenes

### Python Web Server
- **PythonWebServer/**: Flask-SocketIO based web server
  - **app.py**: Main server with dual drone support (main/test)
  - **templates/**: HTML templates for web interface
  - **static/**: CSS and JavaScript for frontend

### Key Integration Points
- **WebManager.cs**: Unity-Web bridge using WebGL JavaScript interop
- **WebDataBridge.jslib**: JavaScript library for Unity-Web communication
- **SocketIO**: Real-time bidirectional communication between Unity and web clients

## Development Commands

### Python Web Server Setup
```powershell
# Navigate to web server directory
cd PythonWebServer

# Create virtual environment (first time only)
python -m venv venv

# Activate virtual environment
. .\venv\Scripts\activate

# Install dependencies
pip install Flask Flask-SocketIO eventlet

# Run server
python app.py
```

### Unity Development
- Open project in Unity Editor
- Main scenes: `Assets/BGT/Scenes/SampleScene.unity` for drone simulation
- Station scene: `Assets/KWH/Scene/Station_PLC_v3.unity` for PLC integration
- Build for WebGL: Use Unity Build Settings with WebGL platform

### Testing
- Web interface: Access `http://127.0.0.1:5000` for main control
- Test interface: Access `http://127.0.0.1:5000/test` for coordinate testing
- Unity WebGL build: Deploy to web server for full integration testing

## Development Workflow

### Git Branch Strategy
- **main**: Primary stable branch
- **BGT**: Active development branch
- Individual developer branches (JWK, KWH, etc.)

### Branch Workflow
1. Always sync with main branch before starting work
2. Work in personal branches
3. Merge main into personal branch before pushing
4. Create Pull Requests to merge into main

## Technical Details

### Communication Flow
1. **Unity ↔ Python**: SocketIO with JSON data exchange
2. **Python ↔ Web**: Flask-SocketIO for real-time updates
3. **Unity ↔ PLC**: ActUtl library for Mitsubishi PLC communication
4. **Unity ↔ Web (Direct)**: WebGL JavaScript interop for Firebase integration

### Device Mapping (PLC Integration)
- Y0-Y1: Roof control (Front/Back)
- Y2-Y3: Pipe holders (CW/CCW)
- Y4-Y5: Z-Lift (Up/Down)
- Y6-Y7: Base2Down (Down/Up)
- Y8-Y9: X-Gantry (Right/Left)
- YA-YB: Carriage frame rotation (CW/CCW)
- YC-YD: Fork movement (Right/Left)
- Y10-Y11: Fork front/back movement

### Data Structures
- **DroneController.cs**: Main drone physics and control
- **MissionData.cs**: Mission parameters and waypoints
- **DataStructures.cs**: Shared data models between Unity and web

## WebManager.cs - ActUtlType64 라이브러리 대체 방안

### 현재 구조 분석
WebManager.cs는 ActUtlType64 라이브러리를 대체하여 웹에서 받은 JSON 데이터를 Unity에서 처리할 수 있도록 설계되었습니다.

### ActUtlManager.cs vs WebManager.cs 비교
**ActUtlManager.cs (기존):**
- ActUtlType64Lib 사용하여 직접 PLC 통신
- Y 디바이스 읽기: `ReadDeviceBlock("Y0", blockCnt, out data[0])`
- X 디바이스 쓰기: `SetDevice(parts[0], value)`
- 데이터 형식: `"Y0YF:{data[0]}"`, `"Y10Y1F:{data[1]}"`

**WebManager.cs (대체):**
- 웹에서 JSON 데이터 수신
- Firebase/웹 인터페이스를 통한 간접 PLC 통신
- JSON 파싱: `{"devices.Y0": {"value": true}, "devices.Y1": {"value": false}}`
- 디바이스 매핑: Y0-Y11 → Unity 컴포넌트 제어

### JSON 데이터 구조
```json
{
    "devices.Y0": { "value": true },   // Roof Front
    "devices.Y1": { "value": false },  // Roof Back
    "devices.Y2": { "value": true },   // PipeHolders CW
    "devices.Y3": { "value": false },  // PipeHolders CCW
    "devices.Y4": { "value": true },   // ZLift Up
    "devices.Y5": { "value": false },  // ZLift Down
    "devices.Y6": { "value": true },   // Base2Down (Down)
    "devices.Y7": { "value": false },  // Base2Down (Up)
    "devices.Y8": { "value": true },   // XGantry Right
    "devices.Y9": { "value": false },  // XGantry Left
    "devices.YA": { "value": true },   // CarriageFrame CW
    "devices.YB": { "value": false },  // CarriageFrame CCW
    "devices.YC": { "value": true },   // Fork Right
    "devices.YD": { "value": false },  // Fork Left
    "devices.Y10": { "value": true },  // Fork Front
    "devices.Y11": { "value": false }  // Fork Back
}
```

### 통합 방법
1. **웹 인터페이스 → Firebase → Unity**: WebGL JavaScript interop 사용
2. **실시간 업데이트**: `ReceiveWebData(string jsonData)` 함수로 JSON 처리
3. **디바이스 제어**: `ProcessDeviceCommand(string deviceAddress, bool isActive)`로 Unity 컴포넌트 매핑

### 드론 상태 JSON (참고)
```json
{
    "position": {"x": 0.0, "y": 0.0, "z": 0.0},
    "altitude": 0.0,
    "battery": 100.0,
    "mission_state": "IDLE",
    "payload_type": "extinguisher",
    "bomb_load": 6
}
```

## Important Notes

- WebGL builds require specific JavaScript bridge setup for PLC communication
- PLC integration uses Mitsubishi ActUtl library (Windows only) - **WebManager.cs로 대체 가능**
- Real-time communication depends on stable network connection
- Firebase integration available for cloud data synchronization
- Multi-drone support: Main drone and test drone with separate control interfaces
- **PlcDetail.tsx 파일은 현재 프로젝트에 존재하지 않음** - Unity C# 기반 구조 사용