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

        private void Start()
        {
            //Base stuff that actually loads the configs
            _newHorizons = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
            _newHorizons.LoadConfigs(this);
        }
    }

}
