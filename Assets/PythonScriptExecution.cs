using UnityEngine;
using Python.Runtime;

public class PythonScriptExecution : MonoBehaviour
{
    void Start()
    {
        // Python 인터프리터 경로 설정
        string pythonHome = @"C:\Users\HJJ\AppData\Local\Microsoft\WindowsApps\python.exe";  // Python 경로
        PythonEngine.PythonHome = pythonHome;

        // Python 엔진 초기화
        PythonEngine.Initialize();

        using (Py.GIL())  // GIL을 사용하여 Python 코드 실행
        {
            // Python 코드 실행
            string pythonScript = @"
import math
result = math.sqrt(25)
print('The square root of 25 is:', result)
";
            // Python 코드 실행
            PythonEngine.Exec(pythonScript);
        }

        // Python 엔진 종료
        PythonEngine.Shutdown();
    }
}
