document.addEventListener('DOMContentLoaded', () => {
    const socket = io.connect(location.protocol + '//' + document.domain + ':' + location.port, { query: "type=web" });

    const el = (id) => document.getElementById(id);
    const connStatus = el('connection-status');
    const dronePos = el('drone-position');
    const payloadTypeSpan = el('payload-type');
    const payloadButtons = document.querySelectorAll('#payload-buttons button');

    socket.on('connect', () => { connStatus.textContent = '연결됨'; connStatus.className = 'connected'; });
    socket.on('disconnect', () => { connStatus.textContent = '연결 끊김'; connStatus.className = 'disconnected'; });

    socket.on('test_drone_status_update', (data) => {
        if (data && data.position) {
            dronePos.textContent = `X:${data.position.x.toFixed(1)}, Y:${data.position.y.toFixed(1)}, Z:${data.position.z.toFixed(1)}`;
        } else {
            dronePos.textContent = 'N/A';
        }
        payloadTypeSpan.textContent = data.payload_type || 'N/A';

        payloadButtons.forEach(button => {
            button.classList.toggle('active', button.dataset.payload === data.payload_type);
        });
    });

    el('dispatch-test-drone-btn').addEventListener('click', () => {
        const x = parseFloat(el('targetX').value);
        const y = parseFloat(el('targetY').value);
        const z = parseFloat(el('targetZ').value);
        if (!isNaN(x) && !isNaN(y) && !isNaN(z)) {
            socket.emit('test_dispatch_pressed', { coordinates: { x, y, z } });
        } else {
            alert('유효한 좌표를 입력하세요.');
        }
    });

    el('payload-buttons').addEventListener('click', (e) => {
        if (e.target.tagName === 'BUTTON') {
            const payload = e.target.dataset.payload;
            socket.emit('test_change_payload', { payload: payload });
        }
    });

    el('force-return').addEventListener('click', () => {
        if (confirm('테스트 드론을 즉시 스테이션으로 강제 귀환시키겠습니까?')) {
            socket.emit('test_force_return_pressed');
        }
    });
});