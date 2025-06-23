from flask import Flask, render_template, request
from flask_socketio import SocketIO, emit, join_room, leave_room
import json

app = Flask(__name__, template_folder='templates', static_folder='static')
app.config['SECRET_KEY'] = 'drone_firefighter_secret_key_!@#'
socketio = SocketIO(app, cors_allowed_origins="*", async_mode='eventlet')

# 서버 관리 변수
connected_clients = {
    'unity_sid': None,
    'web_clients_room': 'drone_control_web_room'
}
latest_drone_status = {}


@app.route('/')
def index():
    return render_template('index.html')


# --- 소켓 연결 및 해제 ---
@socketio.on('connect')
def handle_connect():
    client_type = request.args.get('type')
    if client_type == 'unity':
        if connected_clients['unity_sid']:
            socketio.disconnect(sid=connected_clients['unity_sid'])
        connected_clients['unity_sid'] = request.sid
        join_room(request.sid)
    elif client_type == 'web':
        join_room(connected_clients['web_clients_room'])
        if latest_drone_status:
            emit('drone_status_update', latest_drone_status, room=request.sid)


@socketio.on('disconnect')
def handle_disconnect():
    if connected_clients['unity_sid'] == request.sid:
        connected_clients['unity_sid'] = None
        latest_drone_status.clear()
        emit('drone_status_update', {}, room=connected_clients['web_clients_room'])


# --- 유니티 메시지 중계 ---
@socketio.on('unity_drone_data')
def handle_unity_drone_data(data):
    global latest_drone_status
    latest_drone_status = data
    emit('drone_status_update', data, room=connected_clients['web_clients_room'])


@socketio.on('unity_dispatch_mission')
def handle_unity_dispatch_mission(data):
    if connected_clients['unity_sid']:
        emit('mission_dispatch_notification', data, room=connected_clients['web_clients_room'])


# --- 웹 UI 명령 중계 ---
@socketio.on('report_wildfire')
def handle_report_wildfire(data):
    """(기능 7) 웹에서 산불 발생 좌표를 받아 유니티로 전달합니다."""

    # --- 수정된 부분 시작 ---
    print("--- DEBUG: 'report_wildfire' 신호를 서버에서 수신했습니다! ---")
    # --- 수정된 부분 끝 ---

    if connected_clients['unity_sid']:
        print(f"[Server] Wildfire report from web: {data}. Forwarding to Unity.")
        emit('wildfire_alert_command', data, room=connected_clients['unity_sid'])
    else:
        # 이 메시지는 웹 UI로 전송됨
        emit('server_message', 'Error: Unity not connected.', room=request.sid)


@socketio.on('change_payload')
def handle_change_payload(data):
    if connected_clients['unity_sid']:
        emit('change_payload_command', data, room=connected_clients['unity_sid'])


@socketio.on('emergency_stop_pressed')
def handle_emergency_stop():
    if connected_clients['unity_sid']:
        emit('emergency_stop_command', {}, room=connected_clients['unity_sid'])


@socketio.on('force_return_pressed')
def handle_force_return():
    if connected_clients['unity_sid']:
        emit('force_return_command', {}, room=connected_clients['unity_sid'])


@socketio.on('dispatch_cancel_pressed')
def handle_dispatch_cancel():
    if connected_clients['unity_sid']:
        emit('force_return_command', {}, room=connected_clients['unity_sid'])


if __name__ == '__main__':
    print("Flask-SocketIO server starting...")
    socketio.run(app, host='0.0.0.0', port=5000, debug=True, allow_unsafe_werkzeug=True)
