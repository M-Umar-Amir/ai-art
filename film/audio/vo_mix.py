# Voiceover on top of the score: place each VO file at its section start, duck the score under speech, write build/audio.wav.
import sys, os, json, subprocess, numpy as np
from scipy.io import wavfile
from scipy.signal import butter, sosfilt
SR = 44100
B = json.load(open('src/beats.json'))
FF = os.environ.get('FFMPEG', 'ffmpeg')
def load(path):
    raw = subprocess.run([FF, '-v', 'error', '-i', path, '-ac', '1', '-ar', str(SR), '-f', 'f32le', '-'], capture_output=True, check=True).stdout
    return np.frombuffer(raw, np.float32).astype(np.float64)
sr, score = wavfile.read('build/score.wav'); score = score.astype(np.float64).T / 32768.0
N = score.shape[1]; vo = np.zeros(N)
for i, f in enumerate(B['voFiles']):
    s = load(f); a = int(B['sec'][i] * SR); vo[a:a + len(s)] += s[:max(0, N - a)]
vo = sosfilt(butter(2, 70, 'high', fs=SR, output='sos'), vo)
# speech envelope -> duck the score by up to 10 dB while the voice is talking (smooth attack/release)
env = np.abs(vo); env = sosfilt(butter(1, 3, 'low', fs=SR, output='sos'), env); env = np.clip(env / (np.percentile(env[env > 1e-4], 90) + 1e-9), 0, 1)
duck = 10 ** (-10 * env / 20)
vo_rms = np.sqrt(np.mean(vo[np.abs(vo) > 1e-3] ** 2)); sc_rms = np.sqrt(np.mean(score ** 2))
score = score / sc_rms * vo_rms * 10 ** (-9 / 20)        # bed sits ~9 dB under the voice
mix = score * duck + vo[None, :] * 1.0
mix = mix / np.abs(mix).max() * 0.95
wavfile.write('build/audio.wav', SR, (mix.T * 32000).astype(np.int16))
print('wrote build/audio.wav', N / SR, 's')
