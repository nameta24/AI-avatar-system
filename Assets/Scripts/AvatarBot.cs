// ============================================================
//  AvatarBot.cs  —  Single script, Windows, zero paid APIs
//
//  STT: UnityEngine.Windows.Speech.DictationRecognizer (built into Unity)
//  TTS: PowerShell → System.Speech (no DLL import needed)
//
//  SETUP (one time):
//  Edit → Project Settings → Player → Other Settings
//  → Api Compatibility Level → .NET Framework
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

[RequireComponent(typeof(AudioSource))]
public class AvatarBot : MonoBehaviour
{
    // ────────────────────────────────────────────────────────
    //  DATASET
    // ────────────────────────────────────────────────────────

    [System.Serializable]
    public class QAPair
    {
        [Tooltip("One trigger phrase per line")]
        [TextArea(3, 6)]
        public string triggerPhrases;

        [TextArea(3, 8)]
        public string response;
    }

    [Header("Q&A Dataset")]
    public List<QAPair> dataset = new List<QAPair>
    {
        new QAPair
        {
            triggerPhrases = "hello\nhi\nhey\ngood morning\ngood afternoon",
            response       = "Hello! Great to see you. How can I help you today?"
        },
        new QAPair
        {
            triggerPhrases = "what is your name\nwho are you\nwhat should I call you",
            response       = "My name is Aria. I am your virtual assistant."
        },
        new QAPair
        {
            triggerPhrases = "how are you\nhow are you doing\nare you okay",
            response       = "I am doing great, thank you for asking!"
        },
        new QAPair
        {
            triggerPhrases = "what can you do\nwhat do you know\nhow can you help",
            response       = "I can answer questions from my dataset. Just ask me anything!"
        },
        new QAPair
        {
            triggerPhrases = "goodbye\nbye\nsee you\ntake care\ngoodnight",
            response       = "Goodbye! It was lovely talking with you. Come back anytime!"
        },
        new QAPair
        {
            triggerPhrases = "thank you\nthanks\nthank you so much",
            response       = "You are very welcome! Is there anything else I can help with?"
        },
    };

    [Header("Fallback - said when nothing matches")]
    [TextArea(2, 3)]
    public string fallbackResponse =
        "I am sorry, I did not understand that. Could you please rephrase?";

    // ────────────────────────────────────────────────────────
    //  SETTINGS
    // ────────────────────────────────────────────────────────

    [Header("Settings")]
    [Tooltip("Hold this key to speak, release to get answer.")]
    public KeyCode pushToTalkKey = KeyCode.Space;

    [Tooltip("Greeting spoken when the scene starts. Leave empty to skip.")]
    [TextArea(1, 3)]
    public string greetingLine = "Hello! I am Aria. Hold Space and ask me anything.";

    [Tooltip("Partial voice name. Installed voices are printed in Console on start. " +
             "For best quality install Microsoft Aria or Jenny via: " +
             "Settings > Time & Language > Speech > Add voices.")]
    public string preferredVoice = "Aria";

    [Tooltip("Speech speed percent. 80 = natural, 100 = normal, 120 = fast.")]
    [Range(50, 150)]
    public int voiceSpeed = 80;

    [Tooltip("Lower = looser matching. Raise if you get too many false matches.")]
    [Range(0.10f, 0.90f)]
    public float matchThreshold = 0.25f;

    // ────────────────────────────────────────────────────────
    //  PRIVATE
    // ────────────────────────────────────────────────────────

    AudioSource _audio;
    string _wavPath;
    bool _speaking;
    bool _pttHeld;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    DictationRecognizer _rec;
#endif

    // ────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ────────────────────────────────────────────────────────

    void Awake()
    {
        _audio = GetComponent<AudioSource>();
        _wavPath = Path.Combine(
            Application.temporaryCachePath, "avatarbot_tts.wav")
            .Replace('\\', '/');

        PrintInstalledVoices();
    }

    void Start()
    {
        if (!string.IsNullOrWhiteSpace(greetingLine))
            StartCoroutine(SpeakAfter(greetingLine, 1.2f));
    }

