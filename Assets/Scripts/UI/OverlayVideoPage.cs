using UnityEngine;
using UnityEngine.Video;

public class OverlayVideoPage : DisableOnAnimationTrigger
{
    [SerializeField] 
    private VideoPlayer _videoPlayer;
    [SerializeField] 
    private GameObject _hud;
    [SerializeField] 
    private GameObject _playButton;
    [SerializeField] 
    private GameObject _pauseButton;
    [SerializeField] 
    private GameObject _backward;
    [SerializeField] 
    private GameObject _forward;
    
    private const float HUD_SHOWTIME = 2.5f;
    private bool isShowing  = true;
    private bool isDone = false;
    private float timeBeforeHiding = 1.5f;

    public VideoPlayer VideoPlayer => _videoPlayer;

    public override void Start()
    {
        base.Start();

        _videoPlayer.loopPointReached += OnVideoCompleted;
        if (_videoPlayer.isPrepared)
        {
            TogglePlayButton();
        }
        else
        {
            _videoPlayer.prepareCompleted += OnVideoPrepared;
        }
    }

    private void OnVideoCompleted(VideoPlayer source)
    {
        isDone = true;
        ToggleHud(true);
        ToggleVideoMoveButton(false);
    }

    private void ToggleVideoMoveButton(bool isEnabled)
    {
        _forward.SetActive(isEnabled);
        _backward.SetActive(isEnabled);
    }


    private void OnVideoPrepared(VideoPlayer source)
    {
        TogglePlayButton();
    }

    public void ShowVideo(string url)
    {
        _videoPlayer.source = VideoSource.Url;
        _videoPlayer.url = url;
        gameObject.SetActive(true);
    }

    public void ShowVideo(VideoClip clip)
    {
        _videoPlayer.source = VideoSource.VideoClip;
        _videoPlayer.clip = clip;
        gameObject.SetActive(true);
    }

    public void ToggleHud()
    {
        ToggleHud(!isShowing);
    }

    public void ToggleHud(bool show)
    {
        if (show)
        {   
            timeBeforeHiding = HUD_SHOWTIME;
        }

        TogglePlayButton();
        isShowing = show;
        _hud.SetActive(isShowing);        
    }

    public void PlayVideo()
    {
        TogglePlay(true);
        ToggleHud(false);
    }

    public void TogglePlayPause()
    {
        TogglePlay(_videoPlayer.isPlaying);
    }

    public void ResetTimer()
    {
        timeBeforeHiding = HUD_SHOWTIME;
    }

    public void TogglePlay(bool playVideo)
    {
        if (playVideo)
        {
            if (_videoPlayer.isPlaying)
            {
                Debug.LogError("Video Player is already playing");
                return;
            }

            if (isDone)
            {
                isDone = false;
                ToggleVideoMoveButton(true);
            }
            _videoPlayer.Play();
        }
        else
        {
            if (!_videoPlayer.isPlaying)
            {
                return;
            }
            _videoPlayer.Pause();
        }
        ResetTimer();
        TogglePlayButton();
    }

    private void TogglePlayButton()
    {
        _playButton.SetActive(!_videoPlayer.isPlaying);
        _pauseButton.SetActive(_videoPlayer.isPlaying);
    }

    public void Backward()
    {
        _videoPlayer.time -= 3;
        ResetTimer();
    }

    public void Forward()
    {
        _videoPlayer.time += 3;
        ResetTimer();
    }

    public void Update()
    {
        if (!isShowing || isDone)
        {
            return;
        }

        timeBeforeHiding -= 1f * Time.deltaTime;
        if (timeBeforeHiding <= 0.0f)
        {
            ToggleHud(false);
        }
    }
}
