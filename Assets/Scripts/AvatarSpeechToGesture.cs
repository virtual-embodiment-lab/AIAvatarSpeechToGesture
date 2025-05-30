// Script by Yejoon. Let me know if you have any questions!
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using OpenAI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class AvatarSpeechToGesture : MonoBehaviour
{
    [Header("Recording Settings")]
    public string microphoneDevice;
    public int recordingLengthSeconds = 20;
    public int samplingRate = 44100;

    [Header("OpenAI Settings")]
    [Tooltip("OpenAI API key (set in Inspector)")]
    public string openAiApiKey;
    private OpenAIApi openai;

    [Header("Animation Settings")]
    [Tooltip("Animator with boolean parameters 'Agree' and 'Disagree', default Idle state.")]
    public Animator avatarAnimator;
    [Tooltip("Seconds per animation playthrough")]
    public float animationDuration = 1.0f;

    private AudioSource audioSource;
    private AudioClip recordingClip;
    private bool isRecording = false;
    private bool isProcessing = false;

    void Start()
    {
        // Initialize OpenAI client
        openai = new OpenAIApi(openAiApiKey);

        // Pick default mic if none set
        if (string.IsNullOrEmpty(microphoneDevice) && Microphone.devices.Length > 0)
            microphoneDevice = Microphone.devices[0];

        // Grab AudioSource and disable playOnAwake
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isProcessing)
            BeginRecording();

        if (Input.GetKeyUp(KeyCode.R) && isRecording)
            EndRecordingAndProcess();
    }

    void BeginRecording()
    {
        if (Microphone.IsRecording(microphoneDevice))
            Microphone.End(microphoneDevice);

        recordingClip = Microphone.Start(microphoneDevice, false, recordingLengthSeconds, samplingRate);
        isRecording = true;
        Debug.Log("Recording started…");
    }

    async void EndRecordingAndProcess()
    {
        isRecording = false;
        isProcessing = true;

        // Stop mic 
        Microphone.End(microphoneDevice);
        await Task.Yield();
        while (Microphone.IsRecording(microphoneDevice))
            await Task.Yield();

        // Save to WAV with unique filename
        string filename = $"speech_{DateTime.Now:yyyyMMdd_HHmmssfff}.wav";
        string fullPath = Path.Combine(Application.persistentDataPath, filename);
        SavWav.Save(fullPath, recordingClip);
        Debug.Log($"Saved WAV to: {fullPath}");

        try
        {
            // Speech-to-Text via Whisper
            var tReq = new CreateAudioTranscriptionsRequest {
                File = fullPath,
                Model = "whisper-1",
                Language = "en"
            };
            var tRes = await openai.CreateAudioTranscription(tReq);
            string userText = tRes.Text.Trim();
            Debug.Log($"Transcribed: {userText}");

            // Decision based on keywords
            string lower = userText.ToLowerInvariant();
            string decision;
            var negatives = new[] {
                "no", "nope", "never", "not really",
                "disagree", "don't like", "dislike",
                "hate", "cannot stand", "can't stand",
                "oppose", "object", "reject", "refuse",
                "unacceptable", "no way", "nah",
                "no thanks", "not a fan", "unfortunately"
            };
            var positives = new[] {
                "yes", "yeah", "yep", "sure", "agree",
                "absolutely", "definitely", "certainly", "affirmative",
                "like", "love", "enjoy",
                "awesome", "great", "fantastic", "wonderful",
                "excellent", "superb", "sounds good", "ok", "okay",
                "alright", "gladly", "fine", "perfect"
            };

            if (negatives.Any(k => lower.Contains(k)))
                decision = "Disagree";
            else if (positives.Any(k => lower.Contains(k)))
                decision = "Agree";
            else
            {
                // GPT fallback
                var chatReq = new CreateChatCompletionRequest {
                    Model = "gpt-4",
                    Messages = new List<ChatMessage> {
                        new ChatMessage { Role = "system", Content =
                            "You are an avatar controller. If the user's speech expresses agreement, reply exactly 'Agree'. " +
                            "If it expresses disagreement, reply exactly 'Disagree'. Otherwise reply exactly 'Idle'." },
                        new ChatMessage { Role = "user", Content = userText }
                    }
                };
                var chatRes = await openai.CreateChatCompletion(chatReq);
                decision = chatRes.Choices[0].Message.Content.Trim();
            }

            Debug.Log($"Decision: {decision}");

            // Play animation then audio, and ready quickly
            StartCoroutine(PlayAnimationThenAudio(decision));
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during processing: {e}");
            isProcessing = false;
        }
    }

    private IEnumerator PlayAnimationThenAudio(string decision)
    {
        // Play gesture animation
        if (decision == "Agree" || decision == "Disagree")
        {
            for (int i = 0; i < 2; i++)
            {
                avatarAnimator.SetBool(decision, false);
                yield return null;
                avatarAnimator.SetBool(decision, true);
                yield return new WaitForSeconds(animationDuration);
                avatarAnimator.SetBool(decision, false);
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Allow immediately for next recording
        isProcessing = false;
        Debug.Log("Ready for next recording.");

        // Then play back the recorded audio (in background)
        audioSource.clip = recordingClip;
        audioSource.Play();
        Debug.Log("Playing back recorded audio…");

        yield break;
    }
}
