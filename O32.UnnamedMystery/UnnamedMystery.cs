using NewHorizons.Handlers;
using NewHorizons.Utility.Files;
using NewHorizons.Utility.OuterWilds;
using OWML.Common;
using OWML.ModHelper;
using System.IO;
using UnityEngine;

namespace UnnamedMystery
{
    public class UnnamedMystery : ModBehaviour
    {
        public INewHorizons _newHorizons;

        public static AudioType HelioraxiaThunder;

        private void Start()
        {
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
                    var thunderB = AudioUtilities.LoadAudio(Path.Combine(ModHelper.Manifest.ModFolderPath, "planets/oneshot/thunderB.mp3"));
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
            sphere._radius = _newHorizons.QueryBody<float>("Helioraxia", "extras.helioraxiaZapper.radius");
            zapperObject.AddComponent<OWTriggerVolume>()._shape = sphere;

            //Add audio source for thunder
            zapperObject.AddComponent<AudioSource>();
            zapperObject.AddComponent<OWAudioSource>();

            //Finally the component to handle the actual stuff
            HelioraxiaLightningVolume zapper = zapperObject.AddComponent<HelioraxiaLightningVolume>();
            zapper.zapSpeed = _newHorizons.QueryBody<float>("Helioraxia", "extras.helioraxiaZapper.zapSpeed");
            zapper._attachedBody = helioraxia.GetAttachedOWRigidbody();
            zapper._firstContactDamage = _newHorizons.QueryBody<float>("Helioraxia", "extras.helioraxiaZapper.damage");

            zapperObject.SetActive(true);
        }
    }

}
