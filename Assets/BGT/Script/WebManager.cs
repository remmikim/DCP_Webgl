using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class WebManager : MonoBehaviour
{
    [Header("Device References - From Manager1.cs")]
    public RoofMove roofMove;
    public PipeHolders pipeHolders;
    public ZLiftTrigger zLift;
    public Base2Down base2down1;
    public Base2Down base2down2;
    public Base2Down base2down3;
    public Base2Down base2down4;
    public XGantry xGantry;
    public CarriageFrameRT carriageFrameRT;
    public ForkMove1 forkMove;
    public ForkFrontBackMove forkFrontBackMoveLeft;
    public ForkFrontBackMove forkFrontBackMoveRight;
    
    // 디바이스 상태 저장
    private Dictionary<string, bool> deviceStates = new Dictionary<string, bool>();
    private Dictionary<string, bool> previousDeviceStates = new Dictionary<string, bool>();
    
    // Firebase 연결 상태
    private bool isConnected = false;

    // JavaScript 함수 임포트 (WebGL에서 사용)
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RequestPlcData();
    
    [DllImport("__Internal")]
    private static extern void SetupPlcDataListener();
    
    [DllImport("__Internal")]
    private static extern void InitializeUnityBridge();
#endif

    void Start()
    {
        Debug.Log("🚀 WebManager: 초기화 시작");
        
        // 디바이스 참조 확인
        ValidateDeviceReferences();
        
        // 초기 상태 설정
        InitializeDeviceStates();
        
        // WebGL 환경에서 Firebase 데이터 리스너 설정
#if UNITY_WEBGL && !UNITY_EDITOR
        SetupPlcDataListener();
        InitializeUnityBridge();
        Debug.Log("🌐 WebGL 환경에서 JavaScript 브리지 설정 완료");
#else
        Debug.Log("🖥️ 에디터 환경에서 실행 중 - JavaScript 브리지 비활성화");
#endif
        
        isConnected = true;
        Debug.Log("✅ WebManager: 웹 데이터 연결 완료 - 실제 웹페이지 데이터 대기 중");
    }
    
    /// <summary>
    /// 테스트용 더미 데이터 생성 (에디터에서만 사용)
    /// </summary>
    [ContextMenu("Test Web Data")]
    private void SendTestData()
    {
        Debug.Log("🧪 WebManager: 테스트 데이터 생성");
        string testData = @"{
            ""devices.Y0"": { ""value"": true },
            ""devices.Y1"": { ""value"": false },
            ""devices.Y2"": { ""value"": true }
        }";
        ReceiveWebData(testData);
    }

    void Update()
    {
        // 주기적으로 Firebase 데이터 요청 (WebGL 환경에서만)
#if UNITY_WEBGL && !UNITY_EDITOR
        if (Time.frameCount % 300 == 0) // 5초마다 (60fps 기준)
        {
            RequestPlcData();
        }
#endif
    }

    /// <summary>
    /// 디바이스 참조 유효성 검사
    /// </summary>
    private void ValidateDeviceReferences()
    {
        if (pipeHolders == null)
            Debug.LogWarning("WebManager: PipeHolders 참조가 설정되지 않았습니다.");
        
        if (zLift == null)
            Debug.LogWarning("WebManager: ZLiftTrigger 참조가 설정되지 않았습니다.");
    }

    /// <summary>
    /// 디바이스 상태 초기화
    /// </summary>
    private void InitializeDeviceStates()
    {
        // Y 디바이스 초기 상태 설정 (Y0~YF)
        for (int i = 0; i < 16; i++)
        {
            string deviceKey = $"Y{i:X}";
            deviceStates[deviceKey] = false;
            previousDeviceStates[deviceKey] = false;
        }
        
        Debug.Log("WebManager: 디바이스 상태 초기화 완료");
    }

    /// <summary>
    /// JavaScript에서 호출되는 Firebase 데이터 수신 함수
    /// </summary>
    /// <param name="jsonData">Firebase에서 받은 devices 데이터 JSON</param>
    public void ReceiveWebData(string jsonData)
    {
        try
        {
            Debug.Log($"📨 WebManager: 웹 데이터 수신 - 길이: {jsonData.Length}");
            Debug.Log($"📨 수신된 JSON: {jsonData}");
            
            // JSON 파싱 및 디바이스 상태 업데이트
            ParseWebData(jsonData);
            
            // 상태 변경된 디바이스들 처리
            ProcessDeviceChanges();
            
            Debug.Log("✅ WebManager: 웹 데이터 처리 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ WebManager: 웹 데이터 처리 중 오류 발생: {e.Message}");
            Debug.LogError($"❌ 스택 트레이스: {e.StackTrace}");
        }
    }

    /// <summary>
    /// 웹 데이터 파싱
    /// </summary>
    /// <param name="jsonData">Firebase에서 받은 JSON 데이터</param>
    private void ParseWebData(string jsonData)
    {
        try
        {
            // 간단한 JSON 파싱 - "devices.Y0": { "value": true } 형태
            string[] lines = jsonData.Split(new char[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                
                if (trimmed.Contains("\"devices.Y") && trimmed.Contains("\"value\""))
                {
                    ParseDeviceLine(trimmed);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"WebManager: 웹 데이터 파싱 오류: {e.Message}");
        }
    }

    /// <summary>
    /// 개별 디바이스 라인 파싱
    /// </summary>
    /// <param name="line">파싱할 라인</param>
    private void ParseDeviceLine(string line)
    {
        try
        {
            // "devices.Y0" 추출
            int devicesIndex = line.IndexOf("\"devices.Y");
            if (devicesIndex == -1) return;
            
            int endQuoteIndex = line.IndexOf("\"", devicesIndex + 1);
            if (endQuoteIndex == -1) return;
            
            string deviceKey = line.Substring(devicesIndex + 1, endQuoteIndex - devicesIndex - 1);
            string deviceAddress = deviceKey.Replace("devices.", "");
            
            // value 값 추출
            bool deviceValue = ExtractValueFromLine(line);
            
            // 이전 상태 저장
            if (deviceStates.ContainsKey(deviceAddress))
            {
                previousDeviceStates[deviceAddress] = deviceStates[deviceAddress];
                deviceStates[deviceAddress] = deviceValue;
            }
            else
            {
                previousDeviceStates[deviceAddress] = false;
                deviceStates[deviceAddress] = deviceValue;
            }
            
            Debug.Log($"WebManager: {deviceAddress} 상태 업데이트: {previousDeviceStates[deviceAddress]} → {deviceValue}");
        }
        catch (Exception e)
        {
            Debug.LogError($"WebManager: 디바이스 라인 파싱 오류: {e.Message} - Line: {line}");
        }
    }

    /// <summary>
    /// 라인에서 value 값 추출
    /// </summary>
    /// <param name="line">파싱할 라인</param>
    /// <returns>추출된 boolean 값</returns>
    private bool ExtractValueFromLine(string line)
    {
        if (line.Contains("\"value\": true") || line.Contains("\"value\":true"))
            return true;
        else if (line.Contains("\"value\": false") || line.Contains("\"value\":false"))
            return false;
        
        return false;
    }

    /// <summary>
    /// 디바이스 상태 변경 처리
    /// </summary>
    private void ProcessDeviceChanges()
    {
        foreach (var deviceState in deviceStates)
        {
            string deviceAddress = deviceState.Key;
            bool currentValue = deviceState.Value;
            bool previousValue = previousDeviceStates.ContainsKey(deviceAddress) ? previousDeviceStates[deviceAddress] : false;
            
            // 상태가 변경된 경우에만 처리
            if (currentValue != previousValue)
            {
                ProcessDeviceCommand(deviceAddress, currentValue);
            }
        }
    }

    /// <summary>
    /// 개별 디바이스 명령 처리 (Manager1.cs 기준)
    /// </summary>
    /// <param name="deviceAddress">디바이스 주소 (예: Y0, Y1)</param>
    /// <param name="isActive">활성화 상태</param>
    private void ProcessDeviceCommand(string deviceAddress, bool isActive)
    {
        Debug.Log($"WebManager: 디바이스 제어 - {deviceAddress}: {isActive}");
        
        // Manager1.cs와 동일한 매핑
        switch (deviceAddress.ToUpper())
        {
            case "Y0": // Roof Front
                if (roofMove != null)
                {
                    if (isActive) roofMove.ActivateFrontRoof();
                    else roofMove.DeactivateFrontRoof();
                }
                break;
                
            case "Y1": // Roof Back
                if (roofMove != null)
                {
                    if (isActive) roofMove.ActivateBackRoof();
                    else roofMove.DeactivateBackRoof();
                }
                break;
                
            case "Y2": // PipeHolders CW
                if (pipeHolders != null)
                {
                    if (isActive) pipeHolders.ActivatePipeHoldersCW();
                    else pipeHolders.DeactivatePipeHoldersCW();
                }
                break;
                
            case "Y3": // PipeHolders CCW
                if (pipeHolders != null)
                {
                    if (isActive) pipeHolders.ActivatePipeHoldersCCW();
                    else pipeHolders.DeactivatePipeHoldersCCW();
                }
                break;
                
            case "Y4": // ZLift Up
                if (zLift != null)
                {
                    if (isActive) zLift.ActivateZLiftUp();
                    else zLift.DeactivateZLiftUp();
                }
                break;
                
            case "Y5": // ZLift Down
                if (zLift != null)
                {
                    if (isActive) zLift.ActivateZLiftDown();
                    else zLift.DeactivateZLiftDown();
                }
                break;
                
            case "Y6": // Base2Down (Down)
                if (base2down1 != null) { if (isActive) base2down1.ActiveDown(); else base2down1.DeactiveDown(); }
                if (base2down2 != null) { if (isActive) base2down2.ActiveDown(); else base2down2.DeactiveDown(); }
                if (base2down3 != null) { if (isActive) base2down3.ActiveDown(); else base2down3.DeactiveDown(); }
                if (base2down4 != null) { if (isActive) base2down4.ActiveDown(); else base2down4.DeactiveDown(); }
                break;
                
            case "Y7": // Base2Down (Up)
                if (base2down1 != null) { if (isActive) base2down1.ActiveUp(); else base2down1.DeactiveUp(); }
                if (base2down2 != null) { if (isActive) base2down2.ActiveUp(); else base2down2.DeactiveUp(); }
                if (base2down3 != null) { if (isActive) base2down3.ActiveUp(); else base2down3.DeactiveUp(); }
                if (base2down4 != null) { if (isActive) base2down4.ActiveUp(); else base2down4.DeactiveUp(); }
                break;
                
            case "Y8": // XGantry Moving Right
                if (xGantry != null)
                {
                    if (isActive) xGantry.ActivateXGantryMovingRight();
                    else xGantry.DeactivateXGantryMovingRight();
                }
                break;
                
            case "Y9": // XGantry Moving Left
                if (xGantry != null)
                {
                    if (isActive) xGantry.ActivateXGantryMovingLeft();
                    else xGantry.DeactivateXGantryMovingLeft();
                }
                break;
                
            case "YA": // CarriageFrame Rotation CW
                if (carriageFrameRT != null)
                {
                    if (isActive) carriageFrameRT.ActivateZLiftRotationCW();
                    else carriageFrameRT.DeactivateZLiftRotationCW();
                }
                break;
                
            case "YB": // CarriageFrame Rotation CCW
                if (carriageFrameRT != null)
                {
                    if (isActive) carriageFrameRT.ActivateZLiftRotationCCW();
                    else carriageFrameRT.DeactivateZLiftRotationCCW();
                }
                break;
                
            case "YC": // Fork Move Right
                if (forkMove != null)
                {
                    if (isActive) forkMove.ActivateRight();
                    else forkMove.DeactivateRight();
                }
                break;
                
            case "YD": // Fork Move Left
                if (forkMove != null)
                {
                    if (isActive) forkMove.ActivateLeft();
                    else forkMove.DeactivateLeft();
                }
                break;
                
            // Y10Y1F 블록 (Manager1.cs의 Y10, Y11 매핑)
            case "Y10": // Fork Front/Back Move Front
                if (forkFrontBackMoveLeft != null) { if (isActive) forkFrontBackMoveLeft.ActivateFront(); else forkFrontBackMoveLeft.DeactivateFront(); }
                if (forkFrontBackMoveRight != null) { if (isActive) forkFrontBackMoveRight.ActivateFront(); else forkFrontBackMoveRight.DeactivateFront(); }
                break;
                
            case "Y11": // Fork Front/Back Move Back
                if (forkFrontBackMoveLeft != null) { if (isActive) forkFrontBackMoveLeft.ActivateBack(); else forkFrontBackMoveLeft.DeactivateBack(); }
                if (forkFrontBackMoveRight != null) { if (isActive) forkFrontBackMoveRight.ActivateBack(); else forkFrontBackMoveRight.DeactivateBack(); }
                break;
                
            default:
                Debug.Log($"WebManager: 매핑되지 않은 디바이스 - {deviceAddress}");
                break;
        }
    }

    /// <summary>
    /// JavaScript에서 연결 상태 변경을 알릴 때 호출
    /// </summary>
    /// <param name="connected">연결 상태 문자열</param>
    public void UpdateWebConnectionStatus(string connected)
    {
        bool wasConnected = isConnected;
        isConnected = connected.ToLower() == "true";
        
        if (wasConnected != isConnected)
        {
            Debug.Log($"WebManager: 웹 연결 상태 변경: {isConnected}");
        }
    }

    /// <summary>
    /// 현재 연결 상태 확인
    /// </summary>
    public bool IsWebConnected => isConnected;

    /// <summary>
    /// 현재 디바이스 상태 확인 (디버깅용)
    /// </summary>
    [ContextMenu("Show Device States")]
    public void ShowDeviceStates()
    {
        Debug.Log("=== WebManager 디바이스 상태 ===");
        foreach (var state in deviceStates)
        {
            Debug.Log($"{state.Key}: {state.Value}");
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 테스트용 더미 데이터
    /// </summary>
    [ContextMenu("Test Web Data")]
    private void TestWebData()
    {
        string testData = @"
        {
            ""devices.Y0"": { ""value"": true },
            ""devices.Y1"": { ""value"": false },
            ""devices.Y2"": { ""value"": true },
            ""devices.Y3"": { ""value"": false }
        }";
        
        ReceiveWebData(testData);
    }
#endif

    void OnDestroy()
    {
        Debug.Log("WebManager: 종료");
    }
}