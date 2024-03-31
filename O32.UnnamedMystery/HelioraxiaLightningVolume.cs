using System.Collections.Generic;
using UnityEngine;

namespace UnnamedMystery
{
    [RequireComponent(typeof(OWTriggerVolume))]
    public class HelioraxiaLightningVolume : HazardVolume
    {
        /// <summary>
        /// Speed relative to the planet that will make the lightning hit you
        /// </summary>
        public float zapSpeed;

        /// <summary>
        /// Cooldown between each shock. Don't make this too low or else it will just keep shocking lol.
        /// </summary>
        public static float zapRepeatTime = 5;

        /// <summary>
        /// How long the flashbang lasts for
        /// </summary>
        public float flashbangFadeLength = 2;

        /// <summary>
        /// Flashbang brightness. I think 10 is good and 15 is too bright.
        /// </summary>
        [Range(0, 15)]
        public float flashbangExposure = 10;

        private float _shipElectroKnockoutTime;
        private bool _isFlashbanged;
        private float _flashbangStartTime;
        private OWAudioSource _audioSource;
        private List<TrackedHazardDetector> _trackedDetectors;

        public override void Awake()
        {
            base.Awake();
            _trackedDetectors = new List<TrackedHazardDetector>(4);
            _audioSource = GetComponentInChildren<OWAudioSource>();
            _firstContactDamageType = InstantDamageType.Electrical;
        }

        public void Start()
        {
            _audioSource.AssignAudioLibraryClip(UnnamedMystery.HelioraxiaThunder);
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
                    var speed = attachedOWRigidbody.GetRelativeVelocity(_attachedBody).magnitude;
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
            PlayLightningSoundEffect(detector.transform.position);
            OWRigidbody attachedOWRigidbody = detector.GetAttachedOWRigidbody();
            if (attachedOWRigidbody == null) return;

            Vector3 normalized = (detector.transform.position - transform.position).normalized;
            //Slow down a little bit when hit
            Vector3 vector = attachedOWRigidbody.GetVelocity() - _attachedBody.GetPointVelocity(detector.transform.position);
            attachedOWRigidbody.AddVelocityChange(-Vector3.Project(vector, normalized));
            attachedOWRigidbody.AddVelocityChange(normalized);

            if (detector.CompareTag("PlayerDetector"))
            {
                ShockPlayer();
            }
            // Everything else but player spin
            else
            {
                attachedOWRigidbody.AddAngularVelocityChange(UnityEngine.Random.onUnitSphere * 10f);
            }

            if (detector.CompareTag("ShipDetector"))
            {
                // While in the ship, it only detects the ship and not you.
                if (PlayerState.IsInsideShip()) ShockPlayer();

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
            Locator.GetPlayerCamera().postProcessingSettings.colorGrading.postExposure = flashbangExposure;
            _isFlashbanged = true;
        }

        private void UpdateFlashbang()
        {
            float lerp = Mathf.InverseLerp(_flashbangStartTime, _flashbangStartTime + flashbangFadeLength, Time.time);
            if (lerp < 1)
            {
                Locator.GetPlayerCamera().postProcessingSettings.colorGrading.postExposure = UnityEngine.Mathf.Lerp(flashbangExposure, 0, lerp);
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

        private void PlayLightningSoundEffect(Vector3 position)
        {
            _audioSource.transform.position = position;
            _audioSource.PlayOneShot(UnnamedMystery.HelioraxiaThunder);
        }

        public void ShockPlayer()
        {
            RumbleManager.Fade(0.8f, 0.2f, 2.5f);
            StartFlashbang();
            Locator.GetPlayerBody().GetComponent<PlayerResources>().ApplyInstantDamage(_firstContactDamage, _firstContactDamageType);
        }

        public override void OnEffectVolumeEnter(GameObject hitObj)
        {
            HazardDetector detector = hitObj.GetComponent<HazardDetector>();
            if (detector != null)
            {
                detector.AddVolume(this);
                var insulated = detector.IsInsulated();
                if (!_trackedDetectors.Exists(tracked => tracked == detector))
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
            _trackedDetectors.RemoveAll(tracked => tracked == detector);
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
            public string name => hazardDetector.name;

            public HazardDetector hazardDetector;

            public float lastShockTime;

            public bool wasInsulated;

            public static bool operator ==(TrackedHazardDetector a, TrackedHazardDetector b) => a.hazardDetector == b.hazardDetector;
            public static bool operator !=(TrackedHazardDetector a, TrackedHazardDetector b) => a.hazardDetector != b.hazardDetector;
            public static bool operator ==(TrackedHazardDetector tracked, HazardDetector detector) => tracked.hazardDetector == detector;
            public static bool operator !=(TrackedHazardDetector tracked, HazardDetector detector) => tracked.hazardDetector != detector;
            public static bool operator ==(HazardDetector detector, TrackedHazardDetector tracked) => tracked.hazardDetector == detector;
            public static bool operator !=(HazardDetector detector, TrackedHazardDetector tracked) => tracked.hazardDetector != detector;

            /// <summary>
            /// Make sure it doesn't shock a morbillion times a second.
            /// </summary>
            /// <returns>Can we shock?</returns>
            public bool CanShock()
            {
                return Time.timeSinceLevelLoad > lastShockTime + zapRepeatTime;
            }

            public override bool Equals(object obj)
            {
                if (obj is TrackedHazardDetector tracked) return tracked.hazardDetector == hazardDetector;
                else if (obj is HazardDetector detector) return detector == hazardDetector;
                return base.Equals(obj);
            }

            public override int GetHashCode() => hazardDetector.GetHashCode();
        }
    }
}
