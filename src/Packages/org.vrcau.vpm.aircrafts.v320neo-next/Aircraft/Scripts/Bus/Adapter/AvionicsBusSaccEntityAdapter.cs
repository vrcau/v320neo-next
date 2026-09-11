using JetBrains.Annotations;
using SaccFlightAndVehicles;
using UdonSharp;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Bus.Adapter
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class AvionicsBusSaccEntityAdapter : AbstractAvionicsBusClient
    {
        // ReSharper disable once InconsistentNaming
        public SaccEntity EntityControl;

        [PublicAPI]
        public void SFEXT_G_RespawnButton()
        {
            if (Networking.IsOwner(EntityControl.gameObject))
            {
                _avionicsBus._AvionicsBusRespawnByLocalPlayer();
            }
            else
            {
                _avionicsBus._AvionicsBusRespawnByRemotePlayer();
            }
        }
    }
}