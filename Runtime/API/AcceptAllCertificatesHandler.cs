using UnityEngine.Networking;

namespace ForkTrack.API
{
    /// <summary>
    /// Certificate handler that accepts all SSL certificates.
    /// Required for connecting to forktrack.pixfork.com
    /// </summary>
    public class AcceptAllCertificatesHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}
