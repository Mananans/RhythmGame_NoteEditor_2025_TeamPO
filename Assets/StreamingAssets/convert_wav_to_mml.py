import librosa
import numpy as np
import os
import json

def convert_wav_to_mml(wav_file, mml_file, bpm):
    if not os.path.exists(wav_file):
        print(f"Error: {wav_file} not found.")
        return
    
    y, sr = librosa.load(wav_file, sr=None)
    onset_env = librosa.onset.onset_strength(y=y, sr=sr)
    
    # 피치(음 높이) 감지
    pitches, magnitudes = librosa.piptrack(y=y, sr=sr)
    detected_notes = []
    
    frame_time = librosa.frames_to_time(1, sr=sr)  # 한 프레임당 시간 계산
    current_note = None
    current_duration = 0
    note_sequence = []
    
    instrument_name = os.path.splitext(os.path.basename(wav_file))[0]
    
    # BPM 기반 음표 길이 기준 설정
    quarter_note_time = 60.0 / bpm  # 4분음표(l4)의 길이 (초)
    note_durations = {
        "l16": quarter_note_time / 4,  # 16분음표
        "l8": quarter_note_time / 2,   # 8분음표
        "l4": quarter_note_time,       # 4분음표
        "l2": quarter_note_time * 2,   # 2분음표
        "l1": quarter_note_time * 4    # 온음표
    }
    
    for t in range(pitches.shape[1]):
        index = np.argmax(magnitudes[:, t])
        pitch = pitches[index, t]
        
        if pitch > 0:
            note = librosa.hz_to_note(pitch)
            
            if note == current_note:
                current_duration += frame_time  # 동일한 음이면 지속 시간 증가
            else:
                if current_note is not None:
                    note_sequence.append({"instrument": instrument_name, "value": current_note, "length": current_duration})
                current_note = note
                current_duration = frame_time
        else:
            if current_note is not None:
                note_sequence.append({"instrument": instrument_name, "value": current_note, "length": current_duration})
                current_note = None
                current_duration = 0
            
            if not note_sequence or note_sequence[-1]["value"] != "r":
                note_sequence.append({"instrument": instrument_name, "value": "r", "length": frame_time})
            else:
                note_sequence[-1]["length"] += frame_time
    
    if current_note is not None:
        note_sequence.append({"instrument": instrument_name, "value": current_note, "length": current_duration})
    
    # JSON 변환 (BPM에 맞춰 음 길이 적용)
    for note in note_sequence:
        duration = note["length"]
        if duration < note_durations["l16"]:
            note["length"] = "l16"
        elif duration < note_durations["l8"]:
            note["length"] = "l8"
        elif duration < note_durations["l4"]:
            note["length"] = "l4"
        elif duration < note_durations["l2"]:
            note["length"] = "l2"
        else:
            note["length"] = f"l1 ({duration:.2f}s)"  # 4박자 초과는 직접 초 포함
    
    with open(mml_file, "w", encoding="utf-8") as f:
        json.dump(note_sequence, f, ensure_ascii=False, indent=4)
    print(f"MML file saved: {mml_file}")

if __name__ == "__main__":
    import sys
    if len(sys.argv) != 4:
        print("Usage: python convert_wav_to_mml.py <wav_file> <mml_file> <bpm>")
    else:
        wav_file = sys.argv[1]
        mml_file = sys.argv[2]
        bpm = float(sys.argv[3])  # BPM을 외부에서 입력받음
        convert_wav_to_mml(wav_file, mml_file, bpm)
