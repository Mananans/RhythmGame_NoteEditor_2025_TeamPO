import sys
import librosa
import numpy as np

def extract_tempo_and_time_signature_from_mp3(mp3_file):
    # MP3 파일 로드
    y, sr = librosa.load(mp3_file, sr=None)

    # 템포 추출
    tempo, _ = librosa.beat.beat_track(y=y, sr=sr)

    # 템포가 0이면 기본 템포 설정
    if tempo == 0:
        tempo = 120  # 기본 템포는 120으로 설정

    # 노래의 전체 길이 (초 단위)
    duration = librosa.get_duration(y=y, sr=sr)

    # 전체 곡의 박자 계산 (1분이 60초니까)
    total_beats = tempo * (duration / 60)  # 전체 비트 수

    # 비트 위치 추출
    onset_env = librosa.onset.onset_strength(y=y, sr=sr)
    intervals = librosa.frames_to_time(librosa.onset.onset_detect(onset_envelope=onset_env, sr=sr), sr=sr)

    # 비트 간격 (초)
    beat_intervals = np.diff(intervals)
    avg_beat_interval = np.mean(beat_intervals)

    # 박자 추정 (가장 빈번한 박자 패턴을 찾음)
    if avg_beat_interval < 0.5:
        time_signature = "4/4"  # 4/4 박자
    elif avg_beat_interval < 0.75:
        time_signature = "3/4"  # 3/4 박자
    elif avg_beat_interval < 1.0:
        time_signature = "6/8"  # 6/8 박자
    elif avg_beat_interval < 1.25:
        time_signature = "2/4"  # 2/4 박자
    elif avg_beat_interval < 1.5:
        time_signature = "2/2"  # 2/2 박자
    elif avg_beat_interval < 1.75:
        time_signature = "9/8"  # 9/8 박자
    elif avg_beat_interval < 2.0:
        time_signature = "12/8"  # 12/8 박자
    elif avg_beat_interval < 2.25:
        time_signature = "3/8"  # 3/8 박자
    elif avg_beat_interval < 2.5:
        time_signature = "5/4"  # 5/4 박자
    elif avg_beat_interval < 3.0:
        time_signature = "7/4"  # 7/4 박자
    elif avg_beat_interval < 3.5:
        time_signature = "11/4"  # 11/4 박자
    else:
        time_signature = "4/4"  # 기본적으로 4/4로 설정

    # 박자 계산 (1분이 60초니까)
    bars = total_beats // 4  # 기본 4/4 박자 기준
    beats_in_last_bar = total_beats % 4

    return tempo, duration, bars, beats_in_last_bar, time_signature

def save_tempo_and_duration_to_file(tempo, duration, bars, beats_in_last_bar, time_signature, output_file):
    # 템포와 박자 정보를 파일에 저장
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(f"Tempo: {tempo} BPM\n")
        f.write(f"Duration: {duration:.2f} seconds\n")
        f.write(f"Total bars (4/4): {int(bars)} bars\n")
        f.write(f"Beats in the last bar: {int(beats_in_last_bar)} beats\n")
        f.write(f"Time Signature: {time_signature}\n")
    print(f"템포와 박자 정보가 '{output_file}' 파일에 저장되었습니다.")

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("사용법: python extract_tempo_and_duration_from_mp3.py <mp3_file> <output_file>")
        sys.exit(1)

    mp3_file = sys.argv[1]
    output_file = sys.argv[2]
    
    # MP3 파일에서 템포 및 박자 정보 추출
    tempo, duration, bars, beats_in_last_bar, time_signature = extract_tempo_and_time_signature_from_mp3(mp3_file)
    
    # 템포와 박자 정보를 파일로 저장
    save_tempo_and_duration_to_file(tempo, duration, bars, beats_in_last_bar, time_signature, output_file)
