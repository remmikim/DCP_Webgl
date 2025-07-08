from flask import Flask, render_template, request
from flask_socketio import SocketIO, emit, join_room

app = Flask(__name__, template_folder='templates', static_folder='static')
app.config['SECRET_KEY'] = 'your_very_secret_key_for_drone_mission'
socketio = SocketIO(app, cors_allowed_origins="*", async_mode='eventlet')

# --- 연결된 클라이언트 관리 ---
# Unity 클라이언트가 메인 드론인지, 테스트 드론인지 구분하여 SID 저장
connected_unity_clients = {
    'main_drone': None,
    'test_drone': None
}
web_clients_room = 'web_clients_drone_mission_room'

# 각 드론의 최신 상태를 저장
latest_status = {
    'main_drone': {},
    'test_drone': {}
}

# --- 라우팅: 각 웹 페이지 제공 ---
@app.route('/')
def index():
    """메인 관제 페이지를 제공합니다."""
    return render_template('index.html')

@app.route('/test')
def test_page():
    """테스트용 좌표 이동 페이지를 제공합니다."""
    return render_template('test.html')


# --- 공용 소켓 이벤트 핸들러 ---
@socketio.on('connect')
def handle_connect():
    client_type = request.args.get('type') # 'unity_main', 'unity_test', 'web'
    print(f"[Server] Client connected: {request.sid}, Type: {client_type}")

    if client_type == 'unity_main':
        connected_unity_clients['main_drone'] = request.sid
        join_room(request.sid)
        print(f"[Server] Main Drone client registered: {request.sid}")
    elif client_type == 'unity_test':
        connected_unity_clients['test_drone'] = request.sid
        join_room(request.sid)
        print(f"[Server] Test Drone client registered: {request.sid}")
    elif client_type == 'web':
        join_room(web_clients_room)
        print(f"[Server] Web client joined room: {request.sid}")
        # 새로운 웹 클라이언트에게 현재 드론 상태들 전송
        if latest_status['main_drone']:
            emit('main_drone_status_update', latest_status['main_drone'], room=request.sid)
        if latest_status['test_drone']:
            emit('test_drone_status_update', latest_status['test_drone'], room=request.sid)

@socketio.on('disconnect')
def handle_disconnect():
    print(f"[Server] Client disconnected: {request.sid}")
    if connected_unity_clients['main_drone'] == request.sid:
        connected_unity_clients['main_drone'] = None
        latest_status['main_drone'] = {}
        emit('main_drone_status_update', {}, room=web_clients_room)
        print("[Server] Main Drone disconnected.")
    elif connected_unity_clients['test_drone'] == request.sid:
        connected_unity_clients['test_drone'] = None
        latest_status['test_drone'] = {}
        emit('test_drone_status_update', {}, room=web_clients_room)
        print("[Server] Test Drone disconnected.")


# --- 메인 관제 시스템용 이벤트 ---
@socketio.on('unity_main_drone_data')
def handle_main_drone_data(data):
    latest_status['main_drone'] = data
    emit('main_drone_status_update', data, room=web_clients_room)

@socketio.on('main_force_return_pressed')
def handle_main_force_return():
    if connected_unity_clients['main_drone']:
        print("[Server] Main Drone Force RETURN from web. Forwarding to Unity.")
        emit('force_return_command', {}, room=connected_unity_clients['main_drone'])

@socketio.on('main_change_payload')
def handle_main_change_payload(data):
    if connected_unity_clients['main_drone']:
        print(f"[Server] Main Drone Change Payload from web: {data}. Forwarding.")
        emit('change_payload_command', data, room=connected_unity_clients['main_drone'])


# --- 테스트 시스템용 이벤트 ---
@socketio.on('unity_test_drone_data')
def handle_test_drone_data(data):
    latest_status['test_drone'] = data
    emit('test_drone_status_update', data, room=web_clients_room)

@socketio.on('test_dispatch_pressed')
def handle_test_dispatch(data):
    if connected_unity_clients['test_drone']:
        print(f"[Server] Test Drone Dispatch from web: {data}. Forwarding.")
        emit('dispatch_command', data, room=connected_unity_clients['test_drone'])

@socketio.on('test_force_return_pressed')
def handle_test_force_return():
    if connected_unity_clients['test_drone']:
        print("[Server] Test Drone Force RETURN from web. Forwarding.")
        emit('force_return_command', {}, room=connected_unity_clients['test_drone'])

@socketio.on('test_change_payload')
def handle_test_change_payload(data):
    if connected_unity_clients['test_drone']:
        print(f"[Server] Test Drone Change Payload from web: {data}. Forwarding.")
        emit('change_payload_command', data, room=connected_unity_clients['test_drone'])


if __name__ == '__main__':
    print("Flask-SocketIO server starting on http://127.0.0.1:5000")
    socketio.run(app, host='0.0.0.0', port=5000, debug=True, allow_unsafe_werkzeug=True)