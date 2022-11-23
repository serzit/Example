using System.Collections;
using System.Linq;
using DG.Tweening;
using GameCore;
using GameCore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BountyContainer : MonoBehaviour
{
    [SerializeField] private Image bountyImage;
    [SerializeField] private Image bountyBackground;
    [SerializeField] private TextMeshProUGUI bountyPoints;
    [SerializeField] private UICommonButton bountyButton;
    
    public Tween BountyTween { get; private set; }
    public bool IsMedal { get; set; }
    public UnitName UnitName { get; set; }
    public ResourcePack UIContainerPack { get; private set; } = new();
    public int UnitsKilled { get; private set; }

    public void Set(Sprite bounty, Sprite background, int points, ResourcePack unitPack, int unitsKilled = 0)
    {
        UIContainerPack = new(unitPack);
        bountyBackground.sprite = background;
        bountyImage.sprite = bounty;
        bountyPoints.text = points.ToString();
        UnitsKilled = unitsKilled;
    }

    public void Reset()
    {
        transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        gameObject.SetActive(false);
    }

    public void ForceAnimation() => BountyTween.Complete();

    public void PlayAnimation()
    {
        DOTween.timeScale = 1;
        gameObject.SetActive(true);
        BountyTween = transform.DOScale(1f, 0.2f).SetUpdate(true).SetEase(Ease.OutQuad);

        StartCoroutine(PunchScaleRoutine());
    }

    private IEnumerator PunchScaleRoutine()
    {
        while (BountyTween.IsPlaying()) yield return null;

        transform.DOPunchScale(new Vector3(.06f, .06f, .06f), 0.1f).SetUpdate(true).SetEase(Ease.OutCubic);
    }
    public void SetContainerTooltip(string containerKey)
    {
        var medalText = $"{containerKey}";
        var unitText = $"{"UI/Common.Enemy.Destroyed".Localized()} {containerKey}: {UnitsKilled}{"UI/Parameter.Unit.Ammunition".Localized()}.";
        bountyButton.SetClickAction(() =>
        {
            Bubble.Set(new BubbleSettings(new RectTransformBubbleTarget((RectTransform)transform,
                RectTransformBubbleTarget.RectSide.Top))
            {
                direction = Vector2.up,
                text = $"{(IsMedal ? medalText : unitText)} <br>{SetResourcesVisual()}"
            });
        });
    }

    private string SetResourcesVisual()
    {
        string result = $"{"UI/Common.Reward".Localized()} ";

        foreach (var resource in UIContainerPack.Reverse())
        {
            result += $"{resource.Key.BBCode(resource.Key == ResourceType.PlayerEXP ? 3 : 0)} {resource.Value} ";
        }

        return result;
    }
}
