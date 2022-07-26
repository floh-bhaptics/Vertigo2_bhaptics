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
            //tactsuitVr.LOG("EarlyHitAngle: " + earlyhitAngle.ToString());
            float myRotation = earlyhitAngle - playerDir.y;
            myRotation *= -1f;
            if (myRotation < 0f) { myRotation = 360f + myRotation; }

            /*
            Vector3 relativeHitDir = Quaternion.Euler(playerDir) * hitPosition;
            Vector2 xzHitDir = new Vector2(relativeHitDir.x, relativeHitDir.z);
            //Vector2 patternOrigin = new Vector2(0f, 1f);
            float hitAngle = Vector2.SignedAngle(xzHitDir, patternOrigin);
            hitAngle *= -1;
            //hitAngle += 90f;
            if (hitAngle < 0f) { hitAngle = 360f + hitAngle; }
            */
            float hitShift = hitPosition.y;
            if (hitShift > 0.0f) { hitShift = 0.5f; }
            else if (hitShift < -0.5f) { hitShift = -0.5f; }
            else { hitShift = (hitShift + 0.25f) * 2.0f; }

            //tactsuitVr.LOG("Relative x-z-position: " + relativeHitDir.x.ToString() + " "  + relativeHitDir.z.ToString());
            //tactsuitVr.LOG("HitAngle: " + hitAngle.ToString());
            //tactsuitVr.LOG("HitShift: " + hitShift.ToString());
            return (myRotation, hitShift);
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

        [HarmonyPatch(typeof(Gun), "ShootHaptics")]
        public class bhaptics_GunFeedback
        {
            [HarmonyPostfix]
            public static void Postfix(Gun __instance, float length, float power)
            {
                bool isRightHand = (((int)__instance.inputSource) == rightHand);
                bool twoHanded = (__instance.heldEquippable.otherHandHolding);

                if (__instance.name == "Minigun")
                {
                    if (power < 0.5f) { tactsuitVr.StopMinigun(isRightHand, twoHanded); }
                    else { tactsuitVr.FireMinigun(isRightHand, twoHanded); }
                    return;
                }
                float intensity = Math.Max(power * 2.0f, 1.2f);
                tactsuitVr.GunRecoil(isRightHand, intensity);
            }
        }

        [HarmonyPatch(typeof(VertigoPlayer), "Hit", new Type[] { typeof(HitInfo), typeof(bool) })]
        public class bhaptics_PlayerHit
        {
            [HarmonyPostfix]
            public static void Postfix(VertigoPlayer __instance, HitInfo hit, bool crit)
            {
                float hitAngle;
                float hitShift;
                if (__instance.health < 0.3f * __instance.maxHealth) { tactsuitVr.StartHeartBeat(); }
                if (crit) { tactsuitVr.LOG("Critical hit!"); }
                if ((hit.damageType & DamageType.Fire) == DamageType.Fire)
                {
                    tactsuitVr.PlaybackHaptics("FlameThrower");
                }
                if ((hit.damageType & DamageType.Explosion) == DamageType.Explosion)
                {
                    tactsuitVr.PlaybackHaptics("ExplosionUp");
                }
                if ((hit.damageType & DamageType.Poison) == DamageType.Poison)
                {
                    tactsuitVr.PlaybackHaptics("GasDeath");
                }
                if ((hit.damageType & DamageType.Grenade) == DamageType.Grenade)
                {
                    tactsuitVr.PlaybackHaptics("ExplosionFace");
                }
                if ((hit.damageType & DamageType.Electricity) == DamageType.Electricity)
                {
                    tactsuitVr.PlaybackHaptics("Electrocution");
                }
                if ((hit.damageType & DamageType.Drowning) == DamageType.Drowning)
                {
                    if (!tactsuitVr.IsPlaying("Smoking")) { tactsuitVr.PlaybackHaptics("Smoking"); }
                }
                if ((hit.damageType & DamageType.Radiation) == DamageType.Radiation)
                {
                    if (!tactsuitVr.IsPlaying("Radiation")) { tactsuitVr.PlaybackHaptics("Radiation"); }
                }
                if ((hit.damageType & DamageType.Bullet) == DamageType.Bullet)
                {
                    (hitAngle, hitShift) = getAngleAndShift(__instance, hit);
                    if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                    tactsuitVr.PlayBackHit("BulletHit", hitAngle, hitShift);
                    return;
                }
                if ((hit.damageType & DamageType.Laser) == DamageType.Laser)
                {
                    (hitAngle, hitShift) = getAngleAndShift(__instance, hit);
                    if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                    tactsuitVr.PlayBackHit("BulletHit", hitAngle, hitShift);
                    return;
                }
                if ((hit.damageType & DamageType.Impact) == DamageType.Impact)
                {
                    (hitAngle, hitShift) = getAngleAndShift(__instance, hit);
                    if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                    tactsuitVr.PlayBackHit("Impact", hitAngle, hitShift);
                    return;
                }
                if ((hit.damageType & DamageType.Blade) == DamageType.Blade)
                {
                    (hitAngle, hitShift) = getAngleAndShift(__instance, hit);
                    if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                    tactsuitVr.PlayBackHit("BladeHit", hitAngle, hitShift);
                    return;
                }
                if ((hit.damageType & DamageType.Bite) == DamageType.Bite)
                {
                    (hitAngle, hitShift) = getAngleAndShift(__instance, hit);
                    if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                    tactsuitVr.PlayBackHit("BulletHit", hitAngle, hitShift);
                    return;
                }
                if ((hit.damageType & DamageType.Plasma) == DamageType.Plasma)
                {
                    (hitAngle, hitShift) = getAngleAndShift(__instance, hit);
                    if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                    tactsuitVr.PlayBackHit("BulletHit", hitAngle, hitShift);
                    return;
                }
                if ((hit.damageType & DamageType.Heat) == DamageType.Heat)
                {
                    (hitAngle, hitShift) = getAngleAndShift(__instance, hit);
                    if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                    tactsuitVr.PlayBackHit("LavaballHit", hitAngle, hitShift);
                    return;
                }
                if ((hit.damageType & DamageType.Cold) == DamageType.Cold)
                {
                    (hitAngle, hitShift) = getAngleAndShift(__instance, hit);
                    if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                    tactsuitVr.PlayBackHit("FreezeHit", hitAngle, hitShift);
                    return;
                }
                if ((hit.damageType & DamageType.Gordle_Blue) == DamageType.Gordle_Blue)
                {
                    tactsuitVr.LOG("Gordle_Blue hitPoint: " + hit.hitPoint.x.ToString() + " " + hit.hitPoint.y.ToString() + " " + hit.hitPoint.z.ToString());
                }
                if ((hit.damageType & DamageType.Generic) == DamageType.Generic)
                {
                    tactsuitVr.LOG("Generic hitPoint: " + hit.hitPoint.x.ToString() + " " + hit.hitPoint.y.ToString() + " " + hit.hitPoint.z.ToString());
                }
                if ((hit.damageType & DamageType.Antimatter) == DamageType.Antimatter)
                {
                    tactsuitVr.LOG("Antimatter hitPoint: " + hit.hitPoint.x.ToString() + " " + hit.hitPoint.y.ToString() + " " + hit.hitPoint.z.ToString());
                }
                if ((hit.damageType & DamageType.Relativistic) == DamageType.Relativistic)
                {
                    tactsuitVr.LOG("Relativistic hitPoint: " + hit.hitPoint.x.ToString() + " " + hit.hitPoint.y.ToString() + " " + hit.hitPoint.z.ToString());
                }
                if ((hit.damageType & DamageType.Gordle_Orange) == DamageType.Gordle_Orange)
                {
                    tactsuitVr.LOG("Gordle_Orange hitPoint: " + hit.hitPoint.x.ToString() + " " + hit.hitPoint.y.ToString() + " " + hit.hitPoint.z.ToString());
                }
                // Default hit if not registered yet
                (hitAngle, hitShift) = getAngleAndShift(__instance, hit);
                if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                tactsuitVr.PlayBackHit("Impact", hitAngle, hitShift);
            }
        }

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

        [HarmonyPatch(typeof(DeathScreen), "Start")]
        public class bhaptics_DeathScreenStart
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopThreads();
            }
        }


    }
}
