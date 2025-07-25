# PRD: PLC Hardware Abstraction via Web Interface

## Overview
The goal of this project is to refactor specific Unity scripts located in the `Assets/BGT/` directory. The core task is to remove the dependency on the `ActUtlType64.dll` library, which facilitates direct communication with a Mitsubishi PLC. This hardware-dependent implementation will be replaced by a web-based control mechanism using WebSockets, making the simulation hardware-independent and controllable via a web interface.

**Key Constraint: Only scripts within the `Assets/BGT/` folder are to be modified.** All other parts of the project, including the Python web server, are considered out of scope for modification, except for what is minimally necessary to support the new control scheme.

## Core Features
- **Hardware Decoupling:** Abstract the control logic in `Manager.cs` (and related scripts) away from the `ActUtlType64` library.
- **WebSocket Client Integration:** Implement a WebSocket client within `Manager.cs` to connect to the existing Python server and receive control commands.
- **Web-based Command Processing:** Translate incoming WebSocket messages (e.g., JSON commands) into actions that were previously triggered by PLC signals (e.g., activating `PipeHolders` or `ZLiftTrigger`).

## Technical Architecture
- **System Components:**
    - **Simulation Client (In Scope):** Unity C# scripts within `Assets/BGT/`, specifically `Manager.cs`.
    - **Backend (Out of Scope):** The existing `PythonWebServer/app.py` is assumed to be the WebSocket server. It will provide the commands.
    - **Frontend (Out of Scope):** The web interface (`index.html`) is assumed to provide the user controls.
- **API / Data Models:**
    - The Unity client will listen for a `dispatch_command` event from the WebSocket server.
    - The data payload will be a JSON object: `{ "command": "[CommandName]", "isActive": [true/false] }`.
    - Example: `{ "command": "PipeHoldersCW", "isActive": true }` will replace the signal previously read from PLC device `Y2`.

## Development Roadmap (MVP)
1.  **Refactor `Manager.cs`:**
    - Remove all `using ActUtlType64Lib;` statements and any code that references `mxComponent` or other types from this library.
    - Delete the PLC connection logic in the `Start()` method.
    - Remove the `ReadDevice()` method and its call from `Update()`.
2.  **Implement WebSocket Client in `Manager.cs`:**
    - Add a WebSocket client library (like `websocket-sharp`, which is already in `Assets/Plugins`).
    - In `Start()`, initialize and connect the WebSocket client to the server endpoint (`ws://127.0.0.1:5000/socket.io/...`).
    - Implement `OnMessage` event handler to receive commands from the server.
3.  **Implement Command Processing Logic in `Manager.cs`:**
    - Create a thread-safe queue to pass commands from the WebSocket's `OnMessage` thread to Unity's main `Update` thread.
    - In `Update()`, dequeue and process commands.
    - Create a `ProcessCommand(CommandData cmd)` method that uses a `switch` statement on `cmd.command` to call the appropriate functions (e.g., `pipeHolders.ActivatePipeHoldersCW()`, `zLift.ActivateZLiftUp()`). This replaces the logic that was previously in `ReadDevice()`.
4.  **Cleanup:**
    - Remove any other scripts in `Assets/BGT/` that are now obsolete after removing the PLC dependency (e.g., `ActUtlManager.cs` if it's no longer needed).

## Logical Dependency Chain
1.  **Isolate and Remove:** The first step is to completely remove the `ActUtlType64Lib` code from `Manager.cs` to break the hardware dependency.
2.  **Connect:** Implement the WebSocket connection logic.
3.  **Translate:** Re-implement the control logic to respond to WebSocket messages instead of PLC signals.

## Risks and Mitigations
- **Risk:** The existing Python server or web UI might not perfectly align with the new control scheme.
- **Mitigation:** The scope is strictly limited to the Unity client. We will assume the server sends the correct commands. If adjustments are needed outside of `Assets/BGT/`, they will be noted but not implemented as part of this core task.
