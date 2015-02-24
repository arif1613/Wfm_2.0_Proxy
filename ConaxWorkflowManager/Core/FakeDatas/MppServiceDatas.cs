using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.FakeDatas
{
    class MppServiceDatas : ContentAgreement
    {

        public List<ContentAgreement> GenerateFakeContentAgreementsList()
        {
            List<ContentAgreement> contentAgreements=new List<ContentAgreement>();
            return contentAgreements;
        }

        public List<MultipleContentService> GenerateFakeContentServicesList()
        {
            return null;
        }

        public List<MultipleServicePrice> GenerateFakeMultipleServicePricesList()
        {
            return null;
        }

    }
}
