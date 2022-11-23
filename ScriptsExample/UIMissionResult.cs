using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BroTools;
using DG.Tweening;
using GameCore.Ads;
using GameCore.Daily;
using GameCore.Utilities.UI;
using Systems.Ads;
using TMPro;
using UI.Header;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Tween = DG.Tweening.Tween;

namespace GameCore.UI
{
    public class UIMissionResult : UIElementAnimated, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private UIRewardContainer[] uiRewardContainers;
        [SerializeField] private GameObject bountyContainerPrefab;
        [SerializeField] private Transform bountyHolder;
        [SerializeField] private UICommonButton takeRewardButton;
        [SerializeField] private UICommonButton commonButton;
        [SerializeField] private CommonHeader mainHeader;
        [SerializeField] private Image header;
        [SerializeField] private Image mainBackground;
        [SerializeField] private Image containerBackground;

        private Action _onCollectReward;
        private Action _onMissionReplay;
        private Func<bool> _canReplayMission;
        private ResourcePack _missionRewardPack;
        private ResourcePack _mergedResourcePack;

        private List<BountyContainer> _bountyContainers = new();
        private List<UICommonButton> _buttons = new();
        private Dictionary<UnitName, UnitInfo> _killedUnitsDict = new();
        private MissionData _missionData;

        public float RewardMultiplier => ABTestRewardMultiplier.Instance.IsSet ? ABTestRewardMultiplier.Instance.GetRewardMultiplier : 2f;


        protected override void OnDisable()
        {
            base.OnDisable();
            
            Bubble.ClearAll();
        }

        public void Set(MissionData missionData, bool isVictory, Action onCollectReward,
            Action onMissionReplay, Func<bool> canReplayMission,
            ResourcePack missionRewardPack, ResourcePack mergedResourcePack, bool allowDoubleReward)
        {
            _missionRewardPack = missionRewardPack;
            _mergedResourcePack = mergedResourcePack;

            _onCollectReward = onCollectReward;
            _onMissionReplay = onMissionReplay;
            _canReplayMission = canReplayMission;

            _killedUnitsDict = RewardManager.KilledUnits;

            var missionResultPreset =
                AssetsProvider.GetConfig<ArtCollection>().GetMissionResultSprites(isVictory ? MissionResult.Win : MissionResult.Lose);
            InitializeContainers(missionResultPreset);
            SetHeader(missionData, isVictory);
            SetCommonButton(missionData, isVictory, allowDoubleReward);
            SetRewardButton(isVictory, CampaignManager.IsColdStart);
            SetMissionResultPreset(missionResultPreset);

            gameObject.SetActive(true);
            DOTween.timeScale = 1;
            transform.DOScale(1f, 0.5f).SetUpdate(true).OnComplete(() =>
            {
                StartCoroutine(ShowEffectsRoutine(isVictory));
            });
            SetButtonsState(true);
            
            AudioManager.Instance.PlayMusicWithFade("Battle");
        }

        private void CreateBountyContainer(UnitName unitName, bool isMedal = false)
        {
            var newBountyContainer = Instantiate(bountyContainerPrefab, bountyHolder);
            var uiBounty = newBountyContainer.GetComponent<BountyContainer>();
            uiBounty.UnitName = unitName;
            uiBounty.IsMedal = isMedal;
            _bountyContainers.Add(uiBounty);
        }

        private void InitializeContainers(MissionResultPreset missionResultPreset)
        {
            if (_bountyContainers.Count == 0) CreateBountyContainer(UnitName.None, true);

            foreach (var unit in _killedUnitsDict)
            {
                if (_bountyContainers.FirstOrDefault(container => container.UnitName == unit.Key) == null)
                {
                    CreateBountyContainer(unit.Key);
                }
            }

            ResetAllContainers();
            
            SetEmptyRewardContainersVisual();

            SetExistingContainers(missionResultPreset);
        }

        private void ResetAllContainers()
        {
            foreach (var bountyContainer in _bountyContainers)
            {
                bountyContainer.Reset();
            }

            foreach (var rewardContainer in uiRewardContainers)
            {
                rewardContainer.Reset();
            }
        }

        private void SetMissionResultPreset(MissionResultPreset missionResultPreset)
        {
            mainBackground.sprite = missionResultPreset.mainBackground;
            containerBackground.sprite = missionResultPreset.containerBackground;
            header.sprite = missionResultPreset.header;
        }

