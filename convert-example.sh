#!/usr/bin/env bash
ffmpeg -i example.mkv \
    -vf "fps=15,scale=600:-1:flags=lanczos,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse" \
    -loop 0 example.gif