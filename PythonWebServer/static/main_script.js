document.addEventListener('DOMContentLoaded', () => {
    const socket = io.connect(location.protocol + '//' + document.domain + ':' + location.port, { query: "type=web" });

    const el = (id) => document.getElementById(id);
    const connStatus = el('connection-status');
    const dronePos = el('drone-position');
    const droneAlt = el('drone-altitude');
    const droneBat = el('drone-battery');
    const missionState = el('mission-state');
    const payloadTypeSpan = el('payload-type');
    const bombLoad = el('bomb-load');
    const payloadButtons = document.querySelectorAll('#payload-buttons button');

    function resetStatus() {
        document.querySelectorAll('#status-container span:not(#connection-status)').forEach(span => span.textContent = 'N/A');
        payloadButtons.forEach(button => button.classList.remove('active'));
    }

    socket.on('connect', () => { connStatus.textContent = '연결됨'; connStatus.className = 'connected'; });
    socket.on('disconnect', () => { connStatus.textContent = '연결 끊김'; connStatus.className = 'disconnected'; resetStatus(); });

    socket.on('main_drone_status_update', (data) => {
        if (!data || Object.keys(data).length === 0) { resetStatus(); return; }
        dronePos.textContent = `X:${data.position.x.toFixed(1)}, Y:${data.position.y.toFixed(1)}, Z:${data.position.z.toFixed(1)}`;
        droneAlt.textContent = `${data.altitude.toFixed(1)} m`;
        droneBat.textContent = `${data.battery.toFixed(1)} %`;
        missionState.textContent = data.mission_state || 'N/A';
        payloadTypeSpan.textContent = data.payload_type || 'N/A';
        bombLoad.textContent = `${data.bomb_load} / 6 개`;

        payloadButtons.forEach(button => {
            button.classList.toggle('active', button.dataset.payload === data.payload_type);
        });
    });

    el('force-return').addEventListener('click', () => {
        if (confirm('메인 드론을 즉시 스테이션으로 강제 귀환시키겠습니까?')) {
            socket.emit('main_force_return_pressed');
        }
    });

    el('payload-buttons').addEventListener('click', (e) => {
        if (e.target.tagName === 'BUTTON') {
            const payload = e.target.dataset.payload;
            socket.emit('main_change_payload', { payload: payload });
        }
    });
});