        private void SetExistingContainers(MissionResultPreset missionResultPreset)
        {
            var artCollection = AssetsProvider.GetConfig<ArtCollection>();
            foreach (var bountyContainer in _bountyContainers)
            {
                if (_killedUnitsDict.ContainsKey(bountyContainer.UnitName))
                {
                    bountyContainer.Set(
                        artCollection.GetUnitIcon(bountyContainer.UnitName),
                        missionResultPreset.bountyBackground,
                        _killedUnitsDict[bountyContainer.UnitName].SoftReward,
                        _killedUnitsDict[bountyContainer.UnitName].GetResourcePack(),
                        _killedUnitsDict[bountyContainer.UnitName].Counter);
                }
                
                if (!bountyContainer.IsMedal) continue;

                bountyContainer.Set(missionResultPreset.medal,
                    missionResultPreset.bountyBackground,
                    (int)_missionRewardPack[ResourceType.SoftCurrency].Value,
                    _missionRewardPack);
            }
        }

        private IEnumerator ShowEffectsRoutine(bool win)
        {
            foreach (var bountyContainer in _bountyContainers)
            {
                if (_killedUnitsDict.ContainsKey(bountyContainer.UnitName) || bountyContainer.IsMedal)
                {
                    bountyContainer.PlayAnimation();
                    SetRewardContainersVisual(bountyContainer.UIContainerPack, bountyContainer.BountyTween.Duration());

                    SetContainerBubbles(bountyContainer, win);
                }
                
                if (!bountyContainer.gameObject.activeSelf) continue;

                while (bountyContainer.BountyTween.IsPlaying())
                {
                    yield return null;
                }
            }

            yield return ShowButtonsEffect();
        }

        private IEnumerator ShowButtonsEffect()
        {
            DOTween.timeScale = 1;
            Tween tween;

            foreach (var button in _buttons)
            {
                if (!button.gameObject.activeSelf) continue;

                tween = button.transform.DOScale(1f, 0.2f).SetUpdate(true).SetEase(Ease.OutCubic);

                while (tween.IsPlaying())
                {
                    yield return null;
                }
            }
        }

        private void SetButtonsState(bool state)
        {
            foreach (var button in _buttons)
            {
                if (!button.gameObject.activeSelf) continue;
                
                button.Interactable = state;
                button.GetComponent<UIButtonAnimator>().enabled = state;
                var broTweeners = button.GetComponents<BroTweener>();
                foreach (var tween in broTweeners)
                {
                    tween.enabled = state;
                }
            }
        }

        private void SetBubble(string text, Transform target)
        {
            Bubble.Set(new BubbleSettings(new RectTransformBubbleTarget((RectTransform)target.transform, RectTransformBubbleTarget.RectSide.Top))
            {
                direction = Vector2.up,
                text = text
            });
        }
        
        private void SetContainerBubbles(BountyContainer bountyContainer, bool win)
        {
            if (bountyContainer.IsMedal)
            {
                bountyContainer.SetContainerTooltip(win
                    ? "UI/Common.Medal.Win".Localized()
                    : "UI/Common.Medal.Lose".Localized());
            }
            else
            {
                var unit = _killedUnitsDict[bountyContainer.UnitName];
                bountyContainer.SetContainerTooltip(unit.LocalizedName);
            }
        }

        private void SetRewardButton(bool win, bool coldStart)
        {
            _buttons.Add(takeRewardButton);
            
            takeRewardButton.gameObject.SetActive(!coldStart || win);
            takeRewardButton.transform.localScale = Vector3.zero;
            takeRewardButton.Text = win? "UI/Claim".Localized() : "UI/Common.Continue".Localized();
            takeRewardButton.SetClickAction(() => StartCoroutine(CollectRewardsRoutine()));
        }

        private void SetCommonButton(MissionData missionData, bool win, bool doubleRewardEnabled)
        {
            _buttons.Add(commonButton);
            commonButton.gameObject.SetActive(true);
            var isMainMission = missionData.Key.missionMode == MissionMode.Main;

            if ((win && !isMainMission) || (win && doubleRewardEnabled) || !DailyManager.Instance.IsRewardAvailable())
            {
                commonButton.gameObject.SetActive(false);
            }
            
            commonButton.transform.localScale = Vector3.zero;
            commonButton.SetClickAction(() => OnMissionConditionEnd(missionData, win));
            commonButton.SetViewPreset(win ? ButtonViewPreset.blue : ButtonViewPreset.yellow_arrow_right);
            commonButton.Text = win
                ? $"<sprite=\"FilmRibbonIcon\" index=0 color=#233541> X{RewardMultiplier} {"UI/Common.Reward.Flat".Localized()}"
                : missionData.CostData.EnergyCost > 0
                    ? $"{"UI/Common.Replay".Localized()} {ResourceType.EnergyCurrency.BBCodeOutline(1)}{missionData.CostData.EnergyCost}"
                    : "UI/Common.Replay".Localized();
        }

