using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace CommonDomainLibrary.Security
{
    [Serializable]
    public class CommonIdentity : MarshalByRefObject, ICommonIdentity
    {
        public List<Claim> Claims;        

        public CommonIdentity(Guid id, string name, string authenticationType, Guid ownerId)
        {
            Id = id;
            OwnerId = ownerId;
            Name = name;
            Claims = new List<Claim>();
            IsAuthenticated = id != Guid.Empty;
            AuthenticationType = authenticationType;
        }

        public Guid Id { get; private set; }
        public Guid OwnerId { get; private set; }
        public string Name { get; private set; }
        public string AuthenticationType { get; private set; }
        public bool IsAuthenticated { get; private set; }
    }
}