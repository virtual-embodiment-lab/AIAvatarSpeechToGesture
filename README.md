# AI Avatar Speech-To-Gesture

## Features
- **Live recording** via `Microphone.Start` (triggered by the **R** from keyboard).
- **Transcription** using OpenAI’s Speech-To-Text API.
- **Keyword-based sentiment** detection (`positive` / `negative` keyword lists).
- **Audio playback**  with lip-sync after `Microphone.End`

## Setup

1. **Install**  
   - [OpenAI C# SDK](https://github.com/OpenAI/openai-dotnet)
   - SALSA LipSync Suite

1. **Add to Unity**  
   - Attach `AvatarSpeechToGesture.cs` to a GameObject with an **Animator** (parameters: `Agree`, `Disagree`; default: `Idle`).
   - Add Audio Source to the GameObject (avatar)
   - Add SALSA, Amplitude, AmplitudeSALSA, and Queue Processor to the GameOject (avatar) for lip-sync

3. **Inspector**  
   - **API Key**: Enter your OpenAI key  
   - **Mic Device**: Defaults to the first available microphone

## Tips

- **Gesture Animation** Check your Animator has the right bool names and transitions. You can modify or add additional animations as needed. 
- **Customize Keywords/Context** You can change the keyword lists in the script.