    void Update()
    {
        if (_speaking) return;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (Input.GetKeyDown(pushToTalkKey) && !_pttHeld)
        {
            _pttHeld = true;
            ListenStart();
        }
        else if (Input.GetKeyUp(pushToTalkKey) && _pttHeld)
        {
            _pttHeld = false;
            ListenStop();
        }
#endif
    }

    void OnDestroy()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        KillRecognizer();
#endif
    }

    // ────────────────────────────────────────────────────────
    //  STT  —  Unity built-in, no extra DLL needed
    // ────────────────────────────────────────────────────────

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

    void ListenStart()
    {
        KillRecognizer();
        _rec = new DictationRecognizer
        {
            AutoSilenceTimeoutSeconds = 2f,
            InitialSilenceTimeoutSeconds = 30f,
        };
        _rec.DictationResult += (text, conf) =>
        {
            Debug.Log($"[AvatarBot] Heard: \"{text}\"");
            StartCoroutine(Speak(FindMatch(text)));
        };
        _rec.DictationComplete += cause =>
            Debug.Log($"[AvatarBot] Dictation done: {cause}");
        _rec.DictationError += (err, code) =>
         Debug.LogWarning($"[AvatarBot] STT error: {err}");
        _rec.Start();
        Debug.Log("[AvatarBot] Listening...");
    }

    void ListenStop()
    {
        try
        {
            if (_rec != null && _rec.Status == SpeechSystemStatus.Running)
                _rec.Stop();
        }
        catch { }
    }

    void KillRecognizer()
    {
        if (_rec == null) return;
        try { _rec.Dispose(); } catch { }
        _rec = null;
    }

