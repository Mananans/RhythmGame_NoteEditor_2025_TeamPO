//using System;
//using System.Collections.Generic;
//using System.IO;
//using UnityEngine;

//public class MP3InstrumentMMLConverter : MonoBehaviour
//{
//    public AudioClip audioClip;
//    public string mmlOutputPath = "InstrumentMML_Output.txt";

//    void Start()
//    {
//        if (audioClip == null)
//        {
//            Debug.LogError("Please assign an AudioClip.");
//            return;
//        }

//        ConvertToMML(audioClip);
//    }

//    void ConvertToMML(AudioClip clip)
//    {
//        // 1. Extract audio data
//        float[] audioData = new float[clip.samples * clip.channels];
//        clip.GetData(audioData, 0);

//        // 2. Simulate instrument separation (placeholder)
//        Dictionary<string, float[]> separatedInstruments = SeparateInstruments(audioData);

//        // 3. Analyze and convert each instrument
//        Dictionary<string, string> mmlCodes = new Dictionary<string, string>();
//        foreach (var instrument in separatedInstruments)
//        {
//            List<MMLNote> notes = AnalyzeAudio(instrument.Value, clip.frequency);
//            string mmlCode = GenerateMML(notes);
//            mmlCodes[instrument.Key] = mmlCode;
//        }

//        // 4. Save MML to file
//        SaveMMLToFile(mmlCodes);
//    }

//    Dictionary<string, float[]> SeparateInstruments(float[] audioData)
//    {
//        // Placeholder for instrument separation logic
//        // Replace this with a real source separation algorithm
//        return new Dictionary<string, float[]>
//        {
//            { "Piano", audioData }, // Simulated data
//            { "Drums", audioData },
//            { "Bass", audioData }
//        };
//    }

//    List<MMLNote> AnalyzeAudio(float[] audioData, int sampleRate)
//    {
//        List<MMLNote> notes = new List<MMLNote>();
//        int windowSize = 1024; // FFT window size
//        float[] window = new float[windowSize];

//        for (int i = 0; i < audioData.Length; i += windowSize)
//        {
//            if (i + windowSize >= audioData.Length) break;

//            Array.Copy(audioData, i, window, 0, windowSize);

//            // Perform FFT
//            float[] spectrum = PerformFFT(window);

//            // Find dominant frequency
//            float dominantFreq = FindDominantFrequency(spectrum, sampleRate, windowSize);

//            // Map frequency to a musical note
//            string note = FrequencyToNote(dominantFreq);
//            notes.Add(new MMLNote { Note = note, Duration = 1 }); // Default duration for simplicity
//        }

//        return notes;
//    }

//    float[] PerformFFT(float[] data)
//    {
//        // Apply FFT (Use a library like Math.NET Numerics for this)
//        return new float[data.Length / 2];
//    }

//    float FindDominantFrequency(float[] spectrum, int sampleRate, int windowSize)
//    {
//        int maxIndex = 0;
//        float maxValue = 0f;

//        for (int i = 0; i < spectrum.Length; i++)
//        {
//            if (spectrum[i] > maxValue)
//            {
//                maxValue = spectrum[i];
//                maxIndex = i;
//            }
//        }

//        return (maxIndex * sampleRate) / (float)windowSize;
//    }

//    string FrequencyToNote(float frequency)
//    {
//        if (frequency <= 0) return "R"; // Rest for invalid frequencies

//        string[] notes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
//        int A4 = 440; // Frequency of A4
//        int semitonesFromA4 = Mathf.RoundToInt(12 * Mathf.Log(frequency / A4, 2));
//        int noteIndex = (semitonesFromA4 + 69) % 12;

//        return notes[noteIndex];
//    }

//    string GenerateMML(List<MMLNote> notes)
//    {
//        string mml = "";

//        foreach (var note in notes)
//        {
//            mml += $"{note.Note}{note.Duration} ";
//        }

//        return mml;
//    }

//    void SaveMMLToFile(Dictionary<string, string> mmlCodes)
//    {
//        using (StreamWriter writer = new StreamWriter(mmlOutputPath))
//        {
//            foreach (var mml in mmlCodes)
//            {
//                writer.WriteLine($"Instrument: {mml.Key}");
//                writer.WriteLine(mml.Value);
//                writer.WriteLine();
//            }
//        }

//        Debug.Log($"MML code saved to: {mmlOutputPath}");
//    }
//}

//public class MMLNote
//{
//    public string Note { get; set; }
//    public int Duration { get; set; }
//}
