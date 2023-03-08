using System;
using Cysharp.Threading.Tasks;
using Jane.Unity;
using Jane.Unity.ServerShared.Enums;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject HUD;
    [SerializeField] private GameObject countDownHolder;
    public GameObject CountDownHolder => countDownHolder;
    [SerializeField] private GameObject playingHolder;
    public GameObject PlayingHolder => playingHolder;
    [SerializeField] private TMP_Text countDownText;
    public TMP_Text CountDownText => countDownText;
    [SerializeField] private TMP_Text minuteText;
    [SerializeField] private TMP_Text secondText;
    [SerializeField] private TMP_Text milisecondText;

    [SerializeField] private SpaceshipEngine engine;
    [SerializeField] private SpaceshipInputManager inputManager;
    [SerializeField] private SpaceshipCameraController cameraController;
    [SerializeField] private GameState CurrentGameState;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip countClip;
    [SerializeField] private AudioClip startClip;

    private void Update()
    {
        CurrentGameState = GameInfo.GameState;
    }

    public async UniTask CountDownAsync(int seconds)
    {
        countDownHolder.SetActive(true);
        while (seconds > 0)
        {
            countDownText.text = $"{seconds}";
            audioSource.PlayOneShot(countClip);
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            seconds--;
        }
    }

    public async UniTask StartGame()
    {
        Debug.Log("Game Start!");
        countDownText.text = "GO!";
        audioSource.PlayOneShot(startClip);
        playingHolder.SetActive(true);

        inputManager.EnableInput();
        inputManager.EnableMovement();
        inputManager.EnableSteering();

        await UniTask.Delay(1000);
        countDownHolder.SetActive(false);
    }

    public void UpdateTimer(long ticks)
    {
        TimeSpan time = TimeSpan.FromTicks(ticks);

        minuteText.text = $"{time.Minutes:00}";
        secondText.text = $"{time.Seconds:00}";
        milisecondText.text = $"{time.Milliseconds / 10:00}";
    }

    public void EndGame()
    {
        // GameState.GameOver
        // Show Result Game UI
        engine.ControlsDisabled = true;
        engine.EnginesActivated = false;

        inputManager.DisableInput();
        inputManager.DisableMovement(true);
        inputManager.DisableSteering(true);
    }
}
