using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Standards.ACS13;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContractState
    {
        internal AssociationContractContainer.AssociationContractReferenceState AssociationContract { get; set; }

        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }

        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }

        internal OracleAggregatorContractContainer.OracleAggregatorContractReferenceState OracleAggregatorContract
        {
            get;
            set;
        }
    }
}