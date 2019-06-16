using System.Linq;
using System.Threading.Tasks;
using Nethermind.DataMarketplace.Integration.Test.JsonRpc.Dto;
using Nethermind.Overseer.Test.Framework;
using NUnit.Framework;

namespace Nethermind.DataMarketplace.Integration.Test
{
    [TestFixture]
    public class NdmTests : TestBuilder
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Test1()
        {
            NdmState state = new NdmState();
            NdmContext ndmContext = new NdmContext(state);
            

            SetContext(ndmContext)
                .StartDataProvider()
                .Wait()
                .DP.DeployNdmContract()
                .DP.AddDataHeader(() => new DataHeaderDto
                    {
                        Name = "Test data header #1",
                        Description = "Test data header #1 description",
                        UnitPrice = "100000000000000000",
                        UnitType = "unit",
                        MinUnits = 1,
                        MaxUnits = 100000,
                        Rules = new DataHeaderRulesDto
                        {
                            Expiry = new DataHeaderRuleDto
                            {
                                Value = "0x10000"
                            }
                        }
                    },
                    validator: d => (d?.Replace("0x", string.Empty).Length ?? 0) == 64,
                    stateUpdater: (s, r) => s.DataHeaderId = r.Result)
                .DP.GetDataHeaders(validator: d => d.Any())
                .StartDataConsumer()
                .Wait()
                .DC.GetDiscoveredDataHeaders(validator: d => d.Any())
                .DC.MakeDeposit(() => new MakeDepositDto
                    {
                        DataHeaderId = state.DataHeaderId,
                        Units = 336,
                        Value = "33600000000000000000",
                    },
                    validator: d => (d?.Replace("0x", string.Empty).Length ?? 0) == 64,
                    stateUpdater: (s, r) => { s.DepositId = r.Result; })
                .Wait()
                .DC.SendDataRequest(() => state.DepositId, validator: d => !string.IsNullOrWhiteSpace(d))
                .DC.GetDeposits(validator: d =>
                {
                    var deposit = d.SingleOrDefault();
                    if (deposit == null)
                    {
                        return false;
                    }

                    return deposit.ConsumedUnitsFromProvider == 0 &&
                           !deposit.StreamEnabled &&
                           !deposit.Subscriptions.Any();
                })
                .DC.EnableDataStream(() => state.DepositId, new[] {"test-sub"})
                .DP.SendData(() => new DataHeaderDataDto
                {
                    DataHeaderId = state.DataHeaderId,
                    Data = "test-data",
                    Subscription = "test-sub"
                })
                .DC.PullData(() => state.DepositId, validator: d => d == "test-data")
                .DC.GetDeposits(validator: d =>
                {
                    var deposit = d.SingleOrDefault();
                    if (deposit == null)
                    {
                        return false;
                    }

                    return deposit.ConsumedUnitsFromProvider == 1 && deposit.StartUnits == 0 &&
                           deposit.CurrentUnits == 1 && deposit.UnpaidUnits == 1 &&
                           deposit.StreamEnabled &&
                           deposit.Subscriptions.Contains("test-sub");
                })
                .DC.DisableDataStream(() => state.DepositId)
                .DP.SendData(() => new DataHeaderDataDto
                {
                    DataHeaderId = state.DataHeaderId,
                    Data = "test-data",
                    Subscription = "test-sub"
                })
                .DC.PullData(() => state.DepositId, validator: string.IsNullOrWhiteSpace)
                .DC.EnableDataStream(() => state.DepositId, new[] {"test-sub"})
                .DP.SendData(() => new DataHeaderDataDto
                {
                    DataHeaderId = state.DataHeaderId,
                    Data = "test-data-2",
                    Subscription = "test-sub"
                })
                .DC.PullData(() => state.DepositId, validator: d => d == "test-data-2")
                .DC.GetDeposits(validator: d =>
                {
                    var dataRequest = d.SingleOrDefault();
                    if (dataRequest == null)
                    {
                        return false;
                    }

                    return dataRequest.ConsumedUnitsFromProvider == 2 && dataRequest.StartUnits == 0 &&
                           dataRequest.CurrentUnits == 2 && dataRequest.UnpaidUnits == 2 &&
                           dataRequest.StreamEnabled &&
                           dataRequest.Subscriptions.Contains("test-sub");
                })
                .KillDataConsumer()
                .StartDataConsumer()
                .DC.GetDeposits(validator: d =>
                {
                    var dataRequest = d.SingleOrDefault();
                    if (dataRequest == null)
                    {
                        return false;
                    }

                    return dataRequest.ConsumedUnitsFromProvider == 2 && dataRequest.StartUnits == 0 &&
                           dataRequest.CurrentUnits == 0 && dataRequest.UnpaidUnits == 2 &&
                           dataRequest.StreamEnabled &&
                           dataRequest.Subscriptions.Contains("test-sub");
                })
                .DC.SendDataRequest(() => state.DepositId, validator: d => !string.IsNullOrWhiteSpace(d))
                .DC.EnableDataStream(() => state.DepositId, new[] {"test-sub"})
                .DP.SendData(() => new DataHeaderDataDto
                {
                    DataHeaderId = state.DataHeaderId,
                    Data = "test-data-3",
                    Subscription = "test-sub"
                })
                .DP.SendData(() => new DataHeaderDataDto
                {
                    DataHeaderId = state.DataHeaderId,
                    Data = "test-data-4",
                    Subscription = "test-sub"
                })
                .DP.SendData(() => new DataHeaderDataDto
                {
                    DataHeaderId = state.DataHeaderId,
                    Data = "test-data-5",
                    Subscription = "test-sub"
                })
                .DC.PullData(() => state.DepositId, validator: d => d == "test-data-3")
                .DC.PullData(() => state.DepositId, validator: d => d == "test-data-4")
                .DC.PullData(() => state.DepositId, validator: d => d == "test-data-5")
                .DC.GetDeposits(validator: d =>
                {
                    var dataRequest = d.SingleOrDefault();
                    if (dataRequest == null)
                    {
                        return false;
                    }

                    return dataRequest.ConsumedUnitsFromProvider == 5 && dataRequest.StartUnits == 2 &&
                           dataRequest.CurrentUnits == 3 && dataRequest.UnpaidUnits == 3 &&
                           dataRequest.StreamEnabled &&
                           dataRequest.Subscriptions.Contains("test-sub");
                })
                .KillDataProvider()
                .KillDataConsumer()
                .LeaveContext();

            await ScenarioCompletion;

            Assert.True(_results.All(r => r.Passed));
        }
    }
}