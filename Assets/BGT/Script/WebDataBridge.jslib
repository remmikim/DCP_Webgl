mergeInto(LibraryManager.library, {
    
    // PLC 데이터 리스너 설정
    SetupPlcDataListener: function() {
        if (typeof window !== 'undefined') {
            console.log("WebDataBridge: PLC 데이터 리스너 설정 시작");
            
            // React 앱의 PLC 데이터 업데이트 이벤트 리스닝
            window.addEventListener('plcDataUpdated', function(event) {
                try {
                    if (event.detail && event.detail.devices) {
                        var jsonString = JSON.stringify(event.detail.devices);
                        SendMessage('WebManager', 'ReceiveWebData', jsonString);
                        console.log("WebDataBridge: PLC 데이터를 Unity로 전송:", jsonString);
                    }
                } catch (error) {
                    console.error("WebDataBridge: PLC 데이터 처리 오류:", error);
                }
            });
            
            // 연결 상태 변경 이벤트 리스닝
            window.addEventListener('plcConnectionChanged', function(event) {
                try {
                    var isConnected = event.detail.isConnected ? "true" : "false";
                    SendMessage('WebManager', 'UpdateWebConnectionStatus', isConnected);
                    console.log("WebDataBridge: 연결 상태 변경:", isConnected);
                } catch (error) {
                    console.error("WebDataBridge: 연결 상태 이벤트 처리 오류:", error);
                }
            });
            
            console.log("WebDataBridge: 이벤트 리스너 설정 완료");
        }
    },
    
    // PLC 데이터 직접 요청
    RequestPlcData: function() {
        if (typeof window !== 'undefined') {
            console.log("WebDataBridge: PLC 데이터 요청");
            
            // Method 1: 전역 함수 호출
            if (window.getCurrentPlcData) {
                try {
                    var data = window.getCurrentPlcData();
                    if (data && data.devices) {
                        var jsonString = JSON.stringify(data.devices);
                        SendMessage('WebManager', 'ReceiveWebData', jsonString);
                        console.log("WebDataBridge: 직접 데이터 요청 성공");
                        return;
                    }
                } catch (error) {
                    console.error("WebDataBridge: 직접 데이터 요청 오류:", error);
                }
            }
            
            // Method 2: Custom Event 발생
            try {
                window.dispatchEvent(new CustomEvent('unityRequestPlcData', {
                    detail: { timestamp: Date.now() }
                }));
                console.log("WebDataBridge: Unity 데이터 요청 이벤트 발생");
            } catch (error) {
                console.error("WebDataBridge: 이벤트 발생 오류:", error);
            }
            
            // Method 3: React Context에서 데이터 가져오기 시도
            try {
                if (window.reactContext && window.reactContext.plcData) {
                    var data = window.reactContext.plcData;
                    if (data && data.devices) {
                        var jsonString = JSON.stringify(data.devices);
                        SendMessage('WebManager', 'ReceiveWebData', jsonString);
                        console.log("WebDataBridge: React Context에서 데이터 가져오기 성공");
                    }
                }
            } catch (error) {
                console.error("WebDataBridge: React Context 접근 오류:", error);
            }
        }
    },
    
    // Unity 로드 완료 후 초기화 (함수로 변경)
    InitializeUnityBridge: function() {
        if (typeof window !== 'undefined') {
            console.log("WebDataBridge: Unity 브리지 초기화");
            
            // 초기 데이터 요청
            if (window.getCurrentPlcData) {
                try {
                    var data = window.getCurrentPlcData();
                    if (data && data.devices) {
                        var jsonString = JSON.stringify(data.devices);
                        SendMessage('WebManager', 'ReceiveWebData', jsonString);
                    }
                } catch (error) {
                    console.error("WebDataBridge: 초기 데이터 로드 오류:", error);
                }
            }
        }
    }
});