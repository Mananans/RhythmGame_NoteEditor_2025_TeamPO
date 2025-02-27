using UnityEngine;
using Python.Runtime;

public class PythonTest : MonoBehaviour
{
    void Start()
    {
        // Python ���������� ��θ� ��������� ����
        string pythonHome = @"C:\Users\HJJ\AppData\Local\Microsoft\WindowsApps\python.exe";  // Windows���� Python ��ġ ��� ����
        PythonEngine.PythonHome = pythonHome;  // PythonHome ����

        // Python ���� �ʱ�ȭ
        PythonEngine.Initialize();

        // Python �ڵ� ����
        using (Py.GIL())  // GIL (Global Interpreter Lock)�� ����Ͽ� Python �ڵ� ����
        {
            // math ����� ����Ʈ
            dynamic math = Py.Import("math");
            // math.sqrt(16) ȣ��
            double result = math.sqrt(16);
            Debug.Log($"Result of sqrt(16): {result}");
        }

        // Python ���� ����
        PythonEngine.Shutdown();
    }
}
