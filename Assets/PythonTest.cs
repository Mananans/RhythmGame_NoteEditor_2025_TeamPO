using UnityEngine;
using Python.Runtime;

public class PythonTest : MonoBehaviour
{
    void Start()
    {
        // Python 인터프리터 경로를 명시적으로 설정
        string pythonHome = @"C:\Users\HJJ\AppData\Local\Microsoft\WindowsApps\python.exe";  // Windows에서 Python 설치 경로 예시
        PythonEngine.PythonHome = pythonHome;  // PythonHome 설정

        // Python 엔진 초기화
        PythonEngine.Initialize();

        // Python 코드 실행
        using (Py.GIL())  // GIL (Global Interpreter Lock)을 사용하여 Python 코드 실행
        {
            // math 모듈을 임포트
            dynamic math = Py.Import("math");
            // math.sqrt(16) 호출
            double result = math.sqrt(16);
            Debug.Log($"Result of sqrt(16): {result}");
        }

        // Python 엔진 종료
        PythonEngine.Shutdown();
    }
}
