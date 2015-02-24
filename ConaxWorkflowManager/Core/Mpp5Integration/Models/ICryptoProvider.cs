using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration.Models
{
    public interface ICryptoProvider
    {
        string Hmac(string text, byte[] key);
        byte[] Hmac(byte[] data, byte[] key);
    }
}
