using UnityEngine;
using UnityEngine.Video;

public class OverlayVideoPage : DisableOnAnimationTrigger
{
    [SerializeField] 
    private VideoPlayer _videoPlayer;
    [SerializeField] 
    private GameObject _playButton;
    [SerializeField] 
    private GameObject _pauseButton;
    [SerializeField] 
    private GameObject _backward;
    [SerializeField] 
    private GameObject _forward;
    
    private bool isShowing  = true;
    private bool isDone = false;

    // Property get for video player for outside access
    public VideoPlayer VideoPlayer => _videoPlayer;

    public override void Start()
    {
        base.Start();

        // Triggers when video ends
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
        // Trigger when video end show play button for replay
        isDone = true;
        ToggleHud(true);
        
        //Disable forward and rewind nothing to move at this point
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
        // Set video source using url
        _videoPlayer.source = VideoSource.Url;
        _videoPlayer.url = url;
        gameObject.SetActive(true);
    }

    public void ShowVideo(VideoClip clip)
    {
        // Set video source using video clip
        _videoPlayer.source = VideoSource.VideoClip;
        _videoPlayer.clip = clip;
        gameObject.SetActive(true);
    }

    public void ToggleHud()
    {
        // Toggle the video controls hud
        ToggleHud(!isShowing);
    }

    public void ToggleHud(bool show)
    {
        TogglePlayButton();
        isShowing = show;
    }

    public void PlayVideo()
    {
        // Disable hud when video is played
        TogglePlay(true);
        ToggleHud(false);
    }

    public void TogglePlayPause()
    {
        TogglePlay(_videoPlayer.isPlaying);
    }

    public void TogglePlay(bool playVideo)
    {
        if (playVideo)
        {
            // Play video when play button is clicked
            if (_videoPlayer.isPlaying)
            {
                Debug.LogError("Video Player is already playing");
                return;
            }

            if (isDone)
            {
                // If video is done and play button is clicked play video
                // and show the rewind and forward button
                isDone = false;
                ToggleVideoMoveButton(true);
            }
            _videoPlayer.Play();
        }
        else
        {
            // Pause video when pause button is clicked
            if (!_videoPlayer.isPlaying)
            {
                return;
            }
            _videoPlayer.Pause();
        }
        TogglePlayButton();
    }

    private void TogglePlayButton()
    {
        // Toggle button shown when video state is pause or play
        _playButton.SetActive(!_videoPlayer.isPlaying);
        _pauseButton.SetActive(_videoPlayer.isPlaying);
    }

    public void Backward()
    {
        // Move back video player time by 3 seconds
        _videoPlayer.time -= 3;
    }

    public void Forward()
    {
        // Move forward video player time by 3 seconds
        _videoPlayer.time += 3;
    }
}
