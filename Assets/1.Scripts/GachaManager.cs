using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaManager : MonoBehaviour
{
    [Header("UI 참조")]
    public Image fadeImage;
    public Image energyEffect;
    public Image silhouetteImage;
    public Image flashImage;
    public Image resultImage;

    [Header("등급 연출 설정")]
    public List<RarityEffectData> rarityEffects;

    [Header("연출용 컴포넌트")]
    public Image backgroundImage;
    public Image resultGlow;
    public AudioSource sfxSource;
    public AudioSource bgmSource;
    public Transform canvasZoomRoot;

    [Header("결과 이미지")]
    public List<Sprite> resultSprites;
    private Sprite currentResultSprite;
    private Rarity currentResultRarity;

    [Header("등급별 배경 스프라이트")]
    public Sprite background;
    public Sprite legendary;
    public Sprite epic;
    public Sprite rare;
    public Sprite normal;

    [Header("버튼 관련")]
    public Button gachaButton;
    public CanvasGroup buttonGroup;
    public Button retryButton;
    public Button confirmButton;

    [Header("이펙트 관련")]
    public GameObject starEffectPrefab;
    public Transform starEffectPoint;

    [Header("사운드 설정")]
    public AudioClip clickSound;
    public AudioClip chargeClip;
    public AudioClip shakeSound;
    public AudioClip revealSound;
    public AudioClip bgmSound;

    private GameObject currentStarEffect;
    private Coroutine glowLoopRoutine;

    public enum Rarity { Normal, Rare, Epic, Legendary }

    [System.Serializable]
    public class RarityEffectData
    {
        public Rarity rarity;
        public Color glowColor;
        public ParticleSystem particleEffect;
        public AudioClip sfx;
    }

    private Dictionary<Rarity, Color> rarityStarColors = new Dictionary<Rarity, Color>
    {
        { Rarity.Normal,    Color.white },
        { Rarity.Rare,      new Color(0.2f, 0.4f, 1f) },
        { Rarity.Epic,      new Color(0.6f, 0.1f, 0.9f) },
        { Rarity.Legendary, new Color(1f, 0.45f, 0f) }
    };

    void Start()
    {
        silhouetteImage.rectTransform.localScale = Vector3.one * 2f;

        if (bgmSound != null && bgmSource != null)
        {
            bgmSource.clip = bgmSound;
            bgmSource.loop = true;
            bgmSource.playOnAwake = true;
            bgmSource.volume = 0.3f;
            bgmSource.Play();
        }
    }

    public void OnGachaClicked()
    {
        if (clickSound != null)
            sfxSource.PlayOneShot(clickSound);

        gachaButton.gameObject.SetActive(false);
        RollGachaResult();
        StartCoroutine(FadeIn());
    }

    public void OnRetry()
    {
        if (clickSound != null)
            sfxSource.PlayOneShot(clickSound);

        StopAllCoroutines();
        ResetVisuals();
        glowLoopRoutine = null;
        resultGlow.rectTransform.localScale = Vector3.one;
        backgroundImage.sprite = background;
        gachaButton.gameObject.SetActive(false);
        RollGachaResult();
        StartCoroutine(FadeIn());
    }

    public void OnConfirm()
    {
        StopAllCoroutines();
        ResetVisuals();
        glowLoopRoutine = null;
        resultGlow.rectTransform.localScale = Vector3.one;
        backgroundImage.sprite = background;
        gachaButton.gameObject.SetActive(true);
    }

    void RollGachaResult()
    {
        int roll = Random.Range(0, 100);
        if (roll < 25) currentResultRarity = Rarity.Normal;
        else if (roll < 50) currentResultRarity = Rarity.Rare;
        else if (roll < 75) currentResultRarity = Rarity.Epic;
        else currentResultRarity = Rarity.Legendary;

        if (resultSprites != null && resultSprites.Count > 0)
        {
            int index = Random.Range(0, resultSprites.Count);
            currentResultSprite = resultSprites[index];
            resultImage.sprite = currentResultSprite;
            silhouetteImage.sprite = currentResultSprite;
            silhouetteImage.color = new Color(0, 0, 0, 0);
        }
    }

    void ResetVisuals()
    {
        fadeImage.color = new Color(0, 0, 0, 0);
        energyEffect.color = new Color(1, 1, 1, 0);
        energyEffect.rectTransform.localScale = Vector3.zero;

        silhouetteImage.color = new Color(0, 0, 0, 0);
        silhouetteImage.rectTransform.anchoredPosition = Vector3.zero;

        flashImage.color = new Color(1, 1, 1, 0);
        resultImage.color = new Color(1, 1, 1, 0);
        resultImage.rectTransform.localScale = Vector3.one * 0.5f;
        resultGlow.color = new Color(1, 1, 1, 0);

        if (currentStarEffect != null)
        {
            Destroy(currentStarEffect);
            currentStarEffect = null;
        }

        buttonGroup.alpha = 0;
        buttonGroup.interactable = false;
        buttonGroup.blocksRaycasts = false;
        buttonGroup.gameObject.SetActive(false);
    }

    IEnumerator FadeIn()
    {
        float duration = 1f;
        float elapsed = 0f;
        Color startColor = new Color(0, 0, 0, 0);
        Color endColor = new Color(0, 0, 0, 0.85f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
        }

        StartCoroutine(ShowEnergyEffect());
    }

    IEnumerator ShowEnergyEffect()
    {
        if (chargeClip != null)
            sfxSource.PlayOneShot(chargeClip);

        float duration = 1.2f;
        float elapsed = 0f;

        energyEffect.rectTransform.localScale = Vector3.zero;
        Color startColor = new Color(1, 1, 1, 0);
        Color endColor = new Color(1, 1, 1, 1);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            energyEffect.rectTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 2f, t);
            energyEffect.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }

        StartCoroutine(ShowSilhouetteThenFlash());
    }

    IEnumerator ShowSilhouetteThenFlash()
    {
        float duration = 3f;
        float elapsed = 0f;
        Vector3 originalPos = silhouetteImage.rectTransform.anchoredPosition;
        float shakeMagnitude = 45f;
        int flashCount = 1;

        if (shakeSound != null)
            sfxSource.PlayOneShot(shakeSound);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            silhouetteImage.color = Color.Lerp(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0.85f), t);
            Vector2 shake = Random.insideUnitCircle * shakeMagnitude * (1f - t);
            silhouetteImage.rectTransform.anchoredPosition = originalPos + (Vector3)shake;
            yield return null;
        }

        silhouetteImage.rectTransform.anchoredPosition = originalPos;

        switch (currentResultRarity)
        {
            case Rarity.Normal: flashCount = 1; break;
            case Rarity.Rare: flashCount = 2; break;
            case Rarity.Epic: flashCount = 3; break;
            case Rarity.Legendary: flashCount = 4; break;
        }

        yield return StartCoroutine(FlashScreen(flashCount));
        StartCoroutine(ShowResult());
    }

    IEnumerator FlashScreen(int flashCount)
    {
        float flashDuration = 0.2f;

        for (int i = 0; i < flashCount; i++)
        {
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flashDuration;
                float alpha = Mathf.Lerp(0, 1, t < 0.5f ? t * 2 : (1 - t) * 2);
                flashImage.color = new Color(1, 1, 1, alpha);
                yield return null;
            }
            flashImage.color = new Color(1, 1, 1, 0);
            yield return new WaitForSeconds(0.1f);
        }

        switch (currentResultRarity)
        {
            case Rarity.Rare:
                energyEffect.color = new Color(0.35f, 0.55f, 1f, 1f); break;
            case Rarity.Epic:
                energyEffect.color = new Color(0.61f, 0.43f, 1f, 1f); break;
            case Rarity.Legendary:
                energyEffect.color = new Color(1f, 0.74f, 0.29f, 1f); break;
        }
    }

    IEnumerator ShowResult()
    {
        if (currentResultRarity == Rarity.Legendary || currentResultRarity == Rarity.Epic)
        {
            yield return StartCoroutine(CanvasZoomEffect());
        }

        ApplyRarityEffect(currentResultRarity);

        float elapsed = 0f;
        float fadeOutDuration = 0.3f;
        Color silStart = silhouetteImage.color;
        Color silEnd = new Color(0, 0, 0, 0);

        if (resultGlow != null)
        {
            Color c = resultGlow.color;
            c.a = 0;
            resultGlow.color = c;
            float glowTime = 0.4f;
            float glowElapsed = 0f;
            while (glowElapsed < glowTime)
            {
                glowElapsed += Time.deltaTime;
                c.a = Mathf.Lerp(0, 1, glowElapsed / glowTime);
                resultGlow.color = c;
                yield return null;
            }
        }

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            silhouetteImage.color = Color.Lerp(silStart, silEnd, elapsed / fadeOutDuration);
            yield return null;
        }

        resultImage.color = new Color(1, 1, 1, 0);
        resultImage.rectTransform.localScale = Vector3.one * 0.5f;

        elapsed = 0f;
        float appearDuration = 0.6f;

        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / appearDuration;
            float scaleT = Mathf.SmoothStep(0.5f, 2.2f, t);
            resultImage.rectTransform.localScale = Vector3.one * scaleT;
            float alpha = Mathf.Lerp(0, 1, t);
            resultImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        resultImage.rectTransform.localScale = Vector3.one * 2.2f;

        if (revealSound != null)
            sfxSource.PlayOneShot(revealSound);

        if (starEffectPrefab != null && starEffectPoint != null)
        {
            currentStarEffect = Instantiate(starEffectPrefab, starEffectPoint.position, Quaternion.identity, starEffectPoint);
            var ps = currentStarEffect.GetComponent<ParticleSystem>();
            if (ps != null && rarityStarColors.ContainsKey(currentResultRarity))
            {
                var main = ps.main;
                main.startColor = rarityStarColors[currentResultRarity];
            }
        }

        StartCoroutine(ShowButtons());
        glowLoopRoutine = StartCoroutine(GlowPulseLoop());
    }

    IEnumerator CanvasZoomEffect(float scale = 1.5f, float duration = 0.4f)
    {
        Vector3 original = canvasZoomRoot.localScale;
        Vector3 target = Vector3.one * scale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasZoomRoot.localScale = Vector3.Lerp(original, target, elapsed / duration);
            yield return null;
        }

        yield return new WaitForSeconds(0.05f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasZoomRoot.localScale = Vector3.Lerp(target, original, elapsed / duration);
            yield return null;
        }

        canvasZoomRoot.localScale = original;
    }

    IEnumerator ShowButtons()
    {
        yield return new WaitForSeconds(0.5f);

        buttonGroup.gameObject.SetActive(true);
        buttonGroup.interactable = false;
        buttonGroup.blocksRaycasts = false;

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            buttonGroup.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }

        buttonGroup.alpha = 1;
        buttonGroup.interactable = true;
        buttonGroup.blocksRaycasts = true;
    }

    IEnumerator GlowPulseLoop()
    {
        float scaleMin = 1.0f;
        float scaleMax = 1.4f;
        float speed = 1.5f;

        while (true)
        {
            float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
            float scale = Mathf.Lerp(scaleMin, scaleMax, t);
            resultGlow.rectTransform.localScale = Vector3.one * scale;
            yield return null;
        }
    }

    void ApplyRarityEffect(Rarity rarity)
    {
        var data = rarityEffects.Find(r => r.rarity == rarity);
        if (data == null) return;

        switch (rarity)
        {
            case Rarity.Legendary: backgroundImage.sprite = legendary; break;
            case Rarity.Epic: backgroundImage.sprite = epic; break;
            case Rarity.Rare: backgroundImage.sprite = rare; break;
            case Rarity.Normal: backgroundImage.sprite = normal; break;
        }

        if (resultGlow != null)
            resultGlow.color = data.glowColor;

        if (data.particleEffect != null)
            data.particleEffect.Play();

        if (data.sfx != null)
            sfxSource.PlayOneShot(data.sfx);
    }
}