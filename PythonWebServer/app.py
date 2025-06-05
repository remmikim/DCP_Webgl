from flask import Flask, render_template, request
from flask_socketio import SocketIO, emit, join_room, leave_room
import json

app = Flask(__name__, template_folder='templates', static_folder='static')
app.config['SECRET_KEY'] = 'drone_firefighter_secret_key_!@#' # 실제 환경에서는 더 복잡하게
socketio = SocketIO(app, cors_allowed_origins="*", async_mode='eventlet')

connected_clients = {
    'unity_sid': None,
    'web_clients_room': 'drone_control_web_room'
}

# 현재 드론 상태를 임시로 저장 (실제로는 DB나 더 견고한 방식 사용 가능)
latest_drone_status = {}

@app.route('/')
def index():
    return render_template('index.html')

@socketio.on('connect')
def handle_connect():
    client_type = request.args.get('type')
    print(f"[Server] Client connected: {request.sid}, Type: {client_type}")

    if client_type == 'unity':
        if connected_clients['unity_sid'] and connected_clients['unity_sid'] != request.sid:
            print(f"[Server] Disconnecting old Unity client: {connected_clients['unity_sid']}")
            socketio.emit('server_message', 'Another Unity instance connected. Disconnecting this one.', room=connected_clients['unity_sid'])
            socketio.disconnect(sid=connected_clients['unity_sid'])
        connected_clients['unity_sid'] = request.sid
        join_room(request.sid) # Unity 클라이언트 고유 룸
        print(f"[Server] Unity client registered with SID: {request.sid}")
        emit('server_message', 'Unity client connected to server.', room=request.sid)
    elif client_type == 'web':
        join_room(connected_clients['web_clients_room']) # 웹 클라이언트 공용 룸
        print(f"[Server] Web client {request.sid} joined room '{connected_clients['web_clients_room']}'.")
        emit('server_message', 'Web client connected to server.', room=request.sid)
        if latest_drone_status: # 새로운 웹 클라이언트에게 현재 드론 상태 전송
            emit('drone_status_update', latest_drone_status, room=request.sid)
    else:
        socketio.disconnect(request.sid)

@socketio.on('disconnect')
def handle_disconnect():
    print(f"[Server] Client disconnected: {request.sid}")
    if connected_clients['unity_sid'] == request.sid:
        connected_clients['unity_sid'] = None
        latest_drone_status.clear() # 유니티 연결 끊기면 상태 정보도 초기화
        print("[Server] Unity client disconnected. Cleared drone status.")
        emit('drone_status_update', {}, room=connected_clients['web_clients_room']) # 웹에도 초기화 알림


# Unity에서 드론 상태 정보를 받을 때 (주기적으로 전송됨)
@socketio.on('unity_drone_data')
def handle_unity_drone_data(data):
    global latest_drone_status
    latest_drone_status = data # 최신 상태 저장
    # print(f"[Server] Drone data from Unity: {data}")
    emit('drone_status_update', data, room=connected_clients['web_clients_room'])

# --- 웹 UI로부터 오는 명령 처리 ---
@socketio.on('report_wildfire')
def handle_report_wildfire(data): # data: {coordinates: {x, y, z}, fire_scale: int}
    if connected_clients['unity_sid']:
        print(f"[Server] Wildfire report from web: {data}. Forwarding to Unity.")
        emit('wildfire_alert_command', data, room=connected_clients['unity_sid'])
    else:
        emit('server_message', 'Error: Unity not connected.', room=request.sid)

@socketio.on('emergency_stop_pressed')
def handle_emergency_stop():
    if connected_clients['unity_sid']:
        print("[Server] Emergency STOP from web. Forwarding to Unity.")
        emit('emergency_stop_command', {}, room=connected_clients['unity_sid'])
    else:
        emit('server_message', 'Error: Unity not connected.', room=request.sid)

@socketio.on('dispatch_cancel_pressed') # 출동 취소 = 강제 귀환과 유사하게 처리 가능
def handle_dispatch_cancel():
    if connected_clients['unity_sid']:
        print("[Server] Dispatch CANCEL from web. Forwarding as Force Return to Unity.")
        emit('force_return_command', {}, room=connected_clients['unity_sid']) # 강제 귀환 명령으로 통합
    else:
        emit('server_message', 'Error: Unity not connected.', room=request.sid)

@socketio.on('force_return_pressed')
def handle_force_return():
    if connected_clients['unity_sid']:
        print("[Server] Force RETURN from web. Forwarding to Unity.")
        emit('force_return_command', {}, room=connected_clients['unity_sid'])
    else:
        emit('server_message', 'Error: Unity not connected.', room=request.sid)


if __name__ == '__main__':
    print("Flask-SocketIO server starting on http://127.0.0.1:5000")
    socketio.run(app, host='0.0.0.0', port=5000, debug=True, allow_unsafe_werkzeug=True)