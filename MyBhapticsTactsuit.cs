using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Bhaptics.SDK2;
using MelonLoader;

namespace MyBhapticsTactsuit
{
    public class TactsuitVR
    {
        private static ManualResetEvent HeartBeat_mrse = new ManualResetEvent(false);

        public void HeartBeatFunc()
        {
            while (true)
            {
                HeartBeat_mrse.WaitOne();
                Thread.Sleep(1000);
                BhapticsSDK2.Play("heartbeat");
            }
        }

        public TactsuitVR()
        {
            LOG("Starting HeartBeat and NeckTingle thread...");
            var res = BhapticsSDK2.Initialize("Gs308257f0HC5YfQgJ7L", "Cgt7qCjhvv38Wd5oJOJD", "");

            if (res > 0)
            {
                LOG("Failed to do bhaptics initialization...");
            }
            
            Thread HeartBeatThread = new Thread(HeartBeatFunc);
            HeartBeatThread.Start();
        }

        public void LOG(string logStr)
        {
            MelonLogger.Msg(logStr);
        }

        public void PlaybackHaptics(String key, float intensity = 1.0f, float duration = 1.0f)
        {
            BhapticsSDK2.Play(key.ToLower(), intensity, duration, 0, 0);
            // LOG("Playing back: " + key);
        }

        public void PlayBackHit(String key, float xzAngle, float yShift)
        {
            // two parameters can be given to the pattern to move it on the vest:
            // 1. An angle in degrees [0, 360] to turn the pattern to the left
            // 2. A shift [-0.5, 0.5] in y-direction (up and down) to move it up or down
            BhapticsSDK2.Play(key.ToLower(), 1f, 1f, xzAngle, yShift);
        }

        public void GunRecoil(bool isRightHand, float intensity = 1.0f, bool twoHanded = false )
        {
            float duration = 1.0f;
            
            string postfix = "_L";
            string otherPostfix = "_R";
            if (isRightHand) { postfix = "_R"; otherPostfix = "_L"; }
            string keyArm = "Recoil" + postfix;
            string keyVest = "RecoilVest" + postfix;
            string keyHands = "RecoilHands" + postfix;
            string keyArmOther = "Recoil" + otherPostfix;
            string keyHandsOther = "RecoilHands" + otherPostfix;
            
            BhapticsSDK2.Play(keyHands.ToLower(), intensity, duration, 0f, 0f);
            BhapticsSDK2.Play(keyArm.ToLower(), intensity, duration, 0f, 0f);
            BhapticsSDK2.Play(keyVest.ToLower(), intensity, duration, 0f, 0f);
            if (twoHanded)
            {
                BhapticsSDK2.Play(keyHandsOther.ToLower(), intensity, duration, 0f, 0f);
                BhapticsSDK2.Play(keyArmOther.ToLower(), intensity, duration, 0f, 0f);
            }
        }

        public void MeatNailerRecoil(bool isRightHand, float intensity = 1.0f, bool twoHanded = false)
        {
            float duration = 1.0f;
            string postfix = "_L";
            string otherPostfix = "_R";
            if (isRightHand) { postfix = "_R"; otherPostfix = "_L"; }
            string keyArm = "Recoil" + postfix;
            string keyVest = "MeatNailerVest" + postfix;
            string keyHands = "RecoilHands" + postfix;
            string keyArmOther = "Recoil" + otherPostfix;
            string keyHandsOther = "RecoilHands" + otherPostfix;
            
            BhapticsSDK2.Play(keyHands.ToLower(), intensity, duration, 0f, 0f);
            BhapticsSDK2.Play(keyArm.ToLower(), intensity, duration, 0f, 0f);
            BhapticsSDK2.Play(keyVest.ToLower(), intensity, duration, 0f, 0f);
            if (twoHanded)
            {
                BhapticsSDK2.Play(keyHandsOther.ToLower(), intensity, duration, 0f, 0f);
                BhapticsSDK2.Play(keyArmOther.ToLower(), intensity, duration, 0f, 0f);
            }
        }

