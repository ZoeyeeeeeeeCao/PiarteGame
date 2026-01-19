using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class LoadingScreenUI : MonoBehaviour
{
    public Image progressBar;
    public TextMeshProUGUI loadingText;
    public Image steeringWheel;

    [Header("Video Settings")]
    public bool useVideoLoading = false; // Toggle this in the prefab
    public VideoPlayer videoPlayer;
    public GameObject videoDisplayObject; // The RawImage or Panel holding the video
}