using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class AudioSourceSeparation : MonoBehaviour
{
    public string SongName = "your_audio_file";
    string PythonEXEPath = "";
    string pythonWaveScriptPath;// = @"C:\Users\HJJ\spleeter_script.py";
    string audioFilePath;// = @"C:\Users\HJJ\Music\your_audio_file.mp3";

    //-------------------------------------------------------------------------------------------

    string pythonMMLScriptPath;
    string wavFilePath;
    string mmlOutputPath;
    string pythonTempoScriptPath;

    public bool EndWorkTrigger = false;

    private void Start()
    {
        string streamingAssetsPath = Application.streamingAssetsPath;
        string batFilePath = Path.Combine(streamingAssetsPath, "install_spleeter_env.bat");

        if (!File.Exists(batFilePath))
        {
            UnityEngine.Debug.LogError("install_spleeter_env.bat not found in StreamingAssets!");
            return;
        }

        if (!File.Exists(streamingAssetsPath + "/spleeter_env/Scripts/python.exe"))
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                //FileName = "cmd.exe",  // cmd.exe�� ���� ����
                FileName = Path.Combine(streamingAssetsPath, "install_spleeter_env.bat"),
                Arguments = $"/c \"{batFilePath}\" UNITY_MODE=1",
                WorkingDirectory = streamingAssetsPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            psi.EnvironmentVariables["PATH"] = psi.EnvironmentVariables["PATH"] + ";C:\\venvs\\ffmpeg\\bin";
            psi.EnvironmentVariables["FFMPEG_CMD"] = @"C:\venvs\ffmpeg\bin\ffmpeg.exe";

            using (Process process = Process.Start(psi))
            {
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                process.WaitForExit();

                string output = outputTask.Result;
                string error = errorTask.Result;

                UnityEngine.Debug.Log($"Batch Output: {output}");
                if (!string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError($"Batch Error: {error}");
                }
                UnityEngine.Debug.Log($"Batch process exited with code: {process.ExitCode}");
            }
        }

        string path = Application.streamingAssetsPath + "/Data.json";
        if (!File.Exists(path))
        {
            Data data = new Data();
            data.Python_spleeter_env_EXE = @"C:\venvs\spleeter_env\Scripts\python.exe";

            StreamWriter sw = new StreamWriter(path);
            string swData = JsonUtility.ToJson(data);
            sw.Write(swData);
            sw.Close();
            PythonEXEPath = data.Python_spleeter_env_EXE;
        }
        else
        {
            Data data = new Data();
            StreamReader sr = new StreamReader(path);
            data = JsonUtility.FromJson<Data>(sr.ReadToEnd());
            PythonEXEPath = data.Python_spleeter_env_EXE;
            sr.Close();
        }
    }

    public void Activate()
    {
        // ���� ���� �ο�
        // GrantFilePermissions(audioFilePath);

        // ���̽� ��ũ��Ʈ ����
        string applicationPath = Application.streamingAssetsPath;
        applicationPath = applicationPath.Replace('/', '\\');
        pythonWaveScriptPath = applicationPath + @"\spleeter_script.py";
        pythonTempoScriptPath = applicationPath + @"\get_tempo.py";
        audioFilePath = applicationPath + @"\Songs\" + SongName + ".mp3";

        //------------------------------------------------------------------------------------------------------

        RunTempoScript(audioFilePath, applicationPath + @"\tempo_output.txt");


        RunWaveScript(audioFilePath, applicationPath);

        pythonMMLScriptPath = applicationPath + @"\convert_wav_to_mml.py";
        string SongRootPath = applicationPath + @"\spleeter_output\" + SongName + "\\";
        string mmlRootPath = SongRootPath + @"mml";
        if (!Directory.Exists(mmlRootPath))
            Directory.CreateDirectory(mmlRootPath);

        DirectoryInfo di = new DirectoryInfo(SongRootPath);
        FileInfo[] fi = di.GetFiles("*.wav");

        StreamReader sr = new StreamReader(Application.streamingAssetsPath + "/tempo_output.txt");
        string data = sr.ReadLine();
        data = data.Split(' ')[1];
        data = data.Replace('[', ' ');
        data = data.Replace(']', ' ');
        data = string.Concat(data.Where(x => !char.IsWhiteSpace(x)));
        sr.Close();

        for (int i = 0; i < fi.Length; i++)
        {
            wavFilePath = SongRootPath + fi[i].Name;
            string mmlTemp = fi[i].Name.Split('.')[0];

            mmlOutputPath = mmlRootPath + "\\" + $"{mmlTemp}.mml";

            RunMMLScript(wavFilePath, mmlOutputPath, data);
        }

        EndWorkTrigger = true;
    }

    void GrantFilePermissions(string filePath)
    {
        // icacls ��ɾ� �����Ͽ� ���� ���� �ο�
        ProcessStartInfo icaclsStart = new ProcessStartInfo();
        icaclsStart.FileName = "cmd.exe";
        icaclsStart.Arguments = $"/C icacls \"{filePath}\" /grant Everyone:F"; // ��� ����ڿ��� ���� �ο�
        icaclsStart.UseShellExecute = false;
        icaclsStart.RedirectStandardOutput = true;
        icaclsStart.RedirectStandardError = true;

        Process icaclsProcess = new Process();
        icaclsProcess.StartInfo = icaclsStart;
        icaclsProcess.Start();

        // ��� �α׸� ���
        string icaclsOutput = icaclsProcess.StandardOutput.ReadToEnd();
        string icaclsError = icaclsProcess.StandardError.ReadToEnd();

        UnityEngine.Debug.Log("ICACLS output: " + icaclsOutput);
        UnityEngine.Debug.Log("ICACLS error: " + icaclsError);

        icaclsProcess.WaitForExit();
    }

    void RunWaveScript(string audioFilePath, string outputPath)
    {
        // ���̽� ��ũ��Ʈ ����
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = PythonEXEPath;//@"C:\Users\HJJ\spleeter_env\Scripts\python.exe"; // Python ���� ���� ���
        start.Arguments = $"{pythonWaveScriptPath} {audioFilePath} {outputPath}"; // Python ��ũ��Ʈ�� ����� ���� ��� ����
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;

        Process process = new Process();
        process.StartInfo = start;
        process.Start();

        // ��� �α׸� ���
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        UnityEngine.Debug.Log("Python output: " + output);
        UnityEngine.Debug.Log("Python error: " + error);

        process.WaitForExit();
    }

    void RunTempoScript(string audioFilePath, string Output)
    {
        // ���̽� ��ũ��Ʈ ����
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = PythonEXEPath; // Python ���� ���� ���
        start.Arguments = $"{pythonTempoScriptPath} {audioFilePath} {Output}"; // ���ڷ� ����
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        Process process = new Process();
        process.StartInfo = start;
        process.Start();

        // ��� �α׸� ���
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        UnityEngine.Debug.Log("Python output: " + output);
        UnityEngine.Debug.Log("Python error: " + error);

        process.WaitForExit();
    }


    void RunMMLScript(string wavFile, string mmlOutput, string bpm)
    {
        // ���̽� ��ũ��Ʈ ����
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = PythonEXEPath;//@"C:\Users\HJJ\spleeter_env\Scripts\python.exe"; // Python ���� ���� ���
        start.Arguments = $"{pythonMMLScriptPath} {wavFile} {mmlOutput} {bpm}"; // ���ڷ� ����
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;

        Process process = new Process();
        process.StartInfo = start;
        process.Start();

        // ��� �α׸� ���
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        UnityEngine.Debug.Log("Python output: " + output);
        UnityEngine.Debug.Log("Python error: " + error);

        process.WaitForExit();
    }
}

[Serializable]
public class Data
{
    public string Python_spleeter_env_EXE;
}
