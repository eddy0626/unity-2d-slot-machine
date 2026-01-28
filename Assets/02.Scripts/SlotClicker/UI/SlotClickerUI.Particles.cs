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
        #region Particle Effects

        /// <summary>
        /// 파티클 프리팹 생성
        /// </summary>
        private void CreateParticlePrefab()
        {
            if (_mainCanvas == null || _particlePrefab != null) return;

            _particlePrefab = new GameObject("ParticlePrefab");
            _particlePrefab.SetActive(false);

            RectTransform rect = _particlePrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(6.406f, 6.406f);

            Image img = _particlePrefab.AddComponent<Image>();
            img.raycastTarget = false;
            // 단색 파티클로 사용 (스프라이트 불필요)
            img.color = _particleColor;

            _particlePrefab.transform.SetParent(_mainCanvas.transform, false);

            // 풀 초기화
            for (int i = 0; i < PARTICLE_POOL_INITIAL_SIZE; i++)
            {
                GameObject pooled = Instantiate(_particlePrefab, _mainCanvas.transform);
                pooled.name = "PooledParticle";
                pooled.SetActive(false);
                _particlePool.Enqueue(pooled);
            }
        }

        private GameObject GetParticleFromPool()
        {
            GameObject obj;
            if (_particlePool.Count > 0)
            {
                obj = _particlePool.Dequeue();
            }
            else if (_activeParticles.Count < PARTICLE_POOL_MAX_SIZE)
            {
                obj = Instantiate(_particlePrefab, _mainCanvas.transform);
                obj.name = "PooledParticle";
            }
            else
            {
                // 가장 오래된 파티클 재활용 (O(1) 큐 사용)
                obj = _activeParticlesQueue.Dequeue();
                _activeParticles.Remove(obj); // HashSet O(1)
                obj.transform.DOKill();
            }

            _activeParticles.Add(obj); // HashSet O(1)
            _activeParticlesQueue.Enqueue(obj); // 순서 유지
            return obj;
        }

        private void ReturnParticleToPool(GameObject obj)
        {
            if (obj == null) return;

            obj.transform.DOKill();
            // GetComponent 캐시 대신 TryGetComponent 사용 (더 가벼움)
            if (obj.TryGetComponent<Image>(out var img))
            {
                img.DOKill();
            }

            obj.SetActive(false);
            _activeParticles.Remove(obj); // HashSet O(1)

            if (_particlePool.Count < PARTICLE_POOL_MAX_SIZE)
            {
                _particlePool.Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        /// <summary>
        /// 클릭 시 파티클 분출
        /// </summary>
        private void SpawnClickParticles(Vector2 position, bool isCritical)
        {
            if (!_enableClickParticles || _mainCanvas == null) return;

            int particleCount = isCritical ? _criticalParticleCount : _normalParticleCount;
            Color baseColor = isCritical ? _criticalParticleColor : _particleColor;
            float speed = isCritical ? _particleSpeed * 1.4f : _particleSpeed;
            float lifetime = isCritical ? _particleLifetime * 1.2f : _particleLifetime;

            if (_enableClickStreak && _streakLevel > 0)
            {
                particleCount += _streakLevel * _streakParticleBonusPerLevel;
                speed *= GetStreakFactor(0.1f);
                lifetime *= GetStreakFactor(0.06f);

                if (!isCritical)
                {
                    float t = Mathf.Clamp01(_streakLevel * 0.22f);
                    baseColor = Color.Lerp(baseColor, _streakBurstColor, t);
                }
            }

            for (int i = 0; i < particleCount; i++)
            {
                GameObject particle = GetParticleFromPool();
                particle.SetActive(true);
                particle.transform.SetAsLastSibling();

                RectTransform rect = particle.GetComponent<RectTransform>();
                rect.anchoredPosition = position;
                rect.localScale = Vector3.one * UnityEngine.Random.Range(0.6f, 1.2f);

                if (isCritical)
                {
                    rect.localScale *= 1.3f;
                }
                else if (_enableClickStreak && _streakLevel > 0)
                {
                    rect.localScale *= GetStreakFactor(0.06f);
                }

                Image img = particle.GetComponent<Image>();
                // 색상에 약간의 랜덤 변화
                float hueShift = UnityEngine.Random.Range(-0.1f, 0.1f);
                Color particleColor = new Color(
                    Mathf.Clamp01(baseColor.r + hueShift),
                    Mathf.Clamp01(baseColor.g + hueShift * 0.5f),
                    baseColor.b,
                    1f
                );
                img.color = particleColor;

                // 방사형 이동 방향 계산
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = speed * lifetime * UnityEngine.Random.Range(0.6f, 1.2f);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 targetPos = position + direction * distance;

                // 약간의 곡선 효과를 위한 중력 시뮬레이션
                targetPos.y -= UnityEngine.Random.Range(30f, 80f);

                // 애니메이션
                float actualLifetime = lifetime * UnityEngine.Random.Range(0.8f, 1.2f);

                Sequence seq = DOTween.Sequence();
                seq.Append(rect.DOAnchorPos(targetPos, actualLifetime).SetEase(Ease.OutQuad));
                seq.Join(rect.DOScale(0f, actualLifetime).SetEase(Ease.InQuad));
                seq.Join(img.DOFade(0f, actualLifetime * 0.8f).SetDelay(actualLifetime * 0.2f));

                // 회전 추가 (크리티컬 시 더 빠르게)
                float rotationSpeed = isCritical ? 720f : 360f;
                seq.Join(rect.DORotate(new Vector3(0, 0, rotationSpeed * (UnityEngine.Random.value > 0.5f ? 1 : -1)), actualLifetime, RotateMode.FastBeyond360));

                seq.OnComplete(() => ReturnParticleToPool(particle));
            }
        }

        private void CleanupParticlePool()
        {
            foreach (var obj in _activeParticles)
            {
                if (obj == null) continue;
                obj.transform.DOKill();
                if (obj.TryGetComponent<Image>(out var img))
                {
                    img.DOKill();
                }
                Destroy(obj);
            }
            _activeParticles.Clear();
            _activeParticlesQueue.Clear();

            while (_particlePool.Count > 0)
            {
                var obj = _particlePool.Dequeue();
                if (obj != null) Destroy(obj);
            }

            if (_particlePrefab != null)
                Destroy(_particlePrefab);
        }

        #endregion
    }
}
