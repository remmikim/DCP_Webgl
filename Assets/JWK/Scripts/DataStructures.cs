using UnityEngine;
using System;

// --- 데이터 구조 클래스들 ---
// 이 클래스들은 주로 데이터를 담는 용도로 사용됨
// [System.Serializable] 속성을 통해 Unity Inspector에서 보거나 JSON으로 변환할 수 있다.

[Serializable]
public class Vector3Data
{
    public float x;
    public float y;
    public float z;
    public Vector3Data(float xVal, float yVal, float zVal) { x = xVal; y = yVal; z = zVal; }
}

[Serializable]
public class DroneStatusData
{
    public Vector3Data position;
    public float altitude;
    public float battery;
    public string mission_state;
    public string payload_type; // 페이로드 종류
    public int bomb_load;

    public DroneStatusData(Vector3 pos, float alt, float bat, string state, string payload, int bombs)
    {
        position = new Vector3Data(pos.x, pos.y, pos.z);
        altitude = alt;
        battery = bat;
        mission_state = state;
        payload_type = payload;
        bomb_load = bombs;
    }
}

[Serializable]
public class DispatchData
{
    public string mission_type;
    public Vector3Data target_coordinates;

    public DispatchData(string type, Vector3 target)
    {
        mission_type = type;
        target_coordinates = new Vector3Data(target.x, target.y, target.z);
    }
}