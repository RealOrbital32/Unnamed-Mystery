using System.Collections.Generic;
using UnityEngine;

namespace UnnamedMystery
{
    [RequireComponent(typeof(OWTriggerVolume), typeof(OWAudioSource))]
    public class HelioraxiaLightningVolume : HazardVolume
    {
        /// <summary>
        /// Speed relative to the planet that will make the lightning hit you
        /// </summary>
        public float zapSpeed;

        /// <summary>
        /// Cooldown between each shock. Don't make this too low or else it will just keep shocking lol.
        /// </summary>
        private static float _shockRepeatTime = 5;

        private float _flashbangFadeLength = 1;

        private float _shipElectroKnockoutTime;
        private bool _isFlashbanged;
        private float _flashbangStartTime;
        private OWAudioSource _audioSource;
        private List<TrackedHazardDetector> _trackedDetectors;

        public override void Awake()
        {
            base.Awake();
            _trackedDetectors = new List<TrackedHazardDetector>(4);
            _audioSource = this.GetRequiredComponent<OWAudioSource>();
            _firstContactDamageType = InstantDamageType.Electrical;
        }

        private void LateUpdate()
        {
            foreach (var tracked in _trackedDetectors)
            {
                bool insulated = tracked.hazardDetector.IsInsulated();
                if (!insulated && (tracked.wasInsulated || tracked.CanShock()))
                {
                    OWRigidbody attachedOWRigidbody = tracked.hazardDetector.GetAttachedOWRigidbody();
                    if (attachedOWRigidbody == null) continue;
                    var speed = attachedOWRigidbody.GetRelativeVelocity(this.GetAttachedOWRigidbody()).magnitude;
                    if (speed > zapSpeed)
                    {
                        ApplyShock(tracked.hazardDetector);
                        tracked.lastShockTime = Time.timeSinceLevelLoad;
                    }
                }
                tracked.wasInsulated = insulated;
            }

            if (_isFlashbanged)
            {
                UpdateFlashbang();
            }
        }

        private void ApplyShock(HazardDetector detector)
        {
            detector.PlayElectricityEffects();
            PlayLightningSoundEffect();
            OWRigidbody attachedOWRigidbody = detector.GetAttachedOWRigidbody();
            if (attachedOWRigidbody == null) return;

            Vector3 normalized = (detector.transform.position - transform.position).normalized;
            if (_triggerVolume.GetPenetrationDistance(detector.transform.position) < 1)
            {
                Vector3 vector = attachedOWRigidbody.GetVelocity() - _attachedBody.GetPointVelocity(detector.transform.position);
                attachedOWRigidbody.AddVelocityChange(-Vector3.Project(vector, normalized));
                attachedOWRigidbody.AddVelocityChange(normalized * 10);
            }

            if (detector.CompareTag("PlayerDetector"))
            {
                RumbleManager.Fade(0.8f, 0.2f, 2.5f);
                StartFlashbang();
                detector.GetAttachedOWRigidbody().GetComponent<PlayerResources>().ApplyInstantDamage(_firstContactDamage, _firstContactDamageType);
            }
            else
            {
                attachedOWRigidbody.AddAngularVelocityChange(UnityEngine.Random.onUnitSphere * 10f);
            }

            if (detector.CompareTag("ShipDetector"))
            {
                ShipDamageController shipDamageController = attachedOWRigidbody.GetComponent<ShipDamageController>();
                if (shipDamageController.IsElectricalFailed())
                {
                    if (Time.timeSinceLevelLoad > _shipElectroKnockoutTime + 3f)
                    {
                        shipDamageController.TriggerReactorCritical();
                    }
                }
                else
                {
                    shipDamageController.TriggerElectricalFailure();
                    _shipElectroKnockoutTime = Time.timeSinceLevelLoad;
                }
                attachedOWRigidbody.GetComponentInChildren<ShipCockpitController>().LockUpControls(3f);
            }
        }

        private void StartFlashbang()
        {
            _flashbangStartTime = Time.time;
            _isFlashbanged = true;
        }

        private void UpdateFlashbang()
        {
            float lerp = Mathf.InverseLerp(_flashbangStartTime, _flashbangStartTime + _flashbangFadeLength, Time.time);
            if (lerp < 1)
            {
                var camera = Locator.GetPlayerCamera();
                camera.postProcessingSettings.colorGrading.postExposure = Mathf.Lerp(camera.postProcessingSettings.colorGradingDefault.postExposure, 20, lerp);
            }
            else
            {
                StopFlashbang();
            }
        }

        private void StopFlashbang()
        {
            _isFlashbanged = false;
            Locator.GetPlayerCamera().postProcessingSettings.colorGrading.postExposure = 0;
        }

        private void PlayLightningSoundEffect()
        {
            _audioSource.PlayOneShot(UnnamedMystery.HelioraxiaThunder);
        }

        public override void OnEffectVolumeEnter(GameObject hitObj)
        {
            HazardDetector detector = hitObj.GetComponent<HazardDetector>();
            if (detector != null)
            {
                detector.AddVolume(this);
                var insulated = detector.IsInsulated();
                if (!_trackedDetectors.Exists(tracked => tracked.hazardDetector == detector))
                {
                    TrackedHazardDetector trackedHazardDetector = new TrackedHazardDetector();
                    trackedHazardDetector.hazardDetector = detector;
                    trackedHazardDetector.lastShockTime = 0;
                    trackedHazardDetector.wasInsulated = insulated;
                    _trackedDetectors.Add(trackedHazardDetector);
                }
            }
        }

        public override void OnEffectVolumeExit(GameObject hitObj)
        {
            base.OnEffectVolumeExit(hitObj);
            HazardDetector detector = hitObj.GetComponent<HazardDetector>();
            _trackedDetectors.RemoveAll((TrackedHazardDetector x) => x.hazardDetector == detector);
        }

        public override HazardType GetHazardType()
        {
            return HazardType.ELECTRICITY;
        }

        /// <summary>
        /// Tracked hazard object for when a detector enters.
        /// </summary>
        private class TrackedHazardDetector
        {
            public HazardDetector hazardDetector;

            public float lastShockTime;

            public bool wasInsulated;

            /// <summary>
            /// Make sure it doesn't shock a morbillion times a second.
            /// </summary>
            /// <returns>Can we shock?</returns>
            public bool CanShock()
            {
                return Time.timeSinceLevelLoad > lastShockTime + _shockRepeatTime;
            }
        }
    }
}