        public void EnlightenRecoil(bool isRightHand, float intensity = 1.0f, bool twoHanded = false)
        {
            float duration = 1.0f;
            string postfix = "_L";
            string otherPostfix = "_R";
            if (isRightHand) { postfix = "_R"; otherPostfix = "_L"; }
            string keyArm = "EnlightenGunArm" + postfix;
            string keyVest = "EnlightenGunVest" + postfix;
            string keyArmOther = "EnlightenGunArm" + otherPostfix;
            
            BhapticsSDK2.Play(keyArm.ToLower(), intensity, duration, 0f, 0f);
            BhapticsSDK2.Play(keyVest.ToLower(), intensity, duration, 0f, 0f);
            if (twoHanded)
            {
                BhapticsSDK2.Play(keyArmOther.ToLower(), intensity, duration, 0f, 0f);
            }
            
        }

        public void SwordRecoil(bool isRightHand, float intensity = 1.0f)
        {
            float duration = 1.0f;
            string postfix = "_L";
            if (isRightHand) { postfix = "_R"; }
            string keyArm = "Sword" + postfix;
            string keyVest = "SwordVest" + postfix;
            string keyHands = "RecoilHands" + postfix;
            
            BhapticsSDK2.Play(keyHands.ToLower(), intensity, duration, 0f, 0f);
            BhapticsSDK2.Play(keyArm.ToLower(), intensity, duration, 0f, 0f);
            BhapticsSDK2.Play(keyVest.ToLower(), intensity, duration, 0f, 0f);
        }

        public void ThrowRecoil(bool isRightHand)
        {
            float intensity = 1f;
            float duration = 1f;
            string postfix = "_L";
            if (isRightHand) { postfix = "_R"; }
            string keyVest = "CastVest" + postfix;
            string keyArm = "CastArm" + postfix;
            string keyHands = "CastHand" + postfix;
            
            BhapticsSDK2.Play(keyHands.ToLower(), intensity, duration, 0f, 0f);
            BhapticsSDK2.Play(keyArm.ToLower(), intensity, duration, 0f, 0f);
            BhapticsSDK2.Play(keyVest.ToLower(), intensity, duration, 0f, 0f);
        }

        public bool isMinigunPlaying()
        {
            if (IsPlaying("Minigun_L")) { return true; }
            if (IsPlaying("Minigun_R")) { return true; }
            if (IsPlaying("MinigunDual_L")) { return true; }
            if (IsPlaying("MinigunDual_R")) { return true; }
            return false;
        }

        public void HeadShot(float hitAngle)
        {
            if (BhapticsSDK2.IsDeviceConnected(PositionType.Head))
            {
                if ((hitAngle < 45f) | (hitAngle > 315f)) { PlaybackHaptics("Headshot_F"); }
                if ((hitAngle > 45f) && (hitAngle < 135f)) { PlaybackHaptics("Headshot_L"); }
                if ((hitAngle > 135f) && (hitAngle < 225f)) { PlaybackHaptics("Headshot_B"); }
                if ((hitAngle > 225f) && (hitAngle < 315f)) { PlaybackHaptics("Headshot_R"); }
            }
            else { PlayBackHit("BulletHit", hitAngle, 0.5f); }
        }

        public void FootStep(bool isRightFoot)
        {
            if (!BhapticsSDK2.IsDeviceConnected(PositionType.FootL)) { return; }
            string postfix = "_L";
            if (isRightFoot) { postfix = "_R"; }
            string key = "FootStep" + postfix;
            PlaybackHaptics(key);
        }

        public void StartHeartBeat()
        {
            HeartBeat_mrse.Set();
        }

        public void StopHeartBeat()
        {
            HeartBeat_mrse.Reset();
        }

        public bool IsPlaying(String effect)
        {
            return BhapticsSDK2.IsPlaying(effect.ToLower());
        }

        public void StopHapticFeedback(String effect)
        {
            BhapticsSDK2.Stop(effect.ToLower());
        }

        public void StopAllHapticFeedback()
        {
            StopThreads();
            BhapticsSDK2.StopAll();
        }

        public void StopThreads()
        {
            StopHeartBeat();
        }


    }
}
