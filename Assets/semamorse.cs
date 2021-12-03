using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class semamorse : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public KMSelectable[] dotButtons;
    public KMSelectable[] arrowButtons;
    public Renderer[] dots;
    public Renderer led;
    public Transform pivot;
    public Color off;
    public Color[] onColors;
    public Color lightGray;
    public Color white;
    public Color[] rainbow;
    public TextMesh colorblindText;

    private int[][] displayedLetters = new int[2][] { new int[5], new int[5] };
    private int[] displayedColors = new int[5];
    private int[] colorOrder = new int[5];
    private bool[] solution = new bool[8];
    private bool[] selected = new bool[8];
    private int difference;
    private int colorOrderIndex;
    private int currentPos;

    private Coroutine[] morseFlashes = new Coroutine[2];
    private Coroutine rotating;
    private bool[] fading = new bool[8];
    private bool transitioning;
    private bool flashing;
    private bool stage2;
    private bool isCCW;
    private static readonly Char[] alphabet = new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
    private static readonly int[][] colorTable = new int[10][]
    {
        new[] { 4, 2, 0, 3, 1 },
        new[] { 1, 0, 4, 3, 2 },
        new[] { 2, 4, 1, 3, 0 },
        new[] { 4, 2, 0, 1, 3 },
        new[] { 1, 2, 3, 4, 0 },
        new[] { 0, 3, 2, 1, 4 },
        new[] { 1, 0, 2, 3, 4 },
        new[] { 0, 3, 1, 4, 2 },
        new[] { 1, 0, 2, 4, 3 },
        new[] { 0, 2, 3, 1, 4 }
    };
    private static readonly Char[][] letterTable = new Char[4][]
    {
        new[] { 'C', 'Q', 'H', 'M', 'I' },
        new[] { 'L', 'E', 'K', 'B', 'S' },
        new[] { 'J', 'N', 'P', 'D', 'F' },
        new[] { 'R', 'A', 'O', 'T', 'G' },
    };
    private static readonly int[][] semaphore = new int[26][]
    {
        new[] { 4, 5 }, // A
		new[] { 4, 6 }, // B
		new[] { 4, 7 }, // C
		new[] { 4, 0 }, // D
		new[] { 4, 1 }, // E
		new[] { 4, 2 }, // F
		new[] { 4, 3 }, // G
		new[] { 5, 6 }, // H
		new[] { 5, 7 }, // I
		new[] { 0, 2 }, // J
		new[] { 5, 0 }, // K
		new[] { 5, 1 }, // L
		new[] { 5, 2 }, // M
		new[] { 5, 3 }, // N
		new[] { 6, 7 }, // O
		new[] { 6, 0 }, // P
		new[] { 6, 1 }, // Q
		new[] { 6, 2 }, // R
		new[] { 6, 3 }, // S
		new[] { 7, 0 }, // T
		new[] { 7, 1 }, // U
		new[] { 0, 3 }, // V
		new[] { 1, 2 }, // W
		new[] { 1, 3 }, // X
		new[] { 7, 2 }, // Y
		new[] { 2, 3 } // Z
	};
    private static readonly bool[][] morse = new bool[26][]
    {
        new[] { true, false, true, true, true }, // A
		new[] { true, true, true, false, true, false, true, false, true }, // B
		new[] { true, true, true, false, true, false, true, true, true, false, true }, // C
		new[] { true, true, true, false, true, false, true }, // D
		new[] { true }, // E
		new[] { true, false, true, false, true, true, true, false, true }, // F
		new[] { true, true, true, false, true, true, true, false, true }, // G
		new[] { true, false, true, false, true, false, true }, // H
		new[] { true, false, true }, // I
		new[] { true, false, true, true, true, false, true, true, true, false, true, true, true }, // J
		new[] { true, true, true, false, true, false, true, true, true }, // K
		new[] { true, false, true, true, true, false, true, false, true }, // L
		new[] { true, true, true, false, true, true, true }, // M
		new[] { true, true, true, false, true }, // N
		new[] { true, true, true, false, true, true, true, false, true, true, true }, // O
		new[] { true, false, true, true, true, false, true, true, true, false, true }, // P
		new[] { true, true, true, false, true, true, true, false, true, false, true, true, true }, // Q
		new[] { true, false, true, true, true, false, true }, // R
		new[] { true, false, true, false, true, false }, // S
		new[] { true, true, true }, // T
		new[] { true, false, true, false, true, true, true }, // U
		new[] { true, false, true, false, true, false, true, true, true }, // V
		new[] { true, false, true, true, true, false, true, true, true }, // W
		new[] { true, true, true, false, true, false, true, false, true, true, true }, // X
		new[] { true, true, true, false, true, false, true, true, true, false, true, true, true }, // Y
		new[] { true, true, true, false, true, true, true, false, true, false, true } // Z
	};

    private static readonly string[] directionNames = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    private static readonly string[] ordinals = new[] { "1st", "2nd", "3rd", "4th" };
    private static readonly string[] colorNames = new[] { "red", "green", "cyan", "indigo", "pink" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in arrowButtons)
            button.OnInteract += delegate () { PressArrowButton(button); return false; };
        foreach (KMSelectable button in dotButtons)
            button.OnInteract += delegate () { PressDotButton(button); return false; };
        isCCW = rnd.Range(0, 2) == 0;
        colorblindText.gameObject.SetActive(GetComponent<KMColorblindMode>().ColorblindModeActive);
    }

    private void Start()
    {
        colorOrderIndex = (bomb.GetBatteryHolderCount() + bomb.GetPortPlates().Count()) % 10;
        colorOrder = colorTable[colorOrderIndex];
        Debug.LogFormat("[Semamorse #{0}] Battery holders plus port plates modulo 10 is {1}.", moduleId, colorOrderIndex);
        stage2 = false;
        displayedColors = Enumerable.Range(0, 5).ToList().Shuffle().ToArray();
        colorblindText.text = "RGCIP"[displayedColors[0]].ToString();
        difference = rnd.Range(0, 5);
        for (int i = 0; i < 5; i++)
        {
            if (i == difference)
            {
                displayedLetters[0][i] = rnd.Range(0, 26);
                displayedLetters[1][i] = rnd.Range(0, 26);
                while (displayedLetters[0][i] == displayedLetters[1][i])
                    displayedLetters[1][i] = rnd.Range(0, 26);
            }
            else
            {
                var r = rnd.Range(0, 26);
                displayedLetters[0][i] = r;
                displayedLetters[1][i] = r;
            }
            Debug.LogFormat("[Semamorse #{0}] Position {1}: The color is {2}. The semaphore letter is {3} and the Morse code letter is {4}.{5}", moduleId, i + 1, colorNames[displayedColors[i]], alphabet[displayedLetters[0][i]], alphabet[displayedLetters[1][i]], i == difference ? " This position has different letters." : "");
        }
        var value = Mathf.Abs(displayedLetters[0][difference] - displayedLetters[1][difference]);
        var relevantColorOrder = colorOrder.Where(c => c != displayedColors[difference]).ToArray();
        for (int i = 0; i < 4; i++)
        {
            var currentColor = relevantColorOrder[i];
            var currentLetter = displayedLetters[0][Array.IndexOf(displayedColors, relevantColorOrder[i])];
            var count = Mathf.Abs(Array.IndexOf(alphabet, letterTable[i][currentColor]) - currentLetter);
            Debug.LogFormat("[Semamorse #{0}] The {1} color in the order is {2}, and the displayed letter for that color is {3}, so {4} gets added.", moduleId, ordinals[i], colorNames[currentColor], alphabet[currentLetter], count);
            value += count;
            value %= 26;
        }
        var solSemaphore = semaphore[value];
        for (int i = 0; i < 8; i++)
            if (solSemaphore.Contains(i))
                solution[i] = true;
        var solDirections = new List<string>();
        for (int i = 0; i < 8; i++)
            if (solution[i])
                solDirections.Add(directionNames[i]);
        Debug.LogFormat("[Semamorse #{0}] Converted to a letter, the final value is {1}. The solution is {2}, {3}.", moduleId, alphabet[value], solDirections[0], solDirections[1]);
        StartFlashing();
        rotating = StartCoroutine(Rotate());
    }

    private void PressArrowButton(KMSelectable button)
    {
        if (moduleSolved || transitioning || (fading.Contains(true) && !stage2) || (fading.Contains(true) && !selected.Contains(true)))
            return;
        button.AddInteractionPunch(.5f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        if (!stage2)
        {
            var offsets = new int[2] { -1, 1 };
            var ix = Array.IndexOf(arrowButtons, button);
            if (!(currentPos == 0 && ix == 0) && !(currentPos == 4 && ix == 1))
            {
                currentPos += offsets[ix];
                colorblindText.text = "RGCIP"[displayedColors[currentPos]].ToString();
                foreach (Renderer dot in dots)
                    dot.material.color = off;
                StartCoroutine(LetterChange());
            }
            else
                audio.PlaySoundAtTransform("error", button.transform);
        }
        else
        {
            if (!selected.Contains(true))
            {
                colorblindText.text = "RGCIP"[displayedColors[currentPos]].ToString();
                StartCoroutine(Reset());
            }
            else
            {
                var subDirections = new List<string>();
                for (int i = 0; i < 8; i++)
                    if (selected[i])
                        subDirections.Add(directionNames[i]);
                if (selected.SequenceEqual(solution))
                {
                    Debug.LogFormat("[Semamorse #{0}] You submitted {1} {2}. That is correct. Module solved!", moduleId, subDirections[0], subDirections[1]);
                    moduleSolved = true;
                    StartCoroutine(Solve());
                }
                else
                {
                    module.HandleStrike();
                    if (selected.Count(b => b) == 2)
                        Debug.LogFormat("[Semamorse #{0}] You submitted {1} {2}. That is incorrect. Strike!", moduleId, subDirections[0], subDirections[1]);
                    else
                        Debug.LogFormat("[Semamorse #{0}] You only submitted 1 direction, but I was expecting 2. Strike!", moduleId);
                }
            }
        }
    }

    private void PressDotButton(KMSelectable button)
    {
        var ix = Array.IndexOf(dotButtons, button);
        if (fading[ix])
            return;
        button.AddInteractionPunch(.5f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        if (moduleSolved)
            return;
        if (!stage2)
        {
            transitioning = true;
            foreach (Renderer dot in dots)
            {
                dot.material.color = off;
                StartCoroutine(Fade(dot, lightGray));
                audio.PlaySoundAtTransform("beginSubmission", pivot);
                colorblindText.text = "";
                stage2 = true;
                StopFlashing();
            }
        }
        else
        {
            if (!selected[ix])
            {
                if (selected.Count(b => b) == 2)
                    audio.PlaySoundAtTransform("error", button.transform);
                else
                {
                    selected[ix] = true;
                    StartCoroutine(Fade(dots[ix], white));
                    audio.PlaySoundAtTransform("popIn", button.transform);
                }
            }
            else
            {
                selected[ix] = false;
                StartCoroutine(Fade(dots[ix], lightGray));
                audio.PlaySoundAtTransform("popOut", button.transform);
            }
        }
    }

    private IEnumerator Reset()
    {
        foreach (Renderer dot in dots)
            StartCoroutine(Fade(dot, off));
        audio.PlaySoundAtTransform("cancelSubmission", pivot);
        yield return new WaitForSeconds(1f);
        stage2 = false;
        StartFlashing();
    }

    private IEnumerator FlashMorse(Renderer dot, bool[] morseLetter)
    {
        while (flashing)
        {
            var length = morseLetter.Length;
        morseReset:
            for (int i = 0; i < length; i++)
            {
                dot.material.color = morseLetter[i] ? onColors[displayedColors[currentPos]] : off;
                yield return new WaitForSeconds(.25f);
            }
            dot.material.color = off;
            yield return new WaitForSeconds(1.75f);
            goto morseReset;
        }
    }

    private IEnumerator LetterChange()
    {
        transitioning = true;
        StopFlashing();
        yield return new WaitForSeconds(.5f);
        StartFlashing();
        transitioning = false;
    }

    private IEnumerator Fade(Renderer dot, Color endColor)
    {
        var elapsed = 0f;
        var duration = 1f;
        fading[Array.IndexOf(dots, dot)] = true;
        while (elapsed < duration)
        {
            var currentColor = dot.material.color;
            dot.material.color = new Color(
                Mathf.Lerp(currentColor.r, endColor.r, elapsed / duration),
                Mathf.Lerp(currentColor.g, endColor.g, elapsed / duration),
                Mathf.Lerp(currentColor.b, endColor.b, elapsed / duration)
            );
            yield return null;
            elapsed += Time.deltaTime;
        }
        fading[Array.IndexOf(dots, dot)] = false;
        transitioning = false;
    }

    private void StartFlashing()
    {
        if (flashing)
            return;
        flashing = true;
        for (int i = 0; i < 2; i++)
            morseFlashes[i] = StartCoroutine(FlashMorse(dots[semaphore[displayedLetters[0][currentPos]][i]], morse[displayedLetters[1][currentPos]]));
    }

    private void StopFlashing()
    {
        flashing = false;
        for (int i = 0; i < 2; i++)
            StopCoroutine(morseFlashes[i]);
    }

    private IEnumerator Rotate()
    {
        while (true)
        {
            var framerate = 1f / Time.deltaTime;
            var rotation = 20f / framerate;
            if (isCCW)
                rotation *= -1;
            var y = pivot.localEulerAngles.y;
            y += rotation;
            pivot.localEulerAngles = new Vector3(0f, y, 0f);
            yield return null;
        }
    }

    private IEnumerator Solve()
    {
        StopCoroutine(rotating);
        StartCoroutine(StopSpinning());
        StartCoroutine(FadeLed());
        for (int i = 0; i < 8; i++)
        {
            StartCoroutine(Fade(dots[i], rainbow[i]));
            audio.PlaySoundAtTransform("flash", pivot);
            if (i != 0)
                StartCoroutine(Fade(dots[i - 1], off));
            yield return new WaitForSeconds(.75f);
            if (i == 7)
                StartCoroutine(Fade(dots[i], off));
        }
        yield return new WaitForSeconds(1f);
        foreach (Renderer dot in dots)
            StartCoroutine(Fade(dot, rainbow[Array.IndexOf(dots, dot)]));
        module.HandlePass();
        audio.PlaySoundAtTransform("solve", pivot);
    }

    private IEnumerator FadeLed()
    {
        var elapsed = 0f;
        var duration = 1f;
        while (elapsed < duration)
        {
            var currentColor = led.material.color;
            led.material.color = new Color(
                currentColor.r,
                currentColor.g,
                currentColor.b,
                Mathf.Lerp(currentColor.a, 0f, elapsed / duration)
            );
            yield return null;
            elapsed += Time.deltaTime;
        }
        yield return null;
    }

    private IEnumerator StopSpinning()
    {
        var elapsed = 0f;
        var duration = 2f;
        while (elapsed < duration)
        {
            var slowTime = 0f;
            if (elapsed <= .5f)
                slowTime = 15f;
            else if (elapsed <= 1f)
                slowTime = 10f;
            else if (elapsed <= 1.5f)
                slowTime = 5f;
            else
                slowTime = 2.5f;
            var framerate = 1f / Time.deltaTime;
            var rotation = slowTime / framerate;
            if (isCCW)
                rotation *= -1;
            var y = pivot.localEulerAngles.y;
            y += rotation;
            pivot.localEulerAngles = new Vector3(0f, y, 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} <left/right> [Presses the left or right arrow] | !{0} start [If not in submission mode, enters submission mode] | !{0} <NW SE> [If in submission mode, presses the dots northwest and southeast]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string input)
    {
        var cmd = input.ToLowerInvariant().Split(' ').ToArray();
        yield return "solve";
        yield return "strike";
        if (cmd.Length == 1)
        {
            if (cmd[0] == "left")
            {
                yield return null;
                arrowButtons[0].OnInteract();
            }
            else if (cmd[0] == "right")
            {
                yield return null;
                arrowButtons[1].OnInteract();
            }
            else if (cmd[0] == "start")
            {
                if (stage2)
                {
                    yield return "sendtochaterror The module is already in submission mode.";
                    yield break;
                }
                else
                {
                    yield return null;
                    dotButtons[0].OnInteract();
                }
            }
            else
                yield break;
        }
        else if (cmd.Length == 2)
        {
            var directions = new string[8] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            for (int i = 0; i < 2; i++)
                cmd[i] = cmd[i].ToUpperInvariant();
            if (!directions.Contains(cmd[0]) || !directions.Contains(cmd[1]))
                yield break;
            else
            {
                yield return null;
                for (int i = 0; i < 2; i++)
                {
                    dotButtons[Array.IndexOf(directions, cmd[i])].OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
            }
        }
        else
            yield break;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        if (stage2)
            goto readyToSubmit;
        dotButtons[0].OnInteract();
        yield return new WaitForSeconds(1.25f);
    readyToSubmit:
        if (selected.Contains(true))
        {
            for (int i = 0; i < 8; i++)
            {
                if (selected[i] && !solution[i])
                {
                    dotButtons[i].OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
            }
            yield return new WaitForSeconds(.25f);
        }
        for (int i = 0; i < 8; i++)
        {
            if (solution[i] && !selected[i])
            {
                dotButtons[i].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
        yield return new WaitForSeconds(1f);
        arrowButtons[0].OnInteract();
        while (!moduleSolved)
        {
            yield return true;
            yield return new WaitForSeconds(.1f);
        }
    }
}
