using TMPro;
using DG.Tweening;
using GameCore.Sequences;
using MyBox;
using UnityEngine;

namespace GameCore.UI
{
    public class UIRewardContainer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] public ResourceType resourceType;
        
        private int _resourceAmount;
        private int _endNumberAmount;

        public Tween RewardTween { get; set; }

        public Sq_ResourcesFly routine { get; private set; }

        public void Set()
        {
            titleText.text = resourceType.GetResourceName(true);
            amountText.text = $"{_resourceAmount}";
        }

        public void SetAmountWithAnimation(ResourcePack resourcePack, float duration)
        {
            DOTween.timeScale = 1;
            int endNumber = _resourceAmount + (int)resourcePack[resourceType].Value;
            RewardTween = DOTween.To(() => _resourceAmount, x => _resourceAmount = x,
                    endNumber, duration)
                .SetUpdate(true)
                .OnUpdate(() => amountText.text = $"{_resourceAmount}")
                .SetEase(Ease.InSine);
        }

        public void PlayEffect(ResourcePack resourcePack)
        {
            ResourcePack pack = new();
            pack.Add(resourcePack[resourceType]);
            routine = new Sq_ResourcesFly(pack, transform);
            routine.StartCoroutine();
        }

        public void Reset() => _resourceAmount = 0;
        public void ForceAnimation() => RewardTween.Complete();
    }
}
