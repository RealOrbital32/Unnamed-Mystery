using NewHorizons.Components.Props;
using System.Net.Sockets;
using System;
using UnityEngine;
using Epic.OnlineServices.P2P;
using OWML.Utils;

namespace UnnamedMystery
{
    public class DarvyVaultDoor : MonoBehaviour
    {
        public NHItemSocket socket;
        public NHItem _key;

        private void Start()
        {
            if (socket == null)
            {
                UnnamedMystery.ModConsole.WriteLine("Darvy Vault Door key socket not set!", OWML.Common.MessageType.Warning);
                return;
            }
            socket.OnSocketablePlaced += OnSocketablePlaced;
            socket.OnSocketableDonePlacing += OnSocketableDonePlacing;
            socket.OnSocketableRemoved += OnSocketableRemoved;
        }

        private void OnDestroy()
        {
            socket.OnSocketablePlaced += OnSocketablePlaced;
            socket.OnSocketableDonePlacing += OnSocketableDonePlacing;
            socket.OnSocketableRemoved += OnSocketableRemoved;
        }

        private void OnSocketablePlaced(OWItem item)
        {
        }

        private void OnSocketableRemoved(OWItem item)
        {
        }

        private void OnSocketableDonePlacing(OWItem item)
        {
            _key = item as NHItem;
            if (_key == null || _key.GetItemType().ToString() != "DarvyVaultKey")
            {
                UnnamedMystery.ModConsole.WriteLine("Placed an empty item or a non DarvyVaultKey in a DarvyVaultDoor", OWML.Common.MessageType.Error);
                return;
            }
            Open();
        }

        public void Open()
        {
        }
    }
}
