using UnityEngine;
using Python.Runtime;

public class PythonScriptExecution : MonoBehaviour
{
    void Start()
    {
        // Python ���������� ��� ����
        string pythonHome = @"C:\Users\HJJ\AppData\Local\Microsoft\WindowsApps\python.exe";  // Python ���
        PythonEngine.PythonHome = pythonHome;

        // Python ���� �ʱ�ȭ
        PythonEngine.Initialize();

        using (Py.GIL())  // GIL�� ����Ͽ� Python �ڵ� ����
        {
            // Python �ڵ� ����
            string pythonScript = @"
import math
result = math.sqrt(25)
print('The square root of 25 is:', result)
";
            // Python �ڵ� ����
            PythonEngine.Exec(pythonScript);
        }

        // Python ���� ����
        PythonEngine.Shutdown();
    }
}
