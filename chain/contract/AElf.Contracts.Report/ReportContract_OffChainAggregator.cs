using System.Linq;
using AElf.Contracts.Association;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override OffChainAggregationInfo RegisterOffChainAggregation(
            RegisterOffChainAggregationInput input)
        {
            Assert(State.RegisterWhiteListMap[Context.Sender], "Sender not in register white list.");
            Assert(State.OffChainAggregationInfoMap[input.EthereumContractAddress] == null,
                $"Off chain aggregation info of {input.EthereumContractAddress} already registered.");
            Assert(input.OffChainQueryInfoList.Value.Count >= 1, "At least 1 off-chain info.");
            if (input.OffChainQueryInfoList.Value.Count > 1)
            {
                Assert(input.AggregatorContractAddress != null,
                    "Merkle tree style aggregator must set aggregator contract address.");
            }

            Address organizationAddress;
            if (input.ObserverList.Value.Count == 1)
            {
                // Using an already-exist organization.

                organizationAddress = input.ObserverList.Value.First();
                if (organizationAddress != State.ParliamentContract.Value)
                {
                    var maybeOrganization = State.AssociationContract.GetOrganization.Call(organizationAddress);
                    if (maybeOrganization == null)
                    {
                        throw new AssertionException("Association not exists.");
                    }

                    AssertObserversQualified(new ObserverList
                        {Value = {maybeOrganization.OrganizationMemberList.OrganizationMembers}});
                }
            }
            else
            {
                AssertObserversQualified(input.ObserverList);
                organizationAddress = CreateObserverAssociation(input.ObserverList);
            }

            var offChainAggregationInfo = new OffChainAggregationInfo
            {
                EthereumContractAddress = input.EthereumContractAddress,
                OffChainQueryInfoList = input.OffChainQueryInfoList,
                ConfigDigest = input.ConfigDigest,
                ObserverAssociationAddress = organizationAddress,
                AggregateThreshold = input.AggregateThreshold,
                AggregatorContractAddress = input.AggregatorContractAddress
            };
            for (var i = 0; i < input.OffChainQueryInfoList.Value.Count; i++)
            {
                offChainAggregationInfo.RoundIds.Add(0);
            }

            State.OffChainAggregationInfoMap[input.EthereumContractAddress] = offChainAggregationInfo;
            State.CurrentRoundIdMap[input.EthereumContractAddress] = 1;

            Context.Fire(new OffChainAggregationRegistered
            {
                EthereumContractAddress = offChainAggregationInfo.EthereumContractAddress,
                OffChainQueryInfoList = offChainAggregationInfo.OffChainQueryInfoList,
                ConfigDigest = offChainAggregationInfo.ConfigDigest,
                ObserverAssociationAddress = offChainAggregationInfo.ObserverAssociationAddress,
                AggregateThreshold = offChainAggregationInfo.AggregateThreshold,
                AggregatorContractAddress = offChainAggregationInfo.AggregatorContractAddress
            });

            return offChainAggregationInfo;
        }

        public override Empty AddRegisterWhiteList(Address input)
        {
            Assert(Context.Sender == State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty()),
                "No permission.");
            State.RegisterWhiteListMap[input] = true;
            return new Empty();
        }

        private void AssertObserversQualified(ObserverList observerList)
        {
            foreach (var address in observerList.Value)
            {
                AssertObserverQualified(address);
            }
        }

        private void AssertObserverQualified(Address address)
        {
            Assert(State.ObserverMortgagedTokensMap[address] >= State.ApplyObserverFee.Value,
                $"{address} is not an observer candidate or mortgaged token not enough.");
        }

        private Address CreateObserverAssociation(ObserverList observerList)
        {
            var createOrganizationInput = new CreateOrganizationInput
            {
                CreationToken = HashHelper.ComputeFrom(Context.Self),
                OrganizationMemberList = new OrganizationMemberList {OrganizationMembers = {observerList.Value}},
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = 1,
                    MinimalVoteThreshold = 1
                },
                ProposerWhiteList = new ProposerWhiteList {Proposers = {Context.Self}}
            };
            State.AssociationContract.CreateOrganization.Send(createOrganizationInput);
            return State.AssociationContract.CalculateOrganizationAddress.Call(createOrganizationInput);
        }
    }
}