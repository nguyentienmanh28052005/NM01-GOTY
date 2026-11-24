"""Kiểm tra WAV xuất từ export_audio.py: đủ file, đúng định dạng, không câm, không vỡ tiếng."""
import os, struct, sys, wave, json
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), 'out', 'audio')
EXPECT = 36 + 6
def check():
    fs = [f for f in os.listdir(OUT) if f.endswith('.wav')]
    assert len(fs) == EXPECT, f'{len(fs)} != {EXPECT}'
    bad = []
    for f in fs:
        w = wave.open(os.path.join(OUT, f)); assert w.getnchannels() == 2 and w.getsampwidth() == 2 and w.getframerate() == 44100, f
        n = w.getnframes(); data = struct.unpack('<%dh' % (n * 2), w.readframes(n))
        peak = max(abs(x) for x in data) / 32767; clip = sum(1 for x in data if abs(x) >= 32766) / len(data)
        if peak < 0.005: bad.append((f, 'câm', peak))
        if clip > 0.01: bad.append((f, 'vỡ tiếng', clip))
    assert not bad, bad
check(); print('PASS', EXPECT, 'files')
