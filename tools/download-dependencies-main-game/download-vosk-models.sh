#!/bin/sh

old_dir=$(pwd)

declare -A voskmodels=(
    ["vosk-model-small-en-us-0.15"]="https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip"
    ["vosk-model-small-de-0.15"]="https://alphacephei.com/vosk/models/vosk-model-small-de-0.15.zip"
    # ["vosk-model-small-es-0.42"]="https://alphacephei.com/vosk/models/vosk-model-small-es-0.42.zip"
    # ["vosk-model-small-fr-0.22"]="https://alphacephei.com/vosk/models/vosk-model-small-fr-0.22.zip"
    # ["vosk-model-small-ja-0.22"]="https://alphacephei.com/vosk/models/vosk-model-small-ja-0.22.zip"
    # ["vosk-model-small-cn-0.22"]="https://alphacephei.com/vosk/models/vosk-model-small-cn-0.22.zip"
)

cd "../../UltraStar Play/Assets/StreamingAssets"
echo "Removing old SpeechRecognitionModels folder..."
rm -rf SpeechRecognitionModels
mkdir SpeechRecognitionModels
cd SpeechRecognitionModels

for key in "${!voskmodels[@]}"
do
    value=${voskmodels[$key]}
    echo "Downloading $key from $value"
    wget -O "$key.zip" "$value"

    echo "Extracting $key.zip"
    unzip "$key.zip"

    echo "Removing $key.zip"
    rm "$key.zip"
done

cd "$old_dir"
echo "Downloading SpeechRecognitionModels done"
echo ""