#endif

    // ────────────────────────────────────────────────────────
    //  TTS  —  PowerShell calls Windows System.Speech
    //  No DLL needed. Works on every Windows 10/11 machine.
    // ────────────────────────────────────────────────────────

    void PrintInstalledVoices()
    {
        try
        {
            string ps = "Add-Type -AssemblyName System.Speech; " +
                        "$s=New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
                        "Write-Output ('VOICES: ' + ($s.GetInstalledVoices() | " +
                        "ForEach-Object {$_.VoiceInfo.Name}) -join ', ')";

            var proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = "powershell.exe";
            proc.StartInfo.Arguments = "-NoProfile -Command \"" + ps + "\"";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            Debug.Log("[AvatarBot] " + output.Trim());
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AvatarBot] Could not list voices: " + e.Message);
        }
    }

    IEnumerator SpeakAfter(string text, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(Speak(text));
    }

    IEnumerator Speak(string text)
    {
        _speaking = true;

        string safeText = text.Replace("'", "''").Replace("\"", "`\"");
        string safeVoice = preferredVoice.Replace("'", "''");
        int rate = Mathf.Clamp(Mathf.RoundToInt((voiceSpeed - 100f) / 10f), -10, 10);
        string wavEsc = _wavPath.Replace("/", "\\\\");

        // PowerShell one-liner: load System.Speech, pick voice, write WAV
        string script =
            "Add-Type -AssemblyName System.Speech; " +
            "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
            // Try preferred voice
            "foreach($v in $s.GetInstalledVoices()){" +
            "  if($v.VoiceInfo.Name -like '*" + safeVoice + "*'){" +
            "    $s.SelectVoice($v.VoiceInfo.Name); break; }}; " +
            "$s.Rate = " + rate + "; " +
            "$fmt = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(" +
            "  22050," +
            "  [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen," +
            "  [System.Speech.AudioFormat.AudioChannel]::Mono); " +
            "$s.SetOutputToWaveFile('" + wavEsc + "', $fmt); " +
            "$s.Speak('" + safeText + "'); " +
            "$s.SetOutputToNull(); " +
            "$s.Dispose();";

        bool done = false;
        bool error = false;

        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                if (File.Exists(_wavPath)) File.Delete(_wavPath);

                var proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = "powershell.exe";
                proc.StartInfo.Arguments = "-NoProfile -Command \"" + script + "\"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (!string.IsNullOrEmpty(stderr))
                    Debug.LogWarning("[AvatarBot] PS stderr: " + stderr);
            }
            catch (Exception e)
            {
                Debug.LogError("[AvatarBot] TTS error: " + e.Message);
                error = true;
            }
            finally { done = true; }
        });

        while (!done) yield return null;

        if (error || !File.Exists(_wavPath))
        {
            Debug.LogError("[AvatarBot] WAV not created. " +
                           "Make sure PowerShell is available on this machine.");
            _speaking = false;
            yield break;
        }

        // Load WAV → AudioClip
        string uri = "file:///" + _wavPath;
        using var req = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[AvatarBot] WAV load error: " + req.error);
            _speaking = false;
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
        if (clip == null || clip.length < 0.05f)
        {
            Debug.LogError("[AvatarBot] Empty audio clip.");
            _speaking = false;
            yield break;
        }

        // Play — uLipSync reads this AudioSource automatically
        _audio.clip = clip;
        _audio.Play();

        yield return new WaitForSeconds(clip.length + 0.35f);
        _audio.Stop();
        _speaking = false;
    }

    // ────────────────────────────────────────────────────────
    //  MATCHING  —  TF-IDF cosine similarity (offline)
    // ────────────────────────────────────────────────────────

    string FindMatch(string input)
    {
        if (dataset == null || dataset.Count == 0) return fallbackResponse;

        var inputTok = Tokenize(input);
        string inputNorm = Norm(input);
        float best = 0f;
        string resp = null;

        foreach (var pair in dataset)
        {
            if (pair == null || string.IsNullOrWhiteSpace(pair.triggerPhrases)) continue;
            foreach (string raw in pair.triggerPhrases
                         .Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string t = raw.Trim();
                if (t.Length == 0) continue;

                string tn = Norm(t);
                float exact = inputNorm.Contains(tn) ? 1f :
                              tn.Contains(inputNorm) ? 0.8f : 0f;
                float score = Cosine(inputTok, Tokenize(t)) * 0.7f + exact * 0.3f;

                if (score > best) { best = score; resp = pair.response; }
            }
        }

        Debug.Log($"[AvatarBot] Best match score: {best:F2}");
        return (best >= matchThreshold && resp != null) ? resp : fallbackResponse;
    }

    static float Cosine(List<string> a, List<string> b)
    {
        if (a.Count == 0 || b.Count == 0) return 0f;
        var fa = TF(a);
        var fb = TF(b);
        double dot = 0, ma = 0, mb = 0;
        foreach (var kv in fa)
        {
            ma += kv.Value * kv.Value;
            if (fb.TryGetValue(kv.Key, out float v)) dot += kv.Value * v;
        }
        foreach (var kv in fb) mb += kv.Value * kv.Value;
        if (ma <= 0 || mb <= 0) return 0f;
        return (float)(dot / (Math.Sqrt(ma) * Math.Sqrt(mb)));
    }

    static Dictionary<string, float> TF(List<string> tokens)
    {
        var d = new Dictionary<string, float>(StringComparer.Ordinal);
        foreach (var w in tokens) { d.TryGetValue(w, out float v); d[w] = v + 1f; }
        foreach (var k in new List<string>(d.Keys)) d[k] /= tokens.Count;
        return d;
    }

    static readonly HashSet<string> Stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "a","an","the","is","are","was","i","you","he","she","it","we","they","me","my",
        "do","does","did","will","can","could","would","have","has","this","that",
        "and","but","or","in","on","at","to","of","for","with","what","how","who","please"
    };

    static List<string> Tokenize(string s)
    {
        var list = new List<string>();
        string clean = Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9\s]", " ");
        foreach (string w in clean.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            if (w.Length > 1 && !Stop.Contains(w)) list.Add(Stem(w));
        return list;
    }

    static string Norm(string s) =>
        Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9\s]", " ").Trim();

    static string Stem(string w)
    {
        if (w.Length <= 3) return w;
        foreach (string s in new[] { "ing", "tion", "ness", "ment", "able", "est", "er", "ly", "ed", "es", "s" })
            if (w.Length > s.Length + 2 && w.EndsWith(s)) return w.Substring(0, w.Length - s.Length);
        return w;
    }
}