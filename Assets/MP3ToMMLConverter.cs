using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System.Collections;

public class MP3ToMMLConverter : MonoBehaviour
{
    public AudioClip audioClip; // 변환할 AudioClip
    public string mmlOutputPath = "MML_Output.txt"; // 저장될 MML 파일 경로

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
        // 오디오 클립이 완전히 로드되었는지 확인
        while (clip.loadState != AudioDataLoadState.Loaded)
        {
            yield return null; // 오디오 클립이 로드될 때까지 대기
        }

        // AudioClip 로드 완료 후 처리
        ConvertToMML(clip);
    }

    void ConvertToMML(AudioClip clip)
    {
        try
        {
            // 오디오 데이터를 추출
            float[] audioData = new float[clip.samples * clip.channels];
            clip.GetData(audioData, 0);

            // FFT를 수행하고 주파수 분석
            List<MMLNote> notes = AnalyzeAudio(audioData, clip.frequency);

            // 디버깅: 분석된 노트 출력
            foreach (var note in notes)
            {
                Debug.Log($"Note: {note.Note}, Duration: {note.Duration}");
            }

            // MML 코드 생성
            string mmlCode = GenerateMML(notes);
            Debug.Log($"Generated MML: {mmlCode}");

            //// 파일 저장
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

        // 분석 및 MML 생성
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
        int windowSize = 1024; // FFT 윈도우 크기
        float[] window = new float[windowSize];

        for (int i = 0; i < audioData.Length; i += windowSize)
        {
            if (i + windowSize >= audioData.Length) break;

            Array.Copy(audioData, i, window, 0, windowSize);

            // FFT 계산
            float[] spectrum = PerformFFT(window);

            // 주요 주파수 찾기
            float dominantFreq = FindDominantFrequency(spectrum, sampleRate, windowSize);

            // 주파수를 노트로 변환
            string note = FrequencyToNote(dominantFreq);

            // 디버깅: 주파수와 노트 출력
            Debug.Log($"Frequency: {dominantFreq}, Note: {note}");

            notes.Add(new MMLNote { Note = note, Duration = 1 }); // 기본 길이 설정
        }

        return notes;
    }

    float[] PerformFFT(float[] data)
    {
        // 복소수 배열 준비
        var complexData = new System.Numerics.Complex[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            complexData[i] = new System.Numerics.Complex(data[i], 0);
        }

        // FFT 수행
        Fourier.Forward(complexData, FourierOptions.Matlab);

        // 스펙트럼 크기 계산
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
        float threshold = 0.01f; // 임계값 설정

        for (int i = 0; i < spectrum.Length; i++)
        {
            if (spectrum[i] > maxValue && spectrum[i] > threshold) // 임계값 추가
            {
                maxValue = spectrum[i];
                maxIndex = i;
            }
        }

        return (maxIndex * sampleRate) / (float)windowSize;
    }

    string FrequencyToNote(float frequency)
    {
        if (frequency <= 0) return "R"; // 유효하지 않은 주파수는 Rest로 처리

        string[] notes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int A4 = 440; // A4 음의 주파수
        int semitonesFromA4 = Mathf.RoundToInt(12 * Mathf.Log(frequency / A4, 2));
        int noteIndex = (semitonesFromA4 + 69) % 12; // 노트 인덱스 계산

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
    public string Note { get; set; } // 음표
    public int Duration { get; set; } // 길이
}
