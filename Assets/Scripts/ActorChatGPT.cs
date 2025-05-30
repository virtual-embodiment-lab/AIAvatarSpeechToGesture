using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using OpenAI;

[RequireComponent(typeof(Animator))]
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

    private AudioClip recordingClip;
    private bool isRecording = false;
    private bool isProcessing = false;

    void Start()
    {
        openai = new OpenAIApi(openAiApiKey);
        if (string.IsNullOrEmpty(microphoneDevice) && Microphone.devices.Length > 0)
            microphoneDevice = Microphone.devices[0];
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
        Debug.Log("Recording…");
    }

    async void EndRecordingAndProcess()
    {
        isRecording = false;
        isProcessing = true;

        // 1) Stop mic and wait one frame for full release
        Microphone.End(microphoneDevice);
        await Task.Yield();
        while (Microphone.IsRecording(microphoneDevice))
            await Task.Yield();

        // 2) Save to WAV 
        string tempPath = Path.Combine(Application.persistentDataPath, "speech.wav");
        SavWav.Save(tempPath, recordingClip);
        Destroy(recordingClip);
        recordingClip = null;

        try
        {
            // 3) STT
            var tReq = new CreateAudioTranscriptionsRequest {
                File = tempPath, Model = "whisper-1", Language = "en"
            };
            var tRes = await openai.CreateAudioTranscription(tReq);
            string userText = tRes.Text.Trim();
            Debug.Log($"Transcribed: {userText}");

            // 4) Decision based on keywords
            string lower = userText.ToLowerInvariant();
            string decision;
            var negatives = new[] {
                "no", "disagree", "don't like", "dislike", "hate",
                "oppose", "object", "reject", "refuse",
                "no way", "can't stand", "detest", "loathe",
                "not a fan", "unacceptable", "no thanks", "nah"
            };

            var positives = new[] {
                "yes", "agree", "like", "love", "support", "approve",
                "enjoy", "enjoyable", "fantastic", "great", "awesome",
                "excellent", "wonderful", "superb", "sure", "definitely",
                "absolutely", "sounds good", "gladly", "OK", "okay",
                "yep", "totally", "enthusiastic"
            };


            if (negatives.Any(kw => lower.Contains(kw)))
            {
                decision = "Disagree";
                Debug.Log("Decision: Disagree");
            }
            else if (positives.Any(kw => lower.Contains(kw)))
            {
                decision = "Agree";
                Debug.Log("Decision: Agree");
            }
            else
            {
                // 5) GPT fallback
                var chatReq = new CreateChatCompletionRequest {
                    Model = "gpt-4",
                    Messages = new List<ChatMessage> {
                        new ChatMessage {
                            Role = "system",
                            Content =
                                "You are an avatar controller.\n" +
                                "If the user's speech expresses agreement, reply exactly 'Agree'.\n" +
                                "If it expresses disagreement, reply exactly 'Disagree'.\n" +
                                "Otherwise reply exactly 'Idle'."
                        },
                        new ChatMessage { Role = "user", Content = $"User said: \"{userText}\"" }
                    }
                };
                var chatRes = await openai.CreateChatCompletion(chatReq);
                decision = chatRes.Choices[0].Message.Content.Trim();
                Debug.Log($"GPT decision → {decision}");
            }

            // 6) Fire the appropriate boolean animation
            if (decision == "Agree" || decision == "Disagree")
            {
                StartCoroutine(PlayBoolAnimation(decision));
            }
            else
            {
                // Idle: reset both so the Animator stays in its default state
                avatarAnimator.SetBool("Agree", false);
                avatarAnimator.SetBool("Disagree", false);
                Debug.Log("Idle → remaining in default state");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during processing: {e}");
        }
        finally
        {
            // 7) Clean up
            if (File.Exists(tempPath))
                File.Delete(tempPath);

            isProcessing = false;
            Debug.Log("Ready for next recording.");
        }
    }

    private IEnumerator PlayBoolAnimation(string param)
    {
        for (int i = 0; i < 2; i++)
        {
            avatarAnimator.SetBool(param, false);
            yield return null;
            avatarAnimator.SetBool(param, true);

            yield return new WaitForSeconds(animationDuration);

            avatarAnimator.SetBool(param, false);
            // brief pause to let the state machine return to Idle
            yield return new WaitForSeconds(0.1f);
        }
    }
}
