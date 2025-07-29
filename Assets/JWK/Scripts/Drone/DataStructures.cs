using System;
using UnityEngine;

namespace JWK.Scripts.Drone
{
    [Serializable]
    public struct Vector3Data // 클래스(참조 타입)에서 구조체(값 타입)로 변경
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data(float xVal, float yVal, float zVal)
        {
            x = xVal;
            y = yVal;
            z = zVal;
        }
    
        // Vector3에서 쉽게 변환할 수 있도록 암시적 변환 연산자 추가
        public static implicit operator Vector3Data(Vector3 v)
        {
            return new Vector3Data(v.x, v.y, v.z);
        }
    }
    
    [Serializable]
    public class DroneStatusData
    {
        public Vector3Data position;
        public float altitude;
        public float battery;
        public string mission_state;
        public string payload_type;
        public int bomb_load;

        public DroneStatusData(Vector3 pos, float alt, float bat, string state, string payload, int bombs)
        {
            position = pos; // 암시적 변환 덕분에 바로 할당 가능
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
            target_coordinates = target;
        }
    }
}