        private void SetHeader(MissionData missionData, bool win)
        {
            mainHeader.SetBackgroundColor(win ? mainHeader.HeaderDefaultColor : mainHeader.HeaderHardColor);
            if (missionData.Key.missionMode == MissionMode.Spec_Ops)
            {
                headerText.text =
                    win
                        ? I2Loc.Get("UI/Common.Operation.Complete", ("NUMBER", missionData.Key.id))
                        : $"<color=#EB9E37>{I2Loc.Get("UI/Common.Operation.Failed", ("NUMBER", missionData.Key.id))}</color>";
            }
            else
            {
                headerText.text =
                    win
                        ? I2Loc.Get("UI/Common.Mission.Complete", ("NUMBER", missionData.Key.id))
                        : $"<color=#EB9E37>{I2Loc.Get("UI/Common.Mission.Failed", ("NUMBER", missionData.Key.id))}</color>";
            }
        }

        private void SetEmptyRewardContainersVisual()
        {
            foreach (var rewardContainer in uiRewardContainers)
            {
                rewardContainer.Set();
            }
        }

        private void SetRewardContainersVisual(ResourcePack pack, float duration)
        {
            foreach (var rewardContainer in uiRewardContainers)
            {
                rewardContainer.SetAmountWithAnimation(pack, duration);
            }
        }

        private void CollectRewards()
        {
            if (_onCollectReward != null)
            {
                _onCollectReward.Invoke();
                return;
            }

            StopEffectImmediately();
        }

        private IEnumerator CollectRewardsRoutine()
        {
            takeRewardButton.CleanClickAction();
            SetButtonsState(false);

            yield return PlayRewardEffectRoutine();

            CollectRewards();
        }

        private IEnumerator PlayRewardEffectRoutine()
        {
            foreach (var container in uiRewardContainers)
            {
                container.PlayEffect(_mergedResourcePack ?? _missionRewardPack);

                while (container.routine.IsRunning)
                {
                    yield return null;
                }
            }
            
            yield return new WaitForSecondsRealtime(1f);
        }

        private void OnMissionConditionEnd(MissionData missionData, bool win)
        {            
            if (win && missionData.Key.missionMode == MissionMode.Main)
            {
                commonButton.CleanClickAction();
                SetButtonsState(false);

                AdsManager.ShowAd(AdType.Rewarded, "Double_reward_win", result =>
                {
                    switch (result)
                    {
                        case AdsResult.Finished:
                            GameMetrics.SendRewardedAds(GameMetrics.RewardedAdsType.Missions_rewardX2, result);
                            _mergedResourcePack *= RewardMultiplier - 1;
                            Player.Resources.Merge(_mergedResourcePack);
                            UIManager.MainLayer.GetElement<UIHeader>().SubtractItemPackFromView(_mergedResourcePack);
                            DailyManager.Instance.UpdateRewardTimer();
                            InterstitialManager.Instance.ResetTimer();
                            StartCoroutine(UpdateRewardContainers());
                            break;
                        case AdsResult.Failed:
                            SetBubble("Bubbles/AdvertisementNotAvailable".Localized(), commonButton.transform);
                            SetButtonsState(true);
                            commonButton.SetClickAction(() => OnMissionConditionEnd(missionData, win));
                            break;
                    }
                });
                return;
            }

            if (!win) StartCoroutine(MissionReplayRoutine());
        }

        private IEnumerator MissionReplayRoutine()
        {
            if (_canReplayMission?.Invoke() ?? false)
            {
                commonButton.CleanClickAction();
                SetButtonsState(false);

                yield return PlayRewardEffectRoutine();

                _onMissionReplay?.Invoke();
                
                yield break;
            }

            _onMissionReplay?.Invoke();
        }
        
        private IEnumerator UpdateRewardContainers()
        {
            SetRewardContainersVisual(_mergedResourcePack, 1f);

            foreach (var container in uiRewardContainers)
            {
                while (container.RewardTween.IsPlaying())
                {
                    yield return null;
                }
            }

            yield return CollectRewardsRoutine();
        }
        
        private void StopEffectImmediately() => StopAllCoroutines();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerClick)
            {
                ForceAnimationsToFinish();
            }
        }

        private void ForceAnimationsToFinish()
        {
            foreach (var container in _bountyContainers)
            {
                container.ForceAnimation();
            }

            foreach (var rewardContainer in uiRewardContainers)
            {
                rewardContainer.ForceAnimation();
            }
        }
    }
    
    [Serializable]
    public class UnitSprites
    {
        [SerializeField] public UnitName unitName;
        [SerializeField] public Sprite unitSprite;
    }

    [Serializable]
    public class MissionResultPreset
    {
        public MissionResult missionResult;
        public Sprite header;
        public Sprite mainBackground;
        public Sprite containerBackground;
        public Sprite medal;
        public Sprite bountyBackground;
    }
    
    public enum MissionResult
    {
        Win,
        Lose
    }
}
