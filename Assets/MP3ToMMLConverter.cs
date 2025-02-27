using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System.Collections;

public class MP3ToMMLConverter : MonoBehaviour
{
    public AudioClip audioClip; // ��ȯ�� AudioClip
    public string mmlOutputPath = "MML_Output.txt"; // ����� MML ���� ���

    void Start()
    {
        if (audioClip == null)
        {
            Debug.LogError("Please assign an AudioClip.");
            return;
        }
        StartCoroutine(WaitForAudioClipLoad(audioClip));
    }


    IEnumerator WaitForAudioClipLoad(AudioClip clip)
    {
        // ����� Ŭ���� ������ �ε�Ǿ����� Ȯ��
        while (clip.loadState != AudioDataLoadState.Loaded)
        {
            yield return null; // ����� Ŭ���� �ε�� ������ ���
        }

        // AudioClip �ε� �Ϸ� �� ó��
        ConvertToMML(clip);
    }

    void ConvertToMML(AudioClip clip)
    {
        try
        {
            // ����� �����͸� ����
            float[] audioData = new float[clip.samples * clip.channels];
            clip.GetData(audioData, 0);

            // FFT�� �����ϰ� ���ļ� �м�
            List<MMLNote> notes = AnalyzeAudio(audioData, clip.frequency);

            // �����: �м��� ��Ʈ ���
            foreach (var note in notes)
            {
                Debug.Log($"Note: {note.Note}, Duration: {note.Duration}");
            }

            // MML �ڵ� ����
            string mmlCode = GenerateMML(notes);
            Debug.Log($"Generated MML: {mmlCode}");

            //// ���� ����
            //string path = Path.Combine(Application.persistentDataPath, mmlOutputPath);
            //File.WriteAllText(path, mmlCode);
            //Debug.Log($"MML code saved to: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during MML conversion: {ex.Message}\n{ex.StackTrace}");
        }
    }

    IEnumerator ConvertToMMLCoroutine(AudioClip clip)
    {
        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogError("AudioClip is not fully loaded.");
            yield break;
        }

        float[] audioData = new float[clip.samples * clip.channels];
        clip.GetData(audioData, 0);

        // �м� �� MML ����
        List<MMLNote> notes = AnalyzeAudio(audioData, clip.frequency);
        string mmlCode = GenerateMML(notes);

        string path = Path.Combine(Application.persistentDataPath, mmlOutputPath);
        File.WriteAllText(path, mmlCode);
        Debug.Log($"MML code saved to: {path}");

        Debug.Log("MML conversion completed.");
    }


    List<MMLNote> AnalyzeAudio(float[] audioData, int sampleRate)
    {
        List<MMLNote> notes = new List<MMLNote>();
        int windowSize = 1024; // FFT ������ ũ��
        float[] window = new float[windowSize];

        for (int i = 0; i < audioData.Length; i += windowSize)
        {
            if (i + windowSize >= audioData.Length) break;

            Array.Copy(audioData, i, window, 0, windowSize);

            // FFT ���
            float[] spectrum = PerformFFT(window);

            // �ֿ� ���ļ� ã��
            float dominantFreq = FindDominantFrequency(spectrum, sampleRate, windowSize);

            // ���ļ��� ��Ʈ�� ��ȯ
            string note = FrequencyToNote(dominantFreq);

            // �����: ���ļ��� ��Ʈ ���
            Debug.Log($"Frequency: {dominantFreq}, Note: {note}");

            notes.Add(new MMLNote { Note = note, Duration = 1 }); // �⺻ ���� ����
        }

        return notes;
    }

    float[] PerformFFT(float[] data)
    {
        // ���Ҽ� �迭 �غ�
        var complexData = new System.Numerics.Complex[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            complexData[i] = new System.Numerics.Complex(data[i], 0);
        }

        // FFT ����
        Fourier.Forward(complexData, FourierOptions.Matlab);

        // ����Ʈ�� ũ�� ���
        float[] spectrum = new float[complexData.Length / 2];
        for (int i = 0; i < spectrum.Length; i++)
        {
            spectrum[i] = (float)complexData[i].Magnitude;
        }

        return spectrum;
    }

    float FindDominantFrequency(float[] spectrum, int sampleRate, int windowSize)
    {
        int maxIndex = 0;
        float maxValue = 0f;
        float threshold = 0.01f; // �Ӱ谪 ����

        for (int i = 0; i < spectrum.Length; i++)
        {
            if (spectrum[i] > maxValue && spectrum[i] > threshold) // �Ӱ谪 �߰�
            {
                maxValue = spectrum[i];
                maxIndex = i;
            }
        }

        return (maxIndex * sampleRate) / (float)windowSize;
    }

    string FrequencyToNote(float frequency)
    {
        if (frequency <= 0) return "R"; // ��ȿ���� ���� ���ļ��� Rest�� ó��

        string[] notes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int A4 = 440; // A4 ���� ���ļ�
        int semitonesFromA4 = Mathf.RoundToInt(12 * Mathf.Log(frequency / A4, 2));
        int noteIndex = (semitonesFromA4 + 69) % 12; // ��Ʈ �ε��� ���

        return notes[noteIndex];
    }

    string GenerateMML(List<MMLNote> notes)
    {
        string mml = "";

        foreach (var note in notes)
        {
            mml += $"{note.Note}{note.Duration} ";
        }

        return mml.Trim();
    }
}

public class MMLNote
{
    public string Note { get; set; } // ��ǥ
    public int Duration { get; set; } // ����
}
