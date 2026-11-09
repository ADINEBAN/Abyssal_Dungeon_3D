using UnityEngine;

namespace AbyssalDungeon.VFX
{
    public static class ProceduralVFXFactory
    {
        public static GameObject CreateVFXPrefab(VFXType type)
        {
            GameObject go = new GameObject($"VFX_{type}");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            
            main.playOnAwake = false;
            
            switch (type)
            {
                case VFXType.Fire:
                    CreateFireVFX(ps);
                    break;
                case VFXType.Magic:
                    CreateMagicVFX(ps);
                    break;
                case VFXType.Hit:
                    CreateHitVFX(ps);
                    break;
                case VFXType.CriticalHit:
                    CreateCriticalHitVFX(ps);
                    break;
                case VFXType.Heal:
                    CreateHealVFX(ps);
                    break;
                case VFXType.Shield:
                    CreateShieldVFX(ps);
                    break;
                case VFXType.Poison:
                    CreatePoisonVFX(ps);
                    break;
                case VFXType.Death:
                    CreateDeathVFX(ps);
                    break;
                case VFXType.Portal:
                    CreatePortalVFX(ps);
                    break;
                case VFXType.LevelUp:
                    CreateLevelUpVFX(ps);
                    break;
                case VFXType.DungeonFog:
                    CreateFogVFX(ps);
                    break;
                default:
                    CreateDefaultVFX(ps);
                    break;
            }
            
            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            Shader urpParticle = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (urpParticle == null) urpParticle = Shader.Find("Particles/Standard Unlit");
            var mat = new Material(urpParticle);
            mat.enableInstancing = true;
            renderer.sharedMaterial = mat;

            go.AddComponent<AbyssGame.ScalableVfxResponder>();

            return go;
        }

        private static void CreateFireVFX(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = new Color(1f, 0.4f, 0f, 1f); // Orange/red
            main.startSpeed = 5f;
            main.startLifetime = 1f;
            main.startSize = 0.5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.5f;

            var emission = ps.emission;
            emission.rateOverTime = 50f;
            
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 1f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        private static void CreateMagicVFX(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = new Color(0.5f, 0f, 1f, 1f); // Purple/blue
            main.startSpeed = 3f;
            main.startLifetime = 0.5f;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });
        }

        private static void CreateHitVFX(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = Color.white;
            main.startSpeed = 10f;
            main.startLifetime = 0.2f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });
        }

        private static void CreateCriticalHitVFX(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = Color.yellow;
            main.startSpeed = 15f;
            main.startLifetime = 0.3f;
            main.startSize = 0.5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });
        }

        private static void CreateHealVFX(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = Color.green;
            main.startSpeed = 2f;
            main.startLifetime = 2f;
            main.gravityModifier = -0.5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.5f;

            var emission = ps.emission;
            emission.rateOverTime = 20f;
        }

        private static void CreateShieldVFX(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = Color.cyan;
            main.startSpeed = 0f;
            main.startLifetime = 1f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 2f;

            var emission = ps.emission;
            emission.rateOverTime = 100f;
        }

        private static void CreatePoisonVFX(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = new Color(0f, 0.5f, 0f, 1f);
            main.startSpeed = 1f;
            main.startLifetime = 1.5f;
            main.startSize = 0.2f;
            main.gravityModifier = -0.2f;

            var emission = ps.emission;
            emission.rateOverTime = 15f;
        }

        private static void CreateDeathVFX(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = Color.black;
            main.startSpeed = 5f;
            main.startLifetime = 1f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50) });
        }

        private static void CreatePortalVFX(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = new Color(0.3f, 0f, 0.8f, 1f);
            main.startSpeed = 0f;
            main.startLifetime = 2f;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 2f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalZ = 5f;

            var emission = ps.emission;
            emission.rateOverTime = 100f;
        }

        private static void CreateLevelUpVFX(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = Color.yellow;
            main.startSpeed = 10f;
            main.startLifetime = 1.5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 0f;
            shape.radius = 1f;
            
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 100) });
        }

        private static void CreateFogVFX(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = new Color(1f, 1f, 1f, 0.2f);
            main.startSpeed = 0.5f;
            main.startLifetime = 5f;
            main.startSize = 5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 1f, 20f);

            var emission = ps.emission;
            emission.rateOverTime = 10f;
        }
        
        private static void CreateDefaultVFX(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = Color.white;
            var emission = ps.emission;
            emission.rateOverTime = 10f;
        }
    }
}
