using System;

namespace CommonDomainLibrary
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ContractVersion : Attribute
    {
        public ContractVersion(int version)
        {
            Version = version;
        }

        public int Version { get; set; }
    }
}