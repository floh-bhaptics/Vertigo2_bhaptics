using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MelonLoader;
using HarmonyLib;
using MyBhapticsTactsuit;

using UnityEngine;
using Vertigo2.Weapons;
using Vertigo2.Interaction;
using Vertigo2;
using Vertigo2.Player;
using Valve.VR;

namespace Vertigo2_bhaptics
{
    public class Vertigo2_bhaptics : MelonMod
    {
        public static TactsuitVR tactsuitVr;
        private static int rightHand = ((int)SteamVR_Input_Sources.RightHand);
        private static bool rightFootLast = true;

        public override void OnApplicationStart()
        {
            base.OnApplicationStart();
            tactsuitVr = new TactsuitVR();
            tactsuitVr.PlaybackHaptics("HeartBeat");
        }


        #region Heartbeat

        [HarmonyPatch(typeof(DeathScreen), "Start")]
        public class bhaptics_DeathScreenStart
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopThreads();
            }
        }

        [HarmonyPatch(typeof(VertigoPlayer), "Die", new Type[] { typeof(HitInfo), typeof(bool) })]
        public class bhaptics_PlayerDies
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopThreads();
            }
        }

        [HarmonyPatch(typeof(VertigoPlayer), "EntityUpdate")]
        public class bhaptics_EntityAddHealth
        {
            [HarmonyPostfix]
            public static void Postfix(VertigoEntity __instance)
            {
                //tactsuitVr.StopHeartBeat();
                if (__instance.health > 0.3 * __instance.maxHealth) { tactsuitVr.StopHeartBeat(); }
            }
        }

        #endregion

        #region Weapons

        [HarmonyPatch(typeof(Gun), "ShootHaptics")]
        public class bhaptics_GunFeedback
        {
            [HarmonyPostfix]
            public static void Postfix(Gun __instance, float length, float power)
            {
                bool isRightHand = (((int)__instance.inputSource) == rightHand);
                bool twoHanded = (__instance.heldEquippable.otherHandHolding);

                float intensity = Math.Max(power * 2.0f, 1.2f);
                tactsuitVr.GunRecoil(isRightHand, intensity, twoHanded);
            }
        }

        [HarmonyPatch(typeof(Tailgun), "ShootHaptics", new Type[] { })]
        public class bhaptics_TailgunFeedback
        {
            [HarmonyPostfix]
            public static void Postfix(Tailgun __instance)
            {
                if (__instance.handle_L.isBeingHeld) tactsuitVr.GunRecoil(false, 0.7f);
                if (__instance.handle_R.isBeingHeld) tactsuitVr.GunRecoil(true, 0.7f);
            }
        }

        [HarmonyPatch(typeof(Enlighten), "Haptics", new Type[] { typeof(float), typeof(float), typeof(float), typeof(float)})]
        public class bhaptics_EnlightenFeedback
        {
            [HarmonyPostfix]
            public static void Postfix(Enlighten __instance)
            {
                bool isRightHand = (((int)__instance.inputSource) == rightHand);
                bool twoHanded = (__instance.heldEquippable.otherHandHolding);
                float intensity = 0.6f;
                tactsuitVr.GunRecoil(isRightHand, intensity, twoHanded);
            }
        }

        [HarmonyPatch(typeof(Blafaladaciousnesticles), "OnImpact", new Type[] { typeof(Collider), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(float) })]
        public class bhaptics_SwordFeedback
        {
            [HarmonyPostfix]
            public static void Postfix(Blafaladaciousnesticles __instance, float normalizedSpeed)
            {
                if (normalizedSpeed <= 0.0) return;
                bool isRightHand = (((int)__instance.inputSource) == rightHand);
                float intensity = normalizedSpeed;
                tactsuitVr.SwordRecoil(isRightHand, intensity);
            }
        }

        [HarmonyPatch(typeof(Blafaladaciousnesticles), "Deflect", new Type[] { typeof(Vector3), typeof(Vector3) })]
        public class bhaptics_SwordDeflect
        {
            [HarmonyPostfix]
            public static void Postfix(Blafaladaciousnesticles __instance)
            {
                bool isRightHand = (((int)__instance.inputSource) == rightHand);
                float intensity = 1.0f;
                tactsuitVr.SwordRecoil(isRightHand, intensity);
            }
        }

        #endregion

        #region Damage

        private static (float, float) getAngleAndShift(VertigoPlayer player, HitInfo hit)
        {
            Vector3 patternOrigin = new Vector3(0f, 0f, 1f);
            // y is "up", z is "forward" in local coordinates
            Vector3 hitPosition = hit.hitPoint - player.position;
            Quaternion PlayerRotation = player.head.rotation;
            Vector3 playerDir = PlayerRotation.eulerAngles;
            // We only want rotation correction in y direction (left-right), top-bottom and yaw we can leave
            Vector3 flattenedHit = new Vector3(hitPosition.x, 0f, hitPosition.z);
            float earlyhitAngle = Vector3.Angle(flattenedHit, patternOrigin);
            Vector3 earlycrossProduct = Vector3.Cross(flattenedHit, patternOrigin);
            if (earlycrossProduct.y > 0f) { earlyhitAngle *= -1f; }
            float myRotation = earlyhitAngle - playerDir.y;
            myRotation *= -1f;
            if (myRotation < 0f) { myRotation = 360f + myRotation; }

            float hitShift = hitPosition.y;
            if (hitShift > 0.0f) { hitShift = 0.5f; }
            else if (hitShift < -0.5f) { hitShift = -0.5f; }
            else { hitShift = (hitShift + 0.25f) * 2.0f; }

            return (myRotation, hitShift);
        }

        [HarmonyPatch(typeof(VertigoPlayer), "Hit", new Type[] { typeof(HitInfo), typeof(bool) })]
        public class bhaptics_PlayerHit
        {
            [HarmonyPostfix]
            public static void Postfix(VertigoPlayer __instance, HitInfo hit, bool crit)
            {
                // Start heart beat if low on health
                if (__instance.health < 0.3f * __instance.maxHealth) { tactsuitVr.StartHeartBeat(); }
                // if (crit) { tactsuitVr.LOG("Critical hit!"); }
                // Non-directional environmental feedback
                if ((hit.damageType & DamageType.Fire) == DamageType.Fire)
                {
                    tactsuitVr.PlaybackHaptics("FlameThrower");
                    return;
                }
                if ((hit.damageType & DamageType.Poison) == DamageType.Poison)
                {
                    tactsuitVr.PlaybackHaptics("Poison");
                    return;
                }
                /*
                if ((hit.damageType & DamageType.Electricity) == DamageType.Electricity)
                {
                    tactsuitVr.PlaybackHaptics("Electrocution");
                    return;
                }
                */
                if ((hit.damageType & DamageType.Drowning) == DamageType.Drowning)
                {
                    if (!tactsuitVr.IsPlaying("Smoking")) { tactsuitVr.PlaybackHaptics("Smoking"); }
                    return;
                }
                if ((hit.damageType & DamageType.Radiation) == DamageType.Radiation)
                {
                    if (!tactsuitVr.IsPlaying("Radiation")) { tactsuitVr.PlaybackHaptics("Radiation"); }
                    return;
                }
                // Directional feedback
                float hitAngle;
                float hitShift;
                string damageType = "Impact";
                switch (hit.damageType)
                {
                    case DamageType.Grenade:
                        damageType = "ExplosionFace";
                        break;
                    case DamageType.Explosion:
                        damageType = "ExplosionFace";
                        break;
                    case DamageType.Bullet:
                        damageType = "BulletHit";
                        break;
                    case DamageType.Laser:
                        damageType = "BulletHit";
                        break;
                    case DamageType.Impact:
                        damageType = "Impact";
                        break;
                    case DamageType.Blade:
                        damageType = "BladeHit";
                        break;
                    case DamageType.Bite:
                        damageType = "BulletHit";
                        break;
                    case DamageType.Plasma:
                        damageType = "BulletHit";
                        break;
                    case DamageType.Heat:
                        damageType = "LavaballHit";
                        break;
                    case DamageType.Cold:
                        damageType = "FreezeHit";
                        break;
                    case DamageType.Electricity:
                        damageType = "ElectricHit";
                        break;
                    default:
                        damageType = "Impact";
                        tactsuitVr.LOG("New damageType in hit: " + hit.damageType.ToString());
                        break;
                }
                // Default hit if not registered yet
                (hitAngle, hitShift) = getAngleAndShift(__instance, hit);
                if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                tactsuitVr.PlayBackHit(damageType, hitAngle, hitShift);
            }
        }

        #endregion

        #region Movement

        [HarmonyPatch(typeof(PlayerFootstepFX), "Step")]
        public class bhaptics_Footstep
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (rightFootLast)
                {
                    rightFootLast = false;
                }
                else
                {
                    rightFootLast = true;
                }
                tactsuitVr.FootStep(rightFootLast);
            }
        }

        [HarmonyPatch(typeof(Elevator), "Move")]
        public class bhaptics_ElevatorMove
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ElevatorTingle");
            }
        }

        [HarmonyPatch(typeof(VertigoCharacterController), "DoTeleportAnim", new Type[] { typeof(Vector3), typeof(Vector3), typeof(TeleportSurface) })]
        public class bhaptics_Teleport
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("Teleport");
            }
        }

        #endregion

        #region Special events

        [HarmonyPatch(typeof(CopterAirChase), "MissileHitCopter", new Type[] { })]
        public class bhaptics_MissileHitCopter
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ExplosionFace");
            }
        }

        [HarmonyPatch(typeof(CopterAirChase), "Crash", new Type[] { })]
        public class bhaptics_CopterCrash
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ExplosionUp");
            }
        }

        #endregion

        #region Explosions

        [HarmonyPatch(typeof(Explosion), "Explode", new Type[] { })]
        public class bhaptics_ExplosionExplode
        {
            [HarmonyPostfix]
            public static void Postfix(Explosion __instance)
            {
                //tactsuitVr.LOG("Explosion: " + __instance.maxDamage.ToString());
                if (__instance.maxDamage == 0.0f) return;
                if (tactsuitVr.IsPlaying("ExplosionUp")) return;
                float distance = (__instance.transform.position - Vertigo2.AI.AIManager.world.player.position).magnitude;
                float max_dist = 150.0f;
                if (distance > max_dist) return;
                float intensity = ((max_dist - distance)/max_dist) * ((max_dist - distance) / max_dist);
                if (intensity > 0.0f) tactsuitVr.PlaybackHaptics("ExplosionUp", intensity);
            }
        }

        #endregion

    }
}
