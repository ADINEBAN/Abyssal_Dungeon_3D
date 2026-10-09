using System.Collections.Generic;
using UnityEngine;

namespace AbyssalDungeon.Combat
{
    public class StatusEffectManager : MonoBehaviour
    {
        private class ActiveEffect
        {
            public StatusEffect type;
            public float duration;
            public float tickTimer;
            public float damagePerTick;
        }

        private List<ActiveEffect> activeEffects = new List<ActiveEffect>();
        private IDamageable damageable;

        private void Awake()
        {
            damageable = GetComponent<IDamageable>();
        }

        private void Update()
        {
            if (activeEffects.Count == 0 || damageable == null || !damageable.IsAlive()) return;

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                ActiveEffect effect = activeEffects[i];
                effect.duration -= Time.deltaTime;
                
                HandleTick(effect);

                if (effect.duration <= 0)
                {
                    RemoveEffect(effect);
                    activeEffects.RemoveAt(i);
                }
            }
        }

        public void ApplyEffect(StatusEffect effectType, float duration, float tickDamage = 0f)
        {
            if (effectType == StatusEffect.None) return;

            // Simple stacking rule: Refresh duration if already exists
            ActiveEffect existing = activeEffects.Find(e => e.type == effectType);
            if (existing != null)
            {
                existing.duration = duration;
                return;
            }

            ActiveEffect newEffect = new ActiveEffect
            {
                type = effectType,
                duration = duration,
                tickTimer = 1f, // tick every 1 second
                damagePerTick = tickDamage
            };

            activeEffects.Add(newEffect);
            ApplyEffectVisuals(newEffect);
        }

        private void RemoveEffect(ActiveEffect effect)
        {
            // Remove visuals/reset stats
            Debug.Log($"Removed status effect: {effect.type}");
        }

        private void HandleTick(ActiveEffect effect)
        {
            if (effect.type == StatusEffect.Burn || effect.type == StatusEffect.Poison)
            {
                effect.tickTimer -= Time.deltaTime;
                if (effect.tickTimer <= 0)
                {
                    effect.tickTimer = 1f;
                    if (damageable != null)
                    {
                        DamageInfo info = new DamageInfo
                        {
                            baseDamage = effect.damagePerTick,
                            damageType = effect.type == StatusEffect.Burn ? DamageType.Fire : DamageType.Poison
                        };
                        damageable.TakeDamage(info);
                    }
                }
            }
        }

        private void ApplyEffectVisuals(ActiveEffect effect)
        {
            // Here you'd spawn particles or change material color based on effect
            Debug.Log($"Applied status effect: {effect.type}");
        }
    }
}
