using NewHorizons.Handlers;
using NewHorizons.Utility.Files;
using NewHorizons.Utility.OuterWilds;
using OWML.Common;
using OWML.Logging;
using OWML.ModHelper;
using OWML.Utils;
using System.IO;
using UnityEngine;

namespace UnnamedMystery
{
    public class UnnamedMystery : ModBehaviour
    {
        public INewHorizons _newHorizons;
        public static IModConsole ModConsole;

        public static AudioType HelioraxiaThunder;

        private void Start()
        {
            ModConsole = ModHelper.Console;

            //Base stuff that actually loads the configs
            _newHorizons = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
            _newHorizons.LoadConfigs(this);

            // Extra stuff to detect when planets get loaded
            _newHorizons.GetBodyLoadedEvent().AddListener((string name) =>
            {
                // check if this is unnamed mystery system
                if (_newHorizons.GetCurrentStarSystem() == "O32.UnnamedMystery")
                {
                    // check if helioraxia
                    if (name == "Helioraxia") OnHelioraxiaLoaded(_newHorizons.GetPlanet("Helioraxia"));
                    if (name == "Zephyria") OnZephyriaLoaded(_newHorizons.GetPlanet("Zephyria"));
                }
            });

            // Extra stuff to detect when planets get loaded
            _newHorizons.GetStarSystemLoadedEvent().AddListener((string name) =>
            {
                // check if this is unnamed mystery system
                if (name == "O32.UnnamedMystery")
                {
                    // Load lightning audio clips for helioraxia
                    var thunderA = AudioUtilities.LoadAudio(Path.Combine(ModHelper.Manifest.ModFolderPath, "planets/oneshot/thunderA.mp3"));
                    thunderA.name = "HelioraxiaThunderA";
                    var thunderB = AudioUtilities.LoadAudio(Path.Combine(ModHelper.Manifest.ModFolderPath, "planets/oneshot/thunderB.mp3"));
                    thunderB.name = "HelioraxiaThunderB";
                    HelioraxiaThunder = AudioTypeHandler.AddCustomAudioType("HelioraxiaThunder", new AudioClip[] { thunderA, thunderB });
                }
            });
        }

        public void OnHelioraxiaLoaded(GameObject helioraxia)
        {
            ModHelper.Console.WriteLine("Helioraxia has loaded", MessageType.Info);

            //Get helioraxia sector
            Sector sector = helioraxia.transform.Find("Sector").GetComponent<Sector>();

            //Add new game object to the sector of helioraxia
            GameObject zapperObject = new GameObject("ElectricalSystemZapper");
            zapperObject.SetActive(false);
            zapperObject.layer = Layer.BasicEffectVolume;
            zapperObject.transform.SetParent(sector.transform, false);

            //Add sphere shape volume to detect when player or something enters.
            SphereShape sphere = zapperObject.AddComponent<SphereShape>();
            sphere._collisionMode = Shape.CollisionMode.Volume;
            sphere._pointChecksOnly = true;
            sphere._radius = _newHorizons.QueryBody<float>("Helioraxia", "extras.helioraxiaLightning.radius");
            zapperObject.AddComponent<OWTriggerVolume>()._shape = sphere;

            //Add audio source for thunder
            GameObject thunderObject = new GameObject("ThunderOneshot");
            thunderObject.transform.SetParent(zapperObject.transform, false);
            var audioSource = thunderObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.dopplerLevel = 0;
            audioSource.spatialBlend = 1;
            audioSource.minDistance = 0;
            audioSource.maxDistance = 600;
            //Taken from base game electricity
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(
                new Keyframe { time = 0.1f, value = 1, inTangent = -3.1855f, outTangent = -3.1855f, inWeight = 0.3333f, outWeight = 0.3333f },
                new Keyframe { time = 1, value = 0, inTangent = -0.0603f, outTangent = -0.0603f, inWeight = 0.3333f, outWeight = 0.3333f }
            ));
            var owAudioSource = thunderObject.AddComponent<OWAudioSource>();
            owAudioSource.SetTrack(OWAudioMixer.TrackName.Environment_Unfiltered);
            owAudioSource.SetClipSelectionType(OWAudioSource.ClipSelectionOnPlay.RANDOM);

            //Finally the component to handle the actual stuff
            HelioraxiaLightningVolume zapper = zapperObject.AddComponent<HelioraxiaLightningVolume>();
            zapper.zapSpeed = _newHorizons.QueryBody<float>("Helioraxia", "extras.helioraxiaLightning.zapSpeed");
            HelioraxiaLightningVolume.zapRepeatTime = _newHorizons.QueryBody<float>("Helioraxia", "extras.helioraxiaLightning.zapRepeatTime");
            zapper.flashbangFadeLength = _newHorizons.QueryBody<float>("Helioraxia", "extras.helioraxiaLightning.flashbangFadeLength");
            zapper.flashbangExposure = _newHorizons.QueryBody<float>("Helioraxia", "extras.helioraxiaLightning.flashbangExposure");
            zapper._attachedBody = helioraxia.GetAttachedOWRigidbody();
            zapper._damagePerSecond = 0;
            zapper._firstContactDamage = _newHorizons.QueryBody<float>("Helioraxia", "extras.helioraxiaLightning.damage");

            zapperObject.SetActive(true);
        }

        public void OnZephyriaLoaded(GameObject zephyria)
        {
            ModHelper.Console.WriteLine("Zephyria has loaded", MessageType.Info);

            //Get zephyria sector
            Sector sector = zephyria.transform.Find("Sector").GetComponent<Sector>();

            //Add new game object to the sector of zephyria
            GameObject sandstormObject = new GameObject("Sandstorm");
            sandstormObject.SetActive(false);
            sandstormObject.layer = Layer.BasicEffectVolume;
            sandstormObject.transform.SetParent(sector.transform, false);

            //Add sphere shape volume to detect when player or something enters.
            SphereShape sphere = sandstormObject.AddComponent<SphereShape>();
            sphere._collisionMode = Shape.CollisionMode.Volume;
            sphere._pointChecksOnly = true;
            sphere._radius = _newHorizons.QueryBody<float>("Zephyria", "extras.zephyriaSandstorm.radius");
            sandstormObject.AddComponent<OWTriggerVolume>()._shape = sphere;

            //Finally the component to handle the actual stuff
            ZephyriaSandstormVolume sandstorm = sandstormObject.AddComponent<ZephyriaSandstormVolume>();

            sandstormObject.SetActive(true);
        }
    }

}
