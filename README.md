# AI Avatar Speech-To-Gesture

## Features
- **Live recording** via `Microphone.Start` (triggered by the **R** from keyboard).
- **Transcription** using OpenAI’s Speech-To-Text API.
- **Keyword-based sentiment** detection (`positive` / `negative` keyword lists).
- **Audio playback**  with lip-sync after `Microphone.End`

## Demo

<video src="Demo_STG.mp4" controls loop muted width="640" height="360">
  ⚠️ Your browser does not support HTML5 video.
</video>

## Setup

1. **Install**  
   - [OpenAI C# SDK](https://github.com/OpenAI/openai-dotnet)
   - SALSA LipSync Suite

1. **Add to Unity**  
   - Attach `AvatarSpeechToGesture.cs` to a GameObject with an **Animator** (parameters: `Agree`, `Disagree`; defalt: `Idle`).
   - Add Audio Source to the GameObject(avatar)
   - Add SALSA, Amplitude, AmplitudeSALSA, and Queue Processor to the GameOject(avatar)

3. **Inspector**  
   - **API Key**: your OpenAI key  
   - **Mic Device**: defaults to first

## Tips

- **Gesture Animation** Check your Animator has the right bool names and transitions. You can change or add more animations. 
- **Customize** You can change the keyword lists in the script.

