using UnityEngine;

public class DoorSfx : MonoBehaviour
{
    public AudioSource sfxSource;
    public AudioClip openStartClip;   // 门开始动/解锁声
    public AudioClip openEndClip;     // 门开到位/撞停声（可选）
    [Range(0f, 1f)] public float volume = 1f;

    private void Awake()
    {
        if (!sfxSource)
        {
            sfxSource = GetComponent<AudioSource>();
            if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;

        // ✅ 强制 2D（不受距离影响）
        sfxSource.spatialBlend = 0f;

        // 可选：门声一般不需要多普勒
        sfxSource.dopplerLevel = 0f;
    }

    // Animation Event 调用：开始开门
    public void PlayOpenStart()
    {
        if (openStartClip)
            sfxSource.PlayOneShot(openStartClip, volume);
    }

    // Animation Event 调用：开门结束（可选）
    public void PlayOpenEnd()
    {
        if (openEndClip)
            sfxSource.PlayOneShot(openEndClip, volume);
    }
}
