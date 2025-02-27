import sys
import os
import tensorflow as tf
import gc  # 🔥 추가: 메모리 정리
from spleeter.separator import Separator

os.environ['FFMPEG_BINARY'] = r'C:\venvs\ffmpeg\bin\ffmpeg.exe'
os.environ['PATH'] += r';C:\venvs\ffmpeg\bin'

tf.config.threading.set_inter_op_parallelism_threads(1)
tf.config.threading.set_intra_op_parallelism_threads(1)
tf.get_logger().setLevel('ERROR')

unity_output_path = os.path.join(os.getcwd(), "Assets", "StreamingAssets", "spleeter_output")

def separate_audio(audio_file_path):
    """ 오디오를 Spleeter로 분리 """
    separator = Separator('spleeter:5stems', multiprocess=False)  # ⬅️ multiprocessing 비활성화
    separator.separate_to_file(audio_file_path, unity_output_path)
    
    # 🔥 TensorFlow 그래프 정리 대신 Python 가비지 컬렉션 실행
    del separator
    gc.collect()  # 메모리 해제

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("사용법: python script.py <오디오 파일 경로>")
        sys.exit(1)

    audio_file_path = sys.argv[1]
    
    if not os.path.exists(audio_file_path):
        print(f"오류: 파일 {audio_file_path}이(가) 존재하지 않습니다.")
        sys.exit(1)

    separate_audio(audio_file_path)

    # 🔥 TensorFlow 프로세스를 완전히 종료하는 코드
    os._exit(0)  # ⬅️ 강제 종료
