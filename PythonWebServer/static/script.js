document.addEventListener('DOMContentLoaded', () => {
    const socket = io('http://' + document.domain + ':' + location.port, {
        query: { type: 'web' }
    });

    const el = (id) => document.getElementById(id); // Helper

    const dronePositionSpan = el('dronePosition');
    const droneAltitudeSpan = el('droneAltitude');
    const droneBatterySpan = el('droneBattery');
    const droneMissionStateSpan = el('droneMissionState');
    const droneBombLoadSpan = el('droneBombLoad');
    const serverMessageSpan = el('serverMessage');

    const btnReportFire = el('btnReportFire');
    const fireXInput = el('fireX');
    const fireYInput = el('fireY');
    const fireZInput = el('fireZ');
    const fireScaleSelect = el('fireScale');

    const btnEmergencyStop = el('btnEmergencyStop');
    const btnDispatchCancel = el('btnDispatchCancel');
    const btnForceReturn = el('btnForceReturn');

    socket.on('connect', () => serverMessageSpan.textContent = '서버에 연결되었습니다.');
    socket.on('disconnect', () => serverMessageSpan.textContent = '서버와 연결이 끊겼습니다!');
    socket.on('server_message', (msg) => serverMessageSpan.textContent = msg);

    socket.on('drone_status_update', (data) => {
        dronePositionSpan.textContent = data.position ? `X: ${parseFloat(data.position.x).toFixed(1)}, Y: ${parseFloat(data.position.y).toFixed(1)}, Z: ${parseFloat(data.position.z).toFixed(1)}` : '-';
        droneAltitudeSpan.textContent = data.altitude ? parseFloat(data.altitude).toFixed(1) : '-';
        droneBatterySpan.textContent = data.battery ? parseFloat(data.battery).toFixed(1) : '-';
        droneMissionStateSpan.textContent = data.mission_state || '-';
        droneBombLoadSpan.textContent = (typeof data.bomb_load !== 'undefined') ? data.bomb_load : '-';
    });

    btnReportFire.addEventListener('click', () => {
        const x = parseFloat(fireXInput.value);
        const y = parseFloat(fireYInput.value);
        const z = parseFloat(fireZInput.value);
        const scale = parseInt(fireScaleSelect.value);

        if (isNaN(x) || isNaN(y) || isNaN(z) || isNaN(scale)) {
            alert('유효한 산불 좌표와 규모를 입력하세요.');
            return;
        }
        const reportData = { coordinates: { x, y, z }, fire_scale: scale };
        if (confirm(`산불 보고 및 출동 명령:\n좌표: X=${x}, Y=${y}, Z=${z}\n폭탄 ${scale}개 사용`)) {
            socket.emit('report_wildfire', reportData);
            serverMessageSpan.textContent = `명령 전송: 산불 보고 (폭탄 ${scale}개)`;
        }
    });

    btnEmergencyStop.addEventListener('click', () => {
        if (confirm('드론을 현재 위치에서 즉시 정지(AGL 호버)시키겠습니까?')) {
            socket.emit('emergency_stop_pressed');
            serverMessageSpan.textContent = '명령 전송: 긴급 정지';
        }
    });

    btnDispatchCancel.addEventListener('click', () => {
        if (confirm('현재 임무를 취소하고 드론을 스테이션으로 복귀시키겠습니까?')) {
            socket.emit('dispatch_cancel_pressed');
            serverMessageSpan.textContent = '명령 전송: 출동 취소';
        }
    });

    btnForceReturn.addEventListener('click', () => {
        if (confirm('드론을 즉시 스테이션으로 강제 귀환시키겠습니까? (모든 임무 중단)')) {
            socket.emit('force_return_pressed');
            serverMessageSpan.textContent = '명령 전송: 강제 귀환';
        }
    });
});