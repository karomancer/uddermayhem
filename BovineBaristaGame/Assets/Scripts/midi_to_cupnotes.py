#!/usr/bin/env python3
"""
Convert MIDI file to CupNotes JSON format for Bovine Barista.

Usage:
    python midi_to_cupnotes.py input.mid output.json [--bpm 120] [--map C4=FrontLeft,D4=FrontRight,E4=BackLeft,F4=BackRight]

Requirements:
    pip install mido
"""

import argparse
import json
import mido
from collections import defaultdict

# Default pitch to cup type mapping (MIDI note numbers)
# You can customize this based on your MIDI setup
DEFAULT_PITCH_MAP = {
    60: "FrontLeft",   # C4
    62: "FrontRight",  # D4
    64: "BackLeft",    # E4
    65: "BackRight",   # F4
}

# Note name to MIDI number lookup
NOTE_TO_MIDI = {
    'C': 0, 'D': 2, 'E': 4, 'F': 5, 'G': 7, 'A': 9, 'B': 11
}

def parse_note_name(note_str):
    """Convert note name like 'C4' or 'F#3' to MIDI number."""
    note_str = note_str.strip().upper()

    # Handle sharps/flats
    base_note = note_str[0]
    offset = 0
    octave_start = 1

    if len(note_str) > 1 and note_str[1] == '#':
        offset = 1
        octave_start = 2
    elif len(note_str) > 1 and note_str[1] == 'B':
        offset = -1
        octave_start = 2

    octave = int(note_str[octave_start:])
    midi_num = (octave + 1) * 12 + NOTE_TO_MIDI[base_note] + offset
    return midi_num

def parse_pitch_map(map_str):
    """Parse pitch map string like 'C4=FrontLeft,D4=FrontRight,...'"""
    pitch_map = {}
    for pair in map_str.split(','):
        note, cup_type = pair.split('=')
        midi_num = parse_note_name(note)
        pitch_map[midi_num] = cup_type.strip()
    return pitch_map

def ticks_to_measure_beat(ticks, ticks_per_beat, beats_per_measure=4):
    """Convert MIDI ticks to measure and beat (1-indexed)."""
    total_beats = ticks / ticks_per_beat
    measure = int(total_beats // beats_per_measure) + 1
    beat = (total_beats % beats_per_measure) + 1
    return measure, beat

def duration_in_beats(ticks, ticks_per_beat):
    """Convert tick duration to beats."""
    return ticks / ticks_per_beat

def convert_midi_to_cupnotes(midi_path, pitch_map, beats_per_measure=4, measure_offset=0):
    """Convert MIDI file to CupNotes format."""
    mid = mido.MidiFile(midi_path)
    ticks_per_beat = mid.ticks_per_beat

    # Track active notes (pitch -> start_tick)
    active_notes = {}
    # Collected notes
    notes = []

    current_tick = 0

    # Process all tracks
    for track in mid.tracks:
        current_tick = 0
        for msg in track:
            current_tick += msg.time

            if msg.type == 'note_on' and msg.velocity > 0:
                # Note started
                if msg.note in pitch_map:
                    active_notes[msg.note] = current_tick

            elif msg.type == 'note_off' or (msg.type == 'note_on' and msg.velocity == 0):
                # Note ended
                if msg.note in pitch_map and msg.note in active_notes:
                    start_tick = active_notes.pop(msg.note)
                    duration_ticks = current_tick - start_tick

                    measure, beat = ticks_to_measure_beat(start_tick, ticks_per_beat, beats_per_measure)
                    duration = duration_in_beats(duration_ticks, ticks_per_beat)

                    # Apply measure offset
                    measure += measure_offset

                    # Skip notes that end up before measure 1
                    if measure < 1:
                        continue

                    # Round duration to nearest 0.5
                    duration = round(duration * 2) / 2
                    if duration < 0.5:
                        duration = 0.5

                    notes.append({
                        "type": pitch_map[msg.note],
                        "measure": measure,
                        "beat": round(beat, 2),
                        "duration": duration
                    })

    # Sort by measure, then beat
    notes.sort(key=lambda n: (n["measure"], n["beat"]))

    return {"notes": notes}

def midi_num_to_note_name(midi_num):
    """Convert MIDI number to note name like C4."""
    note_names = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B']
    octave = (midi_num // 12) - 1
    note = note_names[midi_num % 12]
    return f"{note}{octave}"

def scan_midi(midi_path):
    """Scan MIDI file and report all pitches used."""
    mid = mido.MidiFile(midi_path)
    pitches = defaultdict(int)

    for track_idx, track in enumerate(mid.tracks):
        print(f"Track {track_idx}: {track.name if track.name else '(unnamed)'}")
        for msg in track:
            if msg.type == 'note_on' and msg.velocity > 0:
                pitches[msg.note] += 1

    print(f"\nPitches found in MIDI file:")
    for pitch in sorted(pitches.keys()):
        note_name = midi_num_to_note_name(pitch)
        print(f"  MIDI {pitch} ({note_name}): {pitches[pitch]} notes")

    return pitches

def main():
    parser = argparse.ArgumentParser(description='Convert MIDI to CupNotes JSON')
    parser.add_argument('input', help='Input MIDI file')
    parser.add_argument('output', nargs='?', help='Output JSON file (optional if using --scan)')
    parser.add_argument('--scan', action='store_true', help='Scan MIDI file and show pitches used')
    parser.add_argument('--bpm', type=float, default=120, help='BPM (for reference, not used in conversion)')
    parser.add_argument('--map', dest='pitch_map',
                        default='C4=FrontLeft,D4=FrontRight,E4=BackLeft,F4=BackRight',
                        help='Pitch to cup type mapping (e.g., C4=FrontLeft,D4=FrontRight,...)')
    parser.add_argument('--beats-per-measure', type=int, default=4, help='Beats per measure (default: 4)')
    parser.add_argument('--measure-offset', type=int, default=0, help='Offset to add to measure numbers (use -1 to shift notes earlier by 1 measure)')

    args = parser.parse_args()

    # Scan mode - just show what's in the file
    if args.scan:
        scan_midi(args.input)
        return

    if not args.output:
        print("Error: output file required (or use --scan to inspect MIDI)")
        return

    # Parse pitch map
    pitch_map = parse_pitch_map(args.pitch_map)
    print(f"Using pitch map: {pitch_map}")

    # Convert
    cupnotes = convert_midi_to_cupnotes(args.input, pitch_map, args.beats_per_measure, args.measure_offset)

    # Write output
    with open(args.output, 'w') as f:
        json.dump(cupnotes, f, indent=2)

    print(f"Converted {len(cupnotes['notes'])} notes to {args.output}")

    # Preview first few notes
    print("\nFirst 5 notes:")
    for note in cupnotes['notes'][:5]:
        print(f"  {note}")

if __name__ == '__main__':
    main()
