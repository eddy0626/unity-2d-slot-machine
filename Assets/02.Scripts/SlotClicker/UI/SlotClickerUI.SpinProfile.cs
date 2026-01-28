using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;
using DG.Tweening;
using SlotClicker.Core;
using SlotClicker.Data;

namespace SlotClicker.UI
{
    public partial class SlotClickerUI : MonoBehaviour
    {
        #region SpinProfile API

        /// <summary>
        /// 현재 스핀 프로파일 (읽기 전용)
        /// </summary>
        public SlotClickerSpinProfile SpinProfile => _spinProfile;

        /// <summary>
        /// 런타임에 스핀 프로파일 설정
        /// </summary>
        /// <param name="profile">새로운 스핀 프로파일 (null이면 기본값 사용)</param>
        public void SetSpinProfile(SlotClickerSpinProfile profile)
        {
            _spinProfile = profile;
            Debug.Log($"[SlotClickerUI] SpinProfile changed to: {(profile != null ? profile.name : "Default")}");
        }

        /// <summary>
        /// Resources 폴더에서 스핀 프로파일 로드
        /// </summary>
        /// <param name="profileName">프로파일 이름 (확장자 제외)</param>
        /// <returns>로드 성공 여부</returns>
        public bool LoadSpinProfileFromResources(string profileName)
        {
            SlotClickerSpinProfile profile = Resources.Load<SlotClickerSpinProfile>(profileName);
            if (profile != null)
            {
                SetSpinProfile(profile);
                return true;
            }

            Debug.LogWarning($"[SlotClickerUI] SpinProfile not found in Resources: {profileName}");
            return false;
        }

        /// <summary>
        /// 현재 프로파일 파라미터 정보 문자열
        /// </summary>
        public string GetSpinProfileInfo()
        {
            if (_spinProfile == null)
                return "SpinProfile: Default (no profile assigned)";

            return $"SpinProfile: {_spinProfile.name}\n" +
                   $"  Accel: {_spinProfile.accelDuration}s, Start:{_spinProfile.accelStartSpeed}, Max:{_spinProfile.maxSpeed}\n" +
                   $"  Decel: {_spinProfile.decelerationSteps} steps\n" +
                   $"  Bounce: {_spinProfile.bounceIntensity}, {_spinProfile.bounceDuration}s, vibrato:{_spinProfile.bounceVibrato}\n" +
                   $"  Flash: {(_spinProfile.enableLandingFlash ? "ON" : "OFF")}, intensity:{_spinProfile.flashIntensity}";
        }

        #endregion
    }
